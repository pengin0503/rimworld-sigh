using SimpleSigh.Layout;
using UnityEngine;
using UnityStubs.Gui;
using GameText = Verse.Text;

namespace SimpleSigh.Rendering;

/// <summary>
/// Glyph metrics from RimWorld 1.7's text backend (<see cref="TextMeshProBackend"/>), the same
/// source UnityStubs' TextMeshPro uses for world-space text. Font indices are the game's GUI fonts,
/// so the sign can draw whatever glyphs the game's UI font atlas provides. That atlas bakes only a
/// fixed character set (Japanese typed on a sign shows as "?"), so this is the fallback when the
/// bundled <see cref="SignFont"/> cannot be used.
/// </summary>
public sealed class TmpGlyphSource : ISignGlyphs
{
    private readonly ITextMeshProBackend backend;
    private readonly int fontIndex;

    private static int nextGeneration;

    private TmpGlyphSource(ITextMeshProBackend backend, int fontIndex, float lineHeight)
    {
        this.backend = backend;
        this.fontIndex = fontIndex;
        LineHeight = lineHeight;
        // A new instance means another backend object, font or atlas version: meshes built from the
        // previous one may hold stale UVs even when the backend reports equal version numbers.
        Generation = ++nextGeneration;
    }

    public float LineHeight { get; }

    public int FontIndex => fontIndex;

    public int AtlasVersion => backend.AtlasVersion;

    public int SourceId => fontIndex;

    public int Generation { get; }

    public Texture2D Atlas => backend.GetAtlasTexture(fontIndex);

    /// <summary>The medium UI font: the largest baked size, so world text stays sharp when zoomed in.</summary>
    public static int SignFontIndex => GameText.fontStyles[(int)Verse.GameFont.Medium]?.font?.GameFontIndex ?? 2;

    private static TmpGlyphSource cached;

    private int metricsAtlasVersion;

    /// <summary>Shared source for the game's current text backend and the sign font.</summary>
    public static bool TryCreate(out TmpGlyphSource source)
    {
        return TryCreateFor(TextMeshProBackend.Current, SignFontIndex, out source);
    }

    /// <summary>
    /// Returns the cached source while backend, font and atlas version are unchanged (no allocation in the
    /// draw loop). A new atlas version can mean new font data (e.g. after a language switch), so the font
    /// metrics are read again then.
    /// </summary>
    public static bool TryCreateFor(ITextMeshProBackend backend, int fontIndex, out TmpGlyphSource source)
    {
        source = null;
        if (backend == null)
        {
            return false;
        }
        if (cached != null && ReferenceEquals(cached.backend, backend) && cached.fontIndex == fontIndex
            && cached.metricsAtlasVersion == backend.AtlasVersion)
        {
            source = cached;
            return true;
        }
        if (!backend.TryGetFontMetrics(fontIndex, out TmpFontMetrics metrics) || !metrics.found)
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
        source = cached = new TmpGlyphSource(backend, fontIndex, lineHeight) { metricsAtlasVersion = backend.AtlasVersion };
        return true;
    }

    /// <summary>The game atlas cannot be extended from here; missing characters are drawn as the replacement mark.</summary>
    public void Prepare(string text)
    {
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
