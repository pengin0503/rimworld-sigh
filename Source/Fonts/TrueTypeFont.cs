using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleSigh.Fonts;

/// <summary>One outline point in font units; off-curve points are quadratic control points.</summary>
public readonly struct OutlinePoint
{
    public OutlinePoint(float x, float y, bool onCurve)
    {
        X = x;
        Y = y;
        OnCurve = onCurve;
    }

    public float X { get; }

    public float Y { get; }

    public bool OnCurve { get; }
}

/// <summary>
/// Minimal read-only TrueType ("glyf" outline) font: character map, horizontal metrics and glyph
/// outlines, which is everything the sign text needs. Game-independent so the font pipeline can be
/// unit-tested and previewed without RimWorld. CFF/OpenType-PostScript outlines, hinting, kerning
/// and shaping are deliberately not supported: signs draw one glyph per character.
/// <para>
/// Every read is bounds-checked; a malformed font raises <see cref="InvalidDataException"/> instead of
/// crashing the caller with an index error.
/// </para>
/// </summary>
public sealed class TrueTypeFont
{
    private const int MaxCompositeDepth = 8;

    private readonly byte[] data;
    private readonly int glyfOffset;
    private readonly int glyfLength;
    private readonly int locaOffset;
    private readonly bool longLoca;
    private readonly int hmtxOffset;
    private readonly int numberOfHMetrics;
    private readonly int cmapOffset;
    private readonly int cmapFormat;

    public TrueTypeFont(byte[] fontData)
    {
        data = fontData ?? throw new ArgumentNullException(nameof(fontData));
        try
        {
            uint version = U32(0);
            if (version != 0x00010000u && version != 0x74727565u)
            {
                throw new InvalidDataException("Not a TrueType outline font (expected sfnt version 1.0 or 'true').");
            }
            var tables = new Dictionary<string, (int Offset, int Length)>();
            int count = U16(4);
            for (int i = 0; i < count; i++)
            {
                int record = 12 + i * 16;
                string tag = new string(new[] { (char)data[Check(record, 4)], (char)data[record + 1], (char)data[record + 2], (char)data[record + 3] });
                int offset = checked((int)U32(record + 8));
                int length = checked((int)U32(record + 12));
                Check(offset, length);
                tables[tag] = (offset, length);
            }

            int head = Require(tables, "head").Offset;
            UnitsPerEm = U16(head + 18);
            longLoca = S16(head + 50) == 1;
            if (UnitsPerEm < 16 || UnitsPerEm > 16384)
            {
                throw new InvalidDataException("Invalid unitsPerEm " + UnitsPerEm + ".");
            }

            int hhea = Require(tables, "hhea").Offset;
            Ascender = S16(hhea + 4);
            Descender = S16(hhea + 6);
            LineGap = S16(hhea + 8);
            numberOfHMetrics = U16(hhea + 34);

            GlyphCount = U16(Require(tables, "maxp").Offset + 4);
            hmtxOffset = Require(tables, "hmtx").Offset;
            if (numberOfHMetrics < 1 || numberOfHMetrics > GlyphCount)
            {
                throw new InvalidDataException("Invalid numberOfHMetrics.");
            }
            Check(hmtxOffset, numberOfHMetrics * 4);
            locaOffset = Require(tables, "loca").Offset;
            Check(locaOffset, (GlyphCount + 1) * (longLoca ? 4 : 2));
            (glyfOffset, glyfLength) = Require(tables, "glyf");
            (cmapOffset, cmapFormat) = FindCharacterMap(Require(tables, "cmap").Offset);
        }
        catch (IndexOutOfRangeException e)
        {
            throw new InvalidDataException("Truncated font data.", e);
        }
        catch (OverflowException e)
        {
            throw new InvalidDataException("Invalid font table offset.", e);
        }
    }

    public int UnitsPerEm { get; }

    /// <summary>hhea ascender in font units (positive, above the baseline).</summary>
    public int Ascender { get; }

    /// <summary>hhea descender in font units (negative, below the baseline).</summary>
    public int Descender { get; }

    public int LineGap { get; }

    public int GlyphCount { get; }

    /// <summary>Glyph index for a Unicode scalar value, or 0 (.notdef) when the font has no glyph for it.</summary>
    public int GetGlyphIndex(int codepoint)
    {
        if (codepoint < 0 || codepoint > 0x10FFFF)
        {
            return 0;
        }
        try
        {
            int glyph = cmapFormat == 12 ? LookupFormat12(codepoint) : LookupFormat4(codepoint);
            return glyph < GlyphCount ? glyph : 0;
        }
        catch (IndexOutOfRangeException e)
        {
            throw new InvalidDataException("Truncated character map.", e);
        }
    }

    /// <summary>Advance width in font units.</summary>
    public int GetAdvanceWidth(int glyph)
    {
        int metric = Math.Min(Math.Max(glyph, 0), numberOfHMetrics - 1);
        return U16(hmtxOffset + metric * 4);
    }

    /// <summary>
    /// Closed contours of a glyph in font units (Y up). Empty for glyphs without ink (e.g. space).
    /// Composite glyphs are flattened with their component offsets and scales.
    /// </summary>
    public List<OutlinePoint[]> GetContours(int glyph)
    {
        var contours = new List<OutlinePoint[]>();
        try
        {
            AppendContours(glyph, 1f, 0f, 0f, 1f, 0f, 0f, contours, 0);
        }
        catch (IndexOutOfRangeException e)
        {
            throw new InvalidDataException("Truncated glyph " + glyph + ".", e);
        }
        return contours;
    }

    private void AppendContours(int glyph, float a, float b, float c, float d, float dx, float dy, List<OutlinePoint[]> contours, int depth)
    {
        if (glyph < 0 || glyph >= GlyphCount)
        {
            return;
        }
        int start = LocaAt(glyph);
        int end = LocaAt(glyph + 1);
        if (end <= start)
        {
            return;
        }
        if (start < 0 || end > glyfLength)
        {
            throw new InvalidDataException("Glyph " + glyph + " lies outside the glyf table.");
        }
        int p = glyfOffset + start;
        int contourCount = S16(p);
        if (contourCount >= 0)
        {
            AppendSimple(p, contourCount, a, b, c, d, dx, dy, contours);
        }
        else if (depth < MaxCompositeDepth)
        {
            AppendComposite(p + 10, a, b, c, d, dx, dy, contours, depth);
        }
    }

    private void AppendSimple(int p, int contourCount, float a, float b, float c, float d, float dx, float dy, List<OutlinePoint[]> contours)
    {
        if (contourCount == 0)
        {
            return;
        }
        var endPoints = new int[contourCount];
        int pos = p + 10;
        for (int i = 0; i < contourCount; i++)
        {
            endPoints[i] = U16(pos + i * 2);
            if (i > 0 && endPoints[i] < endPoints[i - 1])
            {
                throw new InvalidDataException("Contour end points are not increasing.");
            }
        }
        int pointCount = endPoints[contourCount - 1] + 1;
        pos += contourCount * 2;
        int instructionLength = U16(pos);
        pos += 2 + instructionLength;

        var flags = new byte[pointCount];
        for (int i = 0; i < pointCount;)
        {
            byte flag = data[pos++];
            flags[i++] = flag;
            if ((flag & 8) != 0)
            {
                int repeat = data[pos++];
                for (int r = 0; r < repeat && i < pointCount; r++)
                {
                    flags[i++] = flag;
                }
            }
        }

        var xs = new int[pointCount];
        int value = 0;
        for (int i = 0; i < pointCount; i++)
        {
            byte flag = flags[i];
            if ((flag & 2) != 0)
            {
                int delta = data[pos++];
                value += (flag & 16) != 0 ? delta : -delta;
            }
            else if ((flag & 16) == 0)
            {
                value += S16(pos);
                pos += 2;
            }
            xs[i] = value;
        }
        var ys = new int[pointCount];
        value = 0;
        for (int i = 0; i < pointCount; i++)
        {
            byte flag = flags[i];
            if ((flag & 4) != 0)
            {
                int delta = data[pos++];
                value += (flag & 32) != 0 ? delta : -delta;
            }
            else if ((flag & 32) == 0)
            {
                value += S16(pos);
                pos += 2;
            }
            ys[i] = value;
        }

        int first = 0;
        for (int contour = 0; contour < contourCount; contour++)
        {
            int last = endPoints[contour];
            var points = new OutlinePoint[last - first + 1];
            for (int i = first; i <= last; i++)
            {
                points[i - first] = new OutlinePoint(a * xs[i] + c * ys[i] + dx, b * xs[i] + d * ys[i] + dy, (flags[i] & 1) != 0);
            }
            if (points.Length >= 2)
            {
                contours.Add(points);
            }
            first = last + 1;
        }
    }

    private void AppendComposite(int pos, float a, float b, float c, float d, float dx, float dy, List<OutlinePoint[]> contours, int depth)
    {
        const int ArgsAreWords = 0x0001;
        const int ArgsAreXyValues = 0x0002;
        const int HasScale = 0x0008;
        const int MoreComponents = 0x0020;
        const int HasXyScale = 0x0040;
        const int HasTwoByTwo = 0x0080;

        int flags;
        do
        {
            flags = U16(pos);
            int component = U16(pos + 2);
            pos += 4;
            float argX, argY;
            if ((flags & ArgsAreWords) != 0)
            {
                argX = S16(pos);
                argY = S16(pos + 2);
                pos += 4;
            }
            else
            {
                argX = (sbyte)data[pos];
                argY = (sbyte)data[pos + 1];
                pos += 2;
            }
            float ca = 1f, cb = 0f, cc = 0f, cd = 1f;
            if ((flags & HasScale) != 0)
            {
                ca = cd = F2Dot14(pos);
                pos += 2;
            }
            else if ((flags & HasXyScale) != 0)
            {
                ca = F2Dot14(pos);
                cd = F2Dot14(pos + 2);
                pos += 4;
            }
            else if ((flags & HasTwoByTwo) != 0)
            {
                ca = F2Dot14(pos);
                cb = F2Dot14(pos + 2);
                cc = F2Dot14(pos + 4);
                cd = F2Dot14(pos + 6);
                pos += 8;
            }
            // Point-matching anchors (ArgsAreXyValues unset) are rare and not supported: no offset.
            float ox = (flags & ArgsAreXyValues) != 0 ? argX : 0f;
            float oy = (flags & ArgsAreXyValues) != 0 ? argY : 0f;
            // Component transform first, then the parent's: p' = parent(component(p)).
            AppendContours(component,
                a * ca + c * cb, b * ca + d * cb,
                a * cc + c * cd, b * cc + d * cd,
                a * ox + c * oy + dx, b * ox + d * oy + dy,
                contours, depth + 1);
        }
        while ((flags & MoreComponents) != 0);
    }

    private (int Offset, int Format) FindCharacterMap(int cmap)
    {
        int count = U16(cmap + 2);
        int format4 = -1;
        int format12 = -1;
        for (int i = 0; i < count; i++)
        {
            int record = cmap + 4 + i * 8;
            int platform = U16(record);
            int encoding = U16(record + 2);
            int offset = cmap + checked((int)U32(record + 4));
            int format = U16(offset);
            bool unicode = platform == 0 || (platform == 3 && (encoding == 1 || encoding == 10));
            if (!unicode)
            {
                continue;
            }
            if (format == 12 && format12 < 0)
            {
                format12 = offset;
            }
            else if (format == 4 && format4 < 0)
            {
                format4 = offset;
            }
        }
        if (format12 >= 0)
        {
            Check(format12, 16 + checked((int)U32(format12 + 12)) * 12);
            return (format12, 12);
        }
        if (format4 >= 0)
        {
            Check(format4, U16(format4 + 2));
            return (format4, 4);
        }
        throw new InvalidDataException("No Unicode character map (cmap format 4 or 12).");
    }

    private int LookupFormat12(int codepoint)
    {
        int groups = (int)U32(cmapOffset + 12);
        int lo = 0;
        int hi = groups - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int group = cmapOffset + 16 + mid * 12;
            uint startChar = U32(group);
            uint endChar = U32(group + 4);
            if ((uint)codepoint < startChar)
            {
                hi = mid - 1;
            }
            else if ((uint)codepoint > endChar)
            {
                lo = mid + 1;
            }
            else
            {
                return (int)(U32(group + 8) + ((uint)codepoint - startChar));
            }
        }
        return 0;
    }

    private int LookupFormat4(int codepoint)
    {
        if (codepoint > 0xFFFF)
        {
            return 0;
        }
        int segCount = U16(cmapOffset + 6) / 2;
        int endCodes = cmapOffset + 14;
        int startCodes = endCodes + segCount * 2 + 2;
        int idDeltas = startCodes + segCount * 2;
        int idRangeOffsets = idDeltas + segCount * 2;
        int lo = 0;
        int hi = segCount - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int endCode = U16(endCodes + mid * 2);
            if (codepoint > endCode)
            {
                lo = mid + 1;
                continue;
            }
            int startCode = U16(startCodes + mid * 2);
            if (codepoint < startCode)
            {
                hi = mid - 1;
                continue;
            }
            int delta = U16(idDeltas + mid * 2);
            int rangeOffset = U16(idRangeOffsets + mid * 2);
            if (rangeOffset == 0)
            {
                return (codepoint + delta) & 0xFFFF;
            }
            int glyph = U16(idRangeOffsets + mid * 2 + rangeOffset + (codepoint - startCode) * 2);
            return glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
        }
        return 0;
    }

    private int LocaAt(int index)
    {
        return longLoca ? checked((int)U32(locaOffset + index * 4)) : U16(locaOffset + index * 2) * 2;
    }

    private static (int Offset, int Length) Require(Dictionary<string, (int Offset, int Length)> tables, string tag)
    {
        if (!tables.TryGetValue(tag, out (int Offset, int Length) table))
        {
            throw new InvalidDataException("Missing '" + tag + "' table (TrueType glyf outlines required).");
        }
        return table;
    }

    private int Check(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > data.Length - length)
        {
            throw new InvalidDataException("Font table range outside the file.");
        }
        return offset;
    }

    private int U16(int p) => (data[p] << 8) | data[p + 1];

    private short S16(int p) => (short)U16(p);

    private uint U32(int p) => ((uint)data[p] << 24) | ((uint)data[p + 1] << 16) | ((uint)data[p + 2] << 8) | data[p + 3];

    private float F2Dot14(int p) => S16(p) / 16384f;
}
