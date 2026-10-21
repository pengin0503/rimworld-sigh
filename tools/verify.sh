#!/usr/bin/env bash
# Local verification matrix: XML/structure tests, game-independent unit tests, and a
# compile of SimpleSigh.dll against RimWorld 1.7 reference assemblies.
# None of these steps runs the game; in-game checks live in docs/testing/.
set -euo pipefail
cd "$(dirname "$0")/.."
config=${1:-Debug}

echo "== structure tests"
python3 -m unittest discover -s Tests/StructureTests

echo "== unit tests"
dotnet test Tests/SimpleSigh.Text.Tests/SimpleSigh.Text.Tests.csproj --nologo -v quiet

echo "== build ($config)"
if [ -z "${RIMWORLD_REFERENCE_DIR:-}" ] && [ -z "${RIMWORLD_GAME_DIR:-}" ]; then
  echo "BUILD NOT RUN: set RIMWORLD_REFERENCE_DIR or RIMWORLD_GAME_DIR" >&2
  exit 3
fi
dotnet build Source/SimpleSigh.csproj -c "$config" --nologo -v quiet -warnaserror

echo "== smoke: load SimpleSigh.dll with the 1.7 references (not the game)"
dotnet run --project Tests/SimpleSigh.GameSmoke -c "$config" -v quiet -- "$PWD"
echo "== all local checks passed (in-game checks not covered)"
