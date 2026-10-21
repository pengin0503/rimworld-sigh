using System;
using System.Globalization;

namespace SimpleSigh.Text;

/// <summary>Game-independent validation; do not truncate text on save.</summary>
public static class SignTextRules
{
    public static bool TryNormalize(string input, int maxElements, bool multiline, out string normalized, out string errorKey)
    {
        normalized = null;
        errorKey = "SimpleSigh_InvalidText";
        if (input == null || maxElements < 0) return false;
        string value = input.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!multiline && value.IndexOf('\n') >= 0) return false;
        foreach (char c in value)
        {
            if (char.IsControl(c) && c != '\n') return false;
        }
        if (new StringInfo(value).LengthInTextElements > maxElements) return false;
        normalized = value;
        errorKey = null;
        return true;
    }
}
