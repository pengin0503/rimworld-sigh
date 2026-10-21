using UnityEngine;
using Verse;

namespace SimpleSigh.Signs;

/// <summary>
/// Per-building sign settings. Building differences live here (and in the ThingDef),
/// never as defName checks in code.
/// </summary>
public class CompProperties_SignText : CompProperties
{
    /// <summary>Maximum user-perceived characters accepted by the editor.</summary>
    public int maxElements = 64;

    /// <summary>Whether line breaks may be entered.</summary>
    public bool multiline;

    /// <summary>
    /// Display panel inside the graphic for North/South, as (x, y, width, height) fractions
    /// of the graphic's draw size measured from its bottom-left corner.
    /// </summary>
    public Rect panelRect = new Rect(0.15f, 0.4f, 0.7f, 0.45f);

    /// <summary>
    /// Panel for East/West when the graphic uses a different (portrait) texture.
    /// Zero size means "same as <see cref="panelRect"/>".
    /// </summary>
    public Rect panelRectHorizontal;

    /// <summary>Space left between the panel edge and the text, as a fraction of the panel's shorter side.</summary>
    public float panelPadding = 0.08f;

    public Color defaultColor = new Color(0.13f, 0.11f, 0.09f, 1f);

    /// <summary>Electronic signs: text is shown only while the building's CompPowerTrader is powered.</summary>
    public bool requiresPower;

    /// <summary>Draw text after the light overlay so it stays bright in the dark (LED look).</summary>
    public bool glowsInDark;

    /// <summary>Keep the building's CompGlower colour equal to the text colour.</summary>
    public bool syncGlowColor;

    public CompProperties_SignText()
    {
        compClass = typeof(CompSignText);
    }
}
