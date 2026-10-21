using System.Linq;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignTextLayoutTests
{
    private readonly FakeGlyphs glyphs = new FakeGlyphs();
    private static readonly PanelPlacement Panel = new PanelPlacement(0.1f, 0.12f, 0.72f, 0.44f);

    private static void AssertInsidePadded(SignTextLayout.Result result, PanelPlacement panel, float padding)
    {
        float pad = padding * System.Math.Min(panel.Width, panel.Height);
        foreach (GlyphQuad q in result.Quads)
        {
            Assert.InRange(q.X0, -panel.Width / 2 + pad - 1e-4f, panel.Width / 2 - pad + 1e-4f);
            Assert.InRange(q.X1, -panel.Width / 2 + pad - 1e-4f, panel.Width / 2 - pad + 1e-4f);
            Assert.InRange(q.Y0, -panel.Height / 2 + pad - 1e-4f, panel.Height / 2 - pad + 1e-4f);
            Assert.InRange(q.Y1, -panel.Height / 2 + pad - 1e-4f, panel.Height / 2 - pad + 1e-4f);
        }
    }

    [Fact]
    public void EmptyTextBuildsNothing()
    {
        SignTextLayout.Result r = SignTextLayout.Build("", Panel, 0.08f, multiline: false, userScale: 1f, glyphs);
        Assert.Empty(r.Quads);
    }

    [Fact]
    public void QuadsAreRelativeToPanelCentreAndInsidePaddedPanel()
    {
        SignTextLayout.Result r = SignTextLayout.Build("発電設備 24h", Panel, 0.08f, false, 1f, glyphs);
        Assert.NotEmpty(r.Quads);
        Assert.Equal(0.1f, r.CenterX, 5);
        Assert.Equal(0.12f, r.CenterZ, 5);
        AssertInsidePadded(r, Panel, 0.08f);
    }

    [Fact]
    public void TextIsHorizontallyCentred()
    {
        SignTextLayout.Result r = SignTextLayout.Build("AAAA", Panel, 0.08f, false, 1f, glyphs);
        float left = r.Quads.Min(q => q.X0), right = r.Quads.Max(q => q.X1);
        Assert.Equal(-left, right, 4);
    }

    [Fact]
    public void UserScaleShrinksText()
    {
        float full = SignTextLayout.Build("研究室", Panel, 0.08f, false, 1f, glyphs).LineHeight;
        float half = SignTextLayout.Build("研究室", Panel, 0.08f, false, 0.5f, glyphs).LineHeight;
        Assert.Equal(full * 0.5f, half, 4);
    }

    [Fact]
    public void MultilineNoticeStaysInsidePortraitPanel()
    {
        var portrait = new PanelPlacement(0f, 0.08f, 0.72f, 1.52f);
        string text = string.Concat(Enumerable.Repeat("お知らせ。", 40)).Substring(0, 200);
        SignTextLayout.Result r = SignTextLayout.Build(text, portrait, 0.06f, multiline: true, userScale: 1f, glyphs);
        Assert.True(r.Fits);
        Assert.Equal(200, r.Quads.Count);
        AssertInsidePadded(r, portrait, 0.06f);
    }

    [Fact]
    public void PanelWithoutRoomBuildsNothing()
    {
        SignTextLayout.Result r = SignTextLayout.Build("A", new PanelPlacement(0f, 0f, 0.01f, 0.01f), 0.5f, false, 1f, glyphs);
        Assert.Empty(r.Quads);
    }
}
