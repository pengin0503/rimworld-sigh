using System.Collections.Generic;
using RimWorld;
using SimpleSigh.Signs;
using SimpleSigh.Text;
using UnityEngine;
using Verse;
using GameText = Verse.Text;

namespace SimpleSigh.Gui;

/// <summary>
/// Bottom-bar tab listing the player's signs on the current map: search their text (case, width and
/// kana insensitive), jump to a sign or open its editor.
/// </summary>
public class MainTabWindow_Signs : MainTabWindow
{
    private const float RowHeight = 34f;
    private const float ButtonWidth = 80f;
    private const int RefreshIntervalFrames = 60;

    private readonly List<CompSignText> signs = new List<CompSignText>();
    private readonly List<CompSignText> shown = new List<CompSignText>();
    private string query = string.Empty;
    private Vector2 scroll;
    private int refreshedFrame = -1;

    public override Vector2 RequestedTabSize => new Vector2(760f, 520f);

    public override void PreOpen()
    {
        base.PreOpen();
        Refresh();
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Signs can be built or removed while the tab is open; a cheap periodic refresh keeps the list current.
        if (Time.frameCount - refreshedFrame >= RefreshIntervalFrames)
        {
            Refresh();
        }

        GameText.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "SimpleSigh_SignListTitle".Translate(signs.Count));
        GameText.Font = GameFont.Small;
        float y = inRect.y + 40f;

        Widgets.Label(new Rect(inRect.x, y + 4f, 70f, 28f), "SimpleSigh_Search".Translate());
        query = Widgets.TextField(new Rect(inRect.x + 74f, y, inRect.width - 74f, 28f), query);
        y += 36f;

        shown.Clear();
        foreach (CompSignText sign in signs)
        {
            // A sign deconstructed or minified since the last refresh must not be jumped to or edited.
            if (!sign.parent.Spawned)
            {
                continue;
            }
            if (SignSearch.Matches(sign.Text, query) || SignSearch.Matches(sign.parent.LabelCap, query))
            {
                shown.Add(sign);
            }
        }

        var outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, shown.Count * RowHeight));
        Widgets.BeginScrollView(outRect, ref scroll, viewRect);
        for (int i = 0; i < shown.Count; i++)
        {
            DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), shown[i], i);
        }
        if (shown.Count == 0)
        {
            GameText.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 0f, viewRect.width, 60f), "SimpleSigh_NoSigns".Translate());
            GameText.Anchor = TextAnchor.UpperLeft;
        }
        Widgets.EndScrollView();
    }

    private void DrawRow(Rect row, CompSignText sign, int index)
    {
        if (index % 2 == 1)
        {
            Widgets.DrawLightHighlight(row);
        }
        Thing thing = sign.parent;
        Widgets.ThingIcon(new Rect(row.x + 4f, row.y + 3f, 28f, 28f), thing);
        GameText.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(row.x + 40f, row.y, 200f, row.height), thing.LabelCap.Truncate(196f));
        string text = sign.Text.NullOrEmpty() ? "SimpleSigh_NoTextShort".Translate().ToString() : SignSearch.FirstLine(sign.Text);
        float textWidth = row.width - 40f - 200f - ButtonWidth * 2f - 16f;
        Widgets.Label(new Rect(row.x + 244f, row.y, textWidth, row.height), text.Truncate(textWidth));
        GameText.Anchor = TextAnchor.UpperLeft;

        float x = row.xMax - ButtonWidth * 2f - 4f;
        if (Widgets.ButtonText(new Rect(x, row.y + 3f, ButtonWidth - 4f, row.height - 6f), "SimpleSigh_JumpToSign".Translate()))
        {
            CameraJumper.TryJumpAndSelect(thing);
            Find.MainTabsRoot.EscapeCurrentTab();
        }
        if (Widgets.ButtonText(new Rect(x + ButtonWidth, row.y + 3f, ButtonWidth - 4f, row.height - 6f), "SimpleSigh_EditText".Translate()))
        {
            Find.WindowStack.Add(new Dialog_EditSign(sign));
        }
    }

    private void Refresh()
    {
        refreshedFrame = Time.frameCount;
        signs.Clear();
        Map map = Find.CurrentMap;
        if (map == null)
        {
            return;
        }
        foreach (Building building in map.listerBuildings.allBuildingsColonist)
        {
            CompSignText sign = building.GetComp<CompSignText>();
            if (sign != null)
            {
                signs.Add(sign);
            }
        }
        signs.Sort((a, b) =>
        {
            int byLabel = string.Compare(a.parent.def.label, b.parent.def.label, System.StringComparison.CurrentCulture);
            return byLabel != 0 ? byLabel : string.Compare(a.Text, b.Text, System.StringComparison.CurrentCulture);
        });
    }
}
