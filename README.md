# Simple Sigh (RimWorld 1.7)

木材・石材・金属で建てられるバニラ風の看板 MOD です。日本語・英数字・記号を自由に入力でき、
文字は浮動ラベルではなく **看板の表示面（パネル）の上に、ワールド空間のメッシュとして** 描画されます。

Vanilla-style signs built from wood, stone or metal. Type any text (Japanese, letters, digits, symbols);
it is drawn as a world-space mesh **on the sign's display panel**, not as a floating label.

| Item | Value |
| --- | --- |
| Name | Simple Sigh |
| packageId | `pengin0503.simplesigh` |
| Assembly | `1.7/Assemblies/SimpleSigh.dll` |
| Game version | RimWorld 1.7 only |
| Required mods | none (Harmony is **not** used) |

## Status — read this first

| Layer | State |
| --- | --- |
| XML / structure tests | pass (see `docs/testing/`) |
| Unit tests (game-independent C#) | pass |
| Compile against RimWorld 1.7 reference assemblies | pass |
| Smoke: load `SimpleSigh.dll` + 1.7 assemblies in .NET, real `ScribeSaver` write | pass |
| **In-game verification** | **NOT RUN** — no RimWorld executable was available during development |

A successful build does not prove the game loads or renders the mod. Every in-game case is listed as
`NOT RUN` in `docs/testing/m1-manual-checklist.md` (and the M2 checklist) until someone runs it.

## Features

- **Sign (1×1)** — standing sign, single line, up to 64 characters.
- Material (stuff) colours the frame; the display panel stays light so text remains readable.
- Select the sign → **Edit text / 文字を編集**: text, colour palette, size slider (30–100 % of the largest size that fits).
  Cancel never changes the sign. Overlong text and forbidden characters are rejected with a message, never truncated.
- **Copy / paste text** between signs.
- Text is fitted (shrunk) to the panel and clipped so it never leaves the board.
- Characters the current game font cannot draw are shown as `?` and noted in the inspect pane.
- Saves: text, colour and size are stored per sign; minify/reinstall keeps them.
- Rendering cost: one cached mesh per sign, rebuilt only when something visible changes; released on
  despawn, destroy, map removal and game change. Dev mode shows cache counters in the inspect pane.

## Install (for testing)

1. Build `SimpleSigh.dll` (below) so that `1.7/Assemblies/SimpleSigh.dll` exists.
2. Copy the repository folder into `RimWorld/Mods/SimpleSigh/` (About, 1.7, Defs, Languages, Textures are needed).
3. Enable **Simple Sigh** after Core and run through `docs/testing/m1-manual-checklist.md`.

## Build and test

Requirements: .NET SDK 10, Python 3, and RimWorld 1.7's `Assembly-CSharp.dll`, `Waylith.dll`, `UnityStubs.dll`.
Game assemblies are **never** committed or copied into the output.

```bash
export RIMWORLD_REFERENCE_DIR=/path/to/folder/with/Assembly-CSharp.dll   # or RIMWORLD_GAME_DIR=/path/to/RimWorld
tools/verify.sh            # structure tests, unit tests, Debug build, smoke
tools/verify.sh Release    # same with a Release build
```

Individual steps:

```bash
python3 -m unittest discover -s Tests/StructureTests -v
dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj
dotnet build Source/SimpleSigh.csproj -c Release
dotnet run --project Tests/SimpleSigh.GameSmoke -- "$PWD"
```

Textures are original procedural art: regenerate with `python3 tools/textures/generate_textures.py`.

## Layout

```
About/                 mod metadata
Defs/ThingDefs/        sign ThingDefs
Languages/             English and Japanese strings
Textures/              generated sign art (+ CutoutComplex masks)
Source/Text/           text rules, edit session, save codec (no game dependency)
Source/Layout/         panel geometry, fitting, glyph quads, render cache slot (no game dependency)
Source/Signs/          ThingComp, editor dialog, clipboard
Source/Rendering/      1.7 text backend adapter, mesh renderer, cache lifecycle
Tests/                 Python structure tests, xUnit tests, game-assembly smoke harness
docs/                  spec, plans, implementation log, test checklists
```

## Known limitations

- Whether Japanese glyphs are present in the game's font atlas when the game language is not Japanese
  depends on RimWorld 1.7's text backend, which is not part of the reference assemblies. If they are
  missing you see `?` marks (documented, not silent).
- Emoji and complex scripts are out of scope; they render as `?`.
- Text keeps one colour per sign.

## Development records

- `docs/development/implementation-log.md` — technical decisions with evidence.
- `docs/testing/` — checklists and acceptance records (build vs. in-game kept separate).
