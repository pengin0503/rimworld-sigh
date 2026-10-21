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

    [Theory]
    [InlineData(0, 0f, 0.9f)]
    [InlineData(1, 0.9f, 0f)]
    [InlineData(2, 0f, -0.9f)]
    [InlineData(3, -0.9f, 0f)]
    public void WallSignPanelFollowsTheWallOffsetForEveryRotation(int rotation, float offsetX, float offsetZ)
    {
        // Wall sign: square upright Graphic_Single (drawRotated=false) with vanilla 0.9 wall offsets.
        var wallPanel = new PanelRect(0.125f, 0.25f, 0.75f, 0.4688f);
        PanelPlacement p = SignPanelGeometry.Place(wallPanel, default, 1f, 1f, rotation, offsetX, offsetZ, horizontalUsesRotatedSize: true);
        Assert.Equal(0.125f + 0.375f - 0.5f + offsetX, p.CenterX, 4);
        Assert.Equal(0.25f + 0.2344f - 0.5f + offsetZ, p.CenterZ, 4);
        Assert.Equal(0.75f, p.Width, 4);
        Assert.Equal(0.4688f, p.Height, 4);
    }
}
