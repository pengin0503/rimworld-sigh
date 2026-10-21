using System.Collections.Generic;
using RimWorld;
using SimpleSigh.Text;
using UnityEngine;
using Verse;
using GameText = Verse.Text;

namespace SimpleSigh.Signs;

/// <summary>
/// Editor window. Works on a <see cref="SignEditSession"/> copy; the sign is only changed
/// when OK succeeds. Cancel, Escape or clicking the close button leave the sign untouched.
/// </summary>
public class Dialog_EditSign : Window
{
    private const string TextControlName = "SimpleSighTextField";
    private const float RowGap = 8f;
    private const float ButtonHeight = 35f;

    private static readonly List<Color> Palette = new List<Color>
    {
        new Color(0.13f, 0.11f, 0.09f), new Color(0.35f, 0.35f, 0.35f), new Color(0.95f, 0.95f, 0.92f),
        new Color(0.62f, 0.12f, 0.10f), new Color(0.85f, 0.25f, 0.15f), new Color(0.90f, 0.55f, 0.10f),
        new Color(0.85f, 0.75f, 0.15f), new Color(0.20f, 0.45f, 0.18f), new Color(0.15f, 0.55f, 0.50f),
        new Color(0.15f, 0.30f, 0.65f), new Color(0.10f, 0.14f, 0.35f), new Color(0.45f, 0.20f, 0.55f),
        new Color(0.42f, 0.27f, 0.14f),
    };

    private readonly CompSignText sign;
    private readonly SignEditSession session;
    private Color color;
    private float scale;
    private bool focused;

    public Dialog_EditSign(CompSignText sign)
    {
        this.sign = sign;
        CompProperties_SignText props = sign.Props;
        session = new SignEditSession(sign.Text, props.maxElements, props.multiline);
        color = sign.TextColor;
        scale = sign.Scale;
        doCloseX = true;
        forcePause = true;
        closeOnAccept = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new Vector2(520f, session.Multiline ? 500f : 380f);

    public override void DoWindowContents(Rect inRect)
    {
        bool submit = false;
        if (!session.Multiline && Event.current.type == EventType.KeyDown
            && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            submit = true;
            Event.current.Use();
        }

        float y = inRect.y;
        GameText.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, y, inRect.width, 35f), "SimpleSigh_EditTitle".Translate(sign.parent.LabelCap));
        y += 35f + RowGap;

        GameText.Font = GameFont.Small;
        float inputHeight = session.Multiline ? 150f : 35f;
        Rect inputRect = new Rect(inRect.x, y, inRect.width, inputHeight);
        GUI.SetNextControlName(TextControlName);
        session.Draft = session.Multiline ? Widgets.TextArea(inputRect, session.Draft) : Widgets.TextField(inputRect, session.Draft);
        if (!focused)
        {
            UI.FocusControl(TextControlName, this);
            focused = true;
        }
        y += inputHeight + 4f;

        DrawStatusRow(new Rect(inRect.x, y, inRect.width, 24f));
        y += 24f + RowGap;

        Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "SimpleSigh_TextColor".Translate());
        y += 24f;
        Widgets.ColorSelector(new Rect(inRect.x, y, inRect.width, 80f), ref color, Palette, out float paletteHeight);
        y += paletteHeight + RowGap;

        string sizeLabel = "SimpleSigh_TextSize".Translate(Mathf.RoundToInt(scale * 100f));
        scale = Widgets.HorizontalSlider(new Rect(inRect.x, y, inRect.width, 30f), scale,
            CompSignText.MinScale, CompSignText.MaxScale, middleAlignment: true, label: sizeLabel, roundTo: 0.05f);

        Rect buttons = new Rect(inRect.x, inRect.yMax - ButtonHeight, inRect.width, ButtonHeight);
        if (Widgets.ButtonText(buttons.LeftHalf().ContractedBy(4f, 0f), "CancelButton".Translate()))
        {
            session.Cancel();
            Close();
        }
        if (Widgets.ButtonText(buttons.RightHalf().ContractedBy(4f, 0f), "OK".Translate()) || submit)
        {
            TryConfirm();
        }
    }

    public override void OnAcceptKeyPressed()
    {
        // Enter inserts a line break in multiline editors; single-line editors handle it in DoWindowContents.
    }

    private void DrawStatusRow(Rect rect)
    {
        int count = session.ElementCount;
        bool valid = session.TryConfirm(out _, out string errorKey);
        Color previous = GUI.color;

        GameText.Anchor = TextAnchor.MiddleRight;
        GUI.color = count > session.MaxElements ? ColorLibrary.RedReadable : Color.gray;
        Widgets.Label(rect, $"{count} / {session.MaxElements}");

        if (!valid)
        {
            GameText.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ColorLibrary.RedReadable;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 90f, rect.height),
                SignErrorText.Describe(errorKey, count, session.MaxElements));
        }
        GameText.Anchor = TextAnchor.UpperLeft;
        GUI.color = previous;
    }

    private void TryConfirm()
    {
        if (!session.TryConfirm(out string normalized, out string errorKey))
        {
            Messages.Message(SignErrorText.Describe(errorKey, session.ElementCount, session.MaxElements),
                MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        sign.Apply(normalized, color, scale);
        Close();
    }
}
