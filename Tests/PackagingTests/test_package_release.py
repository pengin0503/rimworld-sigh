"""Regression tests for the install-ready Simple Sigh release archive.

Fixtures use a fake MZ-prefixed DLL: packaging tests do not prove game execution.
"""
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[2] / "tools" / "package_release.py"


class PackageReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "repo"
        self.root.mkdir()
        self.output = Path(self.temp.name) / "SimpleSigh-v0.1.0-test.zip"
        self.runtime = {
            "About/About.xml": b"<ModMetaData><name>Simple Sigh</name></ModMetaData>",
            "About/Preview.png": b"\x89PNG\r\n\x1a\npreview",
            "About/ModIcon.png": b"\x89PNG\r\n\x1a\nicon",
            "1.7/Assemblies/SimpleSigh.dll": b"MZmock-test-assembly",
            "Defs/ThingDefs/BasicSign.xml": b"<Defs/>",
            "Defs/ThingDefs/WallSign.xml": b"<Defs/>",
            "Defs/ThingDefs/NoticeBoard.xml": b"<Defs/>",
            "Defs/ThingDefs/ElectronicSign.xml": b"<Defs/>",
            "Languages/English/Keyed/SimpleSigh.xml": b"<LanguageData/>",
            "Languages/Japanese/Keyed/SimpleSigh.xml": b"<LanguageData/>",
            "Languages/Japanese/DefInjected/ThingDef/SimpleSigh.xml": b"<LanguageData/>",
            "Textures/Things/Building/SimpleSign.png": b"\x89PNG\r\n\x1a\nmock",
            "Fonts/BIZUDPGothic-Bold.ttf": b"\x00\x01\x00\x00mock-font",
            "Fonts/BIZUDPGothic-OFL.txt": b"SIL OPEN FONT LICENSE Version 1.1",
        }
        for relative, data in self.runtime.items():
            file = self.root / relative
            file.parent.mkdir(parents=True, exist_ok=True)
            file.write_bytes(data)

    def call(self, version="v0.1.0-test"):
        return subprocess.run(
            [sys.executable, str(SCRIPT), "--root", str(self.root),
             "--output", str(self.output), "--version", version],
            capture_output=True, text=True, check=False,
        )

    def test_zip_has_one_installable_mod_folder_and_only_runtime_content(self):
        extras = [
            "README.md", "Source/Secrets.cs", "Tests/Bad.cs", "docs/notes.md",
            "tools/tool.py", ".github/workflows/ci.yml",
            "Textures/Things/Building/debug.py",
            "Fonts/Other.ttf", "Fonts/notes.md",
            "1.7/Assemblies/Assembly-CSharp.dll", "1.7/Assemblies/0Harmony.dll",
        ]
        for path in extras:
            f = self.root / path
            f.parent.mkdir(parents=True, exist_ok=True)
            f.write_bytes(b"NOT_FOR_RELEASE")
        result = self.call()
        self.assertEqual(result.returncode, 0, result.stderr)
        with zipfile.ZipFile(self.output) as archive:
            files = archive.namelist()
            self.assertEqual(files, sorted(files))
            self.assertEqual(set(files), {"SimpleSigh/" + p for p in self.runtime})
            for name, data in self.runtime.items():
                self.assertEqual(archive.read("SimpleSigh/" + name), data)
            self.assertTrue(all(x.startswith("SimpleSigh/") for x in files))

    def test_missing_preview_rejected(self):
        (self.root / "About/Preview.png").unlink()
        result = self.call()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("About/Preview.png", result.stderr + result.stdout)

    def test_workshop_id_is_packaged_when_present(self):
        (self.root / "About/PublishedFileId.txt").write_bytes(b"1234567890")
        (self.root / "About/notes.txt").write_bytes(b"NOT_FOR_RELEASE")
        result = self.call()
        self.assertEqual(result.returncode, 0, result.stderr)
        with zipfile.ZipFile(self.output) as archive:
            names = set(archive.namelist())
        self.assertIn("SimpleSigh/About/PublishedFileId.txt", names)
        self.assertNotIn("SimpleSigh/About/notes.txt", names)

    def test_font_without_licence_rejected(self):
        """The OFL requires the licence to accompany the font, so a ZIP without it must not be built."""
        (self.root / "Fonts/BIZUDPGothic-OFL.txt").unlink()
        result = self.call()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("BIZUDPGothic-OFL.txt", result.stderr)

    def test_missing_dll_rejected(self):
        (self.root / "1.7/Assemblies/SimpleSigh.dll").unlink()
        result = self.call()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("SimpleSigh.dll", result.stderr)
        self.assertFalse(self.output.exists())

    def test_invalid_dll_rejected(self):
        (self.root / "1.7/Assemblies/SimpleSigh.dll").write_bytes(b"not-a-DLL")
        result = self.call()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("not a PE DLL", result.stderr)

    def test_invalid_version_rejected(self):
        result = self.call("../evil")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("version", result.stderr)

    def test_deterministic_zip_bytes(self):
        self.assertEqual(self.call().returncode, 0)
        first = self.output.read_bytes()
        self.assertEqual(self.call().returncode, 0)
        self.assertEqual(self.output.read_bytes(), first)

    def test_missing_required_definition_fails(self):
        (self.root / "Defs/ThingDefs/WallSign.xml").unlink()
        result = self.call()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("WallSign.xml", result.stderr)

    def test_symlink_in_payload_rejected(self):
        outside = Path(self.temp.name) / "private.png"
        outside.write_bytes(b"secret")
        link = self.root / "Textures/Things/Building/Secret.png"
        try:
            link.symlink_to(outside)
        except (OSError, NotImplementedError) as exc:
            self.skipTest(f"symlink unavailable: {exc}")
        result = self.call()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("symlink", result.stderr)


if __name__ == "__main__":
    unittest.main()
