import pathlib
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]

class SimpleSighStructureTests(unittest.TestCase):
    def parse(self, relative):
        return ET.parse(ROOT / relative).getroot()

    def test_identity_and_version(self):
        metadata = self.parse('About/About.xml')
        self.assertEqual(metadata.findtext('name'), 'Simple Sigh')
        self.assertEqual(metadata.findtext('packageId'), 'pengin0503.simplesigh')
        self.assertIn('1.7', [x.text for x in metadata.findall('./supportedVersions/li')])
        self.assertEqual(metadata.findall('./modDependencies/li'), [])

    def test_all_buildings_have_unique_names_and_materials(self):
        seen = set()
        expected = {'SimpleSigh_BasicSign':(1,1,64,False), 'SimpleSigh_WallSign':(1,1,64,False), 'SimpleSigh_NoticeBoard':(2,1,256,True)}
        found = {}
        for path in (ROOT / 'Defs/ThingDefs').glob('*.xml'):
            for thing in ET.parse(path).getroot().findall('./ThingDef'):
                dn = thing.findtext('defName')
                if not dn:continue
                self.assertNotIn(dn, seen)
                seen.add(dn)
                if dn in expected:
                    comp = thing.find('./comps/li')
                    self.assertIsNotNone(comp)
                    self.assertEqual(comp.get('Class'), 'SimpleSigh.Signs.CompProperties_SignText')
                    found[dn]=(thing,comp)
        self.assertEqual(set(found), set(expected))
        for name,(w,h,max_elements,multiline) in expected.items():
            thing,comp=found[name]
            self.assertEqual(thing.findtext('size'),f'({w},{h})')
            self.assertEqual(comp.findtext('maxElements'),str(max_elements))
            self.assertEqual(comp.findtext('multiline'),str(multiline).lower())
            self.assertIn('Woody', [x.text for x in thing.findall('./stuffCategories/li')])
            self.assertIn('Metallic', [x.text for x in thing.findall('./stuffCategories/li')])
            self.assertIn('Stony', [x.text for x in thing.findall('./stuffCategories/li')])
            path=thing.findtext('./graphicData/texPath')
            self.assertTrue(path)
            self.assertTrue((ROOT / 'Textures' / (path+'.png')).is_file(), path)

    def test_two_localizations(self):
        for locale in ('English','Japanese'):
            tree=self.parse(f'Languages/{locale}/Keyed/SimpleSigh.xml')
            for tag in ('SimpleSigh_EditText','SimpleSigh_InvalidText','SimpleSigh_BasicSignLabel','SimpleSigh_WallSignLabel','SimpleSigh_NoticeBoardLabel'):
                self.assertTrue(tree.findtext(tag),f'{locale} {tag}')

    def test_build_layout_and_ignore_files(self):
        s=(ROOT / 'Source/SimpleSigh.csproj').read_text(encoding='utf-8')
        self.assertIn('<TargetFramework>net10.0</TargetFramework>',s)
        for x in ('Assembly-CSharp.dll','Waylith.dll','UnityStubs.dll'):
            self.assertIn(x,s)
        self.assertIn('RIMWORLD_GAME_DIR',s)
        self.assertIn('Private>False',s)
        self.assertTrue((ROOT/'.gitignore').exists())
        self.assertNotIn('Assembly-CSharp.dll', [p.name for p in ROOT.rglob('*.dll')])

if __name__=='__main__':unittest.main()
