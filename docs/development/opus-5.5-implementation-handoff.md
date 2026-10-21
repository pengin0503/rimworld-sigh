# Simple Sigh — Opus 5.5 implementation handoff

> **Status:** preparation documentation only. No product source or compiled MOD was created during this handoff. Read the spec and implementation plans as the authoritative requirements.

## Read in order

1. [Design specification](../superpowers/specs/2026-10-21-rimworld-sigh-design.md)
2. [M1 plan](../superpowers/plans/2026-10-21-simple-sigh-m1.md)
3. [M2 plan](../superpowers/plans/2026-10-21-simple-sigh-m2.md)
4. [Game reference audit](rimworld-1.7-reference-audit.md)

## Environment handoff

- The user provided **`rimworld_DLL.zip`**, whose SHA-256 is `dab2b7f3d29b87691a6c9e979a3c971814c8c2cfd305714cc4229b1679eba279`. It is **not checked into GitHub**; the Opus execution environment must receive the ZIP privately as an attachment or through authorized local access.
- ZIP root: `rimworld_DLL/`. Assemblies reside immediately beneath that folder; Core Defs and locales are in `rimworld_DLL/Data/Core/`.
- `Assembly-CSharp.dll`, `Waylith.dll`, `UnityStubs.dll` are present. Conditional: `0Harmony.dll`, `netstandard.dll`.
- Core building Def references: `ThingDefs_Buildings/Buildings_Base.xml`, `Buildings_Structure.xml`, `Buildings_Furniture.xml`, `Buildings_Power.xml`. Japanese localization is a nested `Japanese (日本語).tar`.
- **No playable RimWorld installation, game executable, sprite texture set, .NET SDK or runtime is provided by the ZIP.** Actual run verification requires a separate installation; build verification requires a separately installed .NET SDK.
- The M1 plan mentions `RIMWORLD_GAME_DIR`, but this ZIP is only a **compile-time reference directory**, not a game install. Resolve the discrepancy explicitly in build setup without claiming game availability; permit an independent `RIMWORLD_REFERENCE_DIR` if useful.
- Do not reproduce, commit or publish proprietary RimWorld binaries, Defs or locales. `.gitignore` includes common reference-extraction paths, but staged-file review is still mandatory.

## Delegation prompt for Opus 5.5

以下の内容を一度の作業指示として使用する。詳細をこの文へ複製せず、上記のGitHub資料を直接読むこと。

> GitHub `pengin0503/rimworld-sigh` のRimWorld 1.7向けMOD **Simple Sigh** の実装を担当してください。
>
> まず `docs/development/opus-5.5-implementation-handoff.md` と、その「Read in order」の4資料を確認してください。ユーザー提供の `rimworld_DLL.zip` は1.7の**参照ファイル**としてプライベートに使用し、ゲーム本体の実行環境とは区別してください。参照ZIPがこのセッションに見えない場合、GitHubにアップロードせず、確認可能な部分の作業を進め、足りないファイルを最後に明記してください。
>
> **SuperpowersのNative `executing-plans` 方式、サブエージェント禁止。** M1 → M2の順にTDD（RED → GREEN）で実装し、各工程のテスト結果・修正判断を作業記録に残してください。必要ならHarmonyなどの正式な前提MODを採用できますが、必要性の根拠と依存情報を記録してください。
>
> **製品仕様：** MOD名 `Simple Sigh`、packageId `pengin0503.simplesigh`、DLL `SimpleSigh.dll`。RimWorld 1.7用、バニラ風の建材別1×1看板、1×1壁掛け看板、2×1複数行掲示板。日本語・英数字の任意入力と文字色、表示面に収まる文字、セーブ/ロード、描画キャッシュ。UIの単なる浮動ラベルは完成扱いにしない。
>
> **作業規則：** 既存の内容を確認してから開始し、隔離された作業ブランチ/ワークツリーを利用してください。依存DLLやゲームデータをGitに追加しないでください。無関係なファイルの改変や破壊的操作、mainマージ、公開配布は行わないでください。CIは最終確認に限定し、ローカル検証を優先してください。
>
> **進行：** M1・M2のタスクを中断なしで可能な範囲まで進め、節目でテストとコミットを実施してください。実機がなくても静的検証とビルドを試み、実施していないゲーム内確認は `NOT RUN` と明記してください。無理にPASSにしないでください。M1・M2の品質を確保できて余力がある場合のみ、設計書のM3電光掲示板に着手してよいです。
>
> **最後に：** ブランチ/PRのURL、変更点、実行したテストと結果、1.7で未検証の点、参照環境の不足、残件を簡潔に報告してください。

## Readiness checklist for the implementation worker

- [ ] GitHub access to the repo, including a writable isolated branch
- [ ] Access to the actual reference ZIP or its private extraction
- [ ] .NET SDK, restore/build/test commands available (otherwise record blocker)
- [ ] RimWorld 1.7 actual game install if runtime acceptance is expected
- [ ] M1 and M2 plan both inspected and cross-checked for file/interface conflicts
- [ ] No game-origin DLL, translation or game source bundled into a public commit
- [ ] Tests and verification differentiated from non-executed acceptance cases

## Verification contract

Documentation, static validation, compilation and in-game verification are **four different states**. Report each separately. Completing a Github PR does not prove the game loads the MOD. The original reference ZIP is not itself an installable test environment.

This handoff was prepared without changing M1/M2 implementation source code.
