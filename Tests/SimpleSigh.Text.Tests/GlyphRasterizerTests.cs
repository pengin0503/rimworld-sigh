using System;
using System.Collections.Generic;
using System.Linq;
using SimpleSigh.Fonts;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class GlyphRasterizerTests
{
    private static List<OutlinePoint[]> Square(float x0, float y0, float x1, float y1)
    {
        return new List<OutlinePoint[]>
        {
            new[] { new OutlinePoint(x0, y0, true), new OutlinePoint(x0, y1, true), new OutlinePoint(x1, y1, true), new OutlinePoint(x1, y0, true) },
        };
    }

    private static int At(GlyphBitmap bitmap, int x, int yFromTop) => bitmap.Coverage[yFromTop * bitmap.Width + x];

    [Fact]
    public void PixelAlignedSquareIsFullyCoveredInsideAndEmptyOutside()
    {
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(Square(2, 3, 12, 8), 1f);
        Assert.Equal(10, bitmap.Width);
        Assert.Equal(5, bitmap.Height);
        Assert.Equal(2, bitmap.Left);
        Assert.Equal(8, bitmap.Top);
        Assert.All(bitmap.Coverage, c => Assert.Equal(255, c));
    }

    [Fact]
    public void HalfPixelEdgesHaveHalfCoverage()
    {
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(Square(0.5f, 0, 4.5f, 2), 1f);
        Assert.Equal(5, bitmap.Width);
        Assert.InRange(At(bitmap, 0, 0), 126, 129);
        Assert.Equal(255, At(bitmap, 2, 0));
        Assert.InRange(At(bitmap, 4, 1), 126, 129);
    }

    [Fact]
    public void WindingDirectionDoesNotMatter()
    {
        List<OutlinePoint[]> clockwise = Square(0, 0, 6, 6);
        var counterClockwise = new List<OutlinePoint[]> { clockwise[0].Reverse().ToArray() };
        Assert.Equal(GlyphRasterizer.Rasterize(clockwise, 1f).Coverage, GlyphRasterizer.Rasterize(counterClockwise, 1f).Coverage);
    }

    [Fact]
    public void CounterWoundContourCutsAHole()
    {
        // Fixture 'O': 600-unit square with a 200-unit hole, at 0.01 px/unit = 6 px with a 2 px hole.
        TrueTypeFont font = TrueTypeFontTests.Fixture;
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(font.GetContours(font.GetGlyphIndex('O')), 0.01f);
        Assert.Equal(6, bitmap.Width);
        Assert.Equal(6, bitmap.Height);
        Assert.Equal(0, At(bitmap, 2, 2));
        Assert.Equal(0, At(bitmap, 3, 3));
        Assert.Equal(255, At(bitmap, 0, 0));
        Assert.Equal(255, At(bitmap, 5, 5));
    }

    [Fact]
    public void QuadraticArcCoverageMatchesItsArea()
    {
        // Area between chord and the parabola through (0,0),(300,600),(600,0) = 2/3 * base * control height / 2.
        TrueTypeFont font = TrueTypeFontTests.Fixture;
        const float scale = 0.1f;
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(font.GetContours(font.GetGlyphIndex('Q')), scale);
        double expected = 2.0 / 3.0 * 600 * 300 * scale * scale;
        double actual = bitmap.Coverage.Sum(c => c / 255.0);
        Assert.InRange(actual, expected * 0.99, expected * 1.01);
    }

    [Theory]
    [InlineData('研')]
    [InlineData('食')]
    [InlineData('避')]
    [InlineData('あ')]
    [InlineData('A')]
    [InlineData('g')]
    [InlineData('?')]
    public void SignFontCoverageEqualsOutlineArea(char c)
    {
        // Exact-area rasterization: total coverage equals the outline's area (non-overlapping contours).
        TrueTypeFont font = TrueTypeFontTests.SignFont;
        float scale = 48f / font.UnitsPerEm;
        List<OutlinePoint[]> contours = font.GetContours(font.GetGlyphIndex(c));
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(contours, scale);
        double actual = bitmap.Coverage.Sum(v => v / 255.0);
        double expected = Math.Abs(contours.Sum(contour => SignedArea(contour))) * scale * scale;
        Assert.InRange(actual, expected * 0.98, expected * 1.02);
    }

    [Fact]
    public void BitmapBoxMatchesOutlineBounds()
    {
        TrueTypeFont font = TrueTypeFontTests.SignFont;
        float scale = 48f / font.UnitsPerEm;
        List<OutlinePoint[]> contours = font.GetContours(font.GetGlyphIndex('研'));
        GlyphBitmap bitmap = GlyphRasterizer.Rasterize(contours, scale);
        IEnumerable<OutlinePoint> points = contours.SelectMany(p => p).Where(p => p.OnCurve);
        Assert.True(bitmap.Left <= Math.Floor(points.Min(p => p.X) * scale));
        Assert.True(bitmap.Top >= Math.Ceiling(points.Max(p => p.Y) * scale));
        Assert.InRange(bitmap.Width, 38, 50);
        Assert.InRange(bitmap.Height, 38, 50);
    }

    [Fact]
    public void EmptyAndDegenerateInputsProduceEmptyBitmaps()
    {
        Assert.True(GlyphRasterizer.Rasterize(new List<OutlinePoint[]>(), 1f).IsEmpty);
        Assert.True(GlyphRasterizer.Rasterize(null, 1f).IsEmpty);
        Assert.True(GlyphRasterizer.Rasterize(Square(0, 0, 4, 4), 0f).IsEmpty);
        Assert.True(GlyphRasterizer.Rasterize(Square(0, 0, 4, 4), float.NaN).IsEmpty);
    }

    /// <summary>Shoelace area of the contour with each quadratic segment's parabolic area added.</summary>
    private static double SignedArea(OutlinePoint[] contour)
    {
        // Expand implied on-curve points, then integrate lines and quadratic segments exactly.
        var expanded = new List<OutlinePoint>();
        for (int i = 0; i < contour.Length; i++)
        {
            OutlinePoint current = contour[i];
            OutlinePoint next = contour[(i + 1) % contour.Length];
            expanded.Add(current);
            if (!current.OnCurve && !next.OnCurve)
            {
                expanded.Add(new OutlinePoint((current.X + next.X) / 2, (current.Y + next.Y) / 2, true));
            }
        }
        int start = expanded.FindIndex(p => p.OnCurve);
        double area = 0;
        int n = expanded.Count;
        for (int k = 0; k < n;)
        {
            OutlinePoint p0 = expanded[(start + k) % n];
            OutlinePoint p1 = expanded[(start + k + 1) % n];
            if (p1.OnCurve)
            {
                area += Cross(p0, p1) / 2;
                k += 1;
            }
            else
            {
                OutlinePoint p2 = expanded[(start + k + 2) % n];
                // Quadratic Bezier: chord area plus 2/3 of the control triangle.
                area += Cross(p0, p2) / 2 + (Cross(p0, p1) + Cross(p1, p2) - Cross(p0, p2)) / 3;
                k += 2;
            }
        }
        return area;
    }

    private static double Cross(OutlinePoint a, OutlinePoint b) => (double)a.X * b.Y - (double)b.X * a.Y;
}
