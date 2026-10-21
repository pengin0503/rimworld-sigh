# Simple Sigh — M3 Electronic Sign (brief plan)

> Executed inline with superpowers:executing-plans after M1/M2 passed their automated gates and the
> final review left no Critical/Important findings. TDD per task. In-game checks are `NOT RUN` here.

**Goal:** a powered 2×1 electronic (LED) sign: text in the chosen colour only while powered, hidden on
power loss and restored unchanged when power returns, readable in the dark like an LED panel.

**Spec:** `docs/superpowers/specs/2026-10-21-rimworld-sigh-design.md` §3, §6 M3.

## Design (from the 1.7 references)

- Reuse `CompSignText` / `Dialog_EditSign` / `SignTextLayout` / renderer. New `CompProperties_SignText`
  fields: `requiresPower` (hide text unless `CompPowerTrader.PowerOn`), `glowsInDark` (render queue 3150 =
  vanilla `TransparentPostLight`, after `LightOverlay` 3100 and before `FogOfWar` 3175, so text stays bright at
  night but is still fogged), `syncGlowColor` (set the vanilla `CompGlower.GlowColor` to the text colour).
- Power on/off is checked in `PostDraw` (cheap bool); the mesh is kept, so restoring power needs no rebuild.
  The text is never cleared by power loss.
- `CompGlower` follows power automatically (`CompPowerTrader : IThingGlower` in 1.7); its colour override is
  saved by the glower itself.
- Def `SimpleSigh_ElectronicSign`: 2×1, Metallic stuff + 2 `ComponentIndustrial`, `Electricity` research,
  `CompPowerTrader` (40 W), `CompFlickable`, small `CompGlower`, multiline 256, LED amber default colour,
  Graphic_Multi art with a dark, untinted screen panel.

## Tasks

1. **Visibility and render queue (pure, unit-tested):** `SignVisibility.ShouldDrawText(text, requiresPower,
   hasPowerComp, powerOn)` and `SignVisibility.RenderQueueFor(glowsInDark)`; wire into `CompSignText.PostDraw`
   and `SignTextRenderer`. Glow colour sync on `Apply`. Smoke: comp fields map.
2. **Def, art, strings:** generator draws the electronic sign; structure tests for power/flick/glower comps,
   Metallic-only stuff, components, research, dark panel + bright default colour (contrast test for every sign).
3. **Docs:** M3 checklist (power cut/restore, flick, night, fog, colour/glow sync, save/load), results, README.

Commit per task; `tools/verify.sh` is the gate.
