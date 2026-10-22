using System;
using System.IO;
using System.Linq;
using SimpleSigh.Fonts;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class TrueTypeFontTests
{
    internal static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.ttf");
    internal static readonly string SignFontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "BIZUDPGothic-Bold.ttf");

    private static readonly Lazy<TrueTypeFont> fixture = new Lazy<TrueTypeFont>(() => new TrueTypeFont(File.ReadAllBytes(FixturePath)));
    private static readonly Lazy<TrueTypeFont> signFont = new Lazy<TrueTypeFont>(() => new TrueTypeFont(File.ReadAllBytes(SignFontPath)));

    internal static TrueTypeFont Fixture => fixture.Value;

    internal static TrueTypeFont SignFont => signFont.Value;

    [Fact]
    public void ReadsFixtureMetrics()
    {
        Assert.Equal(1000, Fixture.UnitsPerEm);
        Assert.Equal(800, Fixture.Ascender);
        Assert.Equal(-200, Fixture.Descender);
        Assert.Equal(6, Fixture.GlyphCount);
    }

    [Fact]
    public void MapsCharactersThroughFormat4AndReportsUnmappedAsNotdef()
    {
        Assert.Equal(2, Fixture.GetGlyphIndex('A'));
        Assert.Equal(3, Fixture.GetGlyphIndex('B'));
        Assert.Equal(0, Fixture.GetGlyphIndex('Z'));
        Assert.Equal(0, Fixture.GetGlyphIndex(0x1F600));
        Assert.Equal(0, Fixture.GetGlyphIndex(-1));
    }

    [Fact]
    public void ReadsAdvanceWidths()
    {
        Assert.Equal(600, Fixture.GetAdvanceWidth(Fixture.GetGlyphIndex('A')));
        Assert.Equal(250, Fixture.GetAdvanceWidth(Fixture.GetGlyphIndex(' ')));
    }

    [Fact]
    public void ReadsSimpleContour()
    {
        OutlinePoint[] contour = Assert.Single(Fixture.GetContours(Fixture.GetGlyphIndex('A')));
        Assert.All(contour, p => Assert.True(p.OnCurve));
        Assert.Equal(100f, contour.Min(p => p.X));
        Assert.Equal(500f, contour.Max(p => p.X));
        Assert.Equal(0f, contour.Min(p => p.Y));
        Assert.Equal(400f, contour.Max(p => p.Y));
    }

    [Fact]
    public void FlattensCompositeWithScaleAndOffset()
    {
        // 'B' = 'A' (100..500 x 0..400) scaled by 0.5 then moved by (50, 100).
        OutlinePoint[] contour = Assert.Single(Fixture.GetContours(Fixture.GetGlyphIndex('B')));
        Assert.Equal(100f, contour.Min(p => p.X));
        Assert.Equal(300f, contour.Max(p => p.X));
        Assert.Equal(100f, contour.Min(p => p.Y));
        Assert.Equal(300f, contour.Max(p => p.Y));
    }

    [Fact]
    public void KeepsOffCurveControlPoints()
    {
        OutlinePoint[] contour = Assert.Single(Fixture.GetContours(Fixture.GetGlyphIndex('Q')));
        Assert.Contains(contour, p => !p.OnCurve && p.X == 300f && p.Y == 600f);
    }

    [Fact]
    public void GlyphWithoutOutlineHasNoContours()
    {
        Assert.Empty(Fixture.GetContours(Fixture.GetGlyphIndex(' ')));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(100)]
    [InlineData(400)]
    public void TruncatedFontIsRejectedAsInvalidData(int length)
    {
        byte[] data = File.ReadAllBytes(FixturePath).Take(length).ToArray();
        Assert.Throws<InvalidDataException>(() => new TrueTypeFont(data));
    }

    [Fact]
    public void NonTrueTypeDataIsRejected()
    {
        byte[] data = new byte[2048];
        new Random(5).NextBytes(data);
        Assert.Throws<InvalidDataException>(() => new TrueTypeFont(data));
    }

    [Fact]
    public void SignFontUsesFormat12AndHasExpectedVerticalMetrics()
    {
        Assert.Equal(2048, SignFont.UnitsPerEm);
        Assert.Equal(1802, SignFont.Ascender);
        Assert.Equal(-246, SignFont.Descender);
    }

    [Theory]
    [InlineData("研究室")]
    [InlineData("食料保管庫")]
    [InlineData("立入禁止")]
    [InlineData("避難所・研究施設入口")]
    [InlineData("あいうえおかきくけこアイウエオッャー")]
    [InlineData("ABCxyz 0123456789 ?!#%&()")]
    [InlineData("、。「」（）：；")]
    [InlineData("ＡＢＣ１２３")]
    public void SignFontCoversJapaneseLatinAndPunctuation(string text)
    {
        foreach (char c in text)
        {
            Assert.True(SignFont.GetGlyphIndex(c) != 0, $"U+{(int)c:X4} '{c}' missing from the sign font");
        }
    }

    [Fact]
    public void SignFontHasReplacementMark()
    {
        Assert.NotEqual(0, SignFont.GetGlyphIndex('?'));
        Assert.NotEmpty(SignFont.GetContours(SignFont.GetGlyphIndex('?')));
    }
}
