using RimWorld;
using SimpleSigh.Text;
using UnityEngine;
using Verse;

namespace SimpleSigh.Signs;

/// <summary>Session-only clipboard for copying text and style between signs. Holds values, never Thing references.</summary>
public static class SignClipboard
{
    private static string text;
    private static Color color;
    private static float scale;
    private static bool autoWrap;

    public static bool HasContent => text != null;

    public static void Copy(CompSignText source)
    {
        text = source.Text;
        color = source.TextColor;
        scale = source.Scale;
        autoWrap = source.AutoWrap;
        Messages.Message("SimpleSigh_Copied".Translate(), source.parent, MessageTypeDefOf.SilentInput, historical: false);
    }

    public static void PasteInto(CompSignText target)
    {
        if (!HasContent)
        {
            return;
        }
        CompProperties_SignText props = target.Props;
        if (!SignTextRules.TryNormalize(text, props.maxElements, props.multiline, out string normalized, out string errorKey))
        {
            Messages.Message(SignErrorText.Describe(errorKey, SignTextRules.CountElements(text), props.maxElements),
                target.parent, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        target.Apply(normalized, color, scale, autoWrap);
    }
}
