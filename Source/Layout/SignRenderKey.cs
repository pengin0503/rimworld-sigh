using System;

namespace SimpleSigh.Layout;

/// <summary>Everything that changes a sign's visible text. Any difference forces exactly one rebuild.</summary>
public readonly struct SignRenderKey : IEquatable<SignRenderKey>
{
    public SignRenderKey(string text, uint rgba, float scale, int rotation, int glyphGeneration, int glyphSourceId, int layoutHash)
    {
        Text = text ?? string.Empty;
        Rgba = rgba;
        Scale = scale;
        Rotation = rotation;
        GlyphGeneration = glyphGeneration;
        GlyphSourceId = glyphSourceId;
        LayoutHash = layoutHash;
    }

    public string Text { get; }

    public uint Rgba { get; }

    public float Scale { get; }

    public int Rotation { get; }

    /// <summary>Changes whenever glyph UVs or metrics of already-built meshes may be stale (atlas growth, backend swap).</summary>
    public int GlyphGeneration { get; }

    /// <summary>Which glyph source drew the text: the bundled sign font or a game font index (fallback).</summary>
    public int GlyphSourceId { get; }

    /// <summary>Hash of panel geometry inputs (panel rects, padding, draw size, draw offset).</summary>
    public int LayoutHash { get; }

    public bool Equals(SignRenderKey other)
    {
        return Text == other.Text && Rgba == other.Rgba && Scale.Equals(other.Scale) && Rotation == other.Rotation
            && GlyphGeneration == other.GlyphGeneration && GlyphSourceId == other.GlyphSourceId && LayoutHash == other.LayoutHash;
    }

    public override bool Equals(object obj) => obj is SignRenderKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Text, Rgba, Scale, Rotation, GlyphGeneration, GlyphSourceId, LayoutHash);
}
