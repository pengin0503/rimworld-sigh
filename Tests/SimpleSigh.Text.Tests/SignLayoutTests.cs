using SimpleSigh.Layout;
using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignLayoutTests
{
    // Width at scale 1 where one line is 1 unit tall: ASCII 0.5, CJK 1.0 (see FakeGlyphs ratios).
    private static float Measure(string s)
    {
        float width = 0f;
        foreach (char c in s)
        {
            width += FakeGlyphs.Advance(c) / 20f;
        }
        return width;
    }

    [Fact]
    public void EmptyTextHasNoLinesAndFits()
    {
        SignLayoutResult r = SignLayout.Fit("", 4f, 1f, Measure, 1f, 0.05f);
        Assert.Empty(r.Lines);
        Assert.True(r.Fits);
    }

    [Fact]
    public void ShortTextUsesFullLineHeight()
    {
        SignLayoutResult r = SignLayout.Fit("研究室", 4f, 1f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "研究室" }, r.Lines);
        Assert.Equal(1f, r.Scale, 4);
        Assert.True(r.Fits);
    }

    [Fact]
    public void WideTextShrinksToPanelWidth()
    {
        SignLayoutResult r = SignLayout.Fit("発電設備 24h", 3f, 1f, Measure, 1f, 0.05f);
        // width at scale 1: 4 CJK (4.0) + space and "24h" (2.0) = 6.0
        Assert.Equal(0.5f, r.Scale, 4);
        Assert.True(r.Fits);
        Assert.True(Measure(r.Lines[0]) * r.Scale <= 3f + 1e-4f);
    }

    [Fact]
    public void SixtyFourCharacterLineStillFitsAboveMinimumScale()
    {
        string text = new string('W', 64);
        SignLayoutResult r = SignLayout.Fit(text, 0.62f, 0.38f, Measure, 0.38f, 0.01f, wrap: false);
        Assert.True(r.Fits);
        Assert.True(Measure(text) * r.Scale <= 0.62f + 1e-4f);
    }

    [Fact]
    public void BelowMinimumScaleReportsNotFitting()
    {
        SignLayoutResult r = SignLayout.Fit(new string('W', 64), 0.62f, 0.38f, Measure, 0.38f, 0.5f);
        Assert.False(r.Fits);
        Assert.Equal(0.5f, r.Scale, 4);
    }

    [Fact]
    public void ZeroSizedPanelDoesNotThrowAndDoesNotFit()
    {
        SignLayoutResult r = SignLayout.Fit("a", 0f, 0f, Measure, 1f, 0.05f);
        Assert.False(r.Fits);
    }

    // ---- M2: multi-line, wrapping, kinsoku ----

    private static float TotalHeight(SignLayoutResult r, float lineHeight) => r.Lines.Count * lineHeight * r.Scale;

    private static void AssertInside(SignLayoutResult r, float width, float height, float lineHeight)
    {
        Assert.True(r.Fits);
        Assert.True(TotalHeight(r, lineHeight) <= height + 1e-4f, $"height {TotalHeight(r, lineHeight)} > {height}");
        foreach (string line in r.Lines)
        {
            Assert.True(Measure(line) * r.Scale <= width + 1e-4f, $"'{line}' too wide");
        }
    }

    [Fact]
    public void SingleLineSignsDoNotWrap()
    {
        SignLayoutResult r = SignLayout.Fit("あいうえおかきくけこさし", 6f, 2f, Measure, 1f, 0.05f, wrap: false);
        Assert.Equal(new[] { "あいうえおかきくけこさし" }, r.Lines);
        Assert.Equal(0.5f, r.Scale, 4);
    }

    [Fact]
    public void ExplicitNewlinesArePreservedAsLines()
    {
        SignLayoutResult r = SignLayout.Fit("倉庫\n備品", 8f, 2f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "倉庫", "備品" }, r.Lines);
        Assert.Equal(1f, r.Scale, 4);
    }

    [Fact]
    public void EmptyLineBetweenParagraphsIsKept()
    {
        SignLayoutResult r = SignLayout.Fit("A\n\nB", 8f, 3f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "A", "", "B" }, r.Lines);
    }

    [Fact]
    public void LongJapaneseWithoutSpacesWrapsBetweenCharacters()
    {
        // 12 CJK chars = 12 units wide at scale 1 on a 6x2 panel: two lines of 6 at scale 1.
        SignLayoutResult r = SignLayout.Fit("あいうえおかきくけこさし", 6f, 2f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "あいうえおか", "きくけこさし" }, r.Lines);
        Assert.Equal(1f, r.Scale, 4);
    }

    [Fact]
    public void LatinWrapsAtSpacesAndDropsTheBreakingSpace()
    {
        // "Wood storage area" = 17 chars * 0.5 = 8.5 units; panel 5 wide, 2 tall.
        SignLayoutResult r = SignLayout.Fit("Wood storage area", 5f, 2f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "Wood storage", "area" }, r.Lines);
        AssertInside(r, 5f, 2f, 1f);
    }

    [Fact]
    public void LongLatinWordBreaksByCharacterWhenNoSpaceFits()
    {
        SignLayoutResult r = SignLayout.Fit("ABCDEFGHIJKLMNOP", 4f, 2f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "ABCDEFGH", "IJKLMNOP" }, r.Lines);
    }

    [Fact]
    public void MixedJapaneseAndAsciiUseRealWidths()
    {
        // 発電設備 (4.0) + " 24h" (2.0) = 6.0 units; panel 4 wide, 2 tall -> wraps at the space.
        SignLayoutResult r = SignLayout.Fit("発電設備 24h", 4f, 2f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "発電設備", "24h" }, r.Lines);
        Assert.Equal(1f, r.Scale, 4);
    }

    [Fact]
    public void ClosingPunctuationNeverStartsALine()
    {
        // Naive break after 6 chars would start line 2 with "。"; kinsoku pushes "か" down with it.
        SignLayoutResult r = SignLayout.Fit("あいうえおか。きくけこ", 6f, 2f, Measure, 1f, 0.05f);
        Assert.Equal(new[] { "あいうえお", "か。きくけこ" }, r.Lines);
        foreach (string line in r.Lines)
        {
            Assert.False(line.Length > 0 && "、。，．）」』】！？ー".IndexOf(line[0]) >= 0, line);
        }
    }

    [Fact]
    public void OpeningBracketNeverEndsALine()
    {
        SignLayoutResult r = SignLayout.Fit("あいうえお「かきくけ」", 6f, 2f, Measure, 1f, 0.05f);
        foreach (string line in r.Lines)
        {
            Assert.False(line.Length > 0 && "（「『【".IndexOf(line[line.Length - 1]) >= 0, line);
        }
        Assert.Equal("あいうえお「かきくけ」", string.Concat(r.Lines));
    }

    [Fact]
    public void ScaleShrinksUntilWrappedTextFitsHeight()
    {
        string text = new string('あ', 40);
        SignLayoutResult r = SignLayout.Fit(text, 6f, 2f, Measure, 1f, 0.05f);
        AssertInside(r, 6f, 2f, 1f);
        Assert.True(r.Scale < 1f);
        Assert.Equal(text, string.Concat(r.Lines));
    }

    [Fact]
    public void LayoutUsesTheLargestScaleThatFits()
    {
        string text = new string('あ', 40);
        SignLayoutResult r = SignLayout.Fit(text, 6f, 2f, Measure, 1f, 0.05f);
        SignLayoutResult bigger = SignLayout.FitAtScale(text, 6f, Measure, r.Scale * 1.05f);
        Assert.True(bigger.Lines.Count * 1f * r.Scale * 1.05f > 2f + 1e-4f, "a 5% larger scale would also fit");
    }

    [Fact]
    public void MaxScaleCapsTheFitBeforeWrapping()
    {
        SignLayoutResult r = SignLayout.Fit("研究室", 8f, 2f, Measure, 1f, 0.05f, maxScale: 0.5f);
        Assert.Equal(0.5f, r.Scale, 4);
        Assert.Equal(new[] { "研究室" }, r.Lines);
    }

    [Fact]
    public void TwoFiftySixCharacterNoticeFitsAPortraitPanel()
    {
        string text = string.Concat(System.Linq.Enumerable.Repeat("掲示板のお知らせ。", 29)).Substring(0, 256);
        SignLayoutResult r = SignLayout.Fit(text, 0.62f, 1.4f, Measure, 1.4f, 0.01f);
        AssertInside(r, 0.62f, 1.4f, 1.4f);
        Assert.Equal(text, string.Concat(r.Lines));
    }

    [Fact]
    public void TooSmallPanelReportsNotFittingAtMinScale()
    {
        SignLayoutResult r = SignLayout.Fit(new string('あ', 256), 0.1f, 0.1f, Measure, 0.1f, 0.5f);
        Assert.False(r.Fits);
        Assert.Equal(0.5f, r.Scale, 4);
    }

    [Fact]
    public void BasicSignLimitsStillHold()
    {
        Assert.True(SignTextRules.TryNormalize(new string('A', 64), 64, false, out _, out _));
        Assert.False(SignTextRules.TryNormalize(new string('A', 65), 64, false, out _, out _));
        Assert.True(SignTextRules.TryNormalize(new string('あ', 256), 256, true, out _, out _));
        Assert.False(SignTextRules.TryNormalize(new string('あ', 257), 256, true, out _, out _));
    }
}
