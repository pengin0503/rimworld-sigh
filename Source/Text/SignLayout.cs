using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SimpleSigh.Text;

public sealed class SignLayoutResult
{
    public IReadOnlyList<string> Lines { get; }
    public float Scale { get; }
    public bool Fits { get; }
    public SignLayoutResult(IReadOnlyList<string> lines, float scale, bool fits)
    { Lines = lines; Scale = scale; Fits = fits; }
}

/// <summary>Calculates wrapping and fitting without taking a dependency on Unity or Verse.</summary>
public static class SignLayout
{
    public static SignLayoutResult Fit(string text, float availableWidth, float availableHeight,
        Func<string,float> measureWidth, float lineHeight, float minScale, bool allowWrapping = true)
    {
        if (measureWidth == null) throw new ArgumentNullException(nameof(measureWidth));
        if (availableWidth <= 0 || availableHeight <= 0 || lineHeight <= 0 || minScale <= 0 || minScale > 1)
            return new SignLayoutResult(Array.Empty<string>(), minScale, false);
        text ??= string.Empty;
        if (text.Length == 0) return new SignLayoutResult(Array.Empty<string>(), 1f, true);
        // Text is short (64 or 256 grapheme clusters); recompute only when editing/resizing.
        for (int step = 0; step <= 40; step++)
        {
            float scale = Math.Max(minScale, 1f - step * 0.025f);
            var lines = Wrap(text, availableWidth / scale, measureWidth, allowWrapping);
            bool fits = lines.Count * lineHeight * scale <= availableHeight + 0.0001f &&
                        lines.All(line => measureWidth(line) * scale <= availableWidth + 0.0001f);
            if (fits) return new SignLayoutResult(lines, scale, true);
            if (scale <= minScale + 0.0001f) break;
        }
        var fallback = Wrap(text, availableWidth / minScale, measureWidth, allowWrapping);
        return new SignLayoutResult(fallback, minScale, false);
    }

    private static List<string> Wrap(string value, float maxWidth, Func<string,float> measureWidth, bool wrap)
    {
        var lines = new List<string>();
        foreach (string paragraph in value.Split('\n'))
        {
            if (!wrap) { lines.Add(paragraph); continue; }
            var clusters = new List<string>();
            var iterator = StringInfo.GetTextElementEnumerator(paragraph);
            while (iterator.MoveNext()) clusters.Add(iterator.GetTextElement());
            if (clusters.Count == 0) { lines.Add(string.Empty); continue; }
            string current = string.Empty;
            foreach (string cluster in clusters)
            {
                string proposed = current + cluster;
                if (current.Length > 0 && measureWidth(proposed) > maxWidth)
                {
                    lines.Add(current);
                    current = cluster;
                }
                else current = proposed;
            }
            lines.Add(current);
        }
        return lines;
    }
}
