using SimpleSigh.Signs;
using UnityEngine;
using Verse;

namespace SimpleSigh.Rendering;

/// <summary>
/// Temporary screen-space on-panel rendering prototype. NOT yet a validated
/// 1.7 world-space texture renderer; must pass in-game position/glyph checks.
/// No textures are re-created on each DrawGUIOverlay call.
/// </summary>
public static class SignTextRenderer
{
    public static void Draw(Thing parent, string text, Color color, CompProperties_SignText props, SignRenderCache cache)
    {
        if (string.IsNullOrEmpty(text) || parent == null || !parent.Spawned || parent.Fogged()) return;
        float cell = Find.CameraDriver.CellSizePixels;
        if (cell < 15f) return;

        // GUI space coordinate tied to Thing position, not a free-standing tooltip.
        Vector2 position = GenMapUI.LabelDrawPosFor(parent, -0.07f);
        float worldWidth = parent.def.size.x;
        float worldHeight = parent.def.size.z;
        if (parent.Rotation.IsHorizontal) { worldWidth = parent.def.size.z; worldHeight = parent.def.size.x; }
        float width = cell * worldWidth * 0.72f;
        float height = cell * worldHeight * 0.48f;
        var rect = new Rect(position.x - width / 2f, position.y - height / 2f, width, height);

        GameFont originalFont = Text.Font;
        Color originalColor = GUI.color;
        try
        {
            Text.Font = GameFont.Tiny;
            GUIStyle style = new(Text.CurFontStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            float baseLineHeight = style.lineHeight > 0 ? style.lineHeight : 11f;
            var layout = cache.Get(text, width, height, baseLineHeight, props.multiline,
                input => style.CalcSize(new GUIContent(input)).x);
            if (!layout.Fits) return;
            style.fontSize = Mathf.Max(6, Mathf.RoundToInt(style.fontSize * layout.Scale));
            GUI.color = color;
            GUI.Label(rect, string.Join("\n", layout.Lines), style);
        }
        finally
        {
            GUI.color = originalColor;
            Text.Font = originalFont;
        }
    }
}
