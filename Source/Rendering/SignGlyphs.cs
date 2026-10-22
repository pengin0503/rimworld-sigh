using SimpleSigh.Layout;
using UnityEngine;

namespace SimpleSigh.Rendering;

/// <summary>Glyph metrics plus the texture they are drawn from: one way of drawing sign text.</summary>
public interface ISignGlyphs : IGlyphSource
{
    /// <summary>Texture holding the glyph coverage in its alpha channel.</summary>
    Texture2D Atlas { get; }

    /// <summary>Identifies the source in the render key (bundled font or a game font index).</summary>
    int SourceId { get; }

    /// <summary>Changes whenever UVs or metrics of meshes built earlier may be stale.</summary>
    int Generation { get; }

    /// <summary>Makes every character of <paramref name="text"/> drawable. Called on the rebuild path only, never per frame.</summary>
    void Prepare(string text);
}

/// <summary>Chooses the glyph source for sign text: the bundled Japanese-capable font, else the game's UI font.</summary>
public static class SignGlyphSources
{
    public static bool TryGetCurrent(out ISignGlyphs glyphs)
    {
        if (SignFont.TryGet(out SignFont font))
        {
            glyphs = font;
            return true;
        }
        if (TmpGlyphSource.TryCreate(out TmpGlyphSource fallback))
        {
            glyphs = fallback;
            return true;
        }
        glyphs = null;
        return false;
    }
}
