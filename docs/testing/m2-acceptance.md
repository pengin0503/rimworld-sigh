# Simple Sigh — M2 acceptance record

Date: 2026-10-21. Environment: Linux container, .NET SDK 10.0.402, Python 3.13, RimWorld 1.7 reference
assemblies only (no game executable).

## Status

| Milestone | Code | Automated checks | In game |
| --- | --- | --- | --- |
| M1 — basic sign, editor, save, render, cache | implemented | PASS | **NOT RUN** |
| M2 — wall sign, 2×1 notice board, multi-line wrap/fit, rotations | implemented | PASS | **NOT RUN** |

M1 and M2 are **implemented and build-verified, not accepted in game**. They must not be described as
working in RimWorld until `m1-manual-checklist.md` and `m2-manual-checklist.md` have been run.

## Acceptance checklist

| Area | Evidence available here | In game |
| --- | --- | --- |
| Three sign forms (basic, wall, notice board) exist, stuffable wood/stone/metal | Structure tests (defs, stuff categories, Core cross-check), smoke (Def fields vs 1.7 classes) | NOT RUN |
| Material colour on frame, light panel | Mask/panel alignment test; offline preview | NOT RUN |
| Japanese multi-line text, wrapping, kinsoku, fit | `SignLayoutTests`, `SignTextLayoutTests`; offline preview | NOT RUN |
| Text drawn on the board surface (not a floating label) | Renderer builds a world-space mesh at the panel; geometry unit tests; offline preview | NOT RUN |
| Rotations (wall N/E/S/W, board N/E/S/W) | `SignPanelGeometryTests`; offline preview | NOT RUN |
| Save/load | Codec tests replaying 1.7 `ParseString`; real `ScribeSaver` write in smoke | NOT RUN |
| Placement rules (wall attachment) | Vanilla `Placeworker_AttachedToWall`; structure tests | NOT RUN |
| No new log errors | — | NOT RUN |
| Cache behaviour | `SignRenderSlotTests`, `SignRenderKeyTests` (incl. no steady-state allocation); dev inspect counters | NOT RUN |
| Dependencies | None declared; no Harmony reference (enforced by test); no game DLLs in repo (enforced by test) | — |

## Commands and observed results

```
$ tools/verify.sh            # RIMWORLD_REFERENCE_DIR=<private extraction of rimworld_DLL.zip>
== structure tests      Ran 28 tests  OK
== unit tests           Passed! - Failed: 0, Passed: 116, Skipped: 0, Total: 116
== build (Release)      Build succeeded (-warnaserror)
== smoke                SMOKE PASS (assembly/XML only; the game was not run)
```

`1.7/Assemblies/SimpleSigh.dll` (Release, deterministic: identical bytes on clean rebuild and across commits,
no local paths or git commit id embedded) SHA-256 `c4d4189b678170e7ae3b79a18fcee9a6c9372af26fce4ad725bf545a90f8e4d6` at the M2 commit (later commits change the DLL; README lists the current hash).
(The first committed build embedded the commit id via the SDK's source-revision default; fixed by
`IncludeSourceRevisionInInformationalVersion=false`.)

## Blocked / not verifiable here

- Game launch, mod list loading, Def resolution by the real loader (its static constructors need engine/Steam
  assemblies not present in the ZIP).
- Japanese glyphs in the 1.7 font atlas (backend implementation not in the references).
- Lighting, fog, zoom, UI scale, FPS with 100+ signs, log cleanliness.
