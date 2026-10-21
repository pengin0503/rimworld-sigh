using SimpleSigh.Layout;
using UnityEngine;
using UnityStubs.Gui;
using GameText = Verse.Text;

namespace SimpleSigh.Rendering;

/// <summary>
/// Glyph metrics from RimWorld 1.7's text backend (<see cref="TextMeshProBackend"/>), the same
/// source UnityStubs' TextMeshPro uses for world-space text. Font indices are the game's GUI fonts,
/// so the sign can draw whatever glyphs the game's UI font atlas provides.
/// </summary>
public sealed class TmpGlyphSource : IGlyphSource
{
    private readonly ITextMeshProBackend backend;
    private readonly int fontIndex;

    private TmpGlyphSource(ITextMeshProBackend backend, int fontIndex, float lineHeight)
    {
        this.backend = backend;
        this.fontIndex = fontIndex;
        LineHeight = lineHeight;
    }

    public float LineHeight { get; }

    public int FontIndex => fontIndex;

    public int AtlasVersion => backend.AtlasVersion;

    public Texture2D Atlas => backend.GetAtlasTexture(fontIndex);

    /// <summary>The medium UI font: the largest baked size, so world text stays sharp when zoomed in.</summary>
    public static int SignFontIndex => GameText.fontStyles[(int)Verse.GameFont.Medium]?.font?.GameFontIndex ?? 2;

    private static TmpGlyphSource cached;

    /// <summary>Returns the shared source for the current backend and font; allocates only when either changes.</summary>
    public static bool TryCreate(out TmpGlyphSource source)
    {
        source = null;
        ITextMeshProBackend backend = TextMeshProBackend.Current;
        int fontIndex = SignFontIndex;
        if (cached != null && ReferenceEquals(cached.backend, backend) && cached.fontIndex == fontIndex)
        {
            source = cached;
            return true;
        }
        if (backend == null || !backend.TryGetFontMetrics(fontIndex, out TmpFontMetrics metrics) || !metrics.found)
        {
            return false;
        }
        // Same line box as UnityStubs' TextMeshPro: ascent - descent (descent is negative).
        float lineHeight = metrics.ascent - metrics.descent;
        if (lineHeight <= 0f)
        {
            lineHeight = metrics.lineHeight;
        }
        if (lineHeight <= 0f)
        {
            return false;
        }
        source = cached = new TmpGlyphSource(backend, fontIndex, lineHeight);
        return true;
    }

    public bool TryGetGlyph(char c, out GlyphMetrics glyph)
    {
        if (!backend.TryGetGlyph(fontIndex, c, out TmpGlyphMetrics g) || !g.found)
        {
            glyph = default;
            return false;
        }
        glyph = new GlyphMetrics
        {
            Found = true,
            Visible = g.visible,
            AdvanceX = g.advanceX,
            X0 = g.x0,
            Y0 = g.y0,
            X1 = g.x1,
            Y1 = g.y1,
            U0 = g.u0,
            V0 = g.v0,
            U1 = g.u1,
            V1 = g.v1,
        };
        return true;
    }
}
