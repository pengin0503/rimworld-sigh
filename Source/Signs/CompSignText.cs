using System.Collections.Generic;
using RimWorld;
using SimpleSigh.Layout;
using SimpleSigh.Rendering;
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

    private Mesh textMesh;
    private Material textMaterial;
    private Vector3 textCenter;
    private int builtVersion = -1;
    private int builtAtlasVersion = -1;
    private Rot4 builtRotation = Rot4.Invalid;
    private bool missingGlyphs;

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

    public override void PostDraw()
    {
        base.PostDraw();
        Map map = parent.Map;
        if (text.NullOrEmpty() || map == null || parent.Position.Fogged(map) || !TmpGlyphSource.TryCreate(out TmpGlyphSource glyphs))
        {
            return;
        }
        if (builtVersion != version || builtAtlasVersion != glyphs.AtlasVersion || builtRotation != parent.Rotation)
        {
            ReleaseMesh();
            builtVersion = version;
            builtAtlasVersion = glyphs.AtlasVersion;
            builtRotation = parent.Rotation;
            missingGlyphs = GlyphQuadBuilder.HasMissingGlyphs(text, glyphs);
            SignTextRenderer.TryBuild(this, glyphs, out textMesh, out textMaterial, out textCenter);
        }
        if (textMesh != null)
        {
            SignTextRenderer.Draw(parent, textMesh, textMaterial, textCenter);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        ReleaseMesh();
    }

    public override string CompInspectStringExtra()
    {
        if (text.NullOrEmpty())
        {
            return "SimpleSigh_NoText".Translate().ToString();
        }
        return missingGlyphs ? "SimpleSigh_MissingGlyphs".Translate().ToString() : null;
    }

    private void ReleaseMesh()
    {
        if (textMesh != null)
        {
            Object.Destroy(textMesh);
            textMesh = null;
        }
        textMaterial = null;
        builtVersion = -1;
    }
}
