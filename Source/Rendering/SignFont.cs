using System;
using System.IO;
using SimpleSigh.Fonts;
using SimpleSigh.Layout;
using UnityEngine;
using Verse;

namespace SimpleSigh.Rendering;

/// <summary>
/// Sign text from the font bundled with the mod (BIZ UDPGothic Bold, SIL OFL 1.1), rasterized by
/// <see cref="GlyphAtlas"/> into a texture owned by this mod.
/// <para>
/// RimWorld 1.7's own text atlas bakes only a fixed character set for world text and its baking
/// internals differ between 1.7 builds, so Japanese typed on a sign came out as "?". This source
/// depends only on the public Texture2D API: characters are rasterized when a sign that uses them is
/// (re)built, and only the changed atlas region is uploaded. If the font cannot be loaded or the
/// texture cannot be written, it logs once and the signs fall back to the game font
/// (<see cref="TmpGlyphSource"/>).
/// </para>
/// </summary>
public sealed class SignFont : ISignGlyphs
{
    public const string FontRelativePath = "Fonts/BIZUDPGothic-Bold.ttf";

    /// <summary>Em size of the rasterized glyphs; the line box is the same height for this font.</summary>
    public const float PixelSize = 48f;

    /// <summary>Distinguishes this source from game font indices (which are never negative) in render keys.</summary>
    public const int BundledSourceId = -1;

    private const int InitialAtlasSize = 512;
    private const int MaxAtlasSize = 2048;
    private const string LogPrefix = "[Simple Sigh] ";

    private static SignFont instance;
    private static bool failed;

    private readonly GlyphAtlas atlas;
    private bool warnedFull;

    private SignFont(GlyphAtlas atlas)
    {
        this.atlas = atlas;
        Atlas = CreateTexture(atlas.Width, atlas.Height);
        Upload();
    }

    /// <summary>Set by <see cref="SimpleSighMod"/> when the mod is loaded.</summary>
    public static string ModRootDir { get; set; }

    public Texture2D Atlas { get; private set; }

    public float LineHeight => atlas.LineHeight;

    public int SourceId => BundledSourceId;

    public int Generation => atlas.Generation;

    /// <summary>The shared sign font, created on first use. False once loading or uploading has failed.</summary>
    public static bool TryGet(out SignFont font)
    {
        if (instance == null && !failed)
        {
            try
            {
                if (ModRootDir == null)
                {
                    throw new InvalidOperationException("mod folder unknown");
                }
                string path = Path.Combine(ModRootDir, FontRelativePath);
                var trueType = new TrueTypeFont(File.ReadAllBytes(path));
                var created = new SignFont(new GlyphAtlas(trueType, PixelSize, InitialAtlasSize, MaxAtlasSize));
                // The constructor's first upload reports its own failure through Fail().
                instance = failed ? null : created;
            }
            catch (Exception e)
            {
                Fail("could not load the sign font " + FontRelativePath + "; signs use the game font, which may show Japanese as '?'. " + e);
            }
        }
        font = instance;
        return font != null;
    }

    /// <summary>
    /// Forgets every rasterized character, e.g. when another game is started or loaded. Only managed
    /// state changes here (games may be loaded off the main thread); the texture is resized and
    /// re-uploaded by the next <see cref="Prepare"/>, which runs while drawing.
    /// </summary>
    public static void ResetForNewGame()
    {
        instance?.atlas.Clear();
    }

    public bool TryGetGlyph(char c, out GlyphMetrics glyph) => atlas.TryGetGlyph(c, out glyph);

    public void Prepare(string text)
    {
        atlas.Ensure(text);
        Upload();
        if (atlas.IsFull && !warnedFull)
        {
            warnedFull = true;
            Log.Warning(LogPrefix + "the sign glyph atlas is full (" + atlas.GlyphCount + " characters); further new characters are shown as '?' until another game is loaded.");
        }
    }

    private void Upload()
    {
        if (failed || !atlas.TakeUpload(out int x, out int y, out int width, out int height, out bool resized))
        {
            return;
        }
        try
        {
            if (resized && (Atlas.width != atlas.Width || Atlas.height != atlas.Height) && !Atlas.Reinitialize(atlas.Width, atlas.Height))
            {
                // Materials built for the old texture are replaced: the generation change rebuilds every sign.
                Atlas = CreateTexture(atlas.Width, atlas.Height);
            }
            // White glyphs with coverage in alpha, the format the World/WorldText shader samples.
            var pixels = new Color32[width * height];
            byte[] alpha = atlas.Alpha;
            for (int row = 0; row < height; row++)
            {
                int source = (y + row) * atlas.Width + x;
                int target = row * width;
                for (int col = 0; col < width; col++)
                {
                    pixels[target + col] = new Color32(255, 255, 255, alpha[source + col]);
                }
            }
            Atlas.SetPixels32(x, y, width, height, pixels);
            Atlas.Apply(false);
        }
        catch (Exception e)
        {
            Fail("could not upload the sign glyph atlas; signs fall back to the game font. " + e);
        }
    }

    private static Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "SimpleSigh glyph atlas",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
    }

    private static void Fail(string message)
    {
        failed = true;
        instance = null;
        Log.Error(LogPrefix + message);
    }
}
