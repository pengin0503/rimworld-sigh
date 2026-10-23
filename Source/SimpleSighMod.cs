using System.Collections.Generic;
using SimpleSigh.Rendering;
using SimpleSigh.Text;
using UnityEngine;
using Verse;

namespace SimpleSigh;

/// <summary>
/// Created by RimWorld when the mod loads; records the mod folder so the bundled sign font can be found,
/// and owns the player's settings.
/// </summary>
public class SimpleSighMod : Mod
{
    private static SimpleSighMod instance;

    public SimpleSighMod(ModContentPack content) : base(content)
    {
        instance = this;
        SignFont.ModRootDir = content.RootDir;
    }

    /// <summary>The player's settings; defaults when the mod object does not exist (e.g. in tools).</summary>
    public static SimpleSighSettings Settings => instance?.GetSettings<SimpleSighSettings>() ?? FallbackSettings;

    private static readonly SimpleSighSettings FallbackSettings = new SimpleSighSettings();

    public static void SaveSettings()
    {
        instance?.WriteSettings();
    }

    public override string SettingsCategory() => "Simple Sigh";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        SimpleSighSettings settings = Settings;
        var list = new Listing_Standard();
        list.Begin(inRect);

        list.CheckboxLabeled("SimpleSigh_SettingShowText".Translate(), ref settings.showText, "SimpleSigh_SettingShowTextDesc".Translate());
        list.Gap(6f);
        list.Label("SimpleSigh_SettingHideFromZoom".Translate());
        ZoomOption(list, settings, -1, "SimpleSigh_ZoomNever");
        ZoomOption(list, settings, (int)CameraZoomRange.Middle, "SimpleSigh_ZoomMiddle");
        ZoomOption(list, settings, (int)CameraZoomRange.Far, "SimpleSigh_ZoomFar");
        ZoomOption(list, settings, (int)CameraZoomRange.Furthest, "SimpleSigh_ZoomFurthest");
        list.GapLine();

        list.CheckboxLabeled("SimpleSigh_SettingDefaultAutoWrap".Translate(), ref settings.defaultAutoWrap);
        list.CheckboxLabeled("SimpleSigh_SettingUseDefaultColor".Translate(), ref settings.useCustomDefaultColor,
            "SimpleSigh_SettingUseDefaultColorDesc".Translate());
        if (settings.useCustomDefaultColor)
        {
            Rect row = list.GetRect(30f);
            Widgets.DrawBoxSolidWithOutline(new Rect(row.x + 12f, row.y + 3f, 24f, 24f), settings.customDefaultColor, Color.gray);
            if (Widgets.ButtonText(new Rect(row.x + 48f, row.y, 220f, 30f), "SimpleSigh_ChooseDefaultColor".Translate()))
            {
                Find.WindowStack.Add(new Signs.Dialog_SignColorPicker(settings.customDefaultColor, new Color(0.13f, 0.11f, 0.09f),
                    new List<Color>(settings.favoriteColors), picked => settings.customDefaultColor = picked));
            }
        }
        list.GapLine();

        list.Label("SimpleSigh_SettingFavorites".Translate(settings.favoriteColors.Count, FavoriteColorList.MaxCount));
        Rect swatches = list.GetRect(30f);
        Color? remove = null;
        for (int i = 0; i < settings.favoriteColors.Count; i++)
        {
            var box = new Rect(swatches.x + i * 30f, swatches.y + 3f, 24f, 24f);
            Widgets.DrawBoxSolidWithOutline(box, settings.favoriteColors[i], Color.gray);
            TooltipHandler.TipRegion(box, "SimpleSigh_ClickToRemoveFavorite".Translate());
            if (Widgets.ButtonInvisible(box))
            {
                remove = settings.favoriteColors[i];
            }
        }
        if (remove.HasValue)
        {
            settings.RemoveFavorite(remove.Value);
        }
        list.GapLine();
        list.Label("SimpleSigh_SettingHotkeyHint".Translate());
        list.End();
    }

    private static void ZoomOption(Listing_Standard list, SimpleSighSettings settings, int value, string labelKey)
    {
        if (list.RadioButton(labelKey.Translate(), settings.hideTextFromZoom == value, 12f))
        {
            settings.hideTextFromZoom = value;
        }
    }
}
