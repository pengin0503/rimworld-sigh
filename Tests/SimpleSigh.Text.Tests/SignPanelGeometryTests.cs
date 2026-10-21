using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignPanelGeometryTests
{
    private static readonly PanelRect BasicPanel = new PanelRect(0.1406f, 0.3906f, 0.7188f, 0.4375f);

    [Fact]
    public void NorthPanelCenterIsOffsetFromGraphicCenter()
    {
        PanelPlacement p = SignPanelGeometry.Place(BasicPanel, default, 1f, 1f, rotation: 0, offsetX: 0f, offsetZ: 0f, horizontalUsesRotatedSize: false);
        Assert.Equal(0.1406f + 0.7188f / 2f - 0.5f, p.CenterX, 4);
        Assert.Equal(0.3906f + 0.4375f / 2f - 0.5f, p.CenterZ, 4);
        Assert.Equal(0.7188f, p.Width, 4);
        Assert.Equal(0.4375f, p.Height, 4);
    }

    [Fact]
    public void DrawSizeScalesThePanel()
    {
        PanelPlacement p = SignPanelGeometry.Place(new PanelRect(0f, 0f, 1f, 0.5f), default, 2f, 1f, 0, 0f, 0f, false);
        Assert.Equal(2f, p.Width, 4);
        Assert.Equal(0.5f, p.Height, 4);
        Assert.Equal(0f, p.CenterX, 4);
        Assert.Equal(-0.25f, p.CenterZ, 4);
    }

    [Fact]
    public void GraphicDrawOffsetMovesThePanel()
    {
        PanelPlacement p = SignPanelGeometry.Place(BasicPanel, default, 1f, 1f, 0, offsetX: 0f, offsetZ: 0.9f, false);
        Assert.Equal(0.3906f + 0.4375f / 2f - 0.5f + 0.9f, p.CenterZ, 4);
    }
}
