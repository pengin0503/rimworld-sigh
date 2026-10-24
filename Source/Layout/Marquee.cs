using System;
using System.Collections.Generic;

namespace SimpleSigh.Layout;

/// <summary>
/// Scrolling LED text. The text is laid out once as a single row starting at x = 0; every frame it is
/// shifted left and clipped to the panel, entering at the right edge and leaving fully on the left
/// before it starts again.
/// </summary>
public static class Marquee
{
    /// <summary>Scroll speed in row heights per second (about three full-width characters).</summary>
    public const float RowsPerSecond = 3f;

    public sealed class Layout
    {
        public static readonly Layout Empty = new Layout(new List<GlyphQuad>(), 0f, 0f, 0f, 0f, 0f, 0f);

        public Layout(List<GlyphQuad> quads, float textWidth, float lineHeight, float halfWidth, float halfHeight, float centerX, float centerZ)
        {
            Quads = quads;
            TextWidth = textWidth;
            LineHeight = lineHeight;
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
            CenterX = centerX;
            CenterZ = centerZ;
        }

        /// <summary>Unclipped glyphs of the whole row, pen starting at x = 0, Y relative to the panel centre.</summary>
        public List<GlyphQuad> Quads { get; }

        public float TextWidth { get; }

        public float LineHeight { get; }

        /// <summary>Clip box: the whole panel, so text slides in and out at the screen's edges.</summary>
        public float HalfWidth { get; }

        public float HalfHeight { get; }

        public float CenterX { get; }

        public float CenterZ { get; }

        public float PanelWidth => HalfWidth * 2f;
    }

    /// <summary>
    /// One row as tall as the padded panel times the user size; line breaks are shown as spaces.
    /// Vertical alignment applies as for static text; horizontal alignment has no meaning here.
    /// </summary>
    public static Layout Build(string text, PanelPlacement panel, float padding, SignTextStyle style, IGlyphSource glyphs)
    {
        if (string.IsNullOrEmpty(text) || glyphs.LineHeight <= 0f)
        {
            return Layout.Empty;
        }
        float pad = padding * Math.Min(panel.Width, panel.Height);
        float innerHeight = panel.Height - pad * 2f;
        if (panel.Width - pad * 2f <= 0f || innerHeight <= 0f)
        {
            return Layout.Empty;
        }
        string row = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        float lineHeight = innerHeight * style.Scale;
        // Left alignment with halfWidth 0 puts the pen at x = 0.
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { row }, glyphs, lineHeight, SignHAlign.Left, 0f);
        float slack = (innerHeight - lineHeight) * 0.5f;
        float shift = style.VAlign switch
        {
            SignVAlign.Top => slack,
            SignVAlign.Bottom => -slack,
            _ => 0f,
        };
        if (shift != 0f)
        {
            for (int i = 0; i < quads.Count; i++)
            {
                GlyphQuad q = quads[i];
                q.Y0 += shift;
                q.Y1 += shift;
                quads[i] = q;
            }
        }
        float textWidth = GlyphQuadBuilder.MeasureLine(row, glyphs) * lineHeight / glyphs.LineHeight;
        return new Layout(quads, textWidth, lineHeight, panel.Width * 0.5f, panel.Height * 0.5f, panel.CenterX, panel.CenterZ);
    }

    public static float SpeedFor(float lineHeight) => lineHeight * RowsPerSecond;

    /// <summary>
    /// Distance scrolled at <paramref name="timeSeconds"/>, looping every text width + panel width.
    /// Computed in double so long sessions do not make the motion jitter.
    /// </summary>
    public static float Offset(float timeSeconds, float speedCellsPerSecond, float textWidth, float panelWidth)
    {
        double cycle = textWidth + panelWidth;
        if (cycle <= 0.0)
        {
            return 0f;
        }
        double distance = (double)timeSeconds * speedCellsPerSecond % cycle;
        return (float)(distance < 0.0 ? distance + cycle : distance);
    }

    /// <summary>
    /// Writes the visible part of <paramref name="line"/> at <paramref name="offset"/> into
    /// <paramref name="output"/> (cleared first). At offset 0 the text starts at the right edge.
    /// Allocates nothing once <paramref name="output"/> has the line's capacity.
    /// </summary>
    public static void Clip(List<GlyphQuad> line, float offset, float halfWidth, float halfHeight, List<GlyphQuad> output)
    {
        output.Clear();
        float shift = halfWidth - offset;
        for (int i = 0; i < line.Count; i++)
        {
            if (GlyphQuadBuilder.TryClip(line[i], shift, halfWidth, halfHeight, out GlyphQuad clipped))
            {
                output.Add(clipped);
            }
        }
    }
}
