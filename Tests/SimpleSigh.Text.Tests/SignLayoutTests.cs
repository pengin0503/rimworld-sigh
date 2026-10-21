using SimpleSigh.Layout;
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
        SignLayoutResult r = SignLayout.Fit(text, 0.62f, 0.38f, Measure, 0.38f, 0.01f);
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
}
