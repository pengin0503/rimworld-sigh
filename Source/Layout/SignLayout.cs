using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
/// <para>
/// Explicit line breaks are kept. Long paragraphs wrap at spaces (Latin) or between characters
/// (Japanese/CJK); a Latin word longer than a line is broken by character. Basic Japanese line
/// breaking rules (kinsoku) keep closing punctuation and small kana off the start of a line and
/// opening brackets off its end. The largest scale (up to <c>maxScale</c>) that fits is chosen.
/// </para>
/// </summary>
public static class SignLayout
{
    private const string NoLineStart = "、。，．・：；？！ー）」』】〕〉》｝］’”ゝゞヽヾぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮヵヶ々〻‐–—…‥,.!?;:)]}";
    private const string NoLineEnd = "（「『【〔〈《｛［‘“([{";
    /// <summary>Relative tolerance for float sums; small enough that a too-large scale is never accepted.</summary>
    private const float Tolerance = 1e-6f;
    private const int SearchSteps = 24;

    /// <param name="measureWidth">Width of a string at scale 1, in the same units as the panel. Must be additive per character.</param>
    /// <param name="lineHeight">Line height at scale 1.</param>
    /// <param name="maxScale">Upper bound for the scale, e.g. the user's text size setting.</param>
    /// <param name="wrap">False for single-line signs: only explicit line breaks start new lines.</param>
    /// <param name="allowWordBreaks">
    /// False to never break inside a Latin word (or a run of characters that must stay together):
    /// lines then end only at break opportunities and the scale shrinks until every word fits.
    /// </param>
    /// <param name="balanceLines">
    /// Wrapped text keeps the chosen scale and line count but evens out line lengths
    /// ("立入" / "禁止" instead of "立入禁" / "止"). Meant for short labels.
    /// </param>
    public static SignLayoutResult Fit(string text, float availableWidth, float availableHeight,
        Func<string, float> measureWidth, float lineHeight, float minScale, float maxScale = 1f, bool wrap = true,
        bool allowWordBreaks = true, bool balanceLines = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new SignLayoutResult(Array.Empty<string>(), maxScale, true);
        }

        List<Element[]> paragraphs = Tokenize(text, measureWidth);
        if (availableWidth <= 0f || availableHeight <= 0f || lineHeight <= 0f)
        {
            return new SignLayoutResult(Wrap(paragraphs, float.PositiveInfinity, out _, out _), minScale, false);
        }

        if (TryFitAt(paragraphs, availableWidth, availableHeight, lineHeight, maxScale, wrap, allowWordBreaks, out List<string> lines))
        {
            return Fitted(paragraphs, lines, availableWidth, maxScale, wrap && balanceLines, allowWordBreaks);
        }
        if (!TryFitAt(paragraphs, availableWidth, availableHeight, lineHeight, minScale, wrap, allowWordBreaks, out lines))
        {
            return new SignLayoutResult(lines, minScale, false);
        }

        float fitting = minScale;
        float failing = maxScale;
        List<string> best = lines;
        for (int step = 0; step < SearchSteps; step++)
        {
            float middle = (fitting + failing) * 0.5f;
            if (TryFitAt(paragraphs, availableWidth, availableHeight, lineHeight, middle, wrap, allowWordBreaks, out lines))
            {
                fitting = middle;
                best = lines;
            }
            else
            {
                failing = middle;
            }
        }
        return Fitted(paragraphs, best, availableWidth, fitting, wrap && balanceLines, allowWordBreaks);
    }

    private static SignLayoutResult Fitted(List<Element[]> paragraphs, List<string> lines, float width, float scale, bool balance, bool allowWordBreaks)
    {
        if (balance && lines.Count > 1)
        {
            lines = Balance(paragraphs, width / scale, lines.Count, allowWordBreaks);
        }
        return new SignLayoutResult(lines, scale, true);
    }

    /// <summary>The narrowest wrap width that still needs no more than <paramref name="lineCount"/> lines.</summary>
    private static List<string> Balance(List<Element[]> paragraphs, float lineWidth, int lineCount, bool allowWordBreaks)
    {
        float narrow = 0f;
        float wide = lineWidth;
        List<string> best = Wrap(paragraphs, lineWidth, out _, out _);
        for (int step = 0; step < SearchSteps; step++)
        {
            float middle = (narrow + wide) * 0.5f;
            List<string> lines = Wrap(paragraphs, middle, out _, out bool forced);
            if (lines.Count <= lineCount && (allowWordBreaks || !forced))
            {
                wide = middle;
                best = lines;
            }
            else
            {
                narrow = middle;
            }
        }
        return best;
    }

    /// <summary>Lines wrapped for a fixed scale; <see cref="SignLayoutResult.Fits"/> reports only the width check.</summary>
    public static SignLayoutResult FitAtScale(string text, float availableWidth, Func<string, float> measureWidth, float scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new SignLayoutResult(Array.Empty<string>(), scale, true);
        }
        List<Element[]> paragraphs = Tokenize(text, measureWidth);
        List<string> lines = Wrap(paragraphs, availableWidth / scale, out _, out _);
        bool fits = true;
        foreach (string line in lines)
        {
            fits &= Within(measureWidth(line) * scale, availableWidth);
        }
        return new SignLayoutResult(lines, scale, fits);
    }

    private static bool TryFitAt(List<Element[]> paragraphs, float width, float height, float lineHeight, float scale, bool wrap,
        bool allowWordBreaks, out List<string> lines)
    {
        float lineWidth = width / scale;
        lines = Wrap(paragraphs, wrap ? lineWidth : float.PositiveInfinity, out float widest, out bool forcedBreak);
        return (allowWordBreaks || !forcedBreak) && Within(widest * scale, width) && Within(lines.Count * lineHeight * scale, height);
    }

    private static bool Within(float value, float limit) => value <= limit * (1f + Tolerance);

    /// <param name="forcedBreak">True when a line had to end where no break is allowed (inside a word).</param>
    private static List<string> Wrap(List<Element[]> paragraphs, float maxWidth, out float widest, out bool forcedBreak)
    {
        var lines = new List<string>();
        widest = 0f;
        forcedBreak = false;
        foreach (Element[] paragraph in paragraphs)
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            int start = 0;
            while (start < paragraph.Length)
            {
                int end = start;
                float width = 0f;
                while (end < paragraph.Length && (end == start || Within(width + paragraph[end].Width, maxWidth)))
                {
                    width += paragraph[end].Width;
                    end++;
                }
                if (end < paragraph.Length)
                {
                    end = ChooseBreak(paragraph, start, end, out bool forced);
                    forcedBreak |= forced;
                }

                int visibleEnd = end;
                while (visibleEnd > start && paragraph[visibleEnd - 1].IsSpace)
                {
                    visibleEnd--;
                }
                lines.Add(Join(paragraph, start, visibleEnd, out float lineWidth));
                widest = Math.Max(widest, lineWidth);

                start = end;
                while (start < paragraph.Length && paragraph[start].IsSpace)
                {
                    start++;
                }
            }
        }
        return lines;
    }

    /// <summary>
    /// <paramref name="overflow"/> is the first element that did not fit. Returns where the line ends:
    /// the last allowed break opportunity, or a character break adjusted for kinsoku.
    /// </summary>
    private static int ChooseBreak(Element[] elements, int start, int overflow, out bool forcedBreak)
    {
        forcedBreak = false;
        for (int b = overflow; b > start; b--)
        {
            if (CanBreakBefore(elements, b))
            {
                return b;
            }
        }
        forcedBreak = true;
        int forced = overflow;
        while (forced - 1 > start && (IsNoLineStart(elements[forced]) || IsNoLineEnd(elements[forced - 1])))
        {
            forced--;
        }
        return forced;
    }

    private static bool CanBreakBefore(Element[] elements, int index)
    {
        Element previous = elements[index - 1];
        Element current = elements[index];
        if (IsNoLineStart(current) || IsNoLineEnd(previous))
        {
            return false;
        }
        return previous.IsSpace || current.IsSpace || previous.IsCjk || current.IsCjk;
    }

    private static bool IsNoLineStart(Element element) => element.Text.Length == 1 && NoLineStart.IndexOf(element.Text[0]) >= 0;

    private static bool IsNoLineEnd(Element element) => element.Text.Length == 1 && NoLineEnd.IndexOf(element.Text[0]) >= 0;

    private static string Join(Element[] elements, int start, int end, out float width)
    {
        var builder = new StringBuilder();
        width = 0f;
        for (int i = start; i < end; i++)
        {
            builder.Append(elements[i].Text);
            width += elements[i].Width;
        }
        return builder.ToString();
    }

    private static List<Element[]> Tokenize(string text, Func<string, float> measureWidth)
    {
        var widths = new Dictionary<string, float>();
        var paragraphs = new List<Element[]>();
        foreach (string paragraph in text.Split('\n'))
        {
            var elements = new List<Element>();
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(paragraph);
            while (enumerator.MoveNext())
            {
                string element = enumerator.GetTextElement();
                if (!widths.TryGetValue(element, out float width))
                {
                    width = measureWidth(element);
                    widths[element] = width;
                }
                elements.Add(new Element(element, width));
            }
            paragraphs.Add(elements.ToArray());
        }
        return paragraphs;
    }

    private readonly struct Element
    {
        public Element(string text, float width)
        {
            Text = text;
            Width = width;
            IsSpace = text == " " || text == "　";
            IsCjk = IsCjkChar(text[0]);
        }

        public string Text { get; }

        public float Width { get; }

        public bool IsSpace { get; }

        public bool IsCjk { get; }

        private static bool IsCjkChar(char c)
        {
            return (c >= '⺀' && c <= '鿿') || (c >= '가' && c <= '힯') || (c >= '豈' && c <= '﫿')
                || (c >= '＀' && c <= '￯') || (c >= '\uD840' && c <= '\uD87F');
        }
    }
}
