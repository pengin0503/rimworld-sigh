using System.Collections.Generic;
using SimpleSigh.Layout;
using SimpleSigh.Signs;
using UnityEngine;
using Verse;

namespace SimpleSigh.Rendering;

/// <summary>
/// Builds the text mesh for one sign from <see cref="SignTextLayout"/> (glyph quads inside the sign's
/// display panel), in the map's XZ plane, textured from the sign glyph atlas with the 1.7 "WorldText" shader.
/// Building is only done when something visible changed; drawing a built mesh is one DrawMesh.
/// </summary>
public static class SignTextRenderer
{
    private static Shader textShader;

    private static Shader TextShader => textShader ??= ShaderDatabase.LoadShader("World/WorldText");

    public static PanelPlacement PanelFor(CompSignText sign, Rot4 rot)
    {
        Thing thing = sign.parent;
        CompProperties_SignText props = sign.Props;
        Graphic graphic = thing.Graphic;
        Vector2 drawSize = graphic?.drawSize ?? Vector2.one;
        Vector3 offset = graphic?.DrawOffset(rot) ?? Vector3.zero;
        bool rotatedSize = graphic != null && !graphic.ShouldDrawRotated;
        bool mirrored = graphic != null && ((rot == Rot4.West && graphic.WestFlipped) || (rot == Rot4.East && graphic.EastFlipped));
        return SignPanelGeometry.Place(ToPanel(props.panelRect), ToPanel(props.panelRectHorizontal),
            drawSize.x, drawSize.y, rot.AsInt, offset.x, offset.z, rotatedSize, mirrored);
    }

    /// <summary>
    /// Creates the mesh, or returns false when there is nothing to draw. With the shadow option the same
    /// mesh is drawn a second time with <paramref name="shadowMaterial"/>, offset by <paramref name="shadowOffset"/>.
    /// </summary>
    public static bool TryBuild(CompSignText sign, ISignGlyphs glyphs, out Mesh mesh, out Material material, out Vector3 localCenter,
        out Material shadowMaterial, out Vector3 shadowOffset)
    {
        mesh = null;
        material = null;
        localCenter = Vector3.zero;
        shadowMaterial = null;
        shadowOffset = Vector3.zero;
        if (sign.Text.NullOrEmpty())
        {
            return false;
        }

        PanelPlacement panel = PanelFor(sign, sign.parent.Rotation);
        CompProperties_SignText props = sign.Props;
        SignTextLayout.Result layout = SignTextLayout.Build(sign.Text, panel, props.panelPadding, props.multiline, sign.Style, glyphs);
        if (layout.Quads.Count == 0)
        {
            return false;
        }

        Texture2D atlas = glyphs.Atlas;
        if (atlas == null)
        {
            return false;
        }

        mesh = ToMesh(layout.Quads);
        int renderQueue = SignVisibility.RenderQueueFor(props.glowsInDark);
        material = MaterialPool.MatFrom(new MaterialRequest(atlas, TextShader, sign.TextColor) { renderQueue = renderQueue });
        localCenter = new Vector3(layout.CenterX, 0f, layout.CenterZ);
        if (sign.Style.Shadow)
        {
            Color shadowColor = FromRgba(SignShadow.ColorFor(ToRgba(sign.TextColor)));
            shadowMaterial = MaterialPool.MatFrom(new MaterialRequest(atlas, TextShader, shadowColor) { renderQueue = renderQueue });
            float offset = SignShadow.Offset(layout.LineHeight);
            shadowOffset = new Vector3(offset, 0f, -offset);
        }
        return true;
    }

    public static void Draw(Thing thing, Mesh mesh, Material material, Vector3 localCenter, Material shadowMaterial = null,
        Vector3 shadowOffset = default)
    {
        Vector3 position = thing.DrawPos + localCenter;
        if (shadowMaterial != null)
        {
            // Same render queue as the text; the lower altitude sorts the shadow behind it.
            Vector3 shadowPosition = position + shadowOffset;
            shadowPosition.y += Altitudes.AltInc * 0.25f;
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(shadowPosition, Quaternion.identity, Vector3.one), shadowMaterial, 0);
        }
        position.y += Altitudes.AltInc * 0.5f;
        Graphics.DrawMesh(mesh, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one), material, 0);
    }

    public static uint ToRgba(Color color)
    {
        Color32 c = color;
        return ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;
    }

    public static Color FromRgba(uint rgba) =>
        new Color32((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);

    private static Mesh ToMesh(List<GlyphQuad> quads)
    {
        var vertices = new Vector3[quads.Count * 4];
        var uvs = new Vector2[quads.Count * 4];
        var triangles = new int[quads.Count * 6];
        for (int i = 0; i < quads.Count; i++)
        {
            GlyphQuad q = quads[i];
            int v = i * 4;
            // Map XZ plane, viewed from above: text X -> world X, text Y -> world Z.
            vertices[v] = new Vector3(q.X0, 0f, q.Y0);
            vertices[v + 1] = new Vector3(q.X0, 0f, q.Y1);
            vertices[v + 2] = new Vector3(q.X1, 0f, q.Y1);
            vertices[v + 3] = new Vector3(q.X1, 0f, q.Y0);
            uvs[v] = new Vector2(q.U0, q.VBottom);
            uvs[v + 1] = new Vector2(q.U0, q.VTop);
            uvs[v + 2] = new Vector2(q.U1, q.VTop);
            uvs[v + 3] = new Vector2(q.U1, q.VBottom);
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }
        var mesh = new Mesh { name = "SimpleSigh text" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.UploadMeshData(markNoLongerReadable: false);
        return mesh;
    }

    private static PanelRect ToPanel(Rect rect) => new PanelRect(rect.x, rect.y, rect.width, rect.height);
}
