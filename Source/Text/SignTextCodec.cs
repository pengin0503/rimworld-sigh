using System.Text;

namespace SimpleSigh.Text;

/// <summary>
/// Reversible encoding for sign text stored in RimWorld saves.
/// <para>
/// 1.7 loads strings via <c>ParseHelper.ParseString</c>, which rewrites the two characters
/// <c>\n</c> into a newline, and via an <c>XmlDocument</c> that discards whitespace-only
/// text. The encoded form therefore contains no backslash and no newline, and is wrapped
/// in <c>|</c> so it never starts or ends with whitespace. Everything else (including
/// Japanese) is stored verbatim so saves stay readable.
/// </para>
/// </summary>
public static class SignTextCodec
{
    private const char Wrapper = '|';

    public static string Encode(string text)
    {
        var builder = new StringBuilder((text?.Length ?? 0) + 2);
        builder.Append(Wrapper);
        foreach (char c in text ?? string.Empty)
        {
            switch (c)
            {
                case '%': builder.Append("%25"); break;
                case '\\': builder.Append("%5C"); break;
                case '\n': builder.Append("%0A"); break;
                case '\r': builder.Append("%0D"); break;
                default: builder.Append(c); break;
            }
        }
        builder.Append(Wrapper);
        return builder.ToString();
    }

    public static string Decode(string stored)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return string.Empty;
        }
        if (stored.Length < 2 || stored[0] != Wrapper || stored[stored.Length - 1] != Wrapper)
        {
            return stored;
        }

        string body = stored.Substring(1, stored.Length - 2);
        var builder = new StringBuilder(body.Length);
        for (int i = 0; i < body.Length; i++)
        {
            if (body[i] == '%' && i + 2 < body.Length && TryDecodeEscape(body[i + 1], body[i + 2], out char decoded))
            {
                builder.Append(decoded);
                i += 2;
            }
            else
            {
                builder.Append(body[i]);
            }
        }
        return builder.ToString();
    }

    private static bool TryDecodeEscape(char high, char low, out char decoded)
    {
        decoded = (high, low) switch
        {
            ('2', '5') => '%',
            ('5', 'C') => '\\',
            ('0', 'A') => '\n',
            ('0', 'D') => '\r',
            _ => '\0',
        };
        return decoded != '\0';
    }
}
