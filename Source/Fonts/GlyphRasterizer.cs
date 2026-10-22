using System;
using System.Collections.Generic;

namespace SimpleSigh.Fonts;

/// <summary>An 8-bit coverage bitmap, rows top to bottom, positioned relative to the glyph origin.</summary>
public sealed class GlyphBitmap
{
    public static readonly GlyphBitmap Empty = new GlyphBitmap(0, 0, 0, 0, Array.Empty<byte>());

    public GlyphBitmap(int width, int height, int left, int top, byte[] coverage)
    {
        Width = width;
        Height = height;
        Left = left;
        Top = top;
        Coverage = coverage;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Pixels from the pen position to the bitmap's left edge.</summary>
    public int Left { get; }

    /// <summary>Pixels from the baseline up to the bitmap's top edge.</summary>
    public int Top { get; }

    /// <summary>Width × Height coverage values (0 = empty, 255 = fully inside), first row is the top.</summary>
    public byte[] Coverage { get; }

    public bool IsEmpty => Width == 0 || Height == 0;
}

/// <summary>
/// Anti-aliased scanline rasterizer for TrueType outlines. Quadratic curves are flattened into
/// lines and each line adds its exact signed area to a coverage accumulator (the "font-rs" method),
/// so the result is analytic area coverage without hinting. Overlapping contours of the same
/// direction saturate at full coverage; counter-wound contours cut holes.
/// </summary>
public static class GlyphRasterizer
{
    /// <summary>Flattening tolerance: higher values use more segments per curve.</summary>
    private const float Tolerance = 3f;

    /// <param name="scale">Pixels per font unit.</param>
    public static GlyphBitmap Rasterize(List<OutlinePoint[]> contours, float scale)
    {
        if (contours == null || contours.Count == 0 || !(scale > 0f))
        {
            return GlyphBitmap.Empty;
        }

        var lines = new List<(float X0, float Y0, float X1, float Y1)>();
        foreach (OutlinePoint[] contour in contours)
        {
            Flatten(contour, scale, lines);
        }
        if (lines.Count == 0)
        {
            return GlyphBitmap.Empty;
        }

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach ((float x0, float y0, float x1, float y1) in lines)
        {
            minX = Math.Min(minX, Math.Min(x0, x1));
            maxX = Math.Max(maxX, Math.Max(x0, x1));
            minY = Math.Min(minY, Math.Min(y0, y1));
            maxY = Math.Max(maxY, Math.Max(y0, y1));
        }
        if (!float.IsFinite(minX) || !float.IsFinite(maxX) || !float.IsFinite(minY) || !float.IsFinite(maxY))
        {
            return GlyphBitmap.Empty;
        }
        int left = (int)Math.Floor(minX);
        int bottom = (int)Math.Floor(minY);
        int right = (int)Math.Ceiling(maxX);
        int top = (int)Math.Ceiling(maxY);
        int width = right - left;
        int height = top - bottom;
        if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
        {
            return GlyphBitmap.Empty;
        }

        // One spare cell per row: a segment touching the right edge writes to x == width.
        var accumulator = new float[width * height + width + 2];
        foreach ((float x0, float y0, float x1, float y1) in lines)
        {
            // Bitmap space: x from the left edge, y downward from the top edge.
            DrawLine(accumulator, width, height, x0 - left, top - y0, x1 - left, top - y1);
        }

        var coverage = new byte[width * height];
        float sum = 0f;
        for (int i = 0; i < coverage.Length; i++)
        {
            sum += accumulator[i];
            float alpha = Math.Min(1f, Math.Abs(sum));
            coverage[i] = (byte)(alpha * 255f + 0.5f);
        }
        return new GlyphBitmap(width, height, left, top, coverage);
    }

    private static void Flatten(OutlinePoint[] contour, float scale, List<(float, float, float, float)> lines)
    {
        int n = contour.Length;
        // Start at an on-curve point; an all-off-curve contour starts at the midpoint of its first two points.
        int startIndex = Array.FindIndex(contour, p => p.OnCurve);
        float startX, startY;
        if (startIndex >= 0)
        {
            startX = contour[startIndex].X * scale;
            startY = contour[startIndex].Y * scale;
        }
        else
        {
            startIndex = 0;
            startX = (contour[0].X + contour[1].X) * 0.5f * scale;
            startY = (contour[0].Y + contour[1].Y) * 0.5f * scale;
        }

        float penX = startX, penY = startY;
        bool hasControl = false;
        float controlX = 0f, controlY = 0f;
        for (int step = 1; step <= n; step++)
        {
            OutlinePoint point = contour[(startIndex + step) % n];
            float x = point.X * scale;
            float y = point.Y * scale;
            if (point.OnCurve)
            {
                if (hasControl)
                {
                    AddQuadratic(lines, penX, penY, controlX, controlY, x, y);
                    hasControl = false;
                }
                else
                {
                    lines.Add((penX, penY, x, y));
                }
                penX = x;
                penY = y;
            }
            else if (hasControl)
            {
                // Two consecutive control points imply an on-curve point halfway between them.
                float midX = (controlX + x) * 0.5f;
                float midY = (controlY + y) * 0.5f;
                AddQuadratic(lines, penX, penY, controlX, controlY, midX, midY);
                penX = midX;
                penY = midY;
                controlX = x;
                controlY = y;
            }
            else
            {
                hasControl = true;
                controlX = x;
                controlY = y;
            }
        }
        if (hasControl)
        {
            AddQuadratic(lines, penX, penY, controlX, controlY, startX, startY);
        }
        else if (penX != startX || penY != startY)
        {
            lines.Add((penX, penY, startX, startY));
        }
    }

    private static void AddQuadratic(List<(float, float, float, float)> lines, float x0, float y0, float cx, float cy, float x1, float y1)
    {
        float ddx = x0 - 2f * cx + x1;
        float ddy = y0 - 2f * cy + y1;
        float deviationSquared = ddx * ddx + ddy * ddy;
        if (deviationSquared < 1f / 3f)
        {
            lines.Add((x0, y0, x1, y1));
            return;
        }
        int segments = 1 + (int)Math.Floor(Math.Sqrt(Math.Sqrt(Tolerance * deviationSquared)));
        float previousX = x0, previousY = y0;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float u = 1f - t;
            float x = u * u * x0 + 2f * u * t * cx + t * t * x1;
            float y = u * u * y0 + 2f * u * t * cy + t * t * y1;
            lines.Add((previousX, previousY, x, y));
            previousX = x;
            previousY = y;
        }
    }

    /// <summary>Adds the signed area of one line segment (bitmap space) to the accumulator.</summary>
    private static void DrawLine(float[] accumulator, int width, int height, float x0, float y0, float x1, float y1)
    {
        if (Math.Abs(y0 - y1) <= 1e-6f)
        {
            return;
        }
        float direction = 1f;
        if (y0 > y1)
        {
            direction = -1f;
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
        }
        // Rounding in the caller can push a coordinate a hair outside the bitmap.
        x0 = Math.Clamp(x0, 0f, width);
        x1 = Math.Clamp(x1, 0f, width);
        float dxdy = (x1 - x0) / (y1 - y0);
        float x = x0;
        int yStart = Math.Max(0, (int)y0);
        if (y0 < 0f)
        {
            x -= y0 * dxdy;
        }
        int yEnd = Math.Min(height, (int)Math.Ceiling(y1));
        for (int y = yStart; y < yEnd; y++)
        {
            int lineStart = y * width;
            float dy = Math.Min(y + 1f, y1) - Math.Max(y, y0);
            float xNext = x + dxdy * dy;
            float d = dy * direction;
            float left = Math.Min(x, xNext);
            float right = Math.Max(x, xNext);
            float leftFloor = (float)Math.Floor(left);
            int leftIndex = (int)leftFloor;
            float rightCeil = (float)Math.Ceiling(right);
            int rightIndex = (int)rightCeil;
            if (rightIndex <= leftIndex + 1)
            {
                // The segment stays within one pixel column on this row.
                float middle = 0.5f * (x + xNext) - leftFloor;
                accumulator[lineStart + leftIndex] += d - d * middle;
                accumulator[lineStart + leftIndex + 1] += d * middle;
            }
            else
            {
                float s = 1f / (right - left);
                float leftFraction = left - leftFloor;
                float a0 = 0.5f * s * (1f - leftFraction) * (1f - leftFraction);
                float rightFraction = right - rightCeil + 1f;
                float am = 0.5f * s * rightFraction * rightFraction;
                accumulator[lineStart + leftIndex] += d * a0;
                if (rightIndex == leftIndex + 2)
                {
                    accumulator[lineStart + leftIndex + 1] += d * (1f - a0 - am);
                }
                else
                {
                    float a1 = s * (1.5f - leftFraction);
                    accumulator[lineStart + leftIndex + 1] += d * (a1 - a0);
                    for (int xi = leftIndex + 2; xi < rightIndex - 1; xi++)
                    {
                        accumulator[lineStart + xi] += d * s;
                    }
                    float a2 = a1 + (rightIndex - leftIndex - 3) * s;
                    accumulator[lineStart + rightIndex - 1] += d * (1f - a2 - am);
                }
                accumulator[lineStart + rightIndex] += d * am;
            }
            x = xNext;
        }
    }
}
