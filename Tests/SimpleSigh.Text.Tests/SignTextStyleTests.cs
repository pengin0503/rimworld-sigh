using System.Linq;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignTextStyleTests
{
    private readonly FakeGlyphs glyphs = new FakeGlyphs();
    private static readonly PanelPlacement Panel = new PanelPlacement(0.1f, 0.12f, 0.72f, 0.44f);
    private const float Padding = 0.08f;
    private static readonly float Pad = Padding * 0.44f;
    private static readonly float InnerHalfWidth = 0.72f / 2f - Pad;
    private static readonly float InnerHalfHeight = 0.44f / 2f - Pad;

    private static SignTextStyle Style(SignHAlign h = SignHAlign.Center, SignVAlign v = SignVAlign.Middle, float scale = 1f) =>
        new SignTextStyle(scale, autoWrap: true, h, v, shadow: false, SignEffect.None);

    private float K(SignTextLayout.Result r) => r.LineHeight / glyphs.LineHeight;

    [Fact]
    public void DefaultIsCentredUnshadowedStaticFullSize()
    {
        SignTextStyle d = SignTextStyle.Default;
        Assert.Equal(1f, d.Scale);
        Assert.True(d.AutoWrap);
        Assert.Equal(SignHAlign.Center, d.HAlign);
        Assert.Equal(SignVAlign.Middle, d.VAlign);
        Assert.False(d.Shadow);
        Assert.Equal(SignEffect.None, d.Effect);
        Assert.Equal(SignTextStyle.Default, Style());
    }

    [Theory]
    [InlineData("研究室", false)]
    [InlineData("研究施設入口", false)]
    [InlineData("本日の作業予定：\n午前は畑の収穫", true)]
    public void DefaultsMatchV01(string text, bool multiline)
    {
        SignTextLayout.Result old = SignTextLayout.Build(text, Panel, Padding, multiline, 1f, glyphs, autoWrap: true);
        SignTextLayout.Result styled = SignTextLayout.Build(text, Panel, Padding, multiline, SignTextStyle.Default, glyphs);
        Assert.Equal(old.Quads, styled.Quads);
        Assert.Equal(old.LineHeight, styled.LineHeight);
    }

    [Fact]
    public void LeftAlignStartsAtInnerLeftEdge()
    {
        SignTextLayout.Result r = SignTextLayout.Build("AA", Panel, Padding, false, Style(SignHAlign.Left), glyphs);
        Assert.Equal(-InnerHalfWidth + 1f * K(r), r.Quads.Min(q => q.X0), 4);
    }

    [Fact]
    public void RightAlignEndsAtInnerRightEdge()
    {
        SignTextLayout.Result r = SignTextLayout.Build("AA", Panel, Padding, false, Style(SignHAlign.Right), glyphs);
        Assert.Equal(InnerHalfWidth - 1f * K(r), r.Quads.Max(q => q.X1), 4);
    }

    [Fact]
    public void TopAlignTouchesInnerTop()
    {
        SignTextLayout.Result r = SignTextLayout.Build("A", Panel, Padding, false, Style(v: SignVAlign.Top, scale: 0.5f), glyphs);
        Assert.Equal(InnerHalfHeight - 4f * K(r), r.Quads.Max(q => q.Y1), 4);
    }

    [Fact]
    public void BottomAlignTouchesInnerBottom()
    {
        SignTextLayout.Result r = SignTextLayout.Build("A", Panel, Padding, false, Style(v: SignVAlign.Bottom, scale: 0.5f), glyphs);
        Assert.Equal(-InnerHalfHeight + 4f * K(r), r.Quads.Min(q => q.Y0), 4);
    }

    [Fact]
    public void AlignmentKeepsAllGlyphsInside()
    {
        var portrait = new PanelPlacement(0f, 0.08f, 0.72f, 1.52f);
        string text = string.Concat(Enumerable.Repeat("お知らせ。Wood A-12 ", 20)).Substring(0, 256);
        int centred = SignTextLayout.Build(text, portrait, 0.06f, true, SignTextStyle.Default, glyphs).Quads.Count;
        foreach (SignHAlign h in new[] { SignHAlign.Left, SignHAlign.Center, SignHAlign.Right })
        {
            foreach (SignVAlign v in new[] { SignVAlign.Top, SignVAlign.Middle, SignVAlign.Bottom })
            {
                foreach (float scale in new[] { 1f, 0.5f })
                {
                    SignTextLayout.Result r = SignTextLayout.Build(text, portrait, 0.06f, true, Style(h, v, scale), glyphs);
                    Assert.True(r.Fits);
                    Assert.Equal(centred, r.Quads.Count);
                    foreach (GlyphQuad q in r.Quads)
                    {
                        Assert.InRange(q.X0, -0.36f - 1e-4f, 0.36f + 1e-4f);
                        Assert.InRange(q.X1, -0.36f - 1e-4f, 0.36f + 1e-4f);
                        Assert.InRange(q.Y0, -0.76f - 1e-4f, 0.76f + 1e-4f);
                        Assert.InRange(q.Y1, -0.76f - 1e-4f, 0.76f + 1e-4f);
                    }
                }
            }
        }
    }

    [Fact]
    public void EveryStyleFieldChangesTheRenderKey()
    {
        SignTextStyle baseStyle = Style();
        SignTextStyle[] variants =
        {
            new SignTextStyle(0.5f, true, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.None),
            new SignTextStyle(1f, false, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.None),
            new SignTextStyle(1f, true, SignHAlign.Left, SignVAlign.Middle, false, SignEffect.None),
            new SignTextStyle(1f, true, SignHAlign.Center, SignVAlign.Top, false, SignEffect.None),
            new SignTextStyle(1f, true, SignHAlign.Center, SignVAlign.Middle, true, SignEffect.None),
            new SignTextStyle(1f, true, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.Scroll),
        };
        var key = new SignRenderKey("研究室", 1u, 0, 1, 2, 3, baseStyle);
        foreach (SignTextStyle variant in variants)
        {
            Assert.NotEqual(baseStyle, variant);
            Assert.NotEqual(key, new SignRenderKey("研究室", 1u, 0, 1, 2, 3, variant));
        }
        Assert.Equal(key, new SignRenderKey("研究室", 1u, 0, 1, 2, 3, Style()));
    }

    [Theory]
    [InlineData(0)] // 3x1 landscape (north)
    [InlineData(1)] // 1x3 portrait (east)
    public void LargeBoardFits384CharactersInBothOrientations(int rotation)
    {
        var north = new PanelRect(0.0469f, 0.2969f, 0.9062f, 0.5625f);
        var east = new PanelRect(0.1406f, 0.1094f, 0.7188f, 0.8385f);
        PanelPlacement panel = SignPanelGeometry.Place(north, east, 3f, 1f, rotation, 0f, 0f, horizontalUsesRotatedSize: true);
        string text = string.Concat(Enumerable.Repeat("本日の作業予定：畑の収穫と倉庫の整理。", 25)).Substring(0, 384);
        SignTextLayout.Result r = SignTextLayout.Build(text, panel, 0.06f, true, SignTextStyle.Default, glyphs);
        Assert.True(r.Fits);
        Assert.Equal(384, r.Quads.Count);
        foreach (GlyphQuad q in r.Quads)
        {
            Assert.InRange(q.X0, -panel.Width / 2 - 1e-4f, panel.Width / 2 + 1e-4f);
            Assert.InRange(q.X1, -panel.Width / 2 - 1e-4f, panel.Width / 2 + 1e-4f);
            Assert.InRange(q.Y0, -panel.Height / 2 - 1e-4f, panel.Height / 2 + 1e-4f);
            Assert.InRange(q.Y1, -panel.Height / 2 - 1e-4f, panel.Height / 2 + 1e-4f);
        }
    }

    [Fact]
    public void NoticeBoardTurnedEastFitsAFull256CharacterNotice()
    {
        var north = new PanelRect(0.0703f, 0.2969f, 0.8594f, 0.5625f);
        var east = new PanelRect(0.1406f, 0.1641f, 0.7188f, 0.7578f);
        PanelPlacement panel = SignPanelGeometry.Place(north, east, 2f, 1f, 1, 0f, 0f, horizontalUsesRotatedSize: true);
        string text = string.Concat(Enumerable.Repeat("本日の作業予定：畑の収穫と倉庫の整理。", 15)).Substring(0, 256);
        SignTextLayout.Result r = SignTextLayout.Build(text, panel, 0.06f, true, SignTextStyle.Default, glyphs);
        Assert.True(r.Fits);
        Assert.Equal(256, r.Quads.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void LongFloorSignFits192CharactersInEveryOrientation(int rotation)
    {
        var north = new PanelRect(0.0365f, 0.1094f, 0.9271f, 0.7812f);
        var east = new PanelRect(0.1094f, 0.0365f, 0.7812f, 0.9271f);
        PanelPlacement panel = SignPanelGeometry.Place(north, east, 3f, 1f, rotation, 0f, 0f, horizontalUsesRotatedSize: true,
            mirrorX: rotation == 3);
        string text = string.Concat(Enumerable.Repeat("→ 食堂・寝室・倉庫はこちら。", 20)).Substring(0, 192);
        SignTextLayout.Result r = SignTextLayout.Build(text, panel, 0.06f, true, SignTextStyle.Default, glyphs);
        Assert.True(r.Fits);
        Assert.Equal(text.Count(c => c != ' '), r.Quads.Count);
    }
}
