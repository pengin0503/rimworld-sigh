using System.Collections.Generic;
using System.Linq;
using SimpleSigh.Fonts;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

/// <summary>
/// Issue #5: multi-character labels must not shrink into an unreadable strip. Panels and paddings are
/// the values from Defs/ThingDefs (see SignPanels), glyphs come from the bundled sign font.
/// </summary>
public class SignLegibilityTests
{
    private static readonly GlyphAtlas Font = CreateFont();

    private static GlyphAtlas CreateFont()
    {
        var atlas = new GlyphAtlas(TrueTypeFontTests.SignFont, 48f, 512, 2048);
        atlas.Ensure("AB研究倉庫施設入口食料保管庫立入禁止区域避難所発電室医務FoodStorageRefrigeratorKeep out!0123456789");
        return atlas;
    }

    private static SignTextLayout.Result Auto(string text, SignPanels.Panel p) =>
        SignTextLayout.Build(text, p.Placement, p.Padding, p.Multiline, 1f, Font, autoWrap: true);

    private static SignTextLayout.Result OneRow(string text, SignPanels.Panel p) =>
        SignTextLayout.Build(text, p.Placement, p.Padding, p.Multiline, 1f, Font, autoWrap: false);

    private static int Rows(SignTextLayout.Result r) => r.LineCount;

    public static IEnumerable<object[]> SingleLinePanels() => SignPanels.SingleLine.Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(SingleLinePanels))]
    public void ShortLabelsKeepOneLargeRow(string panelName)
    {
        SignPanels.Panel panel = SignPanels.Get(panelName);
        foreach (string text in new[] { "A", "研究", "倉庫", "AB" })
        {
            SignTextLayout.Result auto = Auto(text, panel);
            Assert.Equal(1, Rows(auto));
            Assert.Equal(OneRow(text, panel).LineHeight, auto.LineHeight, 5);
        }
    }

    [Theory]
    [MemberData(nameof(SingleLinePanels))]
    public void LongerJapaneseLabelsWrapIntoClearlyLargerText(string panelName)
    {
        SignPanels.Panel panel = SignPanels.Get(panelName);
        foreach (string text in new[] { "研究施設入口", "食料保管庫", "立入禁止区域" })
        {
            SignTextLayout.Result auto = Auto(text, panel);
            float oneRow = OneRow(text, panel).LineHeight;
            Assert.True(Rows(auto) >= 2, text);
            Assert.True(auto.LineHeight >= oneRow * 1.3f, $"{text}: {auto.LineHeight} vs one row {oneRow}");
            Assert.Equal(text.Length, auto.Quads.Count);
        }
    }

    [Theory]
    [MemberData(nameof(SingleLinePanels))]
    public void AutomaticWrappingNeverMakesTextSmaller(string panelName)
    {
        SignPanels.Panel panel = SignPanels.Get(panelName);
        string[] samples =
        {
            "A", "研究", "Food Storage", "Refrigerator", "Keep out!", "立入禁止 Keep out!", "研究施設入口",
            "0123456789", new string('研', 64), string.Concat(Enumerable.Repeat("Food ", 12)).Trim(),
        };
        foreach (string text in samples)
        {
            Assert.True(Auto(text, panel).LineHeight >= OneRow(text, panel).LineHeight - 1e-6f, text);
        }
    }

    [Theory]
    [MemberData(nameof(SingleLinePanels))]
    public void LatinWordsAreNeverSplitByAutomaticWrapping(string panelName)
    {
        SignPanels.Panel panel = SignPanels.Get(panelName);
        SignTextLayout.Result refrigerator = Auto("Refrigerator", panel);
        Assert.Equal(1, Rows(refrigerator));
        SignTextLayout.Result food = Auto("Food Storage", panel);
        Assert.Equal(2, Rows(food));
        Assert.Equal(11, food.Quads.Count);
    }

    [Fact]
    public void AutomaticWrappingCanBeTurnedOff()
    {
        SignPanels.Panel panel = SignPanels.Get("BasicSign");
        Assert.Equal(1, Rows(OneRow("研究施設入口", panel)));
    }

    [Theory]
    [MemberData(nameof(SingleLinePanels))]
    public void MaximumLengthTextStillFitsAboveTheMinimumScale(string panelName)
    {
        SignPanels.Panel panel = SignPanels.Get(panelName);
        SignTextLayout.Result r = Auto(new string('研', 64), panel);
        Assert.True(r.Fits);
        Assert.Equal(64, r.Quads.Count);
        // Recorded regression floor (bundled font): 64 kanji on a 1x1 panel stay at >= 0.05 cell line height.
        Assert.True(r.LineHeight >= 0.05f, r.LineHeight.ToString());
    }

    [Theory]
    [InlineData("BasicSign", 6, 0.17f)]
    [InlineData("BasicSign", 4, 0.17f)]
    [InlineData("BasicSign", 12, 0.11f)]
    [InlineData("WallSign", 6, 0.18f)]
    [InlineData("WallSign", 12, 0.11f)]
    public void KanjiLabelsKeepARecordedMinimumLineHeight(string panelName, int count, float minCells)
    {
        // Line heights in map cells recorded with the bundled font; guards against layout regressions.
        SignTextLayout.Result r = Auto(new string('研', count), SignPanels.Get(panelName));
        Assert.True(r.LineHeight >= minCells, $"{count} kanji: {r.LineHeight} cells");
    }

    public static IEnumerable<object[]> AllPanels() => SignPanels.All.Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(AllPanels))]
    public void EveryCharacterIsDrawnInsideThePanelInEveryOrientation(string panelName)
    {
        SignPanels.Panel panel = SignPanels.Get(panelName);
        string text = panel.Multiline
            ? "食料保管庫\n立入禁止区域・避難所 Keep out! 研究施設入口 0123456789 発電室 医務"
            : "研究施設入口 Keep out!";
        SignTextLayout.Result r = Auto(text, panel);
        Assert.True(r.Fits);
        Assert.Equal(text.Count(c => c != ' ' && c != '\n'), r.Quads.Count);
        float halfWidth = panel.Placement.Width / 2 + 1e-4f;
        float halfHeight = panel.Placement.Height / 2 + 1e-4f;
        Assert.All(r.Quads, q =>
        {
            Assert.InRange(q.X0, -halfWidth, halfWidth);
            Assert.InRange(q.X1, -halfWidth, halfWidth);
            Assert.InRange(q.Y0, -halfHeight, halfHeight);
            Assert.InRange(q.Y1, -halfHeight, halfHeight);
        });
    }

    [Fact]
    public void GlyphInkAboveTheLineIsKeptNotDropped()
    {
        // An accent drawn above the line box (negative Y0) on a full-height row: the old clip removed the
        // whole character; it must stay, at most trimmed at the panel edge.
        var glyphs = new FakeGlyphs();
        glyphs.Overhang['É'] = 12f;
        SignPanels.Panel panel = SignPanels.Get("BasicSign");
        SignTextLayout.Result r = SignTextLayout.Build("É", panel.Placement, panel.Padding, false, 1f, glyphs);
        GlyphQuad quad = Assert.Single(r.Quads);
        Assert.True(quad.Y1 <= panel.Placement.Height / 2 + 1e-4f);
        // The padding is only a layout margin: the accent is drawn into it, not cut at the padded edge.
        float paddedTop = panel.Placement.Height / 2 - panel.Padding * System.Math.Min(panel.Placement.Width, panel.Placement.Height);
        Assert.True(quad.Y1 > paddedTop + 1e-3f, $"ink top {quad.Y1} cut at padded edge {paddedTop}");
    }

    [Fact]
    public void MarginalGainKeepsOneRowButAClearGainWraps()
    {
        // Fake glyphs: CJK advance 2/3 of the line height. Two rows are height-limited to scale 0.5.
        var glyphs = new FakeGlyphs();
        var marginal = new PanelPlacement(0f, 0f, 0.6536f, 1f); // one row 0.490 -> two rows 0.500 (+2%)
        var clear = new PanelPlacement(0f, 0f, 0.55f, 1f);      // one row 0.4125 -> two rows 0.500 (+21%)
        Assert.Equal(1, SignTextLayout.Build("研究", marginal, 0f, false, 1f, glyphs).LineCount);
        Assert.Equal(2, SignTextLayout.Build("研究", clear, 0f, false, 1f, glyphs).LineCount);
    }
}
