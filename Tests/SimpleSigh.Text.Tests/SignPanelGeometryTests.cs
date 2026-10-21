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

    // ---- 2x1 notice board (Graphic_Multi, not drawn rotated: east/west use a portrait texture) ----

    private static readonly PanelRect BoardNorth = new PanelRect(0.0703f, 0.2969f, 0.8594f, 0.5625f);
    private static readonly PanelRect BoardEast = new PanelRect(0.1406f, 0.1641f, 0.7188f, 0.7578f);

    [Fact]
    public void BoardNorthUsesLandscapePanel()
    {
        PanelPlacement p = SignPanelGeometry.Place(BoardNorth, BoardEast, 2f, 1f, 0, 0f, 0f, true);
        Assert.Equal(0.8594f * 2f, p.Width, 4);
        Assert.Equal(0.5625f, p.Height, 4);
        Assert.Equal((0.0703f + 0.4297f - 0.5f) * 2f, p.CenterX, 4);
    }

    [Fact]
    public void BoardSouthUsesTheSameUprightPanel()
    {
        PanelPlacement north = SignPanelGeometry.Place(BoardNorth, BoardEast, 2f, 1f, 0, 0f, 0f, true);
        PanelPlacement south = SignPanelGeometry.Place(BoardNorth, BoardEast, 2f, 1f, 2, 0f, 0f, true);
        Assert.Equal(north.CenterX, south.CenterX, 4);
        Assert.Equal(north.CenterZ, south.CenterZ, 4);
        Assert.Equal(north.Width, south.Width, 4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void BoardEastWestUsesPortraitPanelWithSwappedDrawSize(int rotation)
    {
        PanelPlacement p = SignPanelGeometry.Place(BoardNorth, BoardEast, 2f, 1f, rotation, 0f, 0f, true);
        Assert.Equal(0.7188f * 1f, p.Width, 4);
        Assert.Equal(0.7578f * 2f, p.Height, 4);
        Assert.Equal((0.1641f + 0.3789f - 0.5f) * 2f, p.CenterZ, 4);
        Assert.True(p.Height > p.Width);
    }

    [Fact]
    public void EmptyHorizontalPanelFallsBackToNorthPanelOnSwappedSize()
    {
        PanelPlacement p = SignPanelGeometry.Place(BoardNorth, default, 2f, 1f, 1, 0f, 0f, true);
        Assert.Equal(0.8594f * 1f, p.Width, 4);
        Assert.Equal(0.5625f * 2f, p.Height, 4);
    }

    [Fact]
    public void FlippedWestMirrorsThePanelHorizontally()
    {
        var asymmetric = new PanelRect(0.1f, 0.2f, 0.5f, 0.6f);
        PanelPlacement east = SignPanelGeometry.Place(BoardNorth, asymmetric, 2f, 1f, 1, 0f, 0f, true);
        PanelPlacement west = SignPanelGeometry.Place(BoardNorth, asymmetric, 2f, 1f, 3, 0f, 0f, true, mirrorX: true);
        Assert.Equal(-0.15f, east.CenterX, 4);
        Assert.Equal(0.15f, west.CenterX, 4);
        Assert.Equal(east.CenterZ, west.CenterZ, 4);
    }
}
