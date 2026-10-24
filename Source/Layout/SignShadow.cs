using System;

namespace SimpleSigh.Layout;

/// <summary>
/// The optional text shadow: the text mesh drawn a second time, slightly down-right and behind the
/// text, in black or white, whichever contrasts more with the text colour. It keeps text legible
/// on any background (floor signs sit on whatever floor the player built).
/// </summary>
public static class SignShadow
{
    /// <summary>Shadow offset as a fraction of the row height, so small and large text look alike.</summary>
    public const float OffsetPerLineHeight = 0.07f;

    /// <summary>Offset limits in map cells: visible on tiny text, never a detached copy on huge text.</summary>
    public const float MinOffset = 0.004f;

    public const float MaxOffset = 0.035f;

    private const uint DarkShadow = 0x000000A6;   // black, 65 % opacity
    private const uint LightShadow = 0xFFFFFF8C;  // white, 55 % opacity

    /// <summary>Shadow colour (RGBA, 8 bits each) for a text colour; the text's own alpha is ignored.</summary>
    public static uint ColorFor(uint textRgba)
    {
        float l = Luminance(textRgba);
        // WCAG contrast against black is (L + 0.05) / 0.05, against white 1.05 / (L + 0.05).
        return (l + 0.05f) / 0.05f >= 1.05f / (l + 0.05f) ? DarkShadow : LightShadow;
    }

    /// <summary>Offset in map cells for text whose rows are <paramref name="lineHeight"/> cells tall.</summary>
    public static float Offset(float lineHeight) => Math.Clamp(lineHeight * OffsetPerLineHeight, MinOffset, MaxOffset);

    private static float Luminance(uint rgba)
    {
        return 0.2126f * Channel(rgba >> 24) + 0.7152f * Channel((rgba >> 16) & 0xFF) + 0.0722f * Channel((rgba >> 8) & 0xFF);
    }

    private static float Channel(uint value)
    {
        float c = value / 255f;
        return c <= 0.03928f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
    }
}
