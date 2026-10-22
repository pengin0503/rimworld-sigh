using SimpleSigh.Rendering;
using Verse;

namespace SimpleSigh;

/// <summary>Created by RimWorld when the mod loads; records the mod folder so the bundled sign font can be found.</summary>
public class SimpleSighMod : Mod
{
    public SimpleSighMod(ModContentPack content) : base(content)
    {
        SignFont.ModRootDir = content.RootDir;
    }
}
