using UnityEngine;
using UnityStubs.Gui;

namespace SimpleSigh.GameSmoke;

/// <summary>Stand-in for RimWorld 1.7's text backend implementing the real UnityStubs interface.</summary>
internal sealed class FakeTextBackend : ITextMeshProBackend
{
    public float Ascent = 30f;
    public float Descent = -8f;

    public float BakePixelSize => 38f;

    public int AtlasVersion { get; set; } = 1;

    public bool TryGetFontMetrics(int fontIndex, out TmpFontMetrics metrics)
    {
        metrics = new TmpFontMetrics { found = true, bakedPixelSize = 38f, ascent = Ascent, descent = Descent, lineHeight = 40f };
        return true;
    }

    public bool TryGetGlyph(int fontIndex, char c, out TmpGlyphMetrics glyph)
    {
        if (c == '☃')
        {
            glyph = default;
            return false;
        }
        float advance = c < 0x3000 ? 18f : 36f;
        glyph = new TmpGlyphMetrics
        {
            found = true, visible = c != ' ', advanceX = advance,
            x0 = 1f, y0 = 4f, x1 = advance - 1f, y1 = 34f,
            u0 = 0.1f, v0 = 0.2f, u1 = 0.3f, v1 = 0.4f,
        };
        return true;
    }

    public bool TryGetGlyphAtSize(int fontIndex, char c, float pixelSize, out TmpGlyphMetrics glyph, out float ascent)
    {
        ascent = Ascent;
        return TryGetGlyph(fontIndex, c, out glyph);
    }

    public string GetCharacters(int fontIndex) => string.Empty;

    public Texture2D GetAtlasTexture(int fontIndex) => null;
}
