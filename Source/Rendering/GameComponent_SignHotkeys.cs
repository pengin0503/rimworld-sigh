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
        // Not while a dialog takes input, and not while any text field has keyboard focus: game components get key
        // events before windows do, so typing a letter bound to this key in a tab's search field (which does not absorb
        // input) would otherwise toggle the text and swallow the letter.
        if (toggleKey != null && !Find.WindowStack.AnyWindowAbsorbingAllInput && UnityEngine.GUIUtility.keyboardControl == 0
            && toggleKey.KeyDownEvent)
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
