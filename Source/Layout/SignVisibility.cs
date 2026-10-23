namespace SimpleSigh.Layout;

/// <summary>When sign text is drawn, and in which render queue (values from 1.7 Shaders.Map.xml).</summary>
public static class SignVisibility
{
    /// <summary>Building sprites (Cutout / CutoutComplex).</summary>
    public const int SpriteQueue = 2900;

    /// <summary>Darkness/light overlay that dims everything drawn before it.</summary>
    public const int LightOverlayQueue = 3100;

    /// <summary>Vanilla TransparentPostLight: drawn after the light overlay, so it stays bright in the dark.</summary>
    public const int PostLightQueue = 3150;

    /// <summary>Fog of war; text must stay below it.</summary>
    public const int FogOfWarQueue = 3175;

    /// <summary>Ordinary painted text: lit like the board it is on.</summary>
    public const int LitQueue = 3000;

    /// <summary>
    /// Powered signs only show text while their power is on. The text itself is kept, so it reappears
    /// unchanged when power returns. A powered def without a power comp never draws (misconfiguration).
    /// </summary>
    public static bool ShouldDrawText(string text, bool requiresPower, bool hasPowerComp, bool powerOn)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        return !requiresPower || (hasPowerComp && powerOn);
    }

    /// <summary>
    /// The player can hide sign text when zoomed out: <paramref name="hideFrom"/> is the first
    /// CameraZoomRange index (Closest 0 … Furthest 4) without text, or -1 to always show it.
    /// </summary>
    public static bool ZoomAllows(int currentZoom, int hideFrom) => hideFrom < 0 || currentZoom < hideFrom;

    public static int RenderQueueFor(bool glowsInDark) => glowsInDark ? PostLightQueue : LitQueue;
}
