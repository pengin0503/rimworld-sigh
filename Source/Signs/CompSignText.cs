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
    private const string AutoWrapKey = "simpleSighAutoWrap";
    private const string HAlignKey = "simpleSighHAlign";
    private const string VAlignKey = "simpleSighVAlign";
    private const string ShadowKey = "simpleSighShadow";
    private const string EffectKey = "simpleSighEffect";

    private string text = string.Empty;
    private Color color;
    private float scale = MaxScale;
    private bool autoWrap = true;
    private SignHAlign hAlign = SignHAlign.Center;
    private SignVAlign vAlign = SignVAlign.Middle;
    private bool shadow;
    private SignEffect effect = SignEffect.None;

    private readonly SignRenderCache renderCache = new SignRenderCache();
    private CompPowerTrader powerComp;
    private CompGlower glower;

    public CompProperties_SignText Props => (CompProperties_SignText)props;

    public string Text => text;

    public Color TextColor => color;

    /// <summary>User text size: 1 = largest size that fits the panel, smaller values shrink the text.</summary>
    public float Scale => scale;

    /// <summary>
    /// Single-line signs: show the text on several rows when that makes it clearly larger (#5).
    /// On by default, also for signs from saves made before the option existed.
    /// </summary>
    public bool AutoWrap => autoWrap;

    /// <summary>All display options of this sign.</summary>
    public SignTextStyle Style => new SignTextStyle(scale, autoWrap, hAlign, vAlign, shadow, effect);

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        color = Props.defaultColor;
        shadow = Props.defaultShadow;
    }

    /// <summary>Newly built signs start from the player's defaults (signs from saves keep their own values).</summary>
    public override void PostPostMake()
    {
        base.PostPostMake();
        SimpleSighSettings settings = SimpleSighMod.Settings;
        if (settings.useCustomDefaultColor && !Props.requiresPower)
        {
            color = settings.customDefaultColor;
        }
        autoWrap = settings.defaultAutoWrap;
    }

    /// <summary>Stores already-validated values, keeping the alignment, shadow and effect.</summary>
    public void Apply(string newText, Color newColor, float newScale, bool newAutoWrap)
    {
        Apply(newText, newColor, new SignTextStyle(newScale, newAutoWrap, hAlign, vAlign, shadow, effect));
    }

    /// <summary>Stores already-validated values. Callers must validate text through <see cref="SignEditSession"/>.</summary>
    public void Apply(string newText, Color newColor, SignTextStyle newStyle)
    {
        newText ??= string.Empty;
        newStyle = Sanitize(newStyle);
        if (newText == text && newColor == color && newStyle == Style)
        {
            return;
        }
        bool colorChanged = newColor != color;
        text = newText;
        color = newColor;
        scale = newStyle.Scale;
        autoWrap = newStyle.AutoWrap;
        hAlign = newStyle.HAlign;
        vAlign = newStyle.VAlign;
        shadow = newStyle.Shadow;
        effect = newStyle.Effect;
        if (text.Length == 0)
        {
            // An empty sign is never drawn, so the draw path would not replace the old mesh: free it now.
            renderCache.Release();
        }
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
        Scribe_Values.Look(ref autoWrap, AutoWrapKey, true);
        // Added in v0.2; signs saved earlier load with the v0.1 look (centred, no shadow, static).
        Scribe_Values.Look(ref hAlign, HAlignKey, SignHAlign.Center);
        Scribe_Values.Look(ref vAlign, VAlignKey, SignVAlign.Middle);
        Scribe_Values.Look(ref shadow, ShadowKey, false);
        Scribe_Values.Look(ref effect, EffectKey, SignEffect.None);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            // Saved text is never truncated, even if a later version lowers the limit;
            // the renderer shrinks it to fit instead.
            text = SignTextCodec.Decode(stored);
            SignTextStyle loaded = Sanitize(Style);
            scale = loaded.Scale;
            effect = loaded.Effect;
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

        // With several signs selected, only the clicked gizmo runs: one editor for all selected signs, one copy.
        yield return new Command_Action
        {
            defaultLabel = "SimpleSigh_EditText".Translate(),
            defaultDesc = "SimpleSigh_EditTextDesc".Translate(),
            icon = TexButton.Rename,
            alsoClickIfOtherInGroupClicked = false,
            action = () => Find.WindowStack.Add(new Dialog_EditSign(SelectedPlayerSigns())),
        };
        yield return new Command_Action
        {
            defaultLabel = "SimpleSigh_CopyText".Translate(),
            defaultDesc = "SimpleSigh_CopyTextDesc".Translate(),
            icon = TexButton.Copy,
            alsoClickIfOtherInGroupClicked = false,
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

    /// <summary>This sign first, then every other selected player sign.</summary>
    private List<CompSignText> SelectedPlayerSigns()
    {
        var signs = new List<CompSignText> { this };
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is ThingWithComps thing && thing.Faction == Faction.OfPlayer
                && thing.GetComp<CompSignText>() is CompSignText other && other != this)
            {
                signs.Add(other);
            }
        }
        return signs;
    }

    private SignTextStyle Sanitize(SignTextStyle style)
    {
        float clamped = Mathf.Clamp(style.Scale, MinScale, MaxScale);
        SignEffect allowed = Props.allowEffects ? style.Effect : SignEffect.None;
        return new SignTextStyle(clamped, style.AutoWrap, style.HAlign, style.VAlign, style.Shadow, allowed);
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
