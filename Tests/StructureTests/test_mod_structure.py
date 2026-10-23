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


def all_defs(tag):
    found = []
    for file in iter_files(path("Defs"), ".xml"):
        for node in ET.parse(file).getroot():
            if node.tag == tag:
                found.append(node)
    return found


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

    def test_about_has_workshop_fields(self):
        """RimWorld 1.7 ModMetaData reads modVersion, About/Preview.png and About/ModIcon.png."""
        import re
        self.assertRegex(self.about.findtext("modVersion") or "", r"^\d+\.\d+\.\d+$")
        preview = path("About", "Preview.png")
        icon = path("About", "ModIcon.png")
        self.assertTrue(os.path.isfile(preview), "About/Preview.png missing")
        self.assertTrue(os.path.isfile(icon), "About/ModIcon.png missing")
        self.assertLessEqual(os.path.getsize(preview), 1024 * 1024, "Workshop previews must be <= 1 MB")
        width, height = png_size(preview)
        self.assertIn(round(width / height, 2), (1.0, 1.78), "preview should be 1:1 or 16:9")
        icon_w, icon_h = png_size(icon)
        self.assertEqual(icon_w, icon_h)

    def test_no_invented_workshop_id(self):
        """PublishedFileId.txt is written by RimWorld on upload; a made-up ID would point at another item."""
        self.assertFalse(os.path.exists(path("About", "PublishedFileId.txt")))

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


class WallSignTests(unittest.TestCase):
    """Follows vanilla 1.7 wall attachments (WallLamp / TorchWallLamp in Buildings_Furniture.xml)."""

    def setUp(self):
        self.sign = thing_def("SimpleSigh_WallSign")

    def test_is_stuffable_single_line_1x1(self):
        cats = {li.text for li in self.sign.find("stuffCategories")}
        self.assertTrue(STUFF_CATEGORIES.issubset(cats))
        self.assertIn(self.sign.findtext("size"), (None, "(1,1)"))
        props = comp_props(self.sign, SignCompTests.PROPS)
        self.assertEqual(props.findtext("maxElements"), "64")
        self.assertEqual(props.findtext("multiline"), "false")

    def test_mounts_on_walls_like_vanilla_attachments(self):
        self.assertEqual(self.sign.findtext("rotatable"), "true")
        self.assertEqual(self.sign.findtext("building/isAttachment"), "true")
        self.assertEqual(self.sign.findtext("building/isEdifice"), "false")
        self.assertEqual([li.text for li in self.sign.find("placeWorkers")], ["Placeworker_AttachedToWall"])
        self.assertEqual(self.sign.find("terrainAffordanceNeeded").get("IsNull"), "True")
        self.assertEqual(self.sign.findtext("passability"), "Standable")
        self.assertEqual(self.sign.findtext("altitudeLayer"), "BuildingOnTop")

    def test_graphic_is_drawn_upright_on_the_wall_for_every_rotation(self):
        graphic = self.sign.find("graphicData")
        self.assertEqual(graphic.findtext("drawRotated"), "false")
        expected = {"North": "(0,0,0.9)", "South": "(0,0,-0.9)", "East": "(0.9,0,0)", "West": "(-0.9,0,0)"}
        for rot, offset in expected.items():
            self.assertEqual(graphic.findtext(f"drawOffset{rot}"), offset, rot)


class NoticeBoardTests(unittest.TestCase):
    def setUp(self):
        self.board = thing_def("SimpleSigh_NoticeBoard")

    def test_is_stuffable_2x1(self):
        cats = {li.text for li in self.board.find("stuffCategories")}
        self.assertTrue(STUFF_CATEGORIES.issubset(cats))
        self.assertEqual(self.board.findtext("size"), "(2,1)")
        self.assertEqual(self.board.findtext("rotatable"), "true")

    def test_is_multiline_256(self):
        props = comp_props(self.board, SignCompTests.PROPS)
        self.assertEqual(props.findtext("maxElements"), "256")
        self.assertEqual(props.findtext("multiline"), "true")
        self.assertTrue(props.findtext("panelRectHorizontal"))

    def test_uses_portrait_art_for_east_west(self):
        graphic = self.board.find("graphicData")
        self.assertEqual(graphic.findtext("graphicClass"), "Graphic_Multi")
        self.assertEqual(graphic.findtext("drawSize"), "(2,1)")
        north = png_size(path("Textures", graphic.findtext("texPath") + "_north.png"))
        east = png_size(path("Textures", graphic.findtext("texPath") + "_east.png"))
        self.assertEqual(north[0], 2 * north[1])
        self.assertEqual(east[1], 2 * east[0])


class ElectronicSignTests(unittest.TestCase):
    def setUp(self):
        self.sign = thing_def("SimpleSigh_ElectronicSign")

    def comp(self, cls):
        return comp_props(self.sign, cls)

    def test_is_metal_2x1_with_components(self):
        self.assertEqual({li.text for li in self.sign.find("stuffCategories")}, {"Metallic"})
        self.assertEqual(self.sign.findtext("size"), "(2,1)")
        self.assertEqual(self.sign.findtext("costList/ComponentIndustrial"), "2")
        self.assertEqual([li.text for li in self.sign.find("researchPrerequisites")], ["Electricity"])

    def test_needs_power_and_can_be_switched(self):
        power = self.comp("CompProperties_Power")
        self.assertEqual(power.findtext("compClass"), "CompPowerTrader")
        self.assertGreater(float(power.findtext("basePowerConsumption")), 0)
        self.comp("CompProperties_Flickable")
        glower = self.comp("CompProperties_Glower")
        self.assertGreater(float(glower.findtext("glowRadius")), 0)

    def test_text_is_power_gated_led(self):
        props = self.comp(SignCompTests.PROPS)
        for field in ("requiresPower", "glowsInDark", "syncGlowColor", "multiline"):
            self.assertEqual(props.findtext(field), "true", field)
        self.assertEqual(props.findtext("maxElements"), "256")
        self.assertTrue(props.findtext("defaultColor"))


class SettingsDefTests(unittest.TestCase):
    def test_toggle_text_key_binding_exists(self):
        keys = {d.findtext("defName"): d for d in all_defs("KeyBindingDef")}
        self.assertIn("SimpleSigh_ToggleSignText", keys)
        key = keys["SimpleSigh_ToggleSignText"]
        self.assertTrue(key.findtext("label"))
        self.assertTrue(key.findtext("category"))

    def test_sign_list_tab_is_a_minimized_icon_button(self):
        buttons = {d.findtext("defName"): d for d in all_defs("MainButtonDef")}
        self.assertIn("SimpleSigh_Signs", buttons)
        button = buttons["SimpleSigh_Signs"]
        self.assertEqual(button.findtext("minimized"), "true")
        icon = path("Textures", button.findtext("iconPath") + ".png")
        self.assertTrue(os.path.isfile(icon), icon)
        cls = button.findtext("tabWindowClass")
        namespace, _, name = cls.rpartition(".")
        sources = "\n".join(source_text().values())
        self.assertIn(f"namespace {namespace};", sources)
        self.assertIn(f"class {name}", sources)

    def test_every_mod_def_has_japanese_label(self):
        injected_dir = path("Languages", "Japanese", "DefInjected")
        for tag in ("KeyBindingDef", "MainButtonDef"):
            for d in all_defs(tag):
                file = os.path.join(injected_dir, tag, "SimpleSigh.xml")
                self.assertTrue(os.path.isfile(file), file)
                injected = {c.tag for c in ET.parse(file).getroot()}
                self.assertIn(d.findtext("defName") + ".label", injected)


class LanguageTests(unittest.TestCase):
    REQUIRED = {"SimpleSigh_EditText", "SimpleSigh_InvalidText"}

    def test_required_keys_in_both_languages(self):
        for language in ("English", "Japanese"):
            keys = keyed(language)
            self.assertTrue(self.REQUIRED.issubset(keys), (language, self.REQUIRED - set(keys)))
            for key, value in keys.items():
                self.assertTrue(value.strip(), (language, key))

    def test_every_def_has_japanese_label_and_description(self):
        root = parse("Languages", "Japanese", "DefInjected", "ThingDef", "SimpleSigh.xml")
        injected = {child.tag: (child.text or "").strip() for child in root}
        for d in all_thing_defs():
            name = d.findtext("defName")
            if not name:
                continue
            self.assertTrue(d.findtext("label"), name)
            self.assertTrue(d.findtext("description"), name)
            self.assertTrue(injected.get(f"{name}.label"), f"{name}.label")
            self.assertTrue(injected.get(f"{name}.description"), f"{name}.description")

    def test_languages_have_identical_key_sets(self):
        self.assertEqual(set(keyed("English")), set(keyed("Japanese")))


def source_text():
    texts = {}
    for file in iter_files(path("Source"), ".cs"):
        with open(file, encoding="utf-8") as handle:
            texts[file] = handle.read()
    return texts


def read_png(file):
    """Minimal 8-bit RGBA/RGB PNG decoder (all filter types) returning (width, height, rows of RGBA tuples)."""
    import zlib
    with open(file, "rb") as handle:
        data = handle.read()
    pos, idat, width, height, channels = 8, b"", 0, 0, 4
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind, body = data[pos + 4:pos + 8], data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height, depth, color_type = struct.unpack(">IIBB", body[:10])
            if depth != 8 or color_type not in (2, 6):
                raise AssertionError(f"{file}: unsupported PNG format")
            channels = 4 if color_type == 6 else 3
        elif kind == b"IDAT":
            idat += body
        pos += 12 + length
    raw, stride, rows, prev = zlib.decompress(idat), width * channels, [], bytearray(width * channels)
    for y in range(height):
        start = y * (stride + 1)
        filt, line = raw[start], bytearray(raw[start + 1:start + 1 + stride])
        for i in range(stride):
            a = line[i - channels] if i >= channels else 0
            b = prev[i]
            c = prev[i - channels] if i >= channels else 0
            if filt == 1:
                line[i] = (line[i] + a) & 255
            elif filt == 2:
                line[i] = (line[i] + b) & 255
            elif filt == 3:
                line[i] = (line[i] + (a + b) // 2) & 255
            elif filt == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append([tuple(line[x * channels:(x + 1) * channels]) + ((255,) if channels == 3 else ()) for x in range(width)])
        prev = line
    return width, height, rows


def parse_rect(text):
    values = [float(v) for v in text.strip().strip("()").split(",")]
    if len(values) != 4:
        raise AssertionError(f"bad rect {text}")
    return values


def comp_props(resolved, class_name):
    comps = resolved.find("comps")
    matches = [li for li in comps if li.get("Class") == class_name] if comps is not None else []
    if len(matches) != 1:
        raise AssertionError(f"expected one {class_name} comp, found {len(matches)}")
    return matches[0]


class ProjectTests(unittest.TestCase):
    GAME_REFERENCES = ("Assembly-CSharp", "Waylith", "UnityStubs")

    def setUp(self):
        self.project = parse("Source", "SimpleSigh.csproj")

    def prop(self, name):
        values = [n.text for n in self.project.iter(name) if n.text]
        self.assertTrue(values, f"{name} missing")
        return values[0]

    def test_targets_net10_and_names_assembly(self):
        self.assertEqual(self.prop("TargetFramework"), "net10.0")
        self.assertEqual(self.prop("AssemblyName"), "SimpleSigh")
        self.assertEqual(self.prop("RootNamespace"), "SimpleSigh")

    def output_paths(self):
        """Map of configuration condition -> OutputPath."""
        result = {}
        for group in self.project.iter("PropertyGroup"):
            node = group.find("OutputPath")
            if node is not None:
                result[group.get("Condition") or ""] = node.text.replace("\\", "/").rstrip("/")
        return result

    def test_only_release_outputs_into_versioned_assemblies_folder(self):
        paths = self.output_paths()
        is_release = lambda condition: "== 'Release'" in condition.replace('"', "'")
        release = [v for k, v in paths.items() if is_release(k)]
        self.assertEqual(release, ["../1.7/Assemblies"])
        others = [v for k, v in paths.items() if not is_release(k)]
        self.assertTrue(others and all("1.7" not in v for v in others), paths)
        self.assertEqual(self.prop("AppendTargetFrameworkToOutputPath"), "false")

    def test_committed_dll_is_reproducible(self):
        """The SDK otherwise bakes the git commit id into the DLL, so every commit would change it."""
        self.assertEqual(self.prop("IncludeSourceRevisionInInformationalVersion"), "false")
        self.assertEqual(self.prop("Deterministic"), "true")

    def test_game_references_are_not_copied(self):
        refs = {r.get("Include"): r for r in self.project.iter("Reference")}
        for name in self.GAME_REFERENCES:
            self.assertIn(name, refs)
            self.assertEqual(refs[name].findtext("Private"), "false", name)

    def test_public_reference_package_is_pinned_and_compile_only(self):
        """Krafs.Rimworld.Ref 1.7.4982-unstable is the game build the shipped DLL is reproduced from."""
        packages = {p.get("Include"): p for p in self.project.iter("PackageReference")}
        self.assertIn("Krafs.Rimworld.Ref", packages)
        package = packages["Krafs.Rimworld.Ref"]
        self.assertEqual(package.get("Version"), "$(RimRefVersion)")
        self.assertEqual(self.prop("RimRefVersion"), "1.7.4982-unstable")
        self.assertEqual(package.get("ExcludeAssets"), "runtime")
        self.assertEqual(package.get("PrivateAssets"), "all")

    def test_no_harmony_reference_without_dependency(self):
        refs = {r.get("Include") for r in self.project.iter("Reference")}
        packages = {r.get("Include") for r in self.project.iter("PackageReference")}
        uses_harmony = "0Harmony" in refs or "Lib.Harmony" in packages
        deps = parse("About", "About.xml").find("modDependencies")
        declared = deps is not None and any(li.findtext("packageId") == "brrainz.harmony" for li in deps)
        self.assertEqual(uses_harmony, declared)


class SignCompTests(unittest.TestCase):
    PROPS = "SimpleSigh.Signs.CompProperties_SignText"

    def test_basic_sign_is_single_line_64(self):
        props = comp_props(thing_def("SimpleSigh_BasicSign"), self.PROPS)
        self.assertEqual(props.findtext("maxElements"), "64")
        self.assertEqual(props.findtext("multiline"), "false")

    def sign_defs(self):
        for d in all_thing_defs():
            if d.get("Abstract") == "True" or not d.findtext("defName"):
                continue
            resolved = thing_def(d.findtext("defName"))
            comps = resolved.find("comps")
            if comps is not None and any(li.get("Class") == self.PROPS for li in comps):
                yield resolved

    def test_sign_defs_enable_realtime_draw_and_map_removal_cleanup(self):
        """PostDraw only runs for realtime drawers; notifyMapRemoved lets the comp free its mesh."""
        signs = list(self.sign_defs())
        self.assertTrue(signs)
        for sign in signs:
            self.assertEqual(sign.findtext("drawerType"), "MapMeshAndRealTime")
            self.assertEqual(sign.findtext("notifyMapRemoved"), "true")

    def test_panel_rects_lie_on_untinted_panel(self):
        """Text is placed inside panelRect; that area must be the light, untinted panel."""
        for d in all_thing_defs():
            if d.get("Abstract") == "True" or d.find("graphicData") is None:
                continue
            resolved = thing_def(d.findtext("defName"))
            comps = resolved.find("comps")
            props = [li for li in comps if li.get("Class") == self.PROPS] if comps is not None else []
            if not props:
                continue
            graphic = resolved.find("graphicData")
            tex = graphic.findtext("texPath")
            multi = graphic.findtext("graphicClass") == "Graphic_Multi"
            cases = [(tex + ("_north" if multi else ""), props[0].findtext("panelRect"))]
            if multi:
                cases.append((tex + "_east", props[0].findtext("panelRectHorizontal") or props[0].findtext("panelRect")))
            for name, rect_text in cases:
                self.assertTrue(rect_text, f"{d.findtext('defName')} needs panelRect")
                x, y, w, h = parse_rect(rect_text)
                self.assertTrue(0 <= x and 0 <= y and x + w <= 1.0001 and y + h <= 1.0001, rect_text)
                width, height, mask = read_png(path("Textures", name + ("m" if multi else "_m") + ".png"))
                _, _, color = read_png(path("Textures", name + ".png"))
                x0, x1 = round(x * width), round((x + w) * width) - 1
                y0, y1 = round((1 - y - h) * height), round((1 - y) * height) - 1
                for px in range(x0, x1 + 1, max(1, (x1 - x0) // 8)):
                    for py in range(y0, y1 + 1, max(1, (y1 - y0) // 8)):
                        self.assertEqual(mask[py][px][0], 0, f"{name} panel tinted at {px},{py}")
                        self.assertEqual(color[py][px][3], 255, f"{name} panel transparent at {px},{py}")
                        if props[0].findtext("glowsInDark") == "true":
                            self.assertLess(sum(color[py][px][:3]), 150, f"{name} LED panel not dark at {px},{py}")
                        else:
                            self.assertGreater(sum(color[py][px][:3]), 550, f"{name} panel not light at {px},{py}")

    def test_default_text_colour_contrasts_with_panel(self):
        """WCAG-style contrast >= 4.5 between the default text colour and the average panel colour."""
        def luminance(rgb):
            def channel(c):
                return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4
            r, g, b = (channel(v) for v in rgb)
            return 0.2126 * r + 0.7152 * g + 0.0722 * b

        for sign in self.sign_defs():
            props = comp_props(sign, self.PROPS)
            default = props.findtext("defaultColor") or "(0.13,0.11,0.09)"
            values = [float(v) for v in default.strip("()").replace("RGBA", "").split(",")]
            if max(values[:3]) > 1:
                values = [v / 255 for v in values]
            graphic = sign.find("graphicData")
            multi = graphic.findtext("graphicClass") == "Graphic_Multi"
            name = graphic.findtext("texPath") + ("_north" if multi else "")
            x, y, w, h = parse_rect(props.findtext("panelRect"))
            width, height, pixels = read_png(path("Textures", name + ".png"))
            samples = [pixels[py][px] for py in range(round((1 - y - h) * height) + 4, round((1 - y) * height) - 4, 3)
                       for px in range(round(x * width) + 4, round((x + w) * width) - 4, 3)]
            panel = [sum(p[i] for p in samples) / len(samples) / 255 for i in range(3)]
            light, dark = sorted((luminance(panel), luminance(values[:3])), reverse=True)
            ratio = (light + 0.05) / (dark + 0.05)
            self.assertGreaterEqual(ratio, 4.5, f"{sign.findtext('defName')}: contrast {ratio:.2f}")

    def test_comp_classes_exist_in_source(self):
        sources = "\n".join(source_text().values())
        for d in all_thing_defs():
            for li in d.findall("comps/li"):
                cls = li.get("Class") or ""
                if cls.startswith("SimpleSigh."):
                    namespace, _, name = cls.rpartition(".")
                    self.assertIn(f"namespace {namespace};", sources)
                    self.assertIn(f"class {name}", sources)

    def test_keyed_strings_used_in_source_exist(self):
        import re
        used = set()
        for text in source_text().values():
            used.update(re.findall(r'"(SimpleSigh_[A-Za-z0-9_]+)"', text))
        def_names = set()
        for file in iter_files(path("Defs"), ".xml"):
            def_names.update(n.findtext("defName") for n in ET.parse(file).getroot() if n.findtext("defName"))
        used -= def_names
        self.assertTrue(used, "no keyed strings found in Source")
        for language in ("English", "Japanese"):
            missing = used - set(keyed(language))
            self.assertEqual(missing, set(), language)


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

    def test_key_binding_categories_exist(self):
        for d in all_defs("KeyBindingDef"):
            self.assert_core("KeyBindingCategoryDef", d.findtext("category"))

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
            for item in d.findall("costList/*"):
                self.assert_core("ThingDef", item.tag)


class PackagingTests(unittest.TestCase):
    def test_all_xml_parses(self):
        for file in iter_files(ROOT, ".xml"):
            ET.parse(file)

    def test_no_proprietary_binaries_in_repository(self):
        found = [f for f in iter_files(ROOT, ".dll") if os.path.basename(f).lower() in FORBIDDEN_BINARIES]
        self.assertEqual(found, [])


class SignFontTests(unittest.TestCase):
    """The bundled sign font (#4): a TrueType file the C# loader accepts, shipped with its OFL licence."""

    FONT = os.path.join(ROOT, "Fonts", "BIZUDPGothic-Bold.ttf")
    LICENCE = os.path.join(ROOT, "Fonts", "BIZUDPGothic-OFL.txt")

    def test_font_is_a_truetype_outline_font(self):
        with open(self.FONT, "rb") as f:
            header = f.read(12)
        self.assertEqual(struct.unpack(">I", header[:4])[0], 0x00010000)

    def test_licence_is_ofl_without_reserved_font_name(self):
        with open(self.LICENCE, encoding="utf-8") as f:
            text = f.read()
        self.assertIn("SIL Open Font License, Version 1.1", text)
        self.assertIn("BIZ UDGothic Project Authors", text)
        # A Reserved Font Name (declared after the copyright line) would restrict modified copies; none is declared.
        copyright_statement = text.split("This Font Software is licensed")[0]
        self.assertNotIn("Reserved Font Name", copyright_statement)

    def test_loader_path_matches_the_bundled_file(self):
        with open(os.path.join(ROOT, "Source", "Rendering", "SignFont.cs"), encoding="utf-8") as f:
            source = f.read()
        self.assertIn('FontRelativePath = "Fonts/BIZUDPGothic-Bold.ttf"', source)

    def test_mod_class_records_the_mod_folder(self):
        with open(os.path.join(ROOT, "Source", "SimpleSighMod.cs"), encoding="utf-8") as f:
            source = f.read()
        self.assertIn("class SimpleSighMod : Mod", source)
        self.assertIn("SignFont.ModRootDir = content.RootDir", source)


if __name__ == "__main__":
    unittest.main()
