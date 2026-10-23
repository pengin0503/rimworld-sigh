using System;

namespace SimpleSigh.Layout;

/// <summary>Everything that changes a sign's visible text. Any difference forces exactly one rebuild.</summary>
public readonly struct SignRenderKey : IEquatable<SignRenderKey>
{
    public SignRenderKey(string text, uint rgba, int rotation, int glyphGeneration, int glyphSourceId, int layoutHash, SignTextStyle style)
    {
        Text = text ?? string.Empty;
        Rgba = rgba;
        Rotation = rotation;
        GlyphGeneration = glyphGeneration;
        GlyphSourceId = glyphSourceId;
        LayoutHash = layoutHash;
        Style = style;
    }

    /// <summary>v0.1 form: size and automatic wrapping only, every other option at its default.</summary>
    public SignRenderKey(string text, uint rgba, float scale, int rotation, int glyphGeneration, int glyphSourceId, int layoutHash, bool autoWrap)
        : this(text, rgba, rotation, glyphGeneration, glyphSourceId, layoutHash,
            new SignTextStyle(scale, autoWrap, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.None))
    {
    }

    public string Text { get; }

    public uint Rgba { get; }

    public int Rotation { get; }

    /// <summary>Changes whenever glyph UVs or metrics of already-built meshes may be stale (atlas growth, backend swap).</summary>
    public int GlyphGeneration { get; }

    /// <summary>Which glyph source drew the text: the bundled sign font or a game font index (fallback).</summary>
    public int GlyphSourceId { get; }

    /// <summary>Hash of panel geometry inputs (panel rects, padding, draw size, draw offset).</summary>
    public int LayoutHash { get; }

    public SignTextStyle Style { get; }

    public float Scale => Style.Scale;

    public bool AutoWrap => Style.AutoWrap;

    public bool Equals(SignRenderKey other)
    {
        return Text == other.Text && Rgba == other.Rgba && Rotation == other.Rotation && GlyphGeneration == other.GlyphGeneration
            && GlyphSourceId == other.GlyphSourceId && LayoutHash == other.LayoutHash && Style.Equals(other.Style);
    }

    public override bool Equals(object obj) => obj is SignRenderKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Text, Rgba, Rotation, GlyphGeneration, GlyphSourceId, LayoutHash, Style);
}
