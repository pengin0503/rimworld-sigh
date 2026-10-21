using System.Collections.Generic;
using SimpleSigh.Layout;

namespace SimpleSigh.Text.Tests;

/// <summary>
/// Deterministic glyph source: ASCII advances 10px, CJK (U+3000–U+9FFF, U+FF00–U+FFEF) 20px,
/// line height 30px. Characters in <see cref="Missing"/> report not found.
/// </summary>
internal sealed class FakeGlyphs : IGlyphSource
{
    public readonly HashSet<char> Missing = new HashSet<char>();

    public float LineHeight => 30f;

    public static float Advance(char c) => (c >= '　' && c <= '鿿') || (c >= '＀' && c <= '￯') ? 20f : 10f;

    public bool TryGetGlyph(char c, out GlyphMetrics glyph)
    {
        if (Missing.Contains(c) || char.IsSurrogate(c))
        {
            glyph = default;
            return false;
        }
        float advance = Advance(c);
        glyph = new GlyphMetrics
        {
            Found = true,
            Visible = c != ' ',
            AdvanceX = advance,
            X0 = 1f, X1 = advance - 1f, Y0 = 4f, Y1 = 26f,
            U0 = c / 1000f, V0 = 0.1f, U1 = c / 1000f + 0.01f, V1 = 0.2f,
        };
        return true;
    }
}
