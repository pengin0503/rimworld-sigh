#!/usr/bin/env python3
"""Build a deterministic, installable Simple Sigh runtime-only ZIP.

The ZIP contains a single top-level SimpleSigh/ directory. The packaged DLL
comes from the checked-in 1.7/Assemblies/SimpleSigh.dll; the bundled sign font
ships with its licence; no game DLLs or source/development files are included. This does not run RimWorld.
"""
import argparse
import hashlib
import re
import sys
import zipfile
from pathlib import Path

REQUIRED_FILES = frozenset({
    "About/About.xml",
    "About/Preview.png",
    "About/ModIcon.png",
    "1.7/Assemblies/SimpleSigh.dll",
    "Defs/ThingDefs/BasicSign.xml",
    "Defs/ThingDefs/WallSign.xml",
    "Defs/ThingDefs/NoticeBoard.xml",
    "Defs/ThingDefs/ElectronicSign.xml",
    "Languages/English/Keyed/SimpleSigh.xml",
    "Languages/Japanese/Keyed/SimpleSigh.xml",
    "Fonts/BIZUDPGothic-Bold.ttf",
    "Fonts/BIZUDPGothic-OFL.txt",
})
RUNTIME_RULES = (
    # PublishedFileId.txt only exists after a real Workshop upload; it is shipped when present.
    ("About/", frozenset({"About/About.xml", "About/Preview.png", "About/ModIcon.png", "About/PublishedFileId.txt"})),
    ("1.7/Assemblies/", frozenset({"1.7/Assemblies/SimpleSigh.dll"})),
    ("Defs/", frozenset({".xml"})),
    ("Languages/", frozenset({".xml"})),
    ("Textures/", frozenset({".png"})),
    # The bundled sign font and its SIL OFL 1.1 licence, which must travel with the font.
    ("Fonts/", frozenset({"Fonts/BIZUDPGothic-Bold.ttf", "Fonts/BIZUDPGothic-OFL.txt"})),
)
ZIP_DATE = (2026, 1, 1, 0, 0, 0)


def list_runtime_files(root: Path) -> list[tuple[str, Path]]:
    files = []
    for top, allowed in RUNTIME_RULES:
        top_path = root / top.rstrip("/")
        if not top_path.is_dir() or top_path.is_symlink():
            raise ValueError(f"missing or symlinked runtime directory: {top}")
        for item in sorted(top_path.rglob("*")):
            if item.is_symlink():
                raise ValueError(f"symlink in runtime payload: {item}")
            if not item.is_file():
                continue
            relative = item.relative_to(root).as_posix()
            if relative in allowed or item.suffix.lower() in allowed:
                files.append((relative, item))
    listed = {relative for relative, _ in files}
    missing = sorted(REQUIRED_FILES - listed)
    if missing:
        raise ValueError("missing required runtime files: " + ", ".join(missing))
    if not any(p.startswith("Textures/") for p in listed):
        raise ValueError("missing sign PNG textures")
    dll = root / "1.7/Assemblies/SimpleSigh.dll"
    if dll.read_bytes()[:2] != b"MZ":
        raise ValueError("SimpleSigh.dll is not a PE DLL")
    return sorted(files)


def create_zip(root: Path, output: Path, version: str) -> str:
    if not re.fullmatch(r"v[0-9][0-9A-Za-z._-]{0,64}", version):
        raise ValueError("invalid release version (expected a safe v-prefixed tag)")
    root = root.resolve(strict=True)
    files = list_runtime_files(root)
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for relative, file in files:
            info = zipfile.ZipInfo("SimpleSigh/" + relative, date_time=ZIP_DATE)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, file.read_bytes(),
                             compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    print(f"Created {output} ({len(files)} runtime files, sha256={digest})")
    return digest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--version", type=str, required=True)
    args = parser.parse_args()
    try:
        create_zip(args.root, args.output, args.version)
    except (OSError, ValueError, zipfile.BadZipFile) as exc:
        print(f"Package error: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
