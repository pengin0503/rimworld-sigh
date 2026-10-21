using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SimpleSigh.Text;

/// <summary>
/// Game-independent validation of sign text. Text is never truncated or silently
/// altered beyond newline normalization: invalid input is rejected with a stable
/// translation key so the editor can explain why.
/// </summary>
public static class SignTextRules
{
    public const string InvalidTextKey = "SimpleSigh_InvalidText";
    public const string TooLongKey = "SimpleSigh_TooLong";
    public const string NewlineNotAllowedKey = "SimpleSigh_NewlineNotAllowed";

    public static bool TryNormalize(string input, int maxElements, bool multiline, out string normalized, out string errorKey)
    {
        normalized = null;
        if (input == null || !IsXmlSafe(input))
        {
            errorKey = InvalidTextKey;
            return false;
        }

        bool hasNewline = input.IndexOf('\n') >= 0 || input.IndexOf('\r') >= 0;
        if (hasNewline && !multiline)
        {
            errorKey = NewlineNotAllowedKey;
            return false;
        }

        string candidate = hasNewline ? input.Replace("\r\n", "\n").Replace('\r', '\n') : input;
        if (CountElements(candidate) > maxElements)
        {
            errorKey = TooLongKey;
            return false;
        }

        normalized = candidate;
        errorKey = null;
        return true;
    }

    /// <summary>Number of user-perceived characters (extended grapheme clusters).</summary>
    public static int CountElements(string text)
    {
        return string.IsNullOrEmpty(text) ? 0 : new StringInfo(text).LengthInTextElements;
    }

    /// <summary>
    /// Saves are XML 1.0: only TAB-free printable text plus line breaks is accepted.
    /// TAB is rejected because the sign renderer has no tab stops.
    /// </summary>
    private static bool IsXmlSafe(string text)
    {
        foreach (Rune rune in EnumerateRunesStrict(text))
        {
            int value = rune.Value;
            if (value == '\n' || value == '\r')
            {
                continue;
            }
            if (value < 0x20 || (value >= 0x7F && value <= 0x9F) || value == 0xFFFE || value == 0xFFFF)
            {
                return false;
            }
        }
        return true;
    }

    private static IEnumerable<Rune> EnumerateRunesStrict(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (Rune.DecodeFromUtf16(text.AsSpan(index), out Rune rune, out int consumed) != OperationStatus.Done)
            {
                // Lone surrogate: report as a control character so it is rejected.
                yield return new Rune(0);
                yield break;
            }
            yield return rune;
            index += consumed;
        }
    }
}
