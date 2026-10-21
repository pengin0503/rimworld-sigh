using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleSigh.Rendering;
using SimpleSigh.Text;

namespace SimpleSigh.Signs;

public sealed class CompSignText : ThingComp
{
    private string content = "";
    private Color textColor = Color.black;
    private readonly SignRenderCache renderCache = new();
    public string Content => content;
    public Color TextColor => textColor;
    public CompProperties_SignText SignProps => (CompProperties_SignText)props;

    public void SetText(string value)
    {
        if (!SignTextRules.TryNormalize(value, SignProps.maxElements, SignProps.multiline,
            out string normalized, out _)) return;
        if (normalized == content) return;
        content = normalized;
        renderCache.Invalidate();
    }

    public void SetColor(Color color)
    {
        if (textColor == color) return;
        textColor = color;
        renderCache.Invalidate();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref content, "simpleSighText", "");
        Scribe_Values.Look(ref textColor, "simpleSighColor", Color.black);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            content ??= "";
            renderCache.Invalidate();
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Command_Action
        {
            defaultLabel = "SimpleSigh_EditText".Translate(),
            icon = TexButton.Rename,
            action = () => Find.WindowStack.Add(new Dialog_EditSign(this))
        };
    }

    public override void DrawGUIOverlay() => SignTextRenderer.Draw(parent, content, textColor, SignProps, renderCache);

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        renderCache.Invalidate();
        base.PostDeSpawn(map, mode);
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        renderCache.Invalidate();
        base.PostDestroy(mode, previousMap);
    }
}
