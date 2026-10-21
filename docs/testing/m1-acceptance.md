# Simple Sigh — M1 acceptance record

Date: 2026-10-21. Environment: Linux cloud container, .NET SDK 10.0.402, Python 3.13,
RimWorld 1.7 **reference assemblies** from `rimworld_DLL.zip` (SHA-256 `dab2b7f3…a279`). No game executable.

## Verdict

| Gate | Result |
| --- | --- |
| Structure / XML (21 tests, incl. Core 1.7 cross-reference) | PASS |
| Unit tests (78 xUnit tests) | PASS |
| Release build against 1.7 references, `-warnaserror` | PASS (0 warnings, 0 errors) |
| Smoke (load DLL with 1.7 assemblies; real `ScribeSaver` write) | PASS |
| In-game acceptance (checklist B*, P*, R*, C*) | **NOT RUN** — no RimWorld executable |

**M1 status: implemented and build-verified; NOT accepted in game.** M1 must not be called complete
for players until the in-game checklist has been run.

## Commands and observed output

```
$ RIMWORLD_REFERENCE_DIR=<refs> python3 -m unittest discover -s Tests/StructureTests -v
Ran 21 tests in 0.203s
OK

$ dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj
Passed!  - Failed: 0, Passed: 78, Skipped: 0, Total: 78

$ dotnet build Source/SimpleSigh.csproj -c Release -warnaserror
Build succeeded.  0 Warning(s)  0 Error(s)
sha256(1.7/Assemblies/SimpleSigh.dll) = 674498921c3949172f6cccc705ab5d0641b1beef5be5479152a9bca5880073c5

$ dotnet run --project Tests/SimpleSigh.GameSmoke -c Release -- "$PWD"
  ok   SimpleSigh.dll types resolve against 1.7 (AssemblyIsUsable)
  ok   BasicSign.xml SimpleSigh_BasicSign: comp XML elements map to public fields of the right type
      saved XML: <simpleSighText>|研究室 C:%5Cnew%0A2行目  |</simpleSighText><simpleSighColor>RGBA(0.620, 0.120, 0.100, 1.000)</simpleSighColor><simpleSighScale>0.5</simpleSighScale>
  ok   CompSignText.PostExposeData writes through the real 1.7 ScribeSaver
SMOKE PASS (assembly/XML only; the game was not run)
```

## What the smoke harness cannot do

The game's XML loader (`DirectXmlToObject`/`XmlInheritance`) and `ParseHelper` cannot initialise outside
the game: their static constructors load engine and Steam assemblies (`Brutal.Core.Logging`,
`com.rlabrecque.steamworks.net`) that are not in the reference ZIP. The load half of the save round trip
is therefore covered by unit tests that replay the decompiled `ParseHelper.ParseString` rule, not by
the real loader.

## Dependencies

- Declared mod dependencies: none. Harmony is not referenced (`Tests/StructureTests` enforces that a
  Harmony reference requires a `brrainz.harmony` dependency). No `0Harmony.dll` or game DLL is in the repository.

## Known limitations at M1

- Japanese glyph availability in non-Japanese game languages is unverified (backend not in references).
- Lighting/fog ordering relies on render-queue values read from `Shaders.Map.xml`; not observed in game.
- Text colour precision in saves is 3 decimals (vanilla `Color.ToString`).
