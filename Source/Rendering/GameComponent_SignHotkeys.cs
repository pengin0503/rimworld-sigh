using RimWorld;
using Verse;

namespace SimpleSigh.Rendering;

/// <summary>Handles the "toggle sign text" key binding while a game is running.</summary>
public class GameComponent_SignHotkeys : GameComponent
{
    private static KeyBindingDef toggleKey;

    public GameComponent_SignHotkeys(Game game)
    {
    }

    public override void GameComponentOnGUI()
    {
        toggleKey ??= DefDatabase<KeyBindingDef>.GetNamedSilentFail("SimpleSigh_ToggleSignText");
        // Not while a dialog takes input (e.g. typing a letter bound to this key in the sign editor).
        if (toggleKey != null && !Find.WindowStack.AnyWindowAbsorbingAllInput && toggleKey.KeyDownEvent)
        {
            UnityEngine.Event.current.Use();
            SimpleSighSettings settings = SimpleSighMod.Settings;
            settings.showText = !settings.showText;
            SimpleSighMod.SaveSettings();
            Messages.Message((settings.showText ? "SimpleSigh_TextShown" : "SimpleSigh_TextHidden").Translate(),
                MessageTypeDefOf.SilentInput, historical: false);
        }
    }
}
