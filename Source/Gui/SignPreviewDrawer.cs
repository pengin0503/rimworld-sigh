using SimpleSigh.Layout;
using SimpleSigh.Rendering;
using SimpleSigh.Signs;
using UnityEngine;
using Verse;

namespace SimpleSigh.Gui;

/// <summary>
/// Draws a sign's panel and text in the GUI, from the same <see cref="SignTextLayout"/> and glyph atlas
/// the map uses, so what the editor shows is what the sign will show. The layout is rebuilt only when
/// its inputs change, not every GUI event.
/// </summary>
public sealed class SignPreviewDrawer
{
    private string builtText;
    private SignTextStyle builtStyle;
    private int builtGeneration = int.MinValue;
    private PanelPlacement builtPanel;
    private SignTextLayout.Result layout;
    private Marquee.Layout marquee;
    private readonly System.Collections.Generic.List<GlyphQuad> scrolled = new System.Collections.Generic.List<GlyphQuad>();

    public void Draw(Rect rect, CompSignText sign, string text, Color color, SignTextStyle style)
    {
        CompProperties_SignText props = sign.Props;
        PanelPlacement panel = SignTextRenderer.PanelFor(sign, sign.parent.Rotation);
        if (panel.Width <= 0f || panel.Height <= 0f)
        {
            return;
        }

        // Fit the panel's aspect ratio into the rect.
        float pixelsPerCell = Mathf.Min(rect.width / panel.Width, rect.height / panel.Height);
        var panelRect = new Rect(0f, 0f, panel.Width * pixelsPerCell, panel.Height * pixelsPerCell)
        {
            center = rect.center,
        };
        Widgets.DrawBoxSolidWithOutline(panelRect, props.panelColor, Color.gray);
        if (string.IsNullOrEmpty(text) || !SignGlyphSources.TryGetCurrent(out ISignGlyphs glyphs))
        {
            return;
        }

        var centred = new PanelPlacement(0f, 0f, panel.Width, panel.Height);
        if (layout == null || builtText != text || builtStyle != style || builtGeneration != glyphs.Generation
            || !builtPanel.Equals(centred))
        {
            glyphs.Prepare(text);
            layout = SignTextLayout.Build(text, centred, props.panelPadding, props.multiline, style, glyphs);
            marquee = style.Effect == SignEffect.Scroll ? Marquee.Build(text, centred, props.panelPadding, style, glyphs) : null;
            builtText = text;
            builtStyle = style;
            builtGeneration = glyphs.Generation;
            builtPanel = centred;
        }

        Texture2D atlas = glyphs.Atlas;
        if (atlas == null || Event.current.type != EventType.Repaint)
        {
            return;
        }
        // The preview shows the LED effects as the map will, on the same real-time clock.
        float now = Time.realtimeSinceStartup;
        if (style.Effect == SignEffect.Blink && !SignVisibility.BlinkOn(now))
        {
            return;
        }
        System.Collections.Generic.List<GlyphQuad> quads = layout.Quads;
        float lineHeight = layout.LineHeight;
        if (marquee != null)
        {
            float offset = Marquee.Offset(now, Marquee.SpeedFor(marquee.LineHeight), marquee.TextWidth, marquee.PanelWidth);
            Marquee.Clip(marquee.Quads, offset, marquee.HalfWidth, marquee.HalfHeight, scrolled);
            quads = scrolled;
            lineHeight = marquee.LineHeight;
        }
        Color previous = GUI.color;
        if (style.Shadow)
        {
            GUI.color = SignTextRenderer.FromRgba(SignShadow.ColorFor(SignTextRenderer.ToRgba(color)));
            DrawQuads(quads, panelRect, pixelsPerCell, atlas, SignShadow.Offset(lineHeight));
        }
        GUI.color = color;
        DrawQuads(quads, panelRect, pixelsPerCell, atlas, 0f);
        GUI.color = previous;
    }

    private static void DrawQuads(System.Collections.Generic.List<GlyphQuad> quads, Rect panelRect, float pixelsPerCell, Texture2D atlas,
        float offset)
    {
        Vector2 center = panelRect.center;
        foreach (GlyphQuad q in quads)
        {
            // Text Y points up (world north); GUI Y points down.
            var screen = new Rect(center.x + (q.X0 + offset) * pixelsPerCell, center.y - (q.Y1 - offset) * pixelsPerCell,
                (q.X1 - q.X0) * pixelsPerCell, (q.Y1 - q.Y0) * pixelsPerCell);
            var uv = new Rect(q.U0, q.VBottom, q.U1 - q.U0, q.VTop - q.VBottom);
            GUI.DrawTextureWithTexCoords(screen, atlas, uv);
        }
    }
}
