using System.Collections.Generic;
using RimWorld;
using SimpleSigh.Text;
using UnityEngine;
using Verse;

namespace SimpleSigh.Signs;

/// <summary>
/// Stores one sign's text, colour and size, saves them, and offers the edit gizmos.
/// Every visible change bumps <see cref="Version"/> so render caches can rebuild once.
/// </summary>
public class CompSignText : ThingComp
{
    public const float MinScale = 0.3f;
    public const float MaxScale = 1f;

    private const string TextKey = "simpleSighText";
    private const string ColorKey = "simpleSighColor";
    private const string ScaleKey = "simpleSighScale";

    private string text = string.Empty;
    private Color color;
    private float scale = MaxScale;
    private int version;

    public CompProperties_SignText Props => (CompProperties_SignText)props;

    public string Text => text;

    public Color TextColor => color;

    /// <summary>User text size: 1 = largest size that fits the panel, smaller values shrink the text.</summary>
    public float Scale => scale;

    public int Version => version;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        color = Props.defaultColor;
    }

    /// <summary>Stores already-validated values. Callers must validate text through <see cref="SignEditSession"/>.</summary>
    public void Apply(string newText, Color newColor, float newScale)
    {
        newText ??= string.Empty;
        newScale = Mathf.Clamp(newScale, MinScale, MaxScale);
        if (newText == text && newColor == color && Mathf.Approximately(newScale, scale))
        {
            return;
        }
        text = newText;
        color = newColor;
        scale = newScale;
        version++;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        string stored = Scribe.mode == LoadSaveMode.Saving ? SignTextCodec.Encode(text) : null;
        Scribe_Values.Look(ref stored, TextKey);
        Scribe_Values.Look(ref color, ColorKey, Props.defaultColor);
        Scribe_Values.Look(ref scale, ScaleKey, MaxScale);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            // Saved text is never truncated, even if a later version lowers the limit;
            // the renderer shrinks it to fit instead.
            text = SignTextCodec.Decode(stored);
            scale = Mathf.Clamp(scale, MinScale, MaxScale);
            version++;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "SimpleSigh_EditText".Translate(),
            defaultDesc = "SimpleSigh_EditTextDesc".Translate(),
            icon = TexButton.Rename,
            action = () => Find.WindowStack.Add(new Dialog_EditSign(this)),
        };
        yield return new Command_Action
        {
            defaultLabel = "SimpleSigh_CopyText".Translate(),
            defaultDesc = "SimpleSigh_CopyTextDesc".Translate(),
            icon = TexButton.Copy,
            action = () => SignClipboard.Copy(this),
        };
        if (SignClipboard.HasContent)
        {
            yield return new Command_Action
            {
                defaultLabel = "SimpleSigh_PasteText".Translate(),
                defaultDesc = "SimpleSigh_PasteTextDesc".Translate(),
                icon = TexButton.Paste,
                action = () => SignClipboard.PasteInto(this),
            };
        }
    }

    public override string CompInspectStringExtra()
    {
        return text.NullOrEmpty() ? "SimpleSigh_NoText".Translate().ToString() : null;
    }
}
