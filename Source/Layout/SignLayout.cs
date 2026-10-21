using System;
using System.Collections.Generic;

namespace SimpleSigh.Layout;

public sealed class SignLayoutResult
{
    public SignLayoutResult(IReadOnlyList<string> lines, float scale, bool fits)
    {
        Lines = lines;
        Scale = scale;
        Fits = fits;
    }

    /// <summary>Lines to draw, top to bottom.</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>Factor applied to the scale-1 line height and widths.</summary>
    public float Scale { get; }

    /// <summary>False when even <c>minScale</c> cannot keep the text inside the panel.</summary>
    public bool Fits { get; }
}

/// <summary>
/// Chooses lines and a text scale so text fits a panel. Pure: widths come from the caller's
/// measurement function so the same logic serves unit tests and the in-game glyph metrics.
/// </summary>
public static class SignLayout
{
    /// <param name="measureWidth">Width of a string at scale 1, in the same units as the panel.</param>
    /// <param name="lineHeight">Line height at scale 1.</param>
    public static SignLayoutResult Fit(string text, float availableWidth, float availableHeight,
        Func<string, float> measureWidth, float lineHeight, float minScale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new SignLayoutResult(Array.Empty<string>(), 1f, true);
        }

        string[] lines = text.Split('\n');
        float widest = 0f;
        foreach (string line in lines)
        {
            widest = Math.Max(widest, measureWidth(line));
        }

        float scale = 1f;
        if (widest > 0f)
        {
            scale = Math.Min(scale, availableWidth / widest);
        }
        if (lineHeight > 0f)
        {
            scale = Math.Min(scale, availableHeight / (lines.Length * lineHeight));
        }

        bool fits = scale >= minScale && availableWidth > 0f && availableHeight > 0f;
        return new SignLayoutResult(lines, fits ? scale : minScale, fits);
    }
}
