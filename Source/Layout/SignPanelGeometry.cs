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
    /// <param name="horizontalUsesRotatedSize">True when the graphic swaps its draw size for east/west (graphics that are not drawn rotated).</param>
    /// <param name="mirrorX">True when the graphic mirrors its texture for this rotation (Graphic_Multi west from east).</param>
    public static PanelPlacement Place(PanelRect panel, PanelRect horizontalPanel, float drawSizeX, float drawSizeZ,
        int rotation, float offsetX, float offsetZ, bool horizontalUsesRotatedSize, bool mirrorX = false)
    {
        bool horizontal = rotation == 1 || rotation == 3;
        PanelRect rect = panel;
        float sizeX = drawSizeX;
        float sizeZ = drawSizeZ;
        if (horizontal && horizontalUsesRotatedSize)
        {
            // Graphic.MeshAt swaps the draw size for east/west when the graphic is not drawn rotated.
            sizeX = drawSizeZ;
            sizeZ = drawSizeX;
            if (!horizontalPanel.IsEmpty)
            {
                rect = horizontalPanel;
            }
        }

        float fractionX = rect.X + rect.Width * 0.5f - 0.5f;
        if (mirrorX)
        {
            fractionX = -fractionX;
        }
        float centerZ = (rect.Y + rect.Height * 0.5f - 0.5f) * sizeZ;
        return new PanelPlacement(fractionX * sizeX + offsetX, centerZ + offsetZ, rect.Width * sizeX, rect.Height * sizeZ);
    }
}
