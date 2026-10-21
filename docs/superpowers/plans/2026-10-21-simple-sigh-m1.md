# Simple Sigh — M1 Basic Sign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** RimWorld 1.7で、木/石/金属で建設でき、日本語を含む任意の文字列を看板の表示面に保存・描画できる「Simple Sigh」の最小利用可能版（M1）を完成させる。

**Architecture:** 1×1のStuffableなThingDef、文字データを持つThingComp、トランザクション型の編集Window、世界空間描画とキャッシュを分離する。検証可能な文字規則はゲーム非依存C#に切り出し、Waylithのグリフ描画と描画レイヤーだけはゲーム内で結合検証する。Harmony等は必要なAPI欠落が確認されたときだけ正式な前提MODとして導入する。

**Tech Stack:** RimWorld 1.7 (Waylith), .NET 10 (`net10.0`), C#, XML, xUnit（開発時のみ）, Python 3（XML構造検証）, Git. 必要時のみ `brrainz.harmony`。

**Spec:** `docs/superpowers/specs/2026-10-21-rimworld-sigh-design.md`

## Global Constraints

- 表示名・内部プロジェクト名: `Simple Sigh`; packageId: `pengin0503.simplesigh`; 名前空間・DLL: `SimpleSigh` / `SimpleSigh.dll`。
- リポジトリ名 `rimworld-sigh` を変えない。対応ゲームバージョンは初期リリース時 `1.7` のみ。
- 木/石/金属で建てられる、バニラの画風になじむ看板。日本語/英数字/記号を入力し、看板表面の領域に表示する。
- Harmonyやその他のWorkshop前提MODは**許容**されるが、まず代替を検証。必要なら `About/About.xml` に必須依存として正しく宣言し、DLLは同梱しない。
- game DLL (`Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll`) を配布・コミットしない。開発時は環境変数 `RIMWORLD_GAME_DIR` でゲーム本体の配置を指定する。
- C#変更には、可能な限り失敗するテストを先に作成し、失敗 → 実装 → 成功の順を守る。ゲーム固有APIは手動ゲーム内検証を別途行う。
- CIは最終確認でのみ設定・実行する。最初はローカルテスト中心。既存データ・ファイルを無関係に編集/削除しない。

## File Map (M1)

| Path | Responsibility |
| --- | --- |
| `About/About.xml` | Simple Sighの識別・依存宣言・1.7対応 |
| `Defs/ThingDefs/BasicSign.xml` | 1×1の材質対応看板定義 |
| `Textures/Things/Building/SimpleSign.png` | 新規制作するバニラ風看板の画像 |
| `Languages/Japanese/Keyed/SimpleSigh.xml` | 日本語UI |
| `Languages/English/Keyed/SimpleSigh.xml` | 英語UI |
| `Source/SimpleSigh.csproj` | .NET10 DLL、ゲームアセンブリ参照 |
| `Source/Text/SignTextRules.cs` | ゲーム非依存の文字列・文字数検証 |
| `Source/Text/SignEditSession.cs` | 編集操作の確定/キャンセル |
| `Source/Signs/CompProperties_SignText.cs` | ThingComp定義設定 |
| `Source/Signs/CompSignText.cs` | 文字/色/セーブと更新通知 |
| `Source/Signs/Dialog_EditSign.cs` | 選択時編集UI |
| `Source/Rendering/SignTextRenderer.cs` | 文字が盤面の内側に位置する1.7用描画 |
| `Source/Rendering/SignRenderCache.cs` | 文字変更時だけ再生成し、寿命管理 |
| `Tests/SimpleSigh.Text.Tests/*.cs` | xUnitテスト（ゲーム非依存の2ソースをリンク） |
| `Tests/StructureTests/test_mod_structure.py` | XML、ID、リソース、依存宣言チェック |
| `docs/testing/m1-manual-checklist.md` | 実ゲームでの1.7検証証跡 |

## Review Focus — 5 high-risk inputs

1. **日本語と結合文字:** UTF-16コードユニットで切らず、文字要素ベースで長さを検証し原文を保存する（Task 2）。
2. **キャンセル・無効な入力:** 確定以外で保存値が変わらず、上限オーバーと改行違反を明示する（Task 3）。
3. **ゲーム再読み込み・再設置:** 保存内容と色をロード後に復元し、再設置後の描画キャッシュを再生成する（Task 4, 6）。
4. **ズーム/UI倍率/日本語フォント:** 文字が看板から浮かず、和文が化けず、看板面に収まる（Task 5, 7）。
5. **大量看板・長い文字:** 平常時に毎フレームのテクスチャ生成や全マップ走査を行わない（Task 6, 7）。

---

### Task 1: MOD identity, building definition, and structural validator

**Files:**
- Create: `Tests/StructureTests/test_mod_structure.py`
- Create: `About/About.xml`, `Defs/ThingDefs/BasicSign.xml`, `Textures/Things/Building/SimpleSign.png`
- Create: `Languages/English/Keyed/SimpleSigh.xml`, `Languages/Japanese/Keyed/SimpleSigh.xml`
- Create: `.gitignore` for local game DLL references, build outputs other than intentionally released Assemblies

**Interfaces:**
- Produces: ThingDef `SimpleSigh_BasicSign` with `stuffCategories` = `Woody`, `Stony`, `Metallic`; `costStuffCount=30`, `size=(1,1)` (1×1), `drawGUIOverlay` only if required by measured rendering approach.
- Produces: keyed labels `SimpleSigh_EditText`, `SimpleSigh_InvalidText` in both languages.

- [ ] **Step 1: Write a failing Python unittest** that parses the expected XML paths using `xml.etree.ElementTree`, asserts `name=Simple Sigh`, `packageId=pengin0503.simplesigh`, version 1.7, each stuff category, both translations, no duplicate defName, and PNG path existence. If `modDependencies` exists, assert every entry has a nonempty packageId and displayName.
- [ ] **Step 2: Run** `python -m unittest discover -s Tests/StructureTests -v`. Expected: FAIL because no MOD XML/assets exist.
- [ ] **Step 3: Create minimal files** satisfying these assertions; texture must be an original simple pixel-art board with a distinct central blank display panel. Do not copy game/source MOD textures.
- [ ] **Step 4: Re-run** the same command. Expected: all structural tests PASS. Save screenshot of the initial graphic in a development note only if it is verifiable.
- [ ] **Step 5: Commit** `test: validate Simple Sigh mod identity and base ThingDef` (stage only files from this task).

### Task 2: Text validation with Unicode semantics

**Files:**
- Create: `Source/Text/SignTextRules.cs`
- Create: `Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj`, `Tests/SimpleSigh.Text.Tests/SignTextRulesTests.cs`

**Interfaces:**
- Produces: `public static class SimpleSigh.Text.SignTextRules` with `TryNormalize(string input, int maxElements, bool multiline, out string normalized, out string errorKey): bool`.
- Behavior: reject `null`; allow empty text; normalize CRLF/CR to LF; do not strip leading/trailing spaces; disallow newline in M1; count Unicode text elements via `System.Globalization.StringInfo`; `maxElements=64` for basic sign; reject not truncate overflow.

- [ ] **Step 1: Write failing xUnit tests** for: empty accepted; ASCII 64 accepted/65 rejected; Japanese 64 accepted; a surrogate-pair emoji counts as one text element; CRLF input rejected when `multiline=false`, normalized when true; whitespace preserved; null rejected with a stable error key `SimpleSigh_InvalidText`.
- [ ] **Step 2: Run** `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj --no-restore` after a one-time `dotnet restore`. Expected: FAIL specifically because `SignTextRules` does not exist (if compile failure, this is the expected red only for missing API, not an unrelated package/tool error).
- [ ] **Step 3: Implement** `SignTextRules.TryNormalize` exactly as above with `StringInfo`, no dependencies on Verse/Unity/Waylith. Include source file as linked Compile item in the test csproj.
- [ ] **Step 4: Run** `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj`. Expected: PASS for every case; run full available test suite.
- [ ] **Step 5: Commit** `feat: validate sign text without truncating Unicode`.

### Task 3: Transactional text editing

**Files:**
- Create: `Source/Text/SignEditSession.cs`
- Create: `Tests/SimpleSigh.Text.Tests/SignEditSessionTests.cs`

**Interfaces:**
- Consumes: `SignTextRules.TryNormalize(...)`.
- Produces: `public sealed class SimpleSigh.Text.SignEditSession` with constructor `(string original, int maxElements, bool multiline)`, `string Draft {get;set;}`, `bool TryConfirm(out string normalized, out string errorKey)`, `void Cancel()`. Editor opens with original text; only the caller of `TryConfirm` success may mutate the ThingComp; Cancel discards unsaved draft.

- [ ] **Step 1: Write failing tests**: draft updates do not mutate original; valid confirm produces normalized output; Cancel restores original draft; invalid/overlength text refuses confirm and preserves original; non-BMP Japanese-mixed input remains intact.
- [ ] **Step 2: Run** `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj --filter FullyQualifiedName~SignEditSessionTests`. Expected: FAIL because class is missing.
- [ ] **Step 3: Implement** the public interface and validation delegation; keep game classes out of this module.
- [ ] **Step 4: Run** filtered test and full test project. Expected: PASS.
- [ ] **Step 5: Commit** `feat: add cancel-safe sign text edit session`.

### Task 4: Buildable sign, ThingComp persistence, selection UI

**Files:**
- Create: `Source/SimpleSigh.csproj`
- Create: `Source/Signs/CompProperties_SignText.cs`, `Source/Signs/CompSignText.cs`, `Source/Signs/Dialog_EditSign.cs`
- Modify: `Defs/ThingDefs/BasicSign.xml`
- Create: `docs/testing/m1-manual-checklist.md`

**Interfaces:**
- Consumes: `SignTextRules`, `SignEditSession`, ThingDef `SimpleSigh_BasicSign`.
- Produces: `CompProperties_SignText : CompProperties` attaching `CompSignText : ThingComp`; fields `text` default `""`, `color` default readable dark; `PostExposeData()` calls `Scribe_Values.Look` on both fields with stable saved keys; gizmo `SimpleSigh_EditText` opens `Dialog_EditSign`.
- Produces: `Dialog_EditSign` reflecting draft/error, invoking setter only after successful confirm, and respecting cancellation.

- [ ] **Step 1: Add failing structure checks** for C# project `net10.0`, `AssemblyName=SimpleSigh`, `Reference` includes the three game DLLs with `Private=false`, and XML `li Class` referring to `SimpleSigh.Signs.CompProperties_SignText`. Independently extend a local checklist with red acceptance entries for saving/reloading text, color, minifying and reinstalling.
- [ ] **Step 2: Run** Python structural suite and a game DLL preflight command `test -f "$RIMWORLD_GAME_DIR/Assembly-CSharp.dll"` (PowerShell alternative: `Test-Path "$env:RIMWORLD_GAME_DIR/Assembly-CSharp.dll"`). Expected: structural RED until files exist. A missing game DLL is an environment BLOCKER, not test success or failure of implementation.
- [ ] **Step 3: Implement** csproj with config property `RIMWORLD_GAME_DIR` + private=false game references, component, serialization, UI and locale. Keep `SignEditSession` as sole draft validation; avoid Harmony unless a specific missing hook is identified.
- [ ] **Step 4: Run** Python suite, xUnit suite, and `dotnet build Source/SimpleSigh.csproj -c Debug` against *actual* RimWorld 1.7 assemblies. Expected: 0 errors. If game assemblies are unavailable, record build **not executed**; never claim it passed.
- [ ] **Step 5: Run game acceptance**: wooden/stone/metal sign building, editing/cancel, save/load and reinstall; record each result in the checklist. For failing outcomes, write an explicit reproduction and fix before marking done.
- [ ] **Step 6: Commit** `feat: build and edit persisted basic sign`.

### Task 5: 1.7 world-space text rendering and Japanese probe

**Files:**
- Create: `Source/Rendering/SignTextRenderer.cs`
- Modify: `Source/Signs/CompSignText.cs`
- Modify: `docs/testing/m1-manual-checklist.md`

**Interfaces:**
- Consumes: `CompSignText` text/color, world position and sign display bounds.
- Produces: `SignTextRenderer` drawing text into *the sign's display panel* with correct placement at normal zoom, not a floating tooltip.
- Design decision: determine actual 1.7 text/glyph API from game assemblies and a minimal in-game probe. Test Japanese glyphs **before** committing to a CPU font atlas: 1.7 reference implementations explicitly warn that world-text atlas baking can fail for non-Latin glyphs.

- [ ] **Step 1: Write red manual acceptance cases** using exact strings `研究室`, `Wood A-12`, `発電設備 24h`, `倉庫\n備品` (multiline rejected in M1), plus zoom/UI scale 100% and 125%; record current absent render/incorrect glyph as RED.
- [ ] **Step 2: Confirm the renderer API** from RimWorld 1.7 assemblies and working 1.7 source (not 1.6 signatures). Record world anchoring, render queue, text measurement, atlas/glyph handling and disposal rationale in `docs/testing/m1-manual-checklist.md`.
- [ ] **Step 3: Implement** `SignTextRenderer` with a bounded on-sign draw region, text fitting and Japanese capable glyph fallback. If the available 1.7 APIs can only show GUI labels, mark M1 rendering BLOCKED until a true on-panel approach is implemented; do not pass the acceptance condition with an overlay detached from the board.
- [ ] **Step 4: Run** `dotnet build Source/SimpleSigh.csproj -c Debug`, existing tests, and the in-game red cases. Expected: zero build/test failures and legible text anchored on the board in game.
- [ ] **Step 5: Commit** `feat: render localized sign text on world sign panel`.

### Task 6: Cache lifecycle and persistence regressions

**Files:**
- Create: `Source/Rendering/SignRenderCache.cs`
- Modify: `Source/Rendering/SignTextRenderer.cs`, `Source/Signs/CompSignText.cs`
- Modify: `docs/testing/m1-manual-checklist.md`

**Interfaces:**
- Consumes: text/color changes and lifecycle notifications.
- Produces: cached texture/material keyed by normalized text + color + render parameters; explicit invalidate/release on change, de-spawn, destroy, minify/reinstall and game load; no per-frame text rasterization.

- [ ] **Step 1: Add red acceptance cases**: unchanged sign for 10 minutes in game causes zero sign texture rebuilds after initial render; text/color edit triggers exactly one rebuild; save/load and minify/reinstall reproduce identical visible label and no lingering stale material.
- [ ] **Step 2: Instrument cache counts** in a Debug-only mode; run 20-sign manual baseline. Expected: cache stats expose redundant recomposition in the unoptimized version or demonstrate a targeted missing lifecycle cleanup.
- [ ] **Step 3: Implement** `SignRenderCache` with disposal in lifecycle callbacks and invalidation on text/color change. Bound cache growth; do not hold map/Thing references in global static dictionaries without deterministic cleanup.
- [ ] **Step 4: Run** game tests above and the full Python/xUnit/build matrix. Expected: 0 failures; compare repeated frames and 100+ sign test, recording figures (do not invent values).
- [ ] **Step 5: Commit** `perf: cache and release sign text rendering resources`.

### Task 7: M1 acceptance, Japanese README, and release checkpoint

**Files:**
- Create: `README.md`, `docs/testing/m1-acceptance.md`
- Modify: `docs/testing/m1-manual-checklist.md`

**Interfaces:**
- Produces: a reproducible build/install workflow, exact declared prerequisites, evidence-backed M1 status; no untested release claim.

- [ ] **Step 1: Add red final-check cases** for no dependency/declared dependency install, invalid input, 100+ signs, save/load/new save, zoom, Japanese glyphs, blueprint and reinstall.
- [ ] **Step 2: Execute** `python -m unittest discover -s Tests/StructureTests -v`, `dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj`, `dotnet build Source/SimpleSigh.csproj -c Release`, and in-game acceptance. Expected: all PASS and no material new log errors.
- [ ] **Step 3: Record** exact commands, installed dependency versions, successful/failed test cases and known limitations. If Harmony has been introduced, verify `About.xml` dependency `brrainz.harmony` with Workshop ID `2009463077` and ensure no `0Harmony.dll` is bundled.
- [ ] **Step 4: Commit** `docs: report Simple Sigh M1 acceptance evidence`; only then consider tagging or declaring M1 complete.

## Harmony decision gate

At each C# task, first list the required game hooks and whether a public/overridable API suffices. If not, document: (a) exact 1.7 type/method signature; (b) why no ThingComp/MapComponent/SectionLayer hook works; (c) narrow Prefix/Postfix patch; (d) compatibility regression for another mod; (e) mandatory `brrainz.harmony` metadata. Use official HarmonyRimWorld library references, **not** vendored `0Harmony.dll`. This is a conditional decision, not an unconditional prohibition or unconditional hard dependency.

## Post-M1 roadmap (separate implementation plans)

- **M2:** wall sign + 2×1/3×1 board + multiline text and scale/fit; depends on M1 renderer and editor. Write its own detailed plan after M1 evidence.
- **M3:** powered 2×1/3×1 electronic board with `CompPowerTrader`, power-off blank state, colors and tested lighting; depends on M2.
- **M4:** performance benchmarks at 100, 500 and 1000 signs, existing-save adoption, compatibility and final packaging; CI only at final check.

## Explicit stop conditions

No runtime assemblies → build verification blocked. Non-Latin glyphs missing or sign text floats outside the board → no M1 sign-off. Structural tests alone are not evidence that the game loads the DLL. The repo should not claim a playable MOD without an actual game build and in-game test.

## Execution

Recommended: **Native / superpowers:executing-plans**, with no subagents and task-by-task TDD; the file and game APIs have sequential dependencies. This plan must be reviewed by the user before product implementation begins.
