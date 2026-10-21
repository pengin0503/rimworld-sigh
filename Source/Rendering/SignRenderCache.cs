using System;
using SimpleSigh.Text;

namespace SimpleSigh.Rendering;

/// <summary>Per-sign layout cache. Does not retain Thing or Map references.</summary>
public sealed class SignRenderCache
{
    private string source;
    private float width = float.NaN, height = float.NaN, lineHeight = float.NaN;
    private bool wrap;
    private SignLayoutResult last;

    public void Invalidate() { source = null; last = null; }

    public SignLayoutResult Get(string text, float availableWidth, float availableHeight, float measuredLineHeight,
        bool allowWrap, Func<string,float> widthCalculator)
    {
        if (last != null && source == text && width == availableWidth && height == availableHeight &&
            lineHeight == measuredLineHeight && wrap == allowWrap) return last;
        source = text;
        width = availableWidth;
        height = availableHeight;
        lineHeight = measuredLineHeight;
        wrap = allowWrap;
        last = SignLayout.Fit(text, width, height, widthCalculator, lineHeight, 0.35f, allowWrap);
        return last;
    }
}
