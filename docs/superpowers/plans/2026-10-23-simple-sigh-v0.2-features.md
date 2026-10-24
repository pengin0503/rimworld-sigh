# Simple Sigh v0.2 — Features, Workshop Packaging Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> This plan is executed **inline (Native, no subagents)** per the user's standing instruction.

**Goal:** Ship v0.2.0: confirm the Japanese fix (#4/#5 from PR #7), make the mod Workshop/RimSort-ready, and add editor, settings, list, 3×1, floor-sign, paint and LED-effect features (user request 2026-10-23: everything proposed except wall-sign side art).

**Architecture:** Keep the split that already works: pure, unit-tested logic in `Source/Layout` and `Source/Text`; thin RimWorld adapters in `Source/Signs`/`Source/Rendering`/`Source/UI`. A new pure `SignTextStyle` value carries every per-sign display option (alignment, size, auto-wrap, shadow, effect) so layout, render keys, saving and the editor share one definition. No Harmony: every hook is a public virtual or a Def (MainButtonDef, KeyBindingDef, ModSettings).

**Tech Stack:** RimWorld 1.7 (Waylith), .NET 10 / C# 14, xUnit, Python 3 unittest, Pillow (art/preview only).

**Spec:** `docs/superpowers/specs/2026-10-21-rimworld-sigh-design.md` (vision) + the user's 2026-10-23 answers: personal use, Japanese UI language, go beyond §8 (LED effects) when it makes a better mod, keep balance lenient, implement items 1–9 + LED extras + M4, not wall-sign side art.

## Global Constraints

- packageId `pengin0503.simplesigh`, DLL `SimpleSigh.dll`, RimWorld `1.7` only; existing defNames and save keys (`simpleSighText`, `simpleSighColor`, `simpleSighScale`, `simpleSighAutoWrap`) never change.
- New save keys are additive with defaults that reproduce v0.1 behaviour (old saves load unchanged).
- No game DLLs/data/OS fonts in Git; only the OFL-licensed bundled font. No `PublishedFileId.txt` with an invented ID.
- TDD per task; `tools/verify.sh` (public refs) and `RIMWORLD_REFERENCE_DIR=… tools/verify.sh` (smoke) both green before each commit; committed Release DLL must match the source.
- In-game results are reported only as NOT RUN unless the user reports them.

## Review Focus

1. Old saves (v0.1 signs) must keep identical appearance: new fields absent → centre/middle, no shadow, static effect — pinned by `SignTextStyleTests.DefaultsMatchV01` and the smoke save test.
2. Multi-sign edit across sign types with different limits (64 vs 256, single vs multi-line) must never store text that violates a target's own rules — `SignEditTargetsTests.UsesStrictestRules`.
3. Scrolling LED text must never draw outside the panel, including glyphs entering/leaving — `MarqueeTests.ClipsGlyphsAtPanelEdges`.
4. Alignment with long wrapped text must still clip to the panel and keep every glyph — `SignTextLayoutTests.AlignmentKeepsAllGlyphsInside`.
5. Floor signs over light floors must stay legible — shadow defaults on for floor signs (`structure: floor signs enable shadow`) and contrast test covers shadow colour.

---

### Task 1: Workshop / RimSort packaging

**Files:** Modify `About/About.xml`; Create `About/Preview.png`, `About/ModIcon.png`, `tools/textures/generate_about_images.py`; Modify `tools/package_release.py`, `Tests/PackagingTests/test_package_release.py`, `Tests/StructureTests/test_mod_structure.py`, `README.md`.

**Interfaces:** Produces `modVersion` = `0.2.0`; runtime ZIP includes `About/Preview.png`, `About/ModIcon.png`.

- [ ] Test: structure test `test_about_has_workshop_fields` asserts `modVersion` matches `\d+\.\d+\.\d+`, `About/Preview.png` exists (≤ 1 MB, 16:9 or 1:1), `About/ModIcon.png` exists (square), and **no** `About/PublishedFileId.txt` in the repo. Packaging test asserts both images are in the ZIP.
- [ ] Run: `python3 -m unittest discover -s Tests/StructureTests` → FAIL (files missing).
- [ ] Generate original images (preview built from the offline layout preview tiles; icon = basic sign with "看板"), add `<modVersion>0.2.0</modVersion>`, update packager allow-list.
- [ ] Run structure + packaging tests → PASS. README section "Workshop / RimSort": RimSort's "Missing Publish Field ID" is expected for an unpublished local mod (use RimSort's *Ignore*); uploading from RimWorld (dev mode → Upload) creates the real ID file.
- [ ] Commit `build: prepare Workshop/RimSort metadata, preview and icon`.

### Task 2: Japanese glyph coverage check (#4 regression guard)

**Files:** Create `Tests/SimpleSigh.Text.Tests/BundledFontCoverageTests.cs`; Modify test csproj (copy font as test content).

**Interfaces:** Consumes `SimpleSigh.Fonts.TrueTypeFont(byte[])`, `GetGlyphIndex(char)`.

- [ ] Test `CoversKanaKanjiAndSymbolsUsedOnSigns`: every char of hiragana U+3041–3096, katakana U+30A1–30FA, full-width ASCII U+FF01–FF5E, `、。・ー「」（）！？` and a fixed list of 200 common sign kanji has glyph index ≠ 0. Test `AtlasRasterizesJapaneseSignText`: `GlyphAtlas.Ensure("研究室 食料保管庫 立入禁止")` then `TryGetGlyph` is found for each.
- [ ] Run → expected PASS on first run is acceptable *only* because this guards already-merged code; verify RED by temporarily pointing the test at a Latin-only char set? No — RED is shown by asserting a deliberately missing char (`U+E000` private use) returns 0 in the same test.
- [ ] One-off local check (not committed): count chars of the game's Japanese localisation strings missing from the font; record the number in the implementation log.
- [ ] Commit `test: guard bundled-font coverage of Japanese sign text`.

### Task 3: `SignTextStyle` + alignment + shadow in the layout

**Files:** Create `Source/Layout/SignTextStyle.cs`, `Tests/SimpleSigh.Text.Tests/SignTextStyleTests.cs`; Modify `Source/Layout/SignTextLayout.cs`, `Source/Layout/GlyphQuadBuilder.cs`, `Source/Layout/SignRenderKey.cs`, tests.

**Interfaces — Produces:**
- `enum SignHAlign { Left, Center, Right }`, `enum SignVAlign { Top, Middle, Bottom }`, `enum SignEffect { None, Blink, Scroll }`.
- `readonly struct SignTextStyle(float scale, bool autoWrap, SignHAlign h, SignVAlign v, bool shadow, SignEffect effect)` with `static SignTextStyle Default` (= 1, true, Center, Middle, false, None) and value equality.
- `SignTextLayout.Build(string text, PanelPlacement panel, float padding, bool multiline, SignTextStyle style, IGlyphSource glyphs)` (old overload kept, forwarding with `Default`).
- `GlyphQuadBuilder.Build(lines, source, worldLineHeight, SignHAlign align, float halfWidth)`; vertical offset applied in `SignTextLayout`.
- `SignRenderKey(text, rgba, rotation, generation, sourceId, layoutHash, SignTextStyle style)`.

- [ ] Tests: `DefaultsMatchV01` (Build with Default == old overload, quad-for-quad); `LeftAlignStartsAtInnerLeftEdge` (min X0 ≈ −innerHalfWidth + glyph X0·k); `RightAlignEndsAtInnerRightEdge`; `TopAlignTouchesInnerTop`; `BottomAlignTouchesInnerBottom`; `AlignmentKeepsAllGlyphsInside` (256-char notice, every combination, all quads inside panel, quad count unchanged vs centred); `RenderKeyChangesWithEveryStyleField`.
- [ ] Run → FAIL (types missing). Implement. Run full xUnit → PASS. Commit `feat: per-sign text alignment and style value`.

### Task 4: Comp, save, editor (alignment, colour picker, favourites, OS clipboard, preview, scroll, multi-edit)

**Files:** Modify `Source/Signs/CompSignText.cs`, `Source/Signs/Dialog_EditSign.cs`, `Source/Signs/SignClipboard.cs`, `Source/Rendering/SignRenderCache.cs`, `Source/Rendering/SignTextRenderer.cs`; Create `Source/Signs/Dialog_SignColorPicker.cs`, `Source/Signs/SignEditTargets.cs` (pure-ish rules part in `Source/Text/SignEditRules.cs`), `Source/UI/SignPreviewDrawer.cs`, tests `SignEditRulesTests.cs`; smoke: style save round trip.

**Interfaces:**
- Save keys: `simpleSighHAlign` (Center), `simpleSighVAlign` (Middle), `simpleSighShadow` (false unless `Props.defaultShadow`), `simpleSighEffect` (None).
- `CompSignText.Style` (SignTextStyle), `CompSignText.Apply(string text, Color color, SignTextStyle style)`; old 4-arg `Apply` kept.
- `SignEditRules.Combine(IEnumerable<(int maxElements, bool multiline)>) → (int maxElements, bool multiline)` = (min, all).
- `Dialog_SignColorPicker : RimWorld.Dialog_ColorPickerBase` with callback `Action<Color>`; palette = editor palette + favourites.
- Favourites stored in `SimpleSighSettings.favoriteColors` (Task 5); editor buttons "★ save colour", favourites row.
- OS clipboard: editor buttons "Paste from clipboard" / "Copy to clipboard" via `GUIUtility.systemCopyBuffer` (1.7 stub → GLFW clipboard); pasted text goes through `SignTextRules` (rejects/flags, never truncates).
- Multi-edit: Edit and Copy commands set `alsoClickIfOtherInGroupClicked = false`; Edit gathers all selected player signs; dialog title "Edit N signs"; confirm applies to each.
- Preview: draws the sign panel colour + glyph quads from `ISignGlyphs.Atlas` with `GUI.DrawTextureWithTexCoords`, using `SignTextLayout.Build` (same maths as the map).
- Multi-line text box inside `Widgets.BeginScrollView`.

- [ ] Tests (xUnit): `UsesStrictestRules`; `SingleTargetKeepsOwnRules`. Smoke: `Style survives ScribeSaver write` (keys present when non-default, absent when default).
- [ ] Run → FAIL. Implement. Build `-warnaserror`, verify both modes → PASS. Commit `feat: richer sign editor (alignment, colour picker, favourites, clipboard, preview, multi-edit)`.

### Task 5: Mod settings and text toggle

**Files:** Create `Source/SimpleSighSettings.cs`, `Defs/Misc/KeyBindings.xml`, `Source/Rendering/GameComponent_SignHotkeys.cs` (or extend cleanup component); Modify `Source/SimpleSighMod.cs`, `CompSignText.PostDraw`, languages; pure `SignVisibility.ZoomAllows(int currentZoom, int hideFrom)`.

**Interfaces:** `SimpleSighSettings : ModSettings` fields `showText=true`, `hideTextFromZoom=-1` (never; else `CameraZoomRange` index 2..4), `useCustomDefaultColor=false`, `customDefaultColor`, `defaultAutoWrap=true`, `favoriteColors` (≤ 12). `SimpleSighMod.Settings` static. KeyBindingDef `SimpleSigh_ToggleSignText` (category `MainTabs`, default none) toggles `showText` with a message.

- [ ] Tests: `ZoomAllows` truth table (−1 always true; hideFrom=3 hides at 3,4 only). Structure: KeyBindingDef parses, category exists in Core.
- [ ] Implement; new signs take `customDefaultColor` in `PostPostMake` only (never recolour existing signs). Commit `feat: mod settings, zoom-based hiding and a text toggle hotkey`.

### Task 6: Sign list tab

**Files:** Create `Source/UI/MainTabWindow_Signs.cs`, `Defs/MainButtonDefs/SimpleSigh_Signs.xml`, `Textures/UI/SimpleSigh_SignsTab.png`, pure `Source/Text/SignSearch.cs` + tests.

**Interfaces:** `SignSearch.Matches(string text, string query)` — case-insensitive, width-insensitive (Ａ=A), kana-insensitive (ひらがな=カタカナ), empty query matches all. MainButtonDef `SimpleSigh_Signs` (`minimized=true`, iconPath, order 100). Window lists player signs on the current map (label, text first line, position), search box, "Jump" (`CameraJumper.TryJumpAndSelect`) and "Edit" buttons.

- [ ] Tests `SignSearchTests` (width, kana, case, newline, empty). Implement. Commit `feat: sign list tab with search and jump`.

### Task 7: 3×1 notice board and electronic sign

**Files:** Modify `tools/textures/generate_textures.py`; Create `Defs/ThingDefs/LargeSigns.xml`, textures `SimpleNoticeBoard3_*`, `SimpleElectronicSign3_*`; languages; structure tests.

**Interfaces:** defNames `SimpleSigh_NoticeBoardLarge` (3×1, 384 chars, multiline, cost 70 stuff) and `SimpleSigh_ElectronicSignLarge` (3×1, metal 60 + 3 components, 60 W, LED props as 2×1).

- [ ] Structure tests (size, art aspect 3:1 / 1:3, panel/mask, contrast, JA labels) → FAIL → generate art, add defs → PASS; layout regression for 3:1 and 1:3 panels. Commit `feat: add 3x1 notice board and electronic sign`.

### Task 8: Floor signs and text shadow rendering

**Files:** Create `Defs/ThingDefs/FloorSign.xml`, floor textures; Modify `CompProperties_SignText` (`defaultShadow`, `shadowColor` default (0,0,0,0.6)), `SignTextRenderer.Draw` (second DrawMesh with shadow material offset (+0.03, −0.03) world units, drawn first), languages, tests.

**Interfaces:** defNames `SimpleSigh_FloorSign` (1×1) and `SimpleSigh_FloorSignLong` (3×1, rotatable, portrait art E/W), altitude `FloorEmplacement`, `Standable`, pathCost 0, `isEdifice=false`, `clearBuildingArea=false`, no stuff, no cost, WorkToBuild 200, `defaultShadow=true`, default colour white, `paintable=false`.

- [ ] Structure tests (floor signs walkable, no stuff, shadow on, not paintable) → FAIL → implement → PASS. Commit `feat: walkable floor signs and optional text shadow`.

### Task 9: Paintable frames

**Files:** Modify `Defs/ThingDefs/BasicSign.xml` (base `building/paintable=true`); structure test `standing and wall signs are paintable; floor signs are not`.

- [ ] Test → FAIL → set flag → PASS. Commit `feat: paintable sign frames`.

### Task 10: LED effects (blink, scroll)

**Files:** Create `Source/Layout/Marquee.cs`, `Tests/SimpleSigh.Text.Tests/MarqueeTests.cs`; Modify renderer/cache to update vertices in place for scrolling signs; `CompProperties_SignText.allowEffects` (true on electronic signs); editor effect selector.

**Interfaces:** `Marquee.Offset(float timeSeconds, float speedCellsPerSecond, float textWidth, float panelWidth) → float` (loops: text enters from the right edge and leaves fully on the left); `Marquee.Clip(List<GlyphQuad> line, float offset, float halfWidth, List<GlyphQuad> output)` (shifts by −offset, trims UVs at both edges, no allocation when output has capacity). `SignVisibility.BlinkOn(float timeSeconds)` = 0.6 s on / 0.4 s off.

- [ ] Tests: `OffsetLoops`, `ClipsGlyphsAtPanelEdges`, `ClipDoesNotAllocateWithCapacity`, `BlinkDutyCycle`. Implement; scroll text is single-line (newlines shown as spaces), height = largest one-row size × user scale. Commit `feat: blink and scroll effects for electronic signs`.

### Task 11: Performance check, docs, final review, PR

- [ ] `Tests/SimpleSigh.Text.Tests/PerformanceTests` measuring layout+mesh-data build for 1000 signs and steady-state hit path (0 allocations); record numbers in `docs/testing/performance.md` (offline numbers, not FPS).
- [ ] Update README, checklists (`docs/testing/v0.2-manual-checklist.md`), implementation log, Release DLL; self-review whole branch; push; open PR.
