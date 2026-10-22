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

    /// <summary>
    /// Automatic wrapping is used only when it makes the text at least this much larger than one row;
    /// for a marginal gain a single row reads more naturally.
    /// </summary>
    public const float AutoWrapMinGain = 1.03f;

    public sealed class Result
    {
        public static readonly Result Empty = new Result(new List<GlyphQuad>(), 0f, 0f, 0f, true, Array.Empty<string>());

        public Result(List<GlyphQuad> quads, float centerX, float centerZ, float lineHeight, bool fits, IReadOnlyList<string> lines)
        {
            Quads = quads;
            CenterX = centerX;
            CenterZ = centerZ;
            LineHeight = lineHeight;
            Fits = fits;
            Lines = lines;
        }

        /// <summary>Quads in world units, relative to the panel centre (X east, Y = world Z north).</summary>
        public List<GlyphQuad> Quads { get; }

        /// <summary>Panel centre relative to the thing's draw position.</summary>
        public float CenterX { get; }

        public float CenterZ { get; }

        /// <summary>Height of one text row in world units (map cells).</summary>
        public float LineHeight { get; }

        public bool Fits { get; }

        /// <summary>Displayed rows, including rows made by automatic wrapping.</summary>
        public IReadOnlyList<string> Lines { get; }

        public int LineCount => Lines.Count;
    }

    /// <param name="padding">Gap between panel edge and text as a fraction of the panel's shorter side.</param>
    /// <param name="multiline">Multi-line signs: explicit line breaks, wrapping anywhere a line is full.</param>
    /// <param name="userScale">User text size in (0, 1]: fraction of the largest size that fits.</param>
    /// <param name="autoWrap">
    /// Single-line signs only: show the text on several rows when that makes it clearly larger (#5).
    /// Rows break between words or Japanese characters, never inside a Latin word, and are balanced in
    /// length ("立入" / "禁止"); the text itself is unchanged.
    /// </param>
    public static Result Build(string text, PanelPlacement panel, float padding, bool multiline, float userScale, IGlyphSource glyphs,
        bool autoWrap = true)
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
        bool wrap = multiline;
        bool allowWordBreaks = true;
        bool balance = false;
        SignLayoutResult layout = SignLayout.Fit(text, innerWidth, innerHeight, measure, innerHeight, MinFitScale, wrap: multiline);
        if (!multiline && autoWrap)
        {
            SignLayoutResult wrapped = SignLayout.Fit(text, innerWidth, innerHeight, measure, innerHeight, MinFitScale,
                wrap: true, allowWordBreaks: false, balanceLines: true);
            if (wrapped.Fits && (!layout.Fits || wrapped.Scale > layout.Scale * AutoWrapMinGain))
            {
                layout = wrapped;
                wrap = true;
                allowWordBreaks = false;
                balance = true;
            }
        }
        if (userScale < 1f && layout.Fits)
        {
            // The user size is a fraction of the largest size that fits; refit with the same wrapping
            // rules so smaller text can use longer lines.
            float target = Math.Max(MinFitScale, layout.Scale * userScale);
            layout = SignLayout.Fit(text, innerWidth, innerHeight, measure, innerHeight, Math.Min(MinFitScale, target), target,
                wrap, allowWordBreaks, balance);
        }
        float lineHeight = innerHeight * layout.Scale;
        // The padding is the layout margin; ink beyond a glyph's line box may use it, but never leaves the panel.
        List<GlyphQuad> quads = GlyphQuadBuilder.ClipTo(
            GlyphQuadBuilder.Build(layout.Lines, glyphs, lineHeight), panel.Width * 0.5f, panel.Height * 0.5f);
        return new Result(quads, panel.CenterX, panel.CenterZ, lineHeight, layout.Fits, layout.Lines);
    }
}
