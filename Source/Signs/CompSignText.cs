using System.Collections.Generic;
using RimWorld;
using SimpleSigh.Layout;
using SimpleSigh.Rendering;
using SimpleSigh.Text;
using UnityEngine;
using Verse;

namespace SimpleSigh.Signs;

/// <summary>
/// Stores one sign's text, colour and size, saves them, offers the edit gizmos and draws the
/// cached text mesh. The render cache keys on the values themselves, so it rebuilds once per change.
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

    private readonly SignRenderCache renderCache = new SignRenderCache();
    private CompPowerTrader powerComp;
    private CompGlower glower;

    public CompProperties_SignText Props => (CompProperties_SignText)props;

    public string Text => text;

    public Color TextColor => color;

    /// <summary>User text size: 1 = largest size that fits the panel, smaller values shrink the text.</summary>
    public float Scale => scale;

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
        bool colorChanged = newColor != color;
        text = newText;
        color = newColor;
        scale = newScale;
        if (colorChanged)
        {
            SyncGlowColor();
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        powerComp = parent.GetComp<CompPowerTrader>();
        glower = parent.GetComp<CompGlower>();
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
        if (!SignVisibility.ShouldDrawText(text, Props.requiresPower, powerComp != null, powerComp != null && powerComp.PowerOn)
            || map == null || parent.Position.Fogged(map) || !SignGlyphSources.TryGetCurrent(out ISignGlyphs glyphs))
        {
            return;
        }
        SignRenderCache.BuiltText built = renderCache.Get(this, glyphs);
        if (built != null)
        {
            SignTextRenderer.Draw(parent, built.Mesh, built.Material, built.LocalCenter);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        renderCache.Release();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        renderCache.Release();
    }

    public override void Notify_MapRemoved()
    {
        base.Notify_MapRemoved();
        renderCache.Release();
    }

    /// <summary>The glower saves its own colour override, so this only runs when the text colour changes.</summary>
    private void SyncGlowColor()
    {
        if (!Props.syncGlowColor)
        {
            return;
        }
        CompGlower target = glower ?? parent.GetComp<CompGlower>();
        if (target != null)
        {
            Color32 c = color;
            // Vanilla glow colours use alpha 0 (e.g. WallLamp glowColor (214,148,94,0)).
            target.GlowColor = new ColorInt(c.r, c.g, c.b, 0);
        }
    }

    public override string CompInspectStringExtra()
    {
        var lines = new List<string>();
        if (text.NullOrEmpty())
        {
            lines.Add("SimpleSigh_NoText".Translate());
        }
        else if (renderCache.MissingGlyphs)
        {
            lines.Add("SimpleSigh_MissingGlyphs".Translate());
        }
        if (Prefs.DevMode)
        {
            RenderCacheStats stats = SignRenderCache.Stats;
            lines.Add($"[dev] Simple Sigh render cache: builds {stats.Builds}, releases {stats.Releases}, live {stats.Live}, registered {SignRenderCache.RegisteredCount}");
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }
}
