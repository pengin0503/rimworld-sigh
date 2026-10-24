using System.Collections.Generic;
using SimpleSigh.Layout;
using UnityEngine;

namespace SimpleSigh.Rendering;

/// <summary>
/// The mesh of one scrolling sign. Its triangle list is fixed for the whole row; each frame only the
/// vertices and UVs of the glyphs currently on the panel are rewritten (the rest collapse to a point),
/// using arrays allocated once when the sign's text or options change.
/// </summary>
public sealed class ScrollingText
{
    private readonly Marquee.Layout row;
    private readonly List<GlyphQuad> visible;
    private readonly Vector3[] vertices;
    private readonly Vector2[] uvs;
    private readonly float speed;
    private float shownOffset = float.NaN;

    public ScrollingText(Marquee.Layout row)
    {
        this.row = row;
        visible = new List<GlyphQuad>(row.Quads.Count);
        vertices = new Vector3[row.Quads.Count * 4];
        uvs = new Vector2[row.Quads.Count * 4];
        speed = Marquee.SpeedFor(row.LineHeight);
        // Readable, so it can be rewritten every frame.
        Mesh = SignTextRenderer.CreateMesh(vertices, uvs, row.Quads.Count, markNoLongerReadable: false);
        Mesh.MarkDynamic();
    }

    public Mesh Mesh { get; }

    /// <summary>Moves the text to where it is at <paramref name="timeSeconds"/>; no work if it has not moved.</summary>
    public void Update(float timeSeconds)
    {
        float offset = Marquee.Offset(timeSeconds, speed, row.TextWidth, row.PanelWidth);
        if (offset == shownOffset)
        {
            return;
        }
        shownOffset = offset;
        Marquee.Clip(row.Quads, offset, row.HalfWidth, row.HalfHeight, visible);
        SignTextRenderer.WriteQuads(visible, vertices, uvs);
        Mesh.vertices = vertices;
        Mesh.uv = uvs;
        Mesh.RecalculateBounds();
    }
}
