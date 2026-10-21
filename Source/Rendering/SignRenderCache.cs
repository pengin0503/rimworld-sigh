using System.Collections.Generic;
using SimpleSigh.Layout;
using SimpleSigh.Signs;
using UnityEngine;
using Verse;

namespace SimpleSigh.Rendering;

/// <summary>
/// One sign's built text mesh. Rebuilt only when <see cref="SignRenderKey"/> changes; released on
/// despawn, destroy, map removal and when a new game starts. The static registry holds only caches
/// (meshes/materials), never Things, and is cleared deterministically by <see cref="ReleaseAll"/>.
/// </summary>
public sealed class SignRenderCache
{
    public static readonly RenderCacheStats Stats = new RenderCacheStats();

    private static readonly HashSet<SignRenderCache> Registered = new HashSet<SignRenderCache>();

    private readonly SignRenderSlot<SignRenderKey, BuiltText> slot;

    public SignRenderCache()
    {
        slot = new SignRenderSlot<SignRenderKey, BuiltText>(built => Object.Destroy(built.Mesh), Stats);
    }

    public static int RegisteredCount => Registered.Count;

    /// <summary>True when the last build found characters the font cannot draw.</summary>
    public bool MissingGlyphs { get; private set; }

    public BuiltText Get(CompSignText sign, TmpGlyphSource glyphs)
    {
        SignRenderKey key = KeyFor(sign, glyphs);
        if (slot.TryGet(key, out BuiltText cached))
        {
            return cached;
        }
        // Rebuild path only: the closure below is allocated once per visible change, not per frame.
        BuiltText built = slot.Get(key, _ =>
        {
            MissingGlyphs = GlyphQuadBuilder.HasMissingGlyphs(sign.Text, glyphs);
            return SignTextRenderer.TryBuild(sign, glyphs, out Mesh mesh, out Material material, out Vector3 center)
                ? new BuiltText(mesh, material, center)
                : null;
        });
        if (slot.HasValue)
        {
            Registered.Add(this);
        }
        return built;
    }

    public void Release()
    {
        slot.Release();
        Registered.Remove(this);
    }

    /// <summary>Frees every live sign mesh, e.g. when a different game is started or loaded.</summary>
    public static void ReleaseAll()
    {
        foreach (SignRenderCache cache in Registered)
        {
            cache.slot.Release();
        }
        Registered.Clear();
    }

    private static SignRenderKey KeyFor(CompSignText sign, TmpGlyphSource glyphs)
    {
        Thing thing = sign.parent;
        Rot4 rot = thing.Rotation;
        Graphic graphic = thing.Graphic;
        CompProperties_SignText props = sign.Props;
        int layoutHash = System.HashCode.Combine(props.panelRect, props.panelRectHorizontal, props.panelPadding,
            graphic?.drawSize ?? Vector2.zero, graphic?.DrawOffset(rot) ?? Vector3.zero);
        Color32 c = sign.TextColor;
        uint rgba = ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;
        return new SignRenderKey(sign.Text, rgba, sign.Scale, rot.AsInt, glyphs.AtlasVersion, glyphs.FontIndex, layoutHash);
    }

    public sealed class BuiltText
    {
        public BuiltText(Mesh mesh, Material material, Vector3 localCenter)
        {
            Mesh = mesh;
            Material = material;
            LocalCenter = localCenter;
        }

        public Mesh Mesh { get; }

        public Material Material { get; }

        public Vector3 LocalCenter { get; }
    }
}
