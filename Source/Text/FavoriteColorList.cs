using System.Collections.Generic;

namespace SimpleSigh.Text;

/// <summary>Most-recent-first list of favourite text colours as packed 0xRRGGBBAA values.</summary>
public static class FavoriteColorList
{
    public const int MaxCount = 12;

    public static void Add(List<uint> colors, uint rgba)
    {
        colors.Remove(rgba);
        colors.Insert(0, rgba);
        if (colors.Count > MaxCount)
        {
            colors.RemoveRange(MaxCount, colors.Count - MaxCount);
        }
    }

    public static void Remove(List<uint> colors, uint rgba)
    {
        colors.Remove(rgba);
    }
}
