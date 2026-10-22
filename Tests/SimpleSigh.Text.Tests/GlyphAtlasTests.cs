using System;
using System.Collections.Generic;
using System.Linq;
using SimpleSigh.Fonts;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class GlyphAtlasTests
{
    private static GlyphAtlas NewAtlas(int initial = 128, int max = 512) =>
        new GlyphAtlas(TrueTypeFontTests.SignFont, 48f, initial, max);

    private static (int X, int Y, int W, int H) PixelRect(GlyphAtlas atlas, GlyphMetrics g) =>
        ((int)Math.Round(g.U0 * atlas.Width), (int)Math.Round(g.V1 * atlas.Height),
            (int)Math.Round((g.U1 - g.U0) * atlas.Width), (int)Math.Round((g.V0 - g.V1) * atlas.Height));

    [Fact]
    public void LineHeightIsAscentMinusDescentInPixels()
    {
        GlyphAtlas atlas = NewAtlas();
        Assert.Equal(48f * (1802 + 246) / 2048f, atlas.LineHeight, 4);
    }

    [Fact]
    public void GlyphsAreOnlyReportedAfterEnsure()
    {
        GlyphAtlas atlas = NewAtlas();
        Assert.False(atlas.TryGetGlyph('研', out _));
        Assert.True(atlas.Ensure("研究室"));
        Assert.True(atlas.TryGetGlyph('研', out GlyphMetrics g));
        Assert.True(g.Found && g.Visible);
        Assert.True(atlas.TryGetGlyph(GlyphQuadBuilder.Replacement, out _));
        Assert.False(atlas.Ensure("研究室"));
    }

    [Fact]
    public void CharactersMissingFromTheFontStayMissing()
    {
        GlyphAtlas atlas = NewAtlas();
        atlas.Ensure("A");
        Assert.False(atlas.TryGetGlyph('', out _));
        Assert.True(atlas.TryGetGlyph('A', out _));
        Assert.True(GlyphQuadBuilder.HasMissingGlyphs("", atlas));
        Assert.False(GlyphQuadBuilder.HasMissingGlyphs("A", atlas));
    }

    [Fact]
    public void SpaceHasAdvanceButNoInk()
    {
        GlyphAtlas atlas = NewAtlas();
        atlas.Ensure("A B");
        Assert.True(atlas.TryGetGlyph(' ', out GlyphMetrics space));
        Assert.False(space.Visible);
        Assert.True(space.AdvanceX > 0f);
    }

    [Fact]
    public void CombiningSequenceIsBakedAsItsComposedCharacter()
    {
        GlyphAtlas atlas = NewAtlas();
        atlas.Ensure("が");
        Assert.True(atlas.TryGetGlyph('が', out _));
        Assert.False(GlyphQuadBuilder.HasMissingGlyphs("が", atlas));
    }

    [Fact]
    public void UvRectanglePointsAtTheGlyphPixels()
    {
        GlyphAtlas atlas = NewAtlas();
        atlas.Ensure("食");
        TrueTypeFont font = TrueTypeFontTests.SignFont;
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(font.GetContours(font.GetGlyphIndex('食')), 48f / font.UnitsPerEm);
        Assert.True(atlas.TryGetGlyph('食', out GlyphMetrics g));
        (int x, int y, int w, int h) = PixelRect(atlas, g);
        Assert.Equal(bitmap.Width, w);
        Assert.Equal(bitmap.Height, h);
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                // Atlas rows run bottom to top, bitmap rows top to bottom.
                Assert.Equal(bitmap.Coverage[(h - 1 - row) * w + col], atlas.Alpha[(y + row) * atlas.Width + x + col]);
            }
        }
        Assert.Equal(bitmap.Left, g.X0);
        Assert.Equal(bitmap.Left + bitmap.Width, g.X1);
        Assert.Equal(atlas.LineHeight * 1802f / 2048f - bitmap.Top, g.Y0, 4);
        Assert.Equal(g.Y0 + bitmap.Height, g.Y1, 4);
    }

    [Fact]
    public void PackedGlyphsDoNotOverlapAndKeepPaddingFromEachOther()
    {
        GlyphAtlas atlas = NewAtlas(256, 1024);
        const string text = "研究施設入口食料保管庫立入禁止避難所発電機医務室倉庫寝室食堂娯楽室監獄工房畑ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        atlas.Ensure(text);
        var rects = new List<(int X, int Y, int W, int H)>();
        foreach (char c in text.Distinct())
        {
            Assert.True(atlas.TryGetGlyph(c, out GlyphMetrics g), c.ToString());
            if (g.Visible)
            {
                rects.Add(PixelRect(atlas, g));
            }
        }
        foreach ((int x, int y, int w, int h) in rects)
        {
            Assert.True(x >= 1 && y >= 1 && x + w <= atlas.Width - 1 && y + h <= atlas.Height - 1);
        }
        for (int i = 0; i < rects.Count; i++)
        {
            for (int j = i + 1; j < rects.Count; j++)
            {
                (int ax, int ay, int aw, int ah) = rects[i];
                (int bx, int by, int bw, int bh) = rects[j];
                bool separated = ax + aw + 1 <= bx || bx + bw + 1 <= ax || ay + ah + 1 <= by || by + bh + 1 <= ay;
                Assert.True(separated, $"glyph rects {rects[i]} and {rects[j]} touch");
            }
        }
    }

    [Fact]
    public void AddingGlyphsKeepsExistingUvsAndGeneration()
    {
        GlyphAtlas atlas = NewAtlas(512, 512);
        atlas.Ensure("研");
        atlas.TryGetGlyph('研', out GlyphMetrics before);
        int generation = atlas.Generation;
        atlas.Ensure("究室食料");
        atlas.TryGetGlyph('研', out GlyphMetrics after);
        Assert.Equal(generation, atlas.Generation);
        Assert.Equal(before.U0, after.U0);
        Assert.Equal(before.V0, after.V0);
    }

    [Fact]
    public void GrowingChangesGenerationAndKeepsPixelsConsistent()
    {
        GlyphAtlas atlas = NewAtlas(128, 1024);
        atlas.Ensure("研");
        int generation = atlas.Generation;
        const string many = "研究施設入口食料保管庫立入禁止避難所発電機医務室倉庫寝室食堂娯楽室監獄工房畑";
        atlas.Ensure(many);
        Assert.True(atlas.Width > 128);
        Assert.NotEqual(generation, atlas.Generation);
        TrueTypeFont font = TrueTypeFontTests.SignFont;
        foreach (char c in many)
        {
            Assert.True(atlas.TryGetGlyph(c, out GlyphMetrics g));
            (int x, int y, int w, int h) = PixelRect(atlas, g);
            GlyphBitmap bitmap = GlyphRasterizer.Rasterize(font.GetContours(font.GetGlyphIndex(c)), 48f / font.UnitsPerEm);
            Assert.Equal(bitmap.Coverage[(h - 1) * w], atlas.Alpha[y * atlas.Width + x]);
            Assert.Equal(bitmap.Coverage[w - 1], atlas.Alpha[(y + h - 1) * atlas.Width + x + w - 1]);
        }
    }

    [Fact]
    public void FullAtlasReportsOverflowAsMissingUntilCleared()
    {
        // 96 px holds only a few 48 px glyphs.
        GlyphAtlas atlas = NewAtlas(96, 96);
        atlas.Ensure("研究施設入口食料保管庫");
        Assert.True(atlas.IsFull);
        string text = "研究施設入口食料保管庫";
        Assert.Contains(text, c => !atlas.TryGetGlyph(c, out _));
        Assert.True(GlyphQuadBuilder.HasMissingGlyphs(text, atlas));
        int generation = atlas.Generation;
        atlas.Clear();
        Assert.False(atlas.IsFull);
        Assert.Equal(0, atlas.GlyphCount);
        Assert.NotEqual(generation, atlas.Generation);
        Assert.False(atlas.TryGetGlyph('研', out _));
        atlas.Ensure("研");
        Assert.True(atlas.TryGetGlyph('研', out _));
    }

    [Fact]
    public void UploadRegionCoversNewGlyphsAndIsTakenOnce()
    {
        GlyphAtlas atlas = NewAtlas(256, 256);
        Assert.True(atlas.TakeUpload(out _, out _, out int fullW, out int fullH, out bool resized));
        Assert.True(resized);
        Assert.Equal((256, 256), (fullW, fullH));
        Assert.False(atlas.TakeUpload(out _, out _, out _, out _, out _));

        atlas.Ensure("研A");
        Assert.True(atlas.HasPendingUpload);
        Assert.True(atlas.TakeUpload(out int x, out int y, out int w, out int h, out resized));
        Assert.False(resized);
        foreach (char c in "研A?")
        {
            atlas.TryGetGlyph(c, out GlyphMetrics g);
            (int gx, int gy, int gw, int gh) = PixelRect(atlas, g);
            Assert.True(gx >= x && gy >= y && gx + gw <= x + w && gy + gh <= y + h, c.ToString());
        }
        Assert.False(atlas.HasPendingUpload);
    }

    [Fact]
    public void LayoutWithTheAtlasDrawsEveryCharacterInsideThePanel()
    {
        GlyphAtlas atlas = NewAtlas(256, 1024);
        const string text = "食料保管庫";
        atlas.Ensure(text);
        var panel = new PanelPlacement(0f, 0f, 0.72f, 0.44f);
        SignTextLayout.Result result = SignTextLayout.Build(text, panel, 0.08f, false, 1f, atlas);
        Assert.Equal(5, result.Quads.Count);
        Assert.All(result.Quads, q =>
        {
            Assert.InRange(q.X0, -0.36f, 0.36f);
            Assert.InRange(q.X1, -0.36f, 0.36f);
            Assert.InRange(q.Y0, -0.22f, 0.22f);
            Assert.InRange(q.Y1, -0.22f, 0.22f);
        });
    }

    [Fact]
    public void RejectsInvalidConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlyphAtlas(TrueTypeFontTests.SignFont, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlyphAtlas(TrueTypeFontTests.SignFont, 48f, 512, 256));
        Assert.Throws<ArgumentNullException>(() => new GlyphAtlas(null, 48f));
    }
}
