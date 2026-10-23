using System;

namespace SimpleSigh.Layout;

public enum SignHAlign
{
    Left,
    Center,
    Right,
}

public enum SignVAlign
{
    Top,
    Middle,
    Bottom,
}

/// <summary>Animated display modes; only signs whose def allows effects (electronic signs) use them.</summary>
public enum SignEffect
{
    None,
    Blink,
    Scroll,
}

/// <summary>
/// Every per-sign display option except text and colour. One value shared by the layout, the render
/// key, saving and the editor, so a new option cannot be forgotten in one of them.
/// </summary>
public readonly struct SignTextStyle : IEquatable<SignTextStyle>
{
    /// <summary>The look of v0.1 signs, used for signs saved before these options existed.</summary>
    public static readonly SignTextStyle Default = new SignTextStyle(1f, true, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.None);

    public SignTextStyle(float scale, bool autoWrap, SignHAlign hAlign, SignVAlign vAlign, bool shadow, SignEffect effect)
    {
        Scale = scale;
        AutoWrap = autoWrap;
        HAlign = hAlign;
        VAlign = vAlign;
        Shadow = shadow;
        Effect = effect;
    }

    /// <summary>User text size in (0, 1]: fraction of the largest size that fits.</summary>
    public float Scale { get; }

    /// <summary>Single-line signs: use several rows when that makes the text clearly larger (#5).</summary>
    public bool AutoWrap { get; }

    public SignHAlign HAlign { get; }

    public SignVAlign VAlign { get; }

    /// <summary>Draw a dark offset copy behind the text for contrast on busy backgrounds.</summary>
    public bool Shadow { get; }

    public SignEffect Effect { get; }

    public SignTextStyle WithScale(float scale) => new SignTextStyle(scale, AutoWrap, HAlign, VAlign, Shadow, Effect);

    public bool Equals(SignTextStyle other)
    {
        return Scale.Equals(other.Scale) && AutoWrap == other.AutoWrap && HAlign == other.HAlign && VAlign == other.VAlign
            && Shadow == other.Shadow && Effect == other.Effect;
    }

    public override bool Equals(object obj) => obj is SignTextStyle other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Scale, AutoWrap, HAlign, VAlign, Shadow, Effect);

    public static bool operator ==(SignTextStyle left, SignTextStyle right) => left.Equals(right);

    public static bool operator !=(SignTextStyle left, SignTextStyle right) => !left.Equals(right);
}
