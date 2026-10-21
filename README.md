# Simple Sigh (RimWorld 1.7)

Work-in-progress vanilla-style material-based signs with editable text. Repo: `pengin0503/rimworld-sigh`.

## Status

**Not validated as playable.** XML structure tests passed in a container, but .NET 10 compilation and the RimWorld 1.7 client are not available in that environment. The renderer is a screen-space prototype awaiting Waylith world-space rendering / Japanese glyph validation. Wall placement also requires in-game verification.

## Building locally

Install .NET 10 SDK and RimWorld 1.7. Set `RIMWORLD_GAME_DIR` to a directory containing `Assembly-CSharp.dll`, `UnityStubs.dll` and `Waylith.dll`, e.g. on PowerShell:

```powershell
$env:RIMWORLD_GAME_DIR = 'C:\Path\To\RimWorld'
dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj
dotnet build Source/SimpleSigh.csproj -c Release
```

Do not commit or redistribute game DLLs. `1.7/Assemblies/SimpleSigh.dll` is produced only by a successful build. Do not claim M1/M2 success until the game checklists pass.
