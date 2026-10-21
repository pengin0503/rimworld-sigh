# Simple Sigh — M3 results (automated checks only)

Plan: `docs/superpowers/plans/2026-10-21-simple-sigh-m3.md`. Environment as in `m1-acceptance.md`.

| Check | Result |
| --- | --- |
| Structure / XML incl. Core cross-reference (`costList`, research, stuff) | 33 tests, OK |
| Unit tests (incl. `SignVisibilityTests`: power gating, render queue ordering) | 121 passed |
| Release build `-warnaserror` | succeeded |
| Smoke: vanilla comp classes and fields (`CompProperties_Power`, `_Flickable`, `_Glower`) resolve against 1.7 | SMOKE PASS |
| Offline preview (electronic sign N/E, colour, power-off) | `docs/images/offline-layout-preview.png` |
| In-game checklist E1–E10 | **NOT RUN (no game)** |

**M3 status: implemented and build-verified; NOT accepted in game.**

Not implemented (spec "後に3×1"): a 3×1 variant; scrolling/animated LED effects (out of scope per spec §8).
