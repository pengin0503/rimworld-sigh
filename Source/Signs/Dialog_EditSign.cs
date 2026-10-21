using SimpleSigh.Text;
using UnityEngine;
using Verse;

namespace SimpleSigh.Signs;

public sealed class Dialog_EditSign : Window
{
    private readonly CompSignText sign;
    private readonly SignEditSession session;
    private Color draftColor;
    private string error;

    public override Vector2 InitialSize => new(520f, 300f);

    public Dialog_EditSign(CompSignText sign)
    {
        this.sign = sign;
        session = new SignEditSession(sign.Content, sign.SignProps.maxElements, sign.SignProps.multiline);
        draftColor = sign.TextColor;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
    }

    public override void DoWindowContents(Rect rect)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 28), "SimpleSigh_EditText".Translate());
        var input = new Rect(rect.x, rect.y + 35, rect.width, 110);
        session.Draft = sign.SignProps.multiline
            ? Widgets.TextArea(input, session.Draft)
            : Widgets.TextField(input, session.Draft);
        Widgets.Label(new Rect(rect.x, rect.y + 151, rect.width, 25),
            $"{new System.Globalization.StringInfo(session.Draft).LengthInTextElements}/{sign.SignProps.maxElements}");

        // Small set of high-contrast choices; an unrestricted color picker can be added later.
        Color[] colors = { Color.black, Color.white, Color.red, Color.yellow };
        for (int i = 0; i < colors.Length; i++)
        {
            Color old = GUI.color;
            GUI.color = colors[i];
            if (Widgets.ButtonText(new Rect(rect.x + i * 55, rect.y + 179, 48, 27), "■")) draftColor = colors[i];
            GUI.color = old;
        }

        if (error != null) Widgets.Label(new Rect(rect.x + 236, rect.y + 179, rect.width - 236, 25), error.Translate());
        if (Widgets.ButtonText(new Rect(rect.x, rect.yMax - 36, 145, 30), "SimpleSigh_Cancel".Translate()))
        {
            session.Cancel();
            Close();
        }
        if (Widgets.ButtonText(new Rect(rect.xMax - 145, rect.yMax - 36, 145, 30), "SimpleSigh_Save".Translate()))
        {
            if (session.TryConfirm(out var normalized, out error))
            {
                sign.SetText(normalized);
                sign.SetColor(draftColor);
                Close();
            }
        }
    }
}
