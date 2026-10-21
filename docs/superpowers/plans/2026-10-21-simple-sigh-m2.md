# Simple Sigh — M2 Sign Variants and Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** M1の1×1看板を基盤として、壁掛け看板と2×1掲示板を追加し、複数行・行折り返し・文字縮小・材質ごとの見た目・1.7での視認性を検証する。

**Architecture:** 既存の `CompSignText`・`Dialog_EditSign`・`SignTextRenderer`・`SignRenderCache` を共用し、建築物差分はThingDefと小さな定義設定に集約する。文字配置はゲーム非依存の `SignLayout` で計算し、既存レンダラーは看板の表示面の四角形を受け取る。壁掛けはバニラの設置・描画APIを優先し、1.7のAPIを実機/参照アセンブリで調べてから必要最小限のコードを加える。

**Tech Stack:** RimWorld 1.7 (Waylith), .NET 10 C#, XML, xUnit, Python unittest; 必要な場合のみ正式なHarmony前提MOD。

**Spec:** `docs/superpowers/specs/2026-10-21-rimworld-sigh-design.md`

**Prerequisite:** `docs/superpowers/plans/2026-10-21-simple-sigh-m1.md` のタスクを先に完了する。未完了またはゲームアセンブリ不足の場合、M2実装は可能な範囲にとどめ、実機確認の成否を捏造しない。

## Global Constraints

- リポジトリ `pengin0503/rimworld-sigh`、表示名 `Simple Sigh`、packageId `pengin0503.simplesigh`、DLL `SimpleSigh.dll` を維持。
- 対応RimWorld 1.7、NET10。既存M1のThingDef名・保存キー・グラフィックの表示位置を破壊しない。
- 木/石/金属などのStuffable建材を使う。バニラの建築物のようなオリジナル画風。文字は説明ラベルではなく看板本体の表示面に固定する。
- 2×1掲示板は複数行・256 Unicodeテキスト要素まで、小型/壁掛け看板は64要素までを初期上限とする。後方互換性のため、以前に保存した文字列を自動切り捨てしない。
- 外部MOD依存を禁止しないが、根拠のない追加はしない。Harmonyを必要とするなら `About.xml` に `brrainz.harmony` を宣言し、`0Harmony.dll` を自前で配布しない。
- TDD: 失敗を観測したテストを実装より先に書き、修正後に全テストを再実行する。CIは最終確認まで増やさず、ローカル検証を優先。
- この計画の「完成」判定は日本語描画とRimWorld 1.7実機でのプレイ可能性を含む。実機がない環境では未確認として残す。

## Review Focus

1. **改行と長い日本語:** 長い単語/全角句読点/混在文字を枠内に納め、本文は保存時に損失しない（Task 1・4）。
2. **壁への設置と回転:** 壁の方位・隣接物・解体・ミニファイ時に、壁を壊さず文字がずれない（Task 2・4）。
3. **2×1掲示板の回転:** 東西・南北で表面・文字の向きと収まりが破綻しない（Task 3・4）。
4. **霧/照明/ズーム/UI倍率:** 霧越しに意図せず文字だけ出ず、拡縮や背景色で視認性を維持する（Task 4）。
5. **描画キャッシュ:** 色/文字/回転/設置位置/盤面サイズに応じて更新し、毎フレーム再ラスタライズを行わない（Task 1・4）。

---

### Task 1: Reusable sign text layout and multi-line editor

**Files:**
- Create: `Source/Text/SignLayout.cs`, `Tests/SimpleSigh.Text.Tests/SignLayoutTests.cs`
- Modify: `Source/Text/SignTextRules.cs`, `Source/Signs/Dialog_EditSign.cs`, `Source/Signs/CompProperties_SignText.cs`
- Modify: `Source/Rendering/SignTextRenderer.cs`

**Interfaces:**
- `SignLayout.Fit(string text, float availableWidth, float availableHeight, Func<string,float> measureWidth, float lineHeight, float minScale): SignLayoutResult` returns lines, chosen font scale and whether text fits. Use a real text measurement adapter only inside the game renderer.
- `CompProperties_SignText` supplies `maxElements` (64 or 256), `multiline` and per-def display bounds; do not hard-code building defName in editor or renderer.
- `SignTextRules.TryNormalize(..., multiline:true,...)` preserves user-entered line breaks on the large sign while M1 single-line behavior remains intact.

- [ ] **Step 1: Write failing xUnit tests** for two Japanese lines, explicit newline preservation, Japanese/ASCII mixed widths (via deterministic measurement function), wrapping at word/character boundary, minimal font scale, empty string, too-small panel, long no-space Japanese string, and 256-element acceptance vs 257-element rejection (M1 64/65 tests must still pass).
- [ ] **Step 2: Run** `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj`; expected RED because `SignLayout` does not exist.
- [ ] **Step 3: Implement** `SignLayout.Fit` plus `SignLayoutResult`, add a properly parameterized multiline editor using M1 draft/confirm semantics, and connect the layout to world renderer rather than drawing a detached GUI tooltip.
- [ ] **Step 4: Run** all xUnit and structure tests; expected PASS without modifying the existing M1 text limits for basic signs.
- [ ] **Step 5: Commit** `feat: fit multiline Unicode text inside sign panels`.

### Task 2: Wall-mounted sign

**Files:**
- Create: `Defs/ThingDefs/WallSign.xml`, `Textures/Things/Building/SimpleWallSign.png`
- Modify: `Languages/Japanese/Keyed/SimpleSigh.xml`, `Languages/English/Keyed/SimpleSigh.xml`
- Modify: `Source/Rendering/SignTextRenderer.cs` only if the observed 1.7 attachment/rotation behavior requires it
- Modify: `Tests/StructureTests/test_mod_structure.py`, `docs/testing/m1-manual-checklist.md` (or move M2 cases into a dedicated checklist)

**Interfaces:**
- Produces ThingDef `SimpleSigh_WallSign`; shares `CompSignText` with M1 `maxElements=64`, single-line.
- For wall placement investigate `canPlaceOverWall`, `isEdifice=false` and `clearBuildingArea=false` per actual 1.7 behavior; verify these flags do not allow unwanted placement on every floor.

- [ ] **Step 1: Add failing structure assertions** for `SimpleSigh_WallSign` being 1×1, Stuffable (Woody/Stony/Metallic), using existing `CompSignText`, stable texture and matching JP/EN labels; add manual rejection cases for unsupported wall/rotation.
- [ ] **Step 2: Run** `python -m unittest discover -s Tests/StructureTests -v`; expected RED due to missing wall sign.
- [ ] **Step 3: Implement** the wall ThingDef, art and minimal placement/rotation requirements. Do not add broad building placement Harmony patches without demonstrating need.
- [ ] **Step 4: Re-run** structure and xUnit tests; compile with real 1.7 DLLs when present; check manual gameplay: valid/invalid wall types, different wall orientations, text stays inside panel, rebuilding wall does not duplicate/destroy base wall unintentionally.
- [ ] **Step 5: Commit** `feat: add vanilla-style wall-mounted text sign`.

### Task 3: 2×1 multi-line notice board

**Files:**
- Create: `Defs/ThingDefs/NoticeBoard.xml`, `Textures/Things/Building/SimpleNoticeBoard.png`
- Modify: `Languages/{Japanese,English}/Keyed/SimpleSigh.xml`
- Modify: `Source/Rendering/SignTextRenderer.cs`, `Source/Signs/CompProperties_SignText.cs` only as necessary
- Modify: `Tests/StructureTests/test_mod_structure.py`, `Tests/SimpleSigh.Text.Tests/SignLayoutTests.cs`

**Interfaces:**
- Produces ThingDef `SimpleSigh_NoticeBoard` with size `(2,1)` and wooden/stone/metal Stuffable categories; max 256 text elements; multiline = true, line breaks retained. M1 1×1 and wall sign retain single-line behavior.
- Renderer uses the object's occupied-cell footprint and rotation to compute the actual on-panel text quad. The edited text is per-instance, not shared between built signs.

- [ ] **Step 1: Write failing structure & xUnit tests**: board dimensions, fields `multiline=true` and `maxElements=256`, resource existence, Japanese two-line placement with panel ratios `2×1` and `1×2`.
- [ ] **Step 2: Run** Python structural and xUnit tests; expected RED before the 2×1 definition/layout support.
- [ ] **Step 3: Add** a new illustrated ThingDef, localized labels, appropriate building statistics and direct reuse of editor/component/renderer. Do not introduce a second divergent editor or unrelated framework.
- [ ] **Step 4: Verify** tests, 1.7 build and gameplay: Japanese paragraphs, manual line breaks, rotation both axes, text display/label color, save/load and minify/reinstall preserve content.
- [ ] **Step 5: Commit** `feat: add 2x1 multi-line material notice board`.

### Task 4: Rendering quality and regression suite

**Files:**
- Modify: `Source/Rendering/SignTextRenderer.cs`, `Source/Rendering/SignRenderCache.cs`
- Create: `docs/testing/m2-manual-checklist.md`, `docs/testing/m2-results.md`
- Modify: `Tests/SimpleSigh.Text.Tests/SignLayoutTests.cs` and structure tests only for real regression cases

**Interfaces:**
- Three built types share fitting logic, rendering and save behavior; different materials and rotations must not move label outside the on-sign display region.
- The cache key must include all parameters affecting visible output (string/formatting, geometry, scale, font/atlas version). Disposal must be deterministic.

- [ ] **Step 1: Record initially failing/manual unverified cases**: camera zoom close/medium/far, UI scale 100%/125%, light/dark material, unlit cave, fog, indoors/outdoors, wall N/E/S/W, 2×1 board rotation, Japanese punctuation/long lines, text change and redraw, 100 signs at rest.
- [ ] **Step 2: Run** the game against the checklist (if installed) and compare actual to expected. Classify missing-game evidence as `NOT RUN (no game)`, not PASS.
- [ ] **Step 3: Fix** only repro'd rendering problems using focused red tests where possible; re-test after each change; avoid per-frame image/glyph re-creation, map scans or unnecessary Harmony patches.
- [ ] **Step 4: Run** the full matrix: `python -m unittest discover -s Tests/StructureTests -v`; `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj`; `dotnet build Source/SimpleSigh.csproj -c Release` with 1.7 assemblies. Record exit codes, 1.7 in-game observations and remaining blocks.
- [ ] **Step 5: Commit** `test: verify M2 layout, wall placement and game rendering`.

### Task 5: M2 handoff and M3 readiness

**Files:**
- Create: `docs/testing/m2-acceptance.md`
- Modify: `README.md` (if created in M1)

**Interfaces:**
- Produces unambiguous current status `M1 PASS/FAIL/NOT RUN`, `M2 PASS/FAIL/NOT RUN`; a working build/install description; no automatic M3 claims.

- [ ] **Step 1: Create the acceptance checklist**: three sign forms (basic, wall, notice board), material/color combinations, JP multi-line, actual board surface rendering, save/load, manual placement, no new log errors, cache behavior and dependencies.
- [ ] **Step 2: Run** full available tests and build; record exact commands, number of tests, game build directory and version, actual in-game test outcomes. Mark individual gates BLOCKED/NOT RUN when external prerequisites absent.
- [ ] **Step 3: Update** README, limitations, installation and reference dependency info; self-review changed files and confirm no shipped private/game DLLs.
- [ ] **Step 4: Commit** `docs: record Simple Sigh M2 acceptance and installation`.

## Decision on M3 and scope

Only after M1/M2's implementation and available verification, M3 may be planned and implemented in the same long-running execution if the developer has time and the user has delegated that scope. M3 is **not** a reason to weaken M1/M2 quality gates. M3 acceptance requires power loss blanking and resuming the displayed text, plus LED visual consistency. If 1.7 game assemblies or game execution are missing, status must remain build/game NOT RUN rather than silently passed.

## Execution

Follow `superpowers:executing-plans` inline; do not dispatch implementer subagents. Proceed task-to-task without redundant pauses, use a dedicated branch/worktree, commit focused changes, and report evidentially validated completion and environment blockers. Do not merge to main, force-push, delete user files, or publish to Workshop without explicit authorization.
