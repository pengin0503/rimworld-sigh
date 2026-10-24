using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleSigh.Signs;

/// <summary>
/// RimWorld's own colour picker (wheel, RGB/HSV fields, palette) for any text colour. The chosen colour
/// is handed back to the sign editor; nothing is stored until the editor's OK.
/// </summary>
public class Dialog_SignColorPicker : Dialog_ColorPickerBase
{
    private readonly Action<Color> onAccept;
    private readonly Color defaultColor;
    private readonly List<Color> pickable;

    public Dialog_SignColorPicker(Color current, Color defaultColor, List<Color> pickable, Action<Color> onAccept)
        : base(Widgets.ColorComponents.All, Widgets.ColorComponents.All)
    {
        this.onAccept = onAccept;
        this.defaultColor = defaultColor;
        this.pickable = pickable;
        color = current;
        color.a = 1f;
        oldColor = color;
    }

    protected override bool ShowDarklight => false;

    protected override Color DefaultColor => defaultColor;

    protected override List<Color> PickableColors => pickable;

    /// <summary>Negative: brightness is free (dark text on light panels needs low values).</summary>
    protected override float ForcedColorValue => -1f;

    protected override bool ShowColorTemperatureBar => false;

    protected override void SaveColor(Color picked)
    {
        picked.a = 1f;
        onAccept(picked);
    }
}
