using System.Collections.Generic;
using SimpleSigh.Rendering;
using SimpleSigh.Text;
using UnityEngine;
using Verse;

namespace SimpleSigh;

/// <summary>Player options, stored by RimWorld in the mod's settings file (not in saves).</summary>
public class SimpleSighSettings : ModSettings
{
    /// <summary>Global switch for all sign text (also toggled by the SimpleSigh_ToggleSignText key).</summary>
    public bool showText = true;

    /// <summary>Hide sign text from this camera zoom range on (CameraZoomRange index); -1 = never hide.</summary>
    public int hideTextFromZoom = -1;

    /// <summary>New signs start with <see cref="customDefaultColor"/> instead of their def's colour.</summary>
    public bool useCustomDefaultColor;

    public Color customDefaultColor = new Color(0.13f, 0.11f, 0.09f, 1f);

    /// <summary>Automatic wrapping for new single-line signs.</summary>
    public bool defaultAutoWrap = true;

    /// <summary>Colours saved from the sign editor, most recent first.</summary>
    public List<Color> favoriteColors = new List<Color>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref showText, "showText", true);
        Scribe_Values.Look(ref hideTextFromZoom, "hideTextFromZoom", -1);
        Scribe_Values.Look(ref useCustomDefaultColor, "useCustomDefaultColor");
        Scribe_Values.Look(ref customDefaultColor, "customDefaultColor", new Color(0.13f, 0.11f, 0.09f, 1f));
        Scribe_Values.Look(ref defaultAutoWrap, "defaultAutoWrap", true);
        Scribe_Collections.Look(ref favoriteColors, "favoriteColors", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            favoriteColors ??= new List<Color>();
        }
    }

    public void AddFavorite(Color color)
    {
        List<uint> packed = Pack(favoriteColors);
        FavoriteColorList.Add(packed, Rgba.Pack(color));
        favoriteColors = Unpack(packed);
    }

    public void RemoveFavorite(Color color)
    {
        List<uint> packed = Pack(favoriteColors);
        FavoriteColorList.Remove(packed, Rgba.Pack(color));
        favoriteColors = Unpack(packed);
    }

    private static List<uint> Pack(List<Color> colors)
    {
        var packed = new List<uint>(colors.Count);
        foreach (Color color in colors)
        {
            packed.Add(Rgba.Pack(color));
        }
        return packed;
    }

    private static List<Color> Unpack(List<uint> packed)
    {
        var colors = new List<Color>(packed.Count);
        foreach (uint rgba in packed)
        {
            colors.Add(Rgba.Unpack(rgba));
        }
        return colors;
    }
}
