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
