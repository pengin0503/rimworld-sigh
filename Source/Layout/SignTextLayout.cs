using System;
using System.Collections.Generic;

namespace SimpleSigh.Layout;

/// <summary>
/// The complete, game-independent text layout of one sign: padded panel → fit/wrap → glyph quads
/// → clip. The in-game renderer only turns the result into a mesh, so tests and the offline preview
/// exercise exactly the same placement maths.
/// </summary>
public static class SignTextLayout
{
    /// <summary>Smallest fit scale; below this the layout reports it cannot fit and clips.</summary>
    public const float MinFitScale = 0.05f;

    public sealed class Result
    {
        public static readonly Result Empty = new Result(new List<GlyphQuad>(), 0f, 0f, 0f, true);

        public Result(List<GlyphQuad> quads, float centerX, float centerZ, float lineHeight, bool fits)
        {
            Quads = quads;
            CenterX = centerX;
            CenterZ = centerZ;
            LineHeight = lineHeight;
            Fits = fits;
        }

        /// <summary>Quads in world units, relative to the panel centre (X east, Y = world Z north).</summary>
        public List<GlyphQuad> Quads { get; }

        /// <summary>Panel centre relative to the thing's draw position.</summary>
        public float CenterX { get; }

        public float CenterZ { get; }

        public float LineHeight { get; }

        public bool Fits { get; }
    }

    /// <param name="padding">Gap between panel edge and text as a fraction of the panel's shorter side.</param>
    /// <param name="userScale">User text size in (0, 1]: fraction of the largest size that fits.</param>
    public static Result Build(string text, PanelPlacement panel, float padding, bool multiline, float userScale, IGlyphSource glyphs)
    {
        if (string.IsNullOrEmpty(text) || glyphs.LineHeight <= 0f)
        {
            return Result.Empty;
        }

        float pad = padding * Math.Min(panel.Width, panel.Height);
        float innerWidth = panel.Width - pad * 2f;
        float innerHeight = panel.Height - pad * 2f;
        if (innerWidth <= 0f || innerHeight <= 0f)
        {
            return Result.Empty;
        }

        // At scale 1 one line fills the panel height.
        float pixelsToWorld = innerHeight / glyphs.LineHeight;
        Func<string, float> measure = s => GlyphQuadBuilder.MeasureLine(s, glyphs) * pixelsToWorld;
        // An editor restricted to one input line may still DISPLAY that text on multiple rows.
        // In a narrow 1x1 panel this preserves legibility rather than shrinking a long Japanese
        // label into an unreadable strip. Short strings remain on one row.
        bool displayWrap = multiline;
        SignLayoutResult layout = SignLayout.Fit(text, innerWidth, innerHeight, measure, innerHeight, MinFitScale, wrap: displayWrap);
        if (!multiline)
        {
            SignLayoutResult wrapped = SignLayout.Fit(text, innerWidth, innerHeight, measure, innerHeight, MinFitScale, wrap: true);
            if (wrapped.Fits && (!layout.Fits || wrapped.Scale > layout.Scale * 1.03f))
            {
                layout = wrapped;
                displayWrap = true;
            }
        }
        if (userScale < 1f && layout.Fits)
        {
            // The size slider is a fraction of the largest legible size; preserve the chosen
            // wrapping strategy when refitting so the slider cannot unexpectedly break layout.
            float target = Math.Max(MinFitScale, layout.Scale * userScale);
            layout = SignLayout.Fit(text, innerWidth, innerHeight, measure, innerHeight, Math.Min(MinFitScale, target), target, displayWrap);
        }
        float lineHeight = innerHeight * layout.Scale;
        List<GlyphQuad> quads = GlyphQuadBuilder.ClipTo(
            GlyphQuadBuilder.Build(layout.Lines, glyphs, lineHeight), innerWidth * 0.5f, innerHeight * 0.5f);
        return new Result(quads, panel.CenterX, panel.CenterZ, lineHeight, layout.Fits);
    }
}
