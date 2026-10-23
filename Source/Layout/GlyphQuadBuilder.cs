using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SimpleSigh.Layout;

/// <summary>Glyph metrics in baked font pixels; Y values are measured downward from the line's top.</summary>
public struct GlyphMetrics
{
    public bool Found;
    public bool Visible;
    public float AdvanceX;
    public float X0;
    public float Y0;
    public float X1;
    public float Y1;
    public float U0;
    public float V0;
    public float U1;
    public float V1;
}

public interface IGlyphSource
{
    /// <summary>Line box height in baked pixels.</summary>
    float LineHeight { get; }

    bool TryGetGlyph(char c, out GlyphMetrics glyph);
}

/// <summary>One textured quad in world units, Y up, relative to the text block centre.</summary>
public struct GlyphQuad
{
    public float X0;
    public float Y0;
    public float X1;
    public float Y1;
    public float U0;
    public float U1;
    public float VTop;
    public float VBottom;
}

/// <summary>
/// Turns lines of text into glyph quads. Each user-perceived character (text element) maps to
/// exactly one glyph; elements the font cannot draw (missing glyphs, surrogate pairs, combining
/// sequences) are shown as a replacement mark instead of silently disappearing.
/// </summary>
public static class GlyphQuadBuilder
{
    public const char Replacement = '?';

    public static float MeasureLine(string line, IGlyphSource source)
    {
        float width = 0f;
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(line ?? string.Empty);
        while (elements.MoveNext())
        {
            if (TryResolve(elements.GetTextElement(), source, out GlyphMetrics glyph))
            {
                width += glyph.AdvanceX;
            }
        }
        return width;
    }

    public static bool HasMissingGlyphs(string text, IGlyphSource source)
    {
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text ?? string.Empty);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (element == "\n")
            {
                continue;
            }
            if (!TryGetSingle(element, source, out _))
            {
                return true;
            }
        }
        return false;
    }

    public static List<GlyphQuad> Build(IReadOnlyList<string> lines, IGlyphSource source, float worldLineHeight)
    {
        return Build(lines, source, worldLineHeight, SignHAlign.Center, 0f);
    }

    /// <summary>
    /// Lines stacked top to bottom around Y = 0. Each line is centred on X = 0, or starts at
    /// <c>-halfWidth</c> (left) / ends at <c>+halfWidth</c> (right).
    /// </summary>
    public static List<GlyphQuad> Build(IReadOnlyList<string> lines, IGlyphSource source, float worldLineHeight, SignHAlign align, float halfWidth)
    {
        var quads = new List<GlyphQuad>();
        if (lines.Count == 0 || source.LineHeight <= 0f)
        {
            return quads;
        }

        float k = worldLineHeight / source.LineHeight;
        float blockTop = lines.Count * worldLineHeight * 0.5f;
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            string line = lines[lineIndex];
            float lineTop = blockTop - lineIndex * worldLineHeight;
            float lineWidth = MeasureLine(line, source) * k;
            float penX = align switch
            {
                SignHAlign.Left => -halfWidth,
                SignHAlign.Right => halfWidth - lineWidth,
                _ => -lineWidth * 0.5f,
            };
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(line ?? string.Empty);
            while (elements.MoveNext())
            {
                if (!TryResolve(elements.GetTextElement(), source, out GlyphMetrics glyph))
                {
                    continue;
                }
                if (glyph.Visible && glyph.X1 > glyph.X0 && glyph.Y1 > glyph.Y0)
                {
                    quads.Add(new GlyphQuad
                    {
                        X0 = penX + glyph.X0 * k,
                        X1 = penX + glyph.X1 * k,
                        Y1 = lineTop - glyph.Y0 * k,
                        Y0 = lineTop - glyph.Y1 * k,
                        U0 = glyph.U0,
                        U1 = glyph.U1,
                        VTop = glyph.V0,
                        VBottom = glyph.V1,
                    });
                }
                penX += glyph.AdvanceX * k;
            }
        }
        return quads;
    }

    /// <summary>
    /// Clips quads to a centred box so text never spills off the panel. A glyph crossing the edge is
    /// trimmed, with its texture coordinates cut in proportion, rather than dropped: a character whose
    /// ink slightly exceeds its line box (accents, tall glyphs) stays visible. Quads entirely outside
    /// the box are removed.
    /// </summary>
    public static List<GlyphQuad> ClipTo(List<GlyphQuad> quads, float halfWidth, float halfHeight)
    {
        var kept = new List<GlyphQuad>(quads.Count);
        foreach (GlyphQuad quad in quads)
        {
            float x0 = Math.Max(quad.X0, -halfWidth);
            float x1 = Math.Min(quad.X1, halfWidth);
            float y0 = Math.Max(quad.Y0, -halfHeight);
            float y1 = Math.Min(quad.Y1, halfHeight);
            if (x1 - x0 <= 1e-6f || y1 - y0 <= 1e-6f)
            {
                continue;
            }
            float width = quad.X1 - quad.X0;
            float height = quad.Y1 - quad.Y0;
            float du = quad.U1 - quad.U0;
            float dv = quad.VTop - quad.VBottom;
            kept.Add(new GlyphQuad
            {
                X0 = x0,
                X1 = x1,
                Y0 = y0,
                Y1 = y1,
                U0 = quad.U0 + du * (x0 - quad.X0) / width,
                U1 = quad.U0 + du * (x1 - quad.X0) / width,
                VBottom = quad.VBottom + dv * (y0 - quad.Y0) / height,
                VTop = quad.VBottom + dv * (y1 - quad.Y0) / height,
            });
        }
        return kept;
    }

    private static bool TryResolve(string element, IGlyphSource source, out GlyphMetrics glyph)
    {
        return TryGetSingle(element, source, out glyph)
            || (source.TryGetGlyph(Replacement, out glyph) && glyph.Found);
    }

    /// <summary>
    /// The single character a text element is drawn with. Display-only NFC: "か" + U+3099 is drawn with
    /// the font's "が"; saved text is unchanged. False for elements that need more than one character
    /// (surrogate pairs, unnormalizable combining sequences): those are drawn as <see cref="Replacement"/>.
    /// </summary>
    public static bool TryGetDisplayChar(string element, out char c)
    {
        if (element.Length > 1 && !element.IsNormalized(NormalizationForm.FormC))
        {
            element = element.Normalize(NormalizationForm.FormC);
        }
        c = element.Length == 1 ? element[0] : '\0';
        return element.Length == 1;
    }

    private static bool TryGetSingle(string element, IGlyphSource source, out GlyphMetrics glyph)
    {
        if (TryGetDisplayChar(element, out char c) && source.TryGetGlyph(c, out glyph) && glyph.Found)
        {
            return true;
        }
        glyph = default;
        return false;
    }
}
