using System.Collections.Generic;
using RimWorld;
using SimpleSigh.Layout;
using SimpleSigh.Text;
using SimpleSigh.Gui;
using UnityEngine;
using Verse;
using GameText = Verse.Text;

namespace SimpleSigh.Signs;

/// <summary>
/// Editor window for one or more signs. Works on a <see cref="SignEditSession"/> copy; the signs are only
/// changed when OK succeeds. Cancel, Escape or the close button leave every sign untouched. With several
/// signs the strictest limits of all of them apply, so the same text is valid for each.
/// </summary>
public class Dialog_EditSign : Window
{
    private const string TextControlName = "SimpleSighTextField";
    private const float RowGap = 8f;
    private const float RowHeight = 28f;
    private const float ButtonHeight = 35f;
    private const float MultilineBoxHeight = 140f;
    private const float PreviewHeight = 120f;

    /// <summary>
    /// From seven characters on, a 1x1 sign's rows fall to about 0.16 cell or less even with automatic
    /// wrapping (under 8 px at 1080p fully zoomed in; docs/testing/legibility.md), so the editor
    /// suggests the 2x1 notice board.
    /// </summary>
    private const int LongTextHintElements = 6;

    private static readonly List<Color> Palette = new List<Color>
    {
        new Color(0.13f, 0.11f, 0.09f), new Color(0.35f, 0.35f, 0.35f), new Color(0.95f, 0.95f, 0.92f),
        new Color(0.62f, 0.12f, 0.10f), new Color(0.85f, 0.25f, 0.15f), new Color(0.90f, 0.55f, 0.10f),
        new Color(0.85f, 0.75f, 0.15f), new Color(0.20f, 0.45f, 0.18f), new Color(0.15f, 0.55f, 0.50f),
        new Color(0.15f, 0.30f, 0.65f), new Color(0.10f, 0.14f, 0.35f), new Color(0.45f, 0.20f, 0.55f),
        new Color(0.42f, 0.27f, 0.14f),
    };

    private readonly List<CompSignText> signs;
    private readonly SignEditSession session;
    private readonly SignPreviewDrawer preview = new SignPreviewDrawer();
    private Color color;
    private float scale;
    private bool autoWrap;
    private SignHAlign hAlign;
    private SignVAlign vAlign;
    private bool shadow;
    private SignEffect effect;
    private readonly bool effectsAllowed;
    private bool focused;
    private Vector2 textScroll;

    public Dialog_EditSign(CompSignText sign) : this(new List<CompSignText> { sign })
    {
    }

    /// <param name="signs">The clicked sign first; its values fill the editor.</param>
    public Dialog_EditSign(List<CompSignText> signs)
    {
        this.signs = signs;
        var rules = new List<SignEditRules>(signs.Count);
        foreach (CompSignText sign in signs)
        {
            rules.Add(new SignEditRules(sign.Props.maxElements, sign.Props.multiline));
        }
        SignEditRules combined = SignEditRules.Combine(rules);
        // Effects are offered only when every selected sign can show them (comps drop them otherwise).
        effectsAllowed = signs.TrueForAll(s => s.Props.allowEffects);
        CompSignText primary = signs[0];
        session = new SignEditSession(primary.Text, combined.MaxElements, combined.Multiline);
        color = primary.TextColor;
        SignTextStyle style = primary.Style;
        scale = style.Scale;
        autoWrap = style.AutoWrap;
        hAlign = style.HAlign;
        vAlign = style.VAlign;
        shadow = style.Shadow;
        effect = style.Effect;
        doCloseX = true;
        forcePause = true;
        closeOnAccept = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new Vector2(640f, session.Multiline ? 790f : 690f);

    private CompSignText Primary => signs[0];

    private SignTextStyle CurrentStyle => new SignTextStyle(scale, autoWrap, hAlign, vAlign, shadow, effect);

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
        string title = signs.Count == 1
            ? "SimpleSigh_EditTitle".Translate(Primary.parent.LabelCap).ToString()
            : "SimpleSigh_EditTitleMany".Translate(signs.Count).ToString();
        Widgets.Label(new Rect(inRect.x, y, inRect.width, 35f), title);
        y += 35f + RowGap;
        GameText.Font = GameFont.Small;

        y = DrawTextInput(inRect, y);
        y = DrawClipboardRow(new Rect(inRect.x, y, inRect.width, RowHeight));
        DrawStatusRow(new Rect(inRect.x, y, inRect.width, 24f));
        y += 24f + RowGap;

        if (!session.Multiline)
        {
            Widgets.CheckboxLabeled(new Rect(inRect.x, y, inRect.width / 2f - 8f, 24f), "SimpleSigh_AutoWrap".Translate(), ref autoWrap);
            if (session.ElementCount > LongTextHintElements)
            {
                Color previous = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(inRect.x + inRect.width / 2f, y, inRect.width / 2f, 24f), "SimpleSigh_LongTextHint".Translate());
                GUI.color = previous;
            }
            y += 24f + RowGap;
        }

        y = DrawOptionsRow(new Rect(inRect.x, y, inRect.width, 24f)) + RowGap;
        y = DrawAlignmentRow(new Rect(inRect.x, y, inRect.width, RowHeight)) + RowGap;
        y = DrawColorSection(inRect, y) + RowGap;

        string sizeLabel = "SimpleSigh_TextSize".Translate(Mathf.RoundToInt(scale * 100f));
        scale = Widgets.HorizontalSlider(new Rect(inRect.x, y, inRect.width, 30f), scale,
            CompSignText.MinScale, CompSignText.MaxScale, middleAlignment: true, label: sizeLabel, roundTo: 0.05f);
        y += 30f + RowGap;

        Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "SimpleSigh_Preview".Translate());
        y += 24f;
        preview.Draw(new Rect(inRect.x, y, inRect.width, PreviewHeight), Primary, session.Draft, color, CurrentStyle);

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

    /// <summary>Shadow toggle (left half); the right half holds the LED effect selector where allowed.</summary>
    private float DrawOptionsRow(Rect rect)
    {
        Rect left = new Rect(rect.x, rect.y, rect.width / 2f - 8f, rect.height);
        Widgets.CheckboxLabeled(left, "SimpleSigh_Shadow".Translate(), ref shadow);
        TooltipHandler.TipRegion(left, "SimpleSigh_ShadowDesc".Translate());
        if (effectsAllowed)
        {
            const float labelWidth = 70f;
            float x = rect.x + rect.width / 2f;
            Widgets.Label(new Rect(x, rect.y, labelWidth, rect.height), "SimpleSigh_Effect".Translate());
            x += labelWidth;
            float buttonWidth = (rect.xMax - x) / 3f;
            x = AlignButton(ref effect, SignEffect.None, "SimpleSigh_EffectNone", x, rect, buttonWidth);
            x = AlignButton(ref effect, SignEffect.Blink, "SimpleSigh_EffectBlink", x, rect, buttonWidth);
            AlignButton(ref effect, SignEffect.Scroll, "SimpleSigh_EffectScroll", x, rect, buttonWidth);
        }
        return rect.yMax;
    }

    private float DrawTextInput(Rect inRect, float y)
    {
        GUI.SetNextControlName(TextControlName);
        if (session.Multiline)
        {
            var outRect = new Rect(inRect.x, y, inRect.width, MultilineBoxHeight);
            float innerWidth = outRect.width - 16f;
            float innerHeight = Mathf.Max(outRect.height, GameText.CalcHeight(session.Draft, innerWidth) + 12f);
            var viewRect = new Rect(0f, 0f, innerWidth, innerHeight);
            Widgets.BeginScrollView(outRect, ref textScroll, viewRect);
            session.Draft = Widgets.TextArea(viewRect, session.Draft);
            Widgets.EndScrollView();
            y += MultilineBoxHeight;
        }
        else
        {
            session.Draft = Widgets.TextField(new Rect(inRect.x, y, inRect.width, 35f), session.Draft);
            y += 35f;
        }
        if (!focused)
        {
            UI.FocusControl(TextControlName, this);
            focused = true;
        }
        return y + 4f;
    }

    private float DrawClipboardRow(Rect rect)
    {
        float width = (rect.width - 8f) / 2f;
        if (Widgets.ButtonText(new Rect(rect.x, rect.y, width, rect.height), "SimpleSigh_PasteClipboard".Translate()))
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clip))
            {
                Messages.Message("SimpleSigh_ClipboardEmpty".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            }
            else
            {
                // Validation (length, line breaks, forbidden characters) shows in the status row; nothing is cut off.
                session.Draft = clip;
            }
        }
        if (Widgets.ButtonText(new Rect(rect.x + width + 8f, rect.y, width, rect.height), "SimpleSigh_CopyClipboard".Translate()))
        {
            GUIUtility.systemCopyBuffer = session.Draft;
            Messages.Message("SimpleSigh_CopiedToClipboard".Translate(), MessageTypeDefOf.SilentInput, historical: false);
        }
        return rect.yMax + 4f;
    }

    private float DrawAlignmentRow(Rect rect)
    {
        const float labelWidth = 90f;
        Widgets.Label(new Rect(rect.x, rect.y + 2f, labelWidth, rect.height), "SimpleSigh_Alignment".Translate());
        float buttonWidth = (rect.width - labelWidth - 16f) / 6f;
        float x = rect.x + labelWidth;
        x = AlignButton(ref hAlign, SignHAlign.Left, "SimpleSigh_AlignLeft", x, rect, buttonWidth);
        x = AlignButton(ref hAlign, SignHAlign.Center, "SimpleSigh_AlignCenter", x, rect, buttonWidth);
        x = AlignButton(ref hAlign, SignHAlign.Right, "SimpleSigh_AlignRight", x, rect, buttonWidth) + 16f;
        x = AlignButton(ref vAlign, SignVAlign.Top, "SimpleSigh_AlignTop", x, rect, buttonWidth);
        x = AlignButton(ref vAlign, SignVAlign.Middle, "SimpleSigh_AlignMiddle", x, rect, buttonWidth);
        AlignButton(ref vAlign, SignVAlign.Bottom, "SimpleSigh_AlignBottom", x, rect, buttonWidth);
        return rect.yMax;
    }

    private static float AlignButton<T>(ref T current, T value, string labelKey, float x, Rect row, float width)
    {
        var button = new Rect(x, row.y, width - 2f, row.height);
        if (Widgets.ButtonText(button, labelKey.Translate()))
        {
            current = value;
        }
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            Widgets.DrawHighlightSelected(button);
        }
        return x + width;
    }

    private float DrawColorSection(Rect inRect, float y)
    {
        Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "SimpleSigh_TextColor".Translate());
        y += 24f;
        SimpleSighSettings settings = SimpleSighMod.Settings;
        var choices = new List<Color>(Palette);
        choices.AddRange(settings.favoriteColors);
        Widgets.ColorSelector(new Rect(inRect.x, y, inRect.width, 80f), ref color, choices, out float paletteHeight);
        y += paletteHeight + 4f;

        float width = (inRect.width - 16f) / 3f;
        if (Widgets.ButtonText(new Rect(inRect.x, y, width, RowHeight), "SimpleSigh_MoreColors".Translate()))
        {
            Find.WindowStack.Add(new Dialog_SignColorPicker(color, Primary.Props.defaultColor, choices, picked => color = picked));
        }
        bool isFavorite = settings.favoriteColors.Exists(c => Rendering.Rgba.Pack(c) == Rendering.Rgba.Pack(color));
        var favoriteRect = new Rect(inRect.x + width + 8f, y, width, RowHeight);
        if (!isFavorite && Widgets.ButtonText(favoriteRect, "SimpleSigh_AddFavorite".Translate()))
        {
            settings.AddFavorite(color);
            SimpleSighMod.SaveSettings();
        }
        if (isFavorite && Widgets.ButtonText(favoriteRect, "SimpleSigh_RemoveFavorite".Translate()))
        {
            settings.RemoveFavorite(color);
            SimpleSighMod.SaveSettings();
        }
        return y + RowHeight;
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
        SignTextStyle style = CurrentStyle;
        foreach (CompSignText sign in signs)
        {
            // Without the effect selector, each sign keeps its own effect.
            SignTextStyle own = effectsAllowed ? style
                : new SignTextStyle(style.Scale, style.AutoWrap, style.HAlign, style.VAlign, style.Shadow, sign.Style.Effect);
            sign.Apply(normalized, color, own);
        }
        Close();
    }
}
