namespace SimpleSigh.Layout;

/// <summary>Panel rectangle as fractions of a graphic's draw size, measured from its bottom-left corner.</summary>
public readonly struct PanelRect
{
    public PanelRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; }

    public float Y { get; }

    public float Width { get; }

    public float Height { get; }

    public bool IsEmpty => Width <= 0f || Height <= 0f;
}

/// <summary>Panel centre relative to the thing's draw position, and its size, in world units (X east, Z north).</summary>
public readonly struct PanelPlacement
{
    public PanelPlacement(float centerX, float centerZ, float width, float height)
    {
        CenterX = centerX;
        CenterZ = centerZ;
        Width = width;
        Height = height;
    }

    public float CenterX { get; }

    public float CenterZ { get; }

    public float Width { get; }

    public float Height { get; }
}

/// <summary>Maps a sign's panel rectangle onto the world for a given rotation and graphic.</summary>
public static class SignPanelGeometry
{
    /// <param name="rotation">RimWorld Rot4 index: 0 north, 1 east, 2 south, 3 west.</param>
    /// <param name="offsetX">Graphic draw offset for this rotation (e.g. wall attachments).</param>
    /// <param name="horizontalUsesRotatedSize">True when the graphic swaps its draw size for east/west (Graphic_Multi that is not drawn rotated).</param>
    public static PanelPlacement Place(PanelRect panel, PanelRect horizontalPanel, float drawSizeX, float drawSizeZ,
        int rotation, float offsetX, float offsetZ, bool horizontalUsesRotatedSize)
    {
        float centerX = (panel.X + panel.Width * 0.5f - 0.5f) * drawSizeX;
        float centerZ = (panel.Y + panel.Height * 0.5f - 0.5f) * drawSizeZ;
        return new PanelPlacement(centerX + offsetX, centerZ + offsetZ, panel.Width * drawSizeX, panel.Height * drawSizeZ);
    }
}
