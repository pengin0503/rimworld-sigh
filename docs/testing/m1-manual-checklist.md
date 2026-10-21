# Simple Sigh — M1 in-game checklist (RimWorld 1.7)

Status values: `PASS` / `FAIL` / `NOT RUN`. A build or unit-test pass is **not** an in-game pass.
No RimWorld executable was available while implementing, so every row below is `NOT RUN`
until someone runs it in the real game and records the result.

Setup for a tester: copy the repository folder (with `1.7/Assemblies/SimpleSigh.dll` built,
see README) into `RimWorld/Mods/`, enable **Simple Sigh** after Core, and start a dev-mode colony.

## Build and edit (Task 4)

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| B1 | Game starts with Simple Sigh enabled | No red errors mentioning SimpleSigh in the log | NOT RUN |
| B2 | Architect → Furniture → Sign, choose Wood / Granite / Steel | Blueprint uses the chosen material; frame is tinted, panel stays light | NOT RUN |
| B3 | Colonist builds each material | Built sign shows the stuff colour on frame and posts | NOT RUN |
| B4 | Select sign → "Edit text" → type `研究室` → OK | Dialog closes; sign text updated | NOT RUN |
| B5 | Edit → type something → Cancel / X / Esc | Sign keeps previous text | NOT RUN |
| B6 | Type 65 characters | Counter turns red, OK shows "too long" message and keeps dialog open | NOT RUN |
| B7 | Paste text containing a line break into the 1×1 sign | Rejected with "line breaks cannot be used" | NOT RUN |
| B8 | Change colour and size, OK | Values stored (visible once rendering is in) | NOT RUN |
| B9 | Copy text from sign A, paste onto sign B | B gets A's text, colour and size | NOT RUN |
| B10 | Non-player sign (e.g. dev-spawned for another faction) | No edit/copy/paste gizmos | NOT RUN |

## Persistence (Task 4)

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| P1 | Save, reload: `研究室`, `Wood A-12`, `発電設備 24h` | Same text, colour, size after load | NOT RUN |
| P2 | Save, reload: `C:\new` and `   ` (three spaces) | Exactly the same text (codec guards against ParseString/XmlDocument) | NOT RUN |
| P3 | Minify (uninstall) the sign, reinstall elsewhere | Text, colour, size preserved | NOT RUN |
| P4 | Deconstruct the sign | No error; resources returned | NOT RUN |
| P5 | Add mod to an existing save | Save loads; signs can be built | NOT RUN |

## Rendering on the panel (Task 5)

Implementation: `SignTextRenderer` builds glyph quads from the 1.7 text backend
(`UnityStubs.Gui.TextMeshProBackend`, medium UI font), lays them out inside `panelRect` minus
padding (`SignLayout`), drops any quad outside the panel (`GlyphQuadBuilder.ClipTo`), and draws one
mesh with the `WorldText` shader at render queue 3000. Unit tests cover layout, clipping, glyph
replacement and UV orientation; the actual pixels have **not** been seen in game.

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| R1 | `研究室` on wood sign, normal zoom | Japanese glyphs legible, centred on the light panel, not floating | NOT RUN |
| R2 | `Wood A-12`, `発電設備 24h` | Shrinks to panel width, stays inside panel | NOT RUN |
| R3 | 64 × `W` | Very small but inside the panel | NOT RUN |
| R4 | Zoom fully in / fully out | Text scales with the sign (world space), stays on panel | NOT RUN |
| R5 | UI scale 100 % and 125 % | No change to world text placement | NOT RUN |
| R6 | Text orientation | Reads left→right, not mirrored or upside down (UV orientation copied from UnityStubs TextMeshPro) | NOT RUN |
| R7 | Night / dark room | Text darkens with the map (queue 3000 < LightOverlay 3100) | NOT RUN |
| R8 | Fogged cell | Text not drawn | NOT RUN |
| R9 | Pawn walking in front of the sign | Pawn not hidden behind text | NOT RUN |
| R10 | Language English with Japanese text | If the English font atlas lacks CJK, `?` marks appear and the inspect pane says so (not silently blank) | NOT RUN |
| R11 | Colour change in editor | Text colour on panel updates once | NOT RUN |
| R12 | Size slider 30 % | Smaller centred text | NOT RUN |

Known risk: whether the 1.7 font atlas contains Japanese glyphs when the game language is not Japanese
is decided by the game's text backend implementation, which is not in the reference assemblies.

## Cache lifecycle (Task 6)

Unit-tested (pure `SignRenderSlot`/`SignRenderKey`): 10 000 unchanged frames → 1 build; any change of
text, colour, size, rotation, atlas version, font or panel geometry → exactly one rebuild with the old
mesh released; release is idempotent; "nothing to draw" is cached. In game, enable **Dev mode** and select
a sign: the inspect pane shows `[dev] Simple Sigh render cache: builds / releases / live / registered`.

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| C1 | One sign with text left alone for 10 in-game minutes | `builds` does not increase | NOT RUN |
| C2 | Edit text once / colour once | `builds` +1 each, `live` unchanged | NOT RUN |
| C3 | 20 signs on screen, then 100+ signs | `live` = number of drawn signs with text; no growth while idle; FPS noted | NOT RUN |
| C4 | Minify a sign | `live` −1 (PostDeSpawn) ; reinstall → same text redrawn, `live` +1 | NOT RUN |
| C5 | Deconstruct / destroy a sign | `live` −1 | NOT RUN |
| C6 | Load another save | `live` restarts from the new game's signs (GameComponent cleanup) | NOT RUN |
| C7 | Abandon / remove a map with signs | `live` drops (notifyMapRemoved) | NOT RUN |
| C8 | Save → load | Identical text drawn; no stale material | NOT RUN |

Figures for C1–C3 must be measured in game; none were invented here.
