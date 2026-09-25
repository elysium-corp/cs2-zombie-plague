#!/usr/bin/env python3
"""Проверяет серверные привязки меню до компиляции ресурсов Workshop Tools."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
base = root / 'CustomHud.Core/resources/hud/menus/content/panorama'
xml = ET.parse(base / 'layout/custom_game/elysium_menu_v4.xml')
css = (base / 'styles/custom_game/elysium_menu_v4.css').read_text()
allowed = {'root', 'styles', 'include', 'Panel', 'Label', 'Button', 'Image'}
assert all(element.tag in allowed for element in xml.iter()), 'Unsupported panel type'
assert xml.getroot().find('Panel').get('id') is None, 'Outer panel must be anonymous'
ids = [element.get('id') for element in xml.iter() if element.get('id')]
assert len(ids) == len(set(ids)), 'Duplicate panel IDs'
for index in range(12):
    for prefix in ('Item', 'Image', 'Name', 'Description', 'Badge', 'Bar'):
        assert prefix + str(index) in ids, f'Missing binding {prefix}{index}'
    item = xml.find(f'.//Button[@id="Item{index}"]')
    assert item.find('./Panel[@class="SelectedCircle"]/Label[@class="SelectedMark"]') is not None
    assert item.find(f'.//Panel[@id="Bar{index}"]') is not None
for row in range(2):
    items = xml.find(f'.//Panel[@id="Row{row}"]').findall('Button')
    assert [item.get('id') for item in items] == [f'Item{i}' for i in range(row * 6, row * 6 + 6)]
for percent in range(101):
    assert re.search(rf'\.VoteBar\.Pct{percent}\s*\{{\s*width:\s*{percent}%;', css)
assert '.SelectedCircle { visibility: collapse;' in css
assert '.Selected .SelectedCircle { visibility: visible;' in css
for element in xml.iter():
    assert not {'html', 'onclick', 'onactivate'}.intersection(element.attrib), 'Unsupported client attribute'
    if element.tag == 'Label':
        assert element.get('text') in {'{s:value}', 'ELYSIUM', '‹', '›', '×', '✓', '◈'}, 'Unlocalized text'
assert xml.find('.//Panel[@class="MenuFrame"]/Panel[@id="Settings"]') is None
assert xml.find('.//Panel[@id="MenuRoot"]/Panel[@id="Settings"]') is not None
for name in ('Participation', 'DockSideLabel', 'AnimationLabel', 'DockSettings', 'AnimationSettings'):
    assert name in ids, f'Missing binding {name}'
print(f'Menu resources validated: {len(ids)} unique IDs, 12 slots, 101 percentage classes')
