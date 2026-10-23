using SimpleSigh.Rendering;
using Verse;

namespace SimpleSigh;

/// <summary>
/// Created by RimWorld when the mod loads; records the mod folder so the bundled sign font can be found,
/// and owns the player's settings.
/// </summary>
public class SimpleSighMod : Mod
{
    private static SimpleSighMod instance;

    public SimpleSighMod(ModContentPack content) : base(content)
    {
        instance = this;
        SignFont.ModRootDir = content.RootDir;
    }

    /// <summary>The player's settings; defaults when the mod object does not exist (e.g. in tools).</summary>
    public static SimpleSighSettings Settings => instance?.GetSettings<SimpleSighSettings>() ?? FallbackSettings;

    private static readonly SimpleSighSettings FallbackSettings = new SimpleSighSettings();

    public static void SaveSettings()
    {
        instance?.WriteSettings();
    }
}
