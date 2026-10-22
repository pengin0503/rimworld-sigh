#!/usr/bin/env bash
# Local verification matrix: XML/structure tests, game-independent unit tests, a compile of
# SimpleSigh.dll against RimWorld 1.7 reference assemblies, and (with real game assemblies) a smoke load.
# None of these steps runs the game; in-game checks live in docs/testing/.
#
# References: by default the public Krafs.Rimworld.Ref package (signatures only). Set
# RIMWORLD_REFERENCE_DIR or RIMWORLD_GAME_DIR to compile against local assemblies instead; the smoke
# test needs those real assemblies because reference assemblies contain no code to run.
set -euo pipefail
cd "$(dirname "$0")/.."
config=${1:-Release}

echo "== structure tests"
python3 -m unittest discover -s Tests/StructureTests

echo "== packaging tests"
python3 -m unittest discover -s Tests/PackagingTests

echo "== unit tests"
dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj --nologo -v quiet

echo "== build ($config)"
dotnet build Source/SimpleSigh.csproj -c "$config" --nologo -v quiet -warnaserror

if [ -n "${RIMWORLD_REFERENCE_DIR:-}" ] || [ -n "${RIMWORLD_GAME_DIR:-}" ]; then
  echo "== smoke: load SimpleSigh.dll with the 1.7 assemblies (not the game)"
  dotnet run --project Tests/SimpleSigh.GameSmoke -c "$config" -v quiet -- "$PWD"
else
  echo "SMOKE NOT RUN: needs real 1.7 assemblies (set RIMWORLD_REFERENCE_DIR or RIMWORLD_GAME_DIR)"
fi

if [ "$config" = "Release" ] && ! git diff --quiet HEAD -- 1.7/Assemblies/SimpleSigh.dll 2>/dev/null; then
  echo "FAIL: 1.7/Assemblies/SimpleSigh.dll differs from the committed DLL. Commit the rebuilt DLL together with the source change." >&2
  exit 4
fi
echo "== all local checks passed (in-game checks not covered)"
