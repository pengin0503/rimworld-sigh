using System;

namespace SimpleSigh.Layout;

/// <summary>Everything that changes a sign's visible text. Any difference forces exactly one rebuild.</summary>
public readonly struct SignRenderKey : IEquatable<SignRenderKey>
{
    public SignRenderKey(string text, uint rgba, float scale, int rotation, int atlasVersion, int fontIndex, int layoutHash)
    {
        Text = text ?? string.Empty;
        Rgba = rgba;
        Scale = scale;
        Rotation = rotation;
        AtlasVersion = atlasVersion;
        FontIndex = fontIndex;
        LayoutHash = layoutHash;
    }

    public string Text { get; }

    public uint Rgba { get; }

    public float Scale { get; }

    public int Rotation { get; }

    public int AtlasVersion { get; }

    public int FontIndex { get; }

    /// <summary>Hash of panel geometry inputs (panel rects, padding, draw size, draw offset).</summary>
    public int LayoutHash { get; }

    public bool Equals(SignRenderKey other)
    {
        return Text == other.Text && Rgba == other.Rgba && Scale.Equals(other.Scale) && Rotation == other.Rotation
            && AtlasVersion == other.AtlasVersion && FontIndex == other.FontIndex && LayoutHash == other.LayoutHash;
    }

    public override bool Equals(object obj) => obj is SignRenderKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Text, Rgba, Scale, Rotation, AtlasVersion, FontIndex, LayoutHash);
}
