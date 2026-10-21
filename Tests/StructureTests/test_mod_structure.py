"""Structural checks for the Simple Sigh mod content.

These tests validate XML syntax, identifiers, translations, textures and
packaging hygiene. Passing them does NOT prove that RimWorld loads the mod.
"""

import os
import struct
import unittest
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

PACKAGE_ID = "pengin0503.simplesigh"
STUFF_CATEGORIES = {"Woody", "Stony", "Metallic"}
FORBIDDEN_BINARIES = {
    "assembly-csharp.dll",
    "waylith.dll",
    "unitystubs.dll",
    "0harmony.dll",
    "netstandard.dll",
}
SKIP_DIRS = {".git", ".superpowers", "bin", "obj", ".worktrees"}


def path(*parts):
    return os.path.join(ROOT, *parts)


def parse(*parts):
    return ET.parse(path(*parts)).getroot()


def iter_files(base, suffix):
    for dirpath, dirnames, filenames in os.walk(base):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for name in filenames:
            if name.lower().endswith(suffix):
                yield os.path.join(dirpath, name)


def all_thing_defs():
    defs = []
    for file in iter_files(path("Defs"), ".xml"):
        for node in ET.parse(file).getroot():
            if node.tag == "ThingDef":
                defs.append(node)
    return defs


class ResolvedDef:
    """Field lookup that follows ParentName inside this mod, like RimWorld's XML inheritance.

    Parents outside the mod (e.g. Core's FurnitureBase) end the chain.
    """

    def __init__(self, node, by_name):
        self.node = node
        self.chain = [node]
        parent = node.get("ParentName")
        while parent in by_name:
            self.chain.append(by_name[parent])
            parent = by_name[parent].get("ParentName")

    def find(self, tag):
        for node in self.chain:
            found = node.find(tag)
            if found is not None:
                return found
        return None

    def findtext(self, tag):
        found = self.find(tag)
        return None if found is None else found.text


def thing_def(def_name):
    defs = all_thing_defs()
    by_name = {d.get("Name"): d for d in defs if d.get("Name")}
    matches = [d for d in defs if d.findtext("defName") == def_name]
    if len(matches) != 1:
        raise AssertionError(f"expected exactly one ThingDef {def_name}, found {len(matches)}")
    return ResolvedDef(matches[0], by_name)


def png_size(file):
    with open(file, "rb") as handle:
        header = handle.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise AssertionError(f"{file} is not a PNG")
    return struct.unpack(">II", header[16:24])


def keyed(language):
    root = parse("Languages", language, "Keyed", "SimpleSigh.xml")
    return {child.tag: (child.text or "") for child in root}


def texture_files(tex_path, graphic_class):
    if graphic_class == "Graphic_Multi":
        return [path("Textures", tex_path + suffix + ".png") for suffix in ("_north", "_northm", "_east", "_eastm")]
    return [path("Textures", tex_path + ".png"), path("Textures", tex_path + "_m.png")]


class AboutTests(unittest.TestCase):
    def setUp(self):
        self.about = parse("About", "About.xml")

    def test_identity(self):
        self.assertEqual(self.about.findtext("name"), "Simple Sigh")
        self.assertEqual(self.about.findtext("packageId"), PACKAGE_ID)
        self.assertTrue(self.about.findtext("author"))
        self.assertTrue(self.about.findtext("description"))

    def test_supports_only_1_7(self):
        versions = [li.text for li in self.about.find("supportedVersions")]
        self.assertEqual(versions, ["1.7"])

    def test_dependencies_are_complete_when_declared(self):
        deps = self.about.find("modDependencies")
        if deps is None:
            return
        for li in deps:
            self.assertTrue(li.findtext("packageId"), "dependency without packageId")
            self.assertTrue(li.findtext("displayName"), "dependency without displayName")


class DefTests(unittest.TestCase):
    def test_no_duplicate_def_names(self):
        names = [d.findtext("defName") for d in all_thing_defs() if d.findtext("defName")]
        self.assertEqual(len(names), len(set(names)), names)

    def test_basic_sign_is_stuffable_1x1(self):
        sign = thing_def("SimpleSigh_BasicSign")
        cats = {li.text for li in sign.find("stuffCategories")}
        self.assertTrue(STUFF_CATEGORIES.issubset(cats), cats)
        self.assertEqual(sign.findtext("costStuffCount"), "30")
        size = sign.findtext("size")
        self.assertIn(size, (None, "(1,1)"))

    def test_textures_exist_for_every_mod_def(self):
        for d in all_thing_defs():
            graphic = d.find("graphicData")
            if graphic is None or d.get("Abstract") == "True":
                continue
            graphic_class = graphic.findtext("graphicClass") or "Graphic_Single"
            for file in texture_files(graphic.findtext("texPath"), graphic_class):
                self.assertTrue(os.path.isfile(file), file)
                width, height = png_size(file)
                self.assertGreater(width, 0)
                self.assertGreater(height, 0)


class LanguageTests(unittest.TestCase):
    REQUIRED = {"SimpleSigh_EditText", "SimpleSigh_InvalidText"}

    def test_required_keys_in_both_languages(self):
        for language in ("English", "Japanese"):
            keys = keyed(language)
            self.assertTrue(self.REQUIRED.issubset(keys), (language, self.REQUIRED - set(keys)))
            for key, value in keys.items():
                self.assertTrue(value.strip(), (language, key))

    def test_languages_have_identical_key_sets(self):
        self.assertEqual(set(keyed("English")), set(keyed("Japanese")))


REFERENCE_DIR = os.environ.get("RIMWORLD_REFERENCE_DIR")


def core_defs():
    """Index Core Defs by (tag, defName) and abstract parents by Name."""
    by_def, by_name = set(), set()
    for file in iter_files(os.path.join(REFERENCE_DIR, "Data", "Core", "Defs"), ".xml"):
        try:
            root = ET.parse(file).getroot()
        except ET.ParseError:
            continue
        for node in root:
            if node.findtext("defName"):
                by_def.add((node.tag, node.findtext("defName")))
            if node.get("Name"):
                by_name.add(node.get("Name"))
    return by_def, by_name


@unittest.skipUnless(REFERENCE_DIR, "set RIMWORLD_REFERENCE_DIR to cross-check against RimWorld 1.7 Core Defs")
class CoreReferenceTests(unittest.TestCase):
    """Every Core name our Defs rely on must exist in the 1.7 reference data."""

    @classmethod
    def setUpClass(cls):
        cls.core_def_names, cls.core_parent_names = core_defs()
        cls.mod_names = {d.get("Name") for d in all_thing_defs() if d.get("Name")}

    def assert_core(self, tag, name):
        self.assertTrue((tag, name) in self.core_def_names, f"{tag} {name} missing from Core")

    def test_parents_exist(self):
        for d in all_thing_defs():
            parent = d.get("ParentName")
            if parent and parent not in self.mod_names:
                self.assertTrue(parent in self.core_parent_names, f"parent {parent} missing from Core")

    def test_referenced_core_defs_exist(self):
        for d in all_thing_defs():
            for li in d.findall("stuffCategories/li"):
                self.assert_core("StuffCategoryDef", li.text)
            for stat in d.findall("statBases/*"):
                self.assert_core("StatDef", stat.tag)
            for tag, def_type in (("designationCategory", "DesignationCategoryDef"),
                                  ("graphicData/shaderType", "ShaderTypeDef"),
                                  ("minifiedDef", "ThingDef")):
                value = d.findtext(tag)
                if value:
                    self.assert_core(def_type, value)
            for li in d.findall("researchPrerequisites/li"):
                self.assert_core("ResearchProjectDef", li.text)


class PackagingTests(unittest.TestCase):
    def test_all_xml_parses(self):
        for file in iter_files(ROOT, ".xml"):
            ET.parse(file)

    def test_no_proprietary_binaries_in_repository(self):
        found = [f for f in iter_files(ROOT, ".dll") if os.path.basename(f).lower() in FORBIDDEN_BINARIES]
        self.assertEqual(found, [])


if __name__ == "__main__":
    unittest.main()
