# Simple Sigh — M2 results (build and automated checks)

Date: 2026-10-21. Same environment as `m1-acceptance.md` (reference assemblies only, no game).

| Check | Command | Result |
| --- | --- | --- |
| Structure / XML incl. Core cross-reference | `RIMWORLD_REFERENCE_DIR=<refs> python3 -m unittest discover -s Tests/StructureTests` | 28 tests, OK |
| Unit tests | `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj` | 116 passed, 0 failed |
| Build (Debug, `-warnaserror`) | `dotnet build Source/SimpleSigh.csproj -warnaserror` | succeeded, 0 warnings |
| Smoke (DLL + 1.7 assemblies; Def fields/placeWorkers by reflection; real ScribeSaver write) | `dotnet run --project Tests/SimpleSigh.GameSmoke -- "$PWD"` | SMOKE PASS |
| Offline layout preview | `python3 tools/preview/render_preview.py docs/images/offline-layout-preview.png` | text inside panels for all defs/rotations (visual inspection) |
| In-game checklist (W*, N*, Q*) | — | **NOT RUN (no game)** |

## Problems found and fixed during M2 Task 4 (by test or review, not in game)

| Problem | How found | Fix | Regression test |
| --- | --- | --- | --- |
| Size slider had a dead zone for long text (50 % of panel height can exceed the fitted size) | `SignTextLayoutTests.UserScaleShrinksText` failed | Two-pass fit: size is a fraction of the largest fitting size, then refit | `UserScaleShrinksText` |
| Per-frame closure allocation in `SignRenderCache.Get` (one per visible sign per frame) | Code review of the draw path | Allocation-free `SignRenderSlot.TryGet` hit path | `TryGetHitDoesNotAllocate`, `BuildingAndComparingKeysDoesNotAllocate` |
| Renderer and tests duplicated the layout maths | Review | `SignTextLayout.Build` shared by renderer, tests and preview | `SignTextLayoutTests` |
