using Verse;

namespace SimpleSigh.Rendering;

/// <summary>
/// RimWorld creates one instance per Game (new or loaded). Signs of the previous game are never
/// despawned when another save is loaded, so their meshes are released here, and the sign font forgets
/// the previous game's characters so the glyph atlas cannot grow across games.
/// </summary>
public class GameComponent_SignRenderCleanup : GameComponent
{
    public GameComponent_SignRenderCleanup(Game game)
    {
        SignRenderCache.ReleaseAll();
        SignFont.ResetForNewGame();
    }
}
