using SimpleSigh.Text;
using Verse;

namespace SimpleSigh.Signs;

/// <summary>Turns a <see cref="SignTextRules"/> error key into a translated message.</summary>
public static class SignErrorText
{
    public static string Describe(string errorKey, int count, int max)
    {
        if (errorKey == SignTextRules.TooLongKey)
        {
            return "SimpleSigh_TooLong".Translate(count, max);
        }
        if (errorKey == SignTextRules.NewlineNotAllowedKey)
        {
            return "SimpleSigh_NewlineNotAllowed".Translate();
        }
        return "SimpleSigh_InvalidText".Translate();
    }
}
