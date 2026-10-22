using System;
using System.Collections.Generic;
using System.Globalization;
using SimpleSigh.Layout;

namespace SimpleSigh.Fonts;

/// <summary>
/// The sign font's glyph atlas: rasterizes characters on demand from a <see cref="TrueTypeFont"/> and
/// packs them into one alpha texture (rows bottom to top, like Unity's SetPixels32). Game-independent:
/// the game side only uploads <see cref="Alpha"/> to a texture.
/// <para>
/// Call <see cref="Ensure"/> with a sign's text before laying it out; <see cref="TryGetGlyph"/> only
/// reports characters already in the atlas. Adding glyphs never moves existing ones, so other signs'
/// meshes stay valid; <see cref="Generation"/> changes only when existing UVs do (growth or clearing).
/// The atlas grows from <c>initialSize</c> up to <c>maxSize</c>; characters that no longer fit are
/// reported as missing (drawn as the replacement mark) until <see cref="Clear"/>.
/// </para>
/// </summary>
public sealed class GlyphAtlas : IGlyphSource
{
    private readonly TrueTypeFont font;
    private readonly float scale;
    private readonly float ascent;
    private readonly int initialSize;
    private readonly int maxSize;
    private readonly int padding;
    private readonly Dictionary<char, Entry> entries = new Dictionary<char, Entry>();
    private readonly HashSet<char> overflow = new HashSet<char>();
    private readonly List<Shelf> shelves = new List<Shelf>();
    private int nextShelfY;
    private int dirtyMinX, dirtyMinY, dirtyMaxX, dirtyMaxY;

    public GlyphAtlas(TrueTypeFont font, float pixelSize, int initialSize = 512, int maxSize = 2048, int padding = 2)
    {
        this.font = font ?? throw new ArgumentNullException(nameof(font));
        if (!(pixelSize > 0f) || initialSize < 16 || maxSize < initialSize || padding < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize), "Invalid atlas configuration.");
        }
        this.initialSize = initialSize;
        this.maxSize = maxSize;
        this.padding = padding;
        PixelSize = pixelSize;
        scale = pixelSize / font.UnitsPerEm;
        ascent = font.Ascender * scale;
        LineHeight = (font.Ascender - font.Descender) * scale;
        if (!(LineHeight > 0f))
        {
            LineHeight = pixelSize;
            ascent = pixelSize * 0.8f;
        }
        Reset(initialSize);
    }

    /// <summary>Em size the glyphs are rasterized at, in atlas pixels.</summary>
    public float PixelSize { get; }

    /// <summary>Line box (ascent - descent) in atlas pixels.</summary>
    public float LineHeight { get; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    /// <summary>Width × Height alpha coverage, first row is the bottom row of the texture.</summary>
    public byte[] Alpha { get; private set; }

    /// <summary>Changes whenever previously reported UVs become invalid (growth or <see cref="Clear"/>).</summary>
    public int Generation { get; private set; }

    /// <summary>Characters rasterized into the atlas.</summary>
    public int GlyphCount { get; private set; }

    /// <summary>True when a character could not be placed even at the maximum atlas size.</summary>
    public bool IsFull => overflow.Count > 0;

    /// <summary>True when <see cref="Alpha"/> changed since the last <see cref="TakeUpload"/>.</summary>
    public bool HasPendingUpload => FullUploadPending || dirtyMaxX > dirtyMinX;

    /// <summary>The texture must be resized and fully re-uploaded.</summary>
    public bool FullUploadPending { get; private set; }

    /// <summary>
    /// Rasterizes every displayable character of <paramref name="text"/> (and the replacement mark)
    /// that is not yet in the atlas. Returns true when the atlas pixels changed.
    /// </summary>
    public bool Ensure(string text)
    {
        bool changed = Add(GlyphQuadBuilder.Replacement);
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text ?? string.Empty);
        while (elements.MoveNext())
        {
            if (GlyphQuadBuilder.TryGetDisplayChar(elements.GetTextElement(), out char c))
            {
                changed |= Add(c);
            }
        }
        return changed;
    }

    public bool TryGetGlyph(char c, out GlyphMetrics glyph)
    {
        if (!entries.TryGetValue(c, out Entry entry) || entry.Missing)
        {
            glyph = default;
            return false;
        }
        GlyphBitmap bitmap = entry.Bitmap;
        float top = ascent - bitmap.Top;
        glyph = new GlyphMetrics
        {
            Found = true,
            Visible = !bitmap.IsEmpty,
            AdvanceX = entry.Advance,
            X0 = bitmap.Left,
            X1 = bitmap.Left + bitmap.Width,
            Y0 = top,
            Y1 = top + bitmap.Height,
            U0 = entry.X / (float)Width,
            U1 = (entry.X + bitmap.Width) / (float)Width,
            V0 = (entry.Y + bitmap.Height) / (float)Height,
            V1 = entry.Y / (float)Height,
        };
        return true;
    }

    /// <summary>
    /// The region to upload since the last call: the whole atlas after growth/clearing, otherwise the
    /// bounding box of newly added glyphs. Returns false when nothing changed.
    /// </summary>
    public bool TakeUpload(out int x, out int y, out int width, out int height, out bool resized)
    {
        resized = FullUploadPending;
        if (FullUploadPending)
        {
            x = 0;
            y = 0;
            width = Width;
            height = Height;
        }
        else if (dirtyMaxX > dirtyMinX)
        {
            x = dirtyMinX;
            y = dirtyMinY;
            width = dirtyMaxX - dirtyMinX;
            height = dirtyMaxY - dirtyMinY;
        }
        else
        {
            x = y = width = height = 0;
        }
        bool any = width > 0 && height > 0;
        FullUploadPending = false;
        ResetDirty();
        return any;
    }

    /// <summary>Drops every glyph and shrinks back to the initial size (e.g. when another game is loaded).</summary>
    public void Clear()
    {
        Reset(initialSize);
        Generation++;
    }

    private bool Add(char c)
    {
        if (entries.ContainsKey(c) || overflow.Contains(c))
        {
            return false;
        }
        int glyphIndex = char.IsSurrogate(c) ? 0 : font.GetGlyphIndex(c);
        if (glyphIndex == 0)
        {
            entries[c] = Entry.MissingGlyph;
            return false;
        }
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(font.GetContours(glyphIndex), scale);
        var entry = new Entry(bitmap, font.GetAdvanceWidth(glyphIndex) * scale);
        if (bitmap.IsEmpty)
        {
            entries[c] = entry;
            return false;
        }
        while (!TryPlace(entry))
        {
            if (Width >= maxSize)
            {
                overflow.Add(c);
                return false;
            }
            Grow();
        }
        entries[c] = entry;
        GlyphCount++;
        return true;
    }

    private void Grow()
    {
        var placed = new List<Entry>();
        foreach (Entry entry in entries.Values)
        {
            if (!entry.Missing && !entry.Bitmap.IsEmpty)
            {
                placed.Add(entry);
            }
        }
        Reset(Math.Min(maxSize, Width * 2), keepEntries: true);
        // Tallest first packs the shelves densely; the order only affects density, not correctness.
        placed.Sort((a, b) => b.Bitmap.Height.CompareTo(a.Bitmap.Height));
        foreach (Entry entry in placed)
        {
            if (!TryPlace(entry))
            {
                // Not expected with four times the area, but never leave an entry pointing at stale pixels.
                foreach (KeyValuePair<char, Entry> pair in entries)
                {
                    if (ReferenceEquals(pair.Value, entry))
                    {
                        entries.Remove(pair.Key);
                        overflow.Add(pair.Key);
                        GlyphCount--;
                        break;
                    }
                }
            }
        }
        Generation++;
    }

    private bool TryPlace(Entry entry)
    {
        int w = entry.Bitmap.Width;
        int h = entry.Bitmap.Height;
        Shelf best = null;
        foreach (Shelf shelf in shelves)
        {
            if (shelf.Height >= h && shelf.Height <= h + h / 3 + 2 && shelf.NextX + w + padding <= Width
                && (best == null || shelf.Height < best.Height))
            {
                best = shelf;
            }
        }
        if (best == null)
        {
            if (nextShelfY + h + padding > Height || padding + w + padding > Width)
            {
                return false;
            }
            best = new Shelf(nextShelfY, h, padding);
            shelves.Add(best);
            nextShelfY += h + padding;
        }
        entry.X = best.NextX;
        entry.Y = best.Y;
        best.NextX += w + padding;
        Blit(entry);
        return true;
    }

    private void Blit(Entry entry)
    {
        GlyphBitmap bitmap = entry.Bitmap;
        for (int row = 0; row < bitmap.Height; row++)
        {
            // Bitmap rows run top to bottom; atlas rows bottom to top.
            int atlasRow = entry.Y + bitmap.Height - 1 - row;
            Array.Copy(bitmap.Coverage, row * bitmap.Width, Alpha, atlasRow * Width + entry.X, bitmap.Width);
        }
        dirtyMinX = Math.Min(dirtyMinX, entry.X);
        dirtyMinY = Math.Min(dirtyMinY, entry.Y);
        dirtyMaxX = Math.Max(dirtyMaxX, entry.X + bitmap.Width);
        dirtyMaxY = Math.Max(dirtyMaxY, entry.Y + bitmap.Height);
    }

    private void Reset(int size, bool keepEntries = false)
    {
        Width = size;
        Height = size;
        Alpha = new byte[size * size];
        shelves.Clear();
        nextShelfY = padding;
        if (!keepEntries)
        {
            entries.Clear();
            overflow.Clear();
            GlyphCount = 0;
        }
        FullUploadPending = true;
        ResetDirty();
    }

    private void ResetDirty()
    {
        dirtyMinX = int.MaxValue;
        dirtyMinY = int.MaxValue;
        dirtyMaxX = int.MinValue;
        dirtyMaxY = int.MinValue;
    }

    private sealed class Shelf
    {
        public Shelf(int y, int height, int startX)
        {
            Y = y;
            Height = height;
            NextX = startX;
        }

        public int Y { get; }

        public int Height { get; }

        public int NextX { get; set; }
    }

    private sealed class Entry
    {
        public static readonly Entry MissingGlyph = new Entry(GlyphBitmap.Empty, 0f) { Missing = true };

        public Entry(GlyphBitmap bitmap, float advance)
        {
            Bitmap = bitmap;
            Advance = advance;
        }

        public GlyphBitmap Bitmap { get; }

        public float Advance { get; }

        public bool Missing { get; private init; }

        public int X { get; set; }

        public int Y { get; set; }
    }
}
