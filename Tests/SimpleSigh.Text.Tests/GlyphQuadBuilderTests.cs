using System.Collections.Generic;
using System.Linq;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class GlyphQuadBuilderTests
{
    private readonly FakeGlyphs glyphs = new FakeGlyphs();

    [Fact]
    public void MeasureSumsAdvancesInPixels()
    {
        Assert.Equal(20f * 3, GlyphQuadBuilder.MeasureLine("研究室", glyphs));
        Assert.Equal(10f * 3, GlyphQuadBuilder.MeasureLine("A-1", glyphs));
    }

    [Fact]
    public void BuildsOneQuadPerVisibleGlyph()
    {
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { "A B" }, glyphs, worldLineHeight: 1f);
        Assert.Equal(2, quads.Count); // space is not visible
    }

    [Fact]
    public void BlockIsCenteredOnOrigin()
    {
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { "AA" }, glyphs, 1f);
        float minX = quads.Min(q => q.X0), maxX = quads.Max(q => q.X1);
        // Line advance 20px at 30px/unit = 0.6667 units, centred: pen starts at -0.3333.
        Assert.Equal(-1f / 3f + 1f / 30f, minX, 4);
        Assert.Equal(1f / 3f - 1f / 30f, maxX, 4);
        float minY = quads.Min(q => q.Y0), maxY = quads.Max(q => q.Y1);
        // Line box 30px = 1 unit centred at 0: top 0.5. Glyph spans 4..26px from the top.
        Assert.Equal(0.5f - 26f / 30f, minY, 4);
        Assert.Equal(0.5f - 4f / 30f, maxY, 4);
    }

    [Fact]
    public void QuadsCarryAtlasUvsWithTopOnV0()
    {
        GlyphQuad q = GlyphQuadBuilder.Build(new[] { "A" }, glyphs, 1f).Single();
        Assert.Equal('A' / 1000f, q.U0, 5);
        Assert.Equal(0.1f, q.VTop, 5);
        Assert.Equal(0.2f, q.VBottom, 5);
    }

    [Fact]
    public void SecondLineSitsBelowFirst()
    {
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { "A", "B" }, glyphs, 1f);
        Assert.True(quads[0].Y0 > quads[1].Y1);
        Assert.Equal(1f, quads[0].Y0 - quads[1].Y0, 4);
    }

    [Fact]
    public void MissingGlyphIsReplacedNotDropped()
    {
        glyphs.Missing.Add('研');
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { "研A" }, glyphs, 1f);
        Assert.Equal(2, quads.Count);
        Assert.Equal(GlyphQuadBuilder.Replacement / 1000f, quads[0].U0, 5);
        Assert.True(GlyphQuadBuilder.HasMissingGlyphs("研A", glyphs));
        Assert.False(GlyphQuadBuilder.HasMissingGlyphs("A", glyphs));
    }

    [Fact]
    public void SurrogatePairBecomesOneReplacement()
    {
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { "A\U0001F600" }, glyphs, 1f);
        Assert.Equal(2, quads.Count);
        Assert.Equal(10f * 2, GlyphQuadBuilder.MeasureLine("A\U0001F600", glyphs));
    }

    [Fact]
    public void CombiningMarkWithoutGlyphCollapsesIntoOneReplacement()
    {
        // "q" + combining acute has no precomposed form, so it is one element drawn as one replacement.
        Assert.Equal(10f, GlyphQuadBuilder.MeasureLine("q\u0301", glyphs));
        Assert.True(GlyphQuadBuilder.HasMissingGlyphs("q\u0301", glyphs));
    }

    [Fact]
    public void DecomposedKanaRendersAsPrecomposedGlyph()
    {
        // "か" + U+3099 normalizes (NFC) to "が", which the font has: one CJK-width glyph, no replacement.
        List<GlyphQuad> quads = GlyphQuadBuilder.Build(new[] { "か\u3099" }, glyphs, 1f);
        Assert.Single(quads);
        Assert.Equal('が' / 1000f, quads[0].U0, 5);
        Assert.False(GlyphQuadBuilder.HasMissingGlyphs("か\u3099", glyphs));
    }

    [Fact]
    public void ClipKeepsInsideQuadsAndDropsQuadsCompletelyOutside()
    {
        var quads = new List<GlyphQuad>
        {
            new GlyphQuad { X0 = -0.2f, X1 = 0.2f, Y0 = -0.1f, Y1 = 0.1f },
            new GlyphQuad { X0 = 0.6f, X1 = 0.8f, Y0 = -0.1f, Y1 = 0.1f },
            new GlyphQuad { X0 = -0.1f, X1 = 0.1f, Y0 = 0.35f, Y1 = 0.45f },
        };
        List<GlyphQuad> kept = GlyphQuadBuilder.ClipTo(quads, halfWidth: 0.5f, halfHeight: 0.3f);
        Assert.Single(kept);
        Assert.Equal(-0.2f, kept[0].X0);
        Assert.Equal(0.2f, kept[0].X1);
    }

    [Fact]
    public void ClipTrimsAGlyphOnTheEdgeInsteadOfDroppingIt()
    {
        // Ink overhanging the right and top edges is cut off; the visible part keeps its texture mapping.
        var quad = new GlyphQuad { X0 = 0.4f, X1 = 0.6f, Y0 = 0.2f, Y1 = 0.4f, U0 = 0.1f, U1 = 0.3f, VTop = 0.9f, VBottom = 0.5f };
        GlyphQuad trimmed = Assert.Single(GlyphQuadBuilder.ClipTo(new List<GlyphQuad> { quad }, 0.5f, 0.3f));
        Assert.Equal(0.4f, trimmed.X0, 5);
        Assert.Equal(0.5f, trimmed.X1, 5);
        Assert.Equal(0.2f, trimmed.Y0, 5);
        Assert.Equal(0.3f, trimmed.Y1, 5);
        Assert.Equal(0.1f, trimmed.U0, 5);
        Assert.Equal(0.2f, trimmed.U1, 5);
        Assert.Equal(0.7f, trimmed.VTop, 5);
        Assert.Equal(0.5f, trimmed.VBottom, 5);
    }

    [Fact]
    public void ClipTrimsLeftAndBottomEdgesToo()
    {
        var quad = new GlyphQuad { X0 = -0.6f, X1 = -0.4f, Y0 = -0.4f, Y1 = -0.2f, U0 = 0f, U1 = 1f, VTop = 1f, VBottom = 0f };
        GlyphQuad trimmed = Assert.Single(GlyphQuadBuilder.ClipTo(new List<GlyphQuad> { quad }, 0.5f, 0.3f));
        Assert.Equal(-0.5f, trimmed.X0, 5);
        Assert.Equal(0.5f, trimmed.U0, 5);
        Assert.Equal(-0.3f, trimmed.Y0, 5);
        Assert.Equal(0.5f, trimmed.VBottom, 5);
        Assert.Equal(1f, trimmed.VTop, 5);
    }

    [Fact]
    public void EmptyLinesProduceNoQuads()
    {
        Assert.Empty(GlyphQuadBuilder.Build(new string[0], glyphs, 1f));
        Assert.Empty(GlyphQuadBuilder.Build(new[] { "" }, glyphs, 1f));
    }
}
