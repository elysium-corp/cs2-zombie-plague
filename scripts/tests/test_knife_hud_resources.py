"""Проверки контракта Panorama; не заменяют CS2 resourcecompiler."""
from pathlib import Path
import importlib.util
import json
import re
import subprocess
import sys
import unittest
import xml.etree.ElementTree as ET
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
PANORAMA = ROOT / 'CustomKnife.Core/resources/hud/knife-selector/content/panorama'


class KnifeHudResourcesTests(unittest.TestCase):
    def setUp(self):
        self.xml = ET.parse(PANORAMA / 'layout/custom_game/elysium_knife_selector_v4.xml').getroot()
        self.css = (PANORAMA / 'styles/custom_game/elysium_knife_selector_v4.css').read_text()
        self.ids = [element.get('id') for element in self.xml.iter() if element.get('id')]

    def test_generated_resources_are_in_sync(self):
        subprocess.run([sys.executable, str(ROOT / 'scripts/generate-knife-hud.py'), '--check'], check=True)

    def test_addressable_panels_are_inside_an_anonymous_root(self):
        self.assertEqual('root', self.xml.tag)
        self.assertNotIn('id', self.xml.find('Panel').attrib)
        self.assertEqual(len(self.ids), len(set(self.ids)))
        self.assertIn('KnifeRoot', self.ids)
        self.assertTrue(all(element.tag in {'root', 'styles', 'include', 'Panel', 'Label', 'Button'} for element in self.xml.iter()))
        for element in self.xml.iter():
            self.assertFalse(any(name.startswith('on') for name in element.attrib))

    def test_compiled_includes_have_matching_sources(self):
        for include in self.xml.findall('styles/include'):
            uri = include.attrib['src']
            self.assertTrue(uri.startswith('s2r://panorama/'))
            self.assertTrue(uri.endswith('_v4.vcss_c'))
            relative = uri.removeprefix('s2r://panorama/').replace('.vcss_c', '.css')
            self.assertTrue((PANORAMA / relative).is_file())
        runtime = (ROOT / 'CustomKnife.Core/src/Hud/KnifeHudRuntime.cs').read_text()
        self.assertIn('elysium_knife_selector_v4.vxml_c', runtime)
        self.assertIn('elysium_knife_selector_v4.vcss_c', runtime)
        self.assertIn('elysium_knife_images_v4.vcss_c', runtime)

    def test_confirmation_has_separate_ids_for_each_visible_knife(self):
        buttons = {element.get('id') for element in self.xml.iter('Button')}
        expected = {'Close', 'Settings', 'PreviousPage', 'NextPage'} | {f'Scale{scale}' for scale in (75, 85, 100, 115, 125)}
        for slot in range(7):
            expected.update({f'Preview{slot}', f'Equip{slot}'})
            self.assertIn(f'.KnifeConfirm.Available.Slot{slot} .EquipSlot{slot}', self.css)
        self.assertEqual(expected, buttons)

    def test_no_browser_css_or_disallowed_html_attribute(self):
        for pattern in (r'\bdisplay\s*:', r'(?<![-\w])position\s*:', r'\bflex[-\w]*\s*:', r'\bgrid[-\w]*\s*:', r'\b(?:var|calc|rgba|linear-gradient)\(', r':root', r'!important', r'\bbackdrop-filter\s*:'):
            self.assertIsNone(re.search(pattern, self.css))
        for label in self.xml.iter('Label'):
            self.assertNotIn('html', label.attrib, 'Custom HUD запрещает атрибут html независимо от значения')
            self.assertEqual('false', label.get('hittest'))
        self.assertIn('flow-children: right', self.css)
        self.assertIn('fill-parent-flow(1)', self.css)
        self.assertIn('visibility: collapse', self.css)

    def test_svg_list_and_png_preview_cannot_share_the_wrong_format(self):
        resource = PANORAMA.parent.parent
        assets = json.loads((resource / 'knife-hud-assets.json').read_text())
        self.assertTrue(all(path.endswith('.vsvg') for path in assets['icons'].values()))
        self.assertTrue(all(path.endswith('_png.vtex') for path in assets['previews'].values()))
        self.assertFalse(any('Rarity' in name for name in self.ids))
        self.assertNotIn('Rarity', self.css)
        spec = importlib.util.spec_from_file_location('knife_hud_generator', ROOT / 'scripts/generate-knife-hud.py')
        generator = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(generator)
        for group, wrong_path in (('icons', assets['previews']['knife']), ('previews', assets['icons']['knife'])):
            invalid = {name: dict(entries) for name, entries in assets.items()}
            invalid[group]['knife'] = wrong_path
            with patch.object(generator.json, 'loads', return_value=invalid), self.assertRaises(ValueError):
                generator.images()


if __name__ == '__main__':
    unittest.main()
