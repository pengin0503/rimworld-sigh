using System.Globalization;
using System.Text;

namespace SimpleSigh.Text;

/// <summary>
/// Text search for the sign list: ignores letter case, full/half width (NFKC) and the difference between
/// hiragana and katakana, and treats line breaks as spaces.
/// </summary>
public static class SignSearch
{
    public static bool Matches(string text, string query)
    {
        string needle = Fold(query).Trim();
        if (needle.Length == 0)
        {
            return true;
        }
        return Fold(text).Contains(needle);
    }

    /// <summary>The first line of a sign's text, shortened to <paramref name="maxElements"/> characters.</summary>
    public static string FirstLine(string text, int maxElements = 40)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        int newline = text.IndexOf('\n');
        string line = newline >= 0 ? text.Substring(0, newline) : text;
        var info = new StringInfo(line);
        return info.LengthInTextElements > maxElements ? info.SubstringByTextElements(0, maxElements) + "…" : line;
    }

    private static string Fold(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        string normalized = text.Replace('\r', ' ').Replace('\n', ' ').Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            // Katakana ァ (U+30A1) … ヶ (U+30F6) to the matching hiragana.
            builder.Append(c >= 'ァ' && c <= 'ヶ' ? (char)(c - 0x60) : c);
        }
        return builder.ToString();
    }
}
