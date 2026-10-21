using Verse;

namespace SimpleSigh.Signs;

public sealed class CompProperties_SignText : CompProperties
{
    public int maxElements = 64;
    public bool multiline;
    public CompProperties_SignText() => compClass = typeof(CompSignText);
}
