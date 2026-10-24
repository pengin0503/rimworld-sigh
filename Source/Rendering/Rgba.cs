using UnityEngine;

namespace SimpleSigh.Rendering;

/// <summary>Colours as 8-bit RGBA packed into a uint (0xRRGGBBAA), the form the pure layout code uses.</summary>
public static class Rgba
{
    public static uint Pack(Color color)
    {
        Color32 c = color;
        return ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;
    }

    public static Color Unpack(uint rgba) =>
        new Color32((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
}
