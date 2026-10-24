using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

/// <summary>The optional text shadow: a contrasting colour drawn slightly offset behind the text.</summary>
public class SignShadowTests
{
    private static float Luminance(uint rgba)
    {
        static float Channel(uint v)
        {
            float c = v / 255f;
            return c <= 0.03928f ? c / 12.92f : (float)System.Math.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
        return 0.2126f * Channel(rgba >> 24) + 0.7152f * Channel((rgba >> 16) & 0xFF) + 0.0722f * Channel((rgba >> 8) & 0xFF);
    }

    private static float Contrast(uint a, uint b)
    {
        float la = Luminance(a);
        float lb = Luminance(b);
        return (System.Math.Max(la, lb) + 0.05f) / (System.Math.Min(la, lb) + 0.05f);
    }

    [Fact]
    public void LightTextGetsADarkShadow()
    {
        uint shadow = SignShadow.ColorFor(0xFFFFFFFF);
        Assert.True(Luminance(shadow) < 0.05f);
        Assert.InRange(shadow & 0xFF, 128u, 220u);
    }

    [Fact]
    public void DarkTextGetsALightShadow()
    {
        uint shadow = SignShadow.ColorFor(0x211C17FF);
        Assert.True(Luminance(shadow) > 0.8f);
        Assert.InRange(shadow & 0xFF, 96u, 220u);
    }

    [Theory]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(0x000000FFu)]
    [InlineData(0xFFAB3DFFu)] // LED amber
    [InlineData(0x9E1F1AFFu)] // dark red
    [InlineData(0x3A7BD5FFu)] // mid blue
    [InlineData(0x808080FFu)] // mid grey
    [InlineData(0x46DC5AFFu)] // green
    public void ShadowContrastsWithEveryTextColour(uint text)
    {
        Assert.True(Contrast(text, SignShadow.ColorFor(text)) >= 3f, $"{text:X8}");
    }

    [Fact]
    public void ShadowKeepsTheTextOpacityOutOfItsOwnAlpha()
    {
        Assert.Equal(SignShadow.ColorFor(0xFFFFFFFF) & 0xFF, SignShadow.ColorFor(0xFFFFFF10) & 0xFF);
    }

    [Fact]
    public void OffsetFollowsTheRowHeightWithinLimits()
    {
        Assert.Equal(0.3f * SignShadow.OffsetPerLineHeight, SignShadow.Offset(0.3f), 5);
        Assert.Equal(SignShadow.MinOffset, SignShadow.Offset(0.001f));
        Assert.Equal(SignShadow.MaxOffset, SignShadow.Offset(5f));
        Assert.True(SignShadow.Offset(0.2f) < SignShadow.Offset(0.4f));
    }
}
