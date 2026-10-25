using System;
using System.Collections.Generic;

namespace SimpleSigh.Text;

/// <summary>Text limits of one sign, or of several signs edited together.</summary>
public readonly struct SignEditRules
{
    public SignEditRules(int maxElements, bool multiline)
    {
        MaxElements = maxElements;
        Multiline = multiline;
    }

    public int MaxElements { get; }

    public bool Multiline { get; }

    /// <summary>
    /// Whether Enter confirms the editor: only single-line editors, and never while an IME is composing text, so
    /// pressing Enter to accept a kana-kanji conversion cannot close the editor (#10). Multiline editors take Enter as
    /// a line break.
    /// </summary>
    public static bool EnterConfirms(bool multiline, string imeComposition) => !multiline && string.IsNullOrEmpty(imeComposition);

    /// <summary>
    /// Rules that every target accepts: the smallest limit, and line breaks only if all targets allow them.
    /// Text valid under the result is therefore valid for each target, so nothing is truncated on save.
    /// </summary>
    public static SignEditRules Combine(IEnumerable<SignEditRules> targets)
    {
        int max = int.MaxValue;
        bool multiline = true;
        bool any = false;
        foreach (SignEditRules target in targets)
        {
            any = true;
            max = Math.Min(max, target.MaxElements);
            multiline &= target.Multiline;
        }
        if (!any)
        {
            throw new ArgumentException("At least one sign is required.", nameof(targets));
        }
        return new SignEditRules(max, multiline);
    }
}
