# Simple Sigh — M3 electronic sign, in-game checklist (RimWorld 1.7)

All rows `NOT RUN (no game)` until run in RimWorld 1.7.

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| E1 | Research Electricity, build from steel/plasteel (+2 components) | Only metallic stuff offered; cost includes components | NOT RUN (no game) |
| E2 | Connect to a powered grid, edit text | Text lit in the chosen colour on the dark screen | NOT RUN (no game) |
| E3 | Flick off / disconnect / drain batteries | Text disappears; screen stays dark; text not lost | NOT RUN (no game) |
| E4 | Restore power | Same text reappears without editing; dev counter `builds` unchanged | NOT RUN (no game) |
| E5 | Night / unlit room | LED text stays bright (queue 3150 after LightOverlay 3100); the board itself is dark | NOT RUN (no game) |
| E6 | Fog of war | Text hidden (queue 3150 < FogOfWar 3175, plus fog check) | NOT RUN (no game) |
| E7 | Change text colour | Small glow around the board changes to the text colour; survives save/load | NOT RUN (no game) |
| E8 | Save during a power cut, load | Text hidden until power returns, then shown | NOT RUN (no game) |
| E9 | Multi-line Japanese, rotations N/E/S/W | Same wrapping/fit behaviour as the notice board; portrait on E/W | NOT RUN (no game) |
| E10 | Power consumption | 40 W shown in the inspect pane | NOT RUN (no game) |
