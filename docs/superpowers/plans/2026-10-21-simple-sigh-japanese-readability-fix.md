# Simple Sigh 1.7 — Japanese Glyphs and Readable Sign Text Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (native only; no subagents) task by task. Mark steps and record test evidence. Do not merge an unbuilt or unplayed release.

**Goal:** Fix user-reproduced v0.1.0-test.1 bugs: Japanese sign text renders as `???`, and multi-character labels become unreadably small.

**Architecture:** Keep the existing `ThingComp`, per-sign saved strings, mesh cache and physical on-panel text. Fix layout in game-independent code; separately provide a Japanese-capable glyph atlas through RimWorld 1.7's actual font registration/baking pathway (not only a Unicode string encoding change). Do not replace the surface text with floating labels.

**Tech Stack:** RimWorld 1.7 Waylith, .NET 10 / C# 14, UnityStubs.Gui, xUnit and Python structure tests.

**Spec:** `docs/superpowers/specs/2026-10-21-rimworld-sigh-design.md`; bug reports #4, #5; implementation history `docs/development/implementation-log.md`.

## Global constraints

- All game DLLs must remain local, **outside public source and release archives**.
- Font files must be lawfully sourced with compatible distribution/attribution, or selected from the user's system without redistribution. Do not copy proprietary installed fonts.
- Japanese text must appear **on the actual sign panel**; save original Unicode unchanged. Unavailable characters may still use `?` with an explicit warning.
- Do not add Harmony unless needed; if needed declare `brrainz.harmony`, reference the correct 1.7 `0Harmony.dll`, and never bundle that DLL.
- Build against real 1.7 references with zero compile errors. CI xUnit by itself does not test `Source/Rendering`.
- Keep branch isolated; do not merge or publish a new release until real `SimpleSigh.dll` was rebuilt, tested and committed.

## Review focus / additional tests

1. **Japanese with English game language:** `研究室` and `食料保管庫` must display Japanese rather than `?`.
2. **New glyph typed after first font bake:** adding `避難` must cause the necessary bake and atlas UV refresh, then all prior signs stay correct.
3. **Backends replaced after language change:** same `FontIndex`/`AtlasVersion` but different backend object must invalidate the per-sign cache.
4. **Visual readability:** a 1x1 `研究施設入口` must render larger than the old single-line fit without clipping or changing the saved text; 1-2 characters should remain one row.
5. **Memory and game lifecycle:** 100–1000 signs, erasing text and reloading a game must not leave stale live meshes or rebuild/upload the whole font atlas every frame.

---

### Task 1: Reproduce and fix the sizing algorithm (SOURCE CHANGE COMPLETED IN THIS DRAFT PR)

**Files:** `Source/Layout/SignTextLayout.cs`, `Tests/SimpleSigh.Text.Tests/SignTextLayoutTests.cs`

- [x] Add a regression test `LongTextOnSmallSingleLineSignUsesMoreLegibleAutomaticLineBreaks` using a 1x1-sized panel and Japanese text.
- [x] Run xUnit on old code: failure reports `Display size 0.16240016 did not improve over one-line size 0.16240014`.
- [x] Implement choosing the larger fitted scale between one-row and wrapped display layout for otherwise single-line editors.
- [x] Verify tests in GitHub Actions: 123 passed (including new short/long cases).
- [ ] Independently verify real 1.7 game at zoom levels and 4–12 Japanese characters; record screenshots and actual visual threshold.
- [ ] Rebuild and commit `1.7/Assemblies/SimpleSigh.dll` so the fix is included in a usable ZIP.

### Task 2: Inspect the actual font backend and establish Japanese font loading (OPEN)

**Files:** `Source/Rendering/TmpGlyphSource.cs`, new `Source/Rendering/JapaneseFontSource.cs` (if required), font license/attribution, game smoke tests.

- [ ] Reproduce `TryGetGlyph(SignFontIndex, '研') == false` in a real 1.7 Waylith process; log the font index, backend identity, metrics, AtlasVersion, and whether Japanese glyphs are baked.
- [ ] Research `RimWorldWaylith.Imgui.ImGuiTextMeshProBackend` behavior in the game runtime, including `GuiBackend.Current.RegisterFont`, `Bake`, `BakeRange`, `BakeNewFonts`, `TryCaptureAtlas` and `GetAtlasTexture`; these methods are **not all available in the user's reference ZIP**.
- [ ] Select a Japanese-capable font strategy with correct license: a separately sourced OFL-licensed font with its license, or a supported installed-font selector with fallbacks. Prove it contains `研`, `究`, `室`, kana, numerals and Latin.
- [ ] Add isolated tests proving a missing Japanese glyph in the default font chooses the Japanese font after registration rather than replacing it with `?`.
- [ ] Register a **dedicated sign font slot** in Waylith; ensure demanded glyphs are actually baked and texture atlas is uploaded/updated. `RegisterFont` alone is insufficient if the default bake covers only Latin. If Harmony is required to extend the bake callback, patch only the necessary engine method with a version guard and compatibility logging.
- [ ] Ensure lazy bake is triggered by editing/loading a sign, never every frame. Handle changes to atlas dimensions/UV and font metrics.
- [ ] For already saved Japanese strings, load the unchanged text and build glyphs from the new source. Keep fallback `?` solely for genuinely unsupported characters; report diagnostic details in developer mode.
- [ ] Independently compile `Source/SimpleSigh.csproj` against 1.7 references and run `Tests/SimpleSigh.GameSmoke`. A successful independent xUnit run does **not** prove the game engine bridge compiles.
- [ ] Run real game with `研究室`, `食料保管庫`, `立入禁止`, mixed ASCII, new characters typed after the first render, and both Japanese/English game languages. Mark unrun checks as NOT RUN.

**Relevant open-source engine precedent (study behavior, verify license before reusing code):**
- https://github.com/basalt991/world-feature-text-compat17
- https://github.com/basalt991/readable-fonts17

### Task 3: Renderer cache and clipping regressions (OPEN)

**Files:** `Source/Rendering/SignRenderCache.cs`, `Source/Signs/CompSignText.cs`, `Source/Layout/GlyphQuadBuilder.cs`, `Tests/SimpleSigh.Text.Tests/*`

- [ ] Reproduce an atlas/backend swap with equal numeric versions and add a key test that rejects the old cached mesh.
- [ ] Include backend identity/generation in render key, maintaining allocation-free cache hits.
- [ ] Reproduce typing empty text after a live mesh exists; release the mesh in `Apply` without destroying active unrelated objects.
- [ ] Reproduce overhanging glyph clipping and adjust geometry/UV rather than discarding a valid boundary character.
- [ ] Run xUnit/structure tests, build and smoke, and inspect actual game text at the edge of 1x1/2x1 screens.

### Task 4: Build, packaging, user validation, release (OPEN)

**Files:** `tools/verify.sh`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `README.md`, `docs/testing/*`

- [x] PR CI now rejects a stale committed `SimpleSigh.dll` if `Source/` changed on the PR branch.
- [ ] Change `tools/verify.sh` to exit nonzero if a Release build's DLL differs from the checked-in DLL; ensure false positives do not arise from deterministic metadata.
- [ ] Build the new DLL using the supplied lawful 1.7 reference ZIP **locally** and commit only `SimpleSigh.dll`, never game DLLs or fonts without proper licensing.
- [ ] Run all 1.7 in-game checklists, particularly Japanese with English UI and a 100-sign load/zoom check.
- [ ] Verify final CI (structure 33 with Core refs if available, xUnit, packaging) and locally verify the game DLL build/smoke.
- [ ] Merge only after sign readability + Japanese rendering are demonstrated in the real game. Then publish a fresh **test prerelease** (e.g. `v0.1.0-test.2`), keeping `v0.1.0-test.1` clearly marked defective.

## Current status

- #5 (auto-readable layout): source logic + 2 regression tests done, CI xUnit passes; **binary is stale and game untested**.
- #4 (Japanese font): diagnosed; dedicated Japanese font registration and actual glyph baking not implemented yet.
- The previous release `v0.1.0-test.1` is unaffected. Do not point users to a newer binary until Task 2 and Task 4 pass.
