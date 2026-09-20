#!/usr/bin/env python3
"""Генерирует Panorama XML и синхронную таблицу классов изображений Knife HUD v4."""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RESOURCE = ROOT / 'CustomKnife.Core/resources/hud/knife-selector'
PANORAMA = RESOURCE / 'content/panorama'


def node(parent, tag, cls=None, id=None, **attrs):
    if cls: attrs['class'] = cls
    if id: attrs['id'] = id
    if tag == 'Label': attrs.update(hittest='false', html='false')
    return ET.SubElement(parent, tag, attrs)


def label(parent, cls, id=None, value='{s:value}'):
    return node(parent, 'Label', cls, id, text=value)


def layout():
    root = ET.Element('root')
    styles = node(root, 'styles')
    for name in ('elysium_knife_selector_v4', 'elysium_knife_images_v4'):
        node(styles, 'include', src=f's2r://panorama/styles/custom_game/{name}.vcss_c')
    viewport = node(root, 'Panel', 'KnifeViewport', hittest='false')
    overlay = node(viewport, 'Panel', 'KnifeRoot', 'KnifeRoot')
    window = node(overlay, 'Panel', 'KnifeWindow')
    header = node(window, 'Panel', 'KnifeHeader')
    label(node(header, 'Panel', 'KnifeMonogram'), None, value='E')
    heading = node(header, 'Panel', 'KnifeHeading')
    label(heading, 'KnifeTitle', 'Title')
    label(heading, 'KnifeSubtitle', 'Subtitle')
    brand = node(header, 'Panel', 'KnifeBrand')
    label(brand, 'KnifeBrandName', value='ELYSIUM')
    label(brand, 'KnifeBrandTagline', value='CUSTOM CS2 EXPERIENCE')
    label(node(header, 'Button', 'KnifeClose', 'Close'), None, value='×')
    body = node(window, 'Panel', 'KnifeBody')
    left = node(body, 'Panel', 'KnifeList')
    top = node(left, 'Panel', 'KnifeListHeader')
    label(top, 'KnifeSectionLabel KnifeListTitle', 'AvailableTitle')
    label(top, 'KnifeCount', 'AvailableCount')
    rows = node(left, 'Panel', 'KnifeRows')
    for slot in range(7):
        row = node(rows, 'Panel', 'KnifeRow' + (' Last' if slot == 6 else ''), f'Row{slot}')
        surface = node(row, 'Panel', 'KnifeRowSurface', hittest='false')
        content = node(surface, 'Panel', 'KnifeRowContent', hittest='false')
        node(content, 'Panel', 'KnifeThumb KnifeImage', f'Image{slot}', hittest='false')
        info = node(content, 'Panel', 'KnifeRowText', hittest='false')
        label(info, 'KnifeName', f'Name{slot}')
        rarity = node(info, 'Panel', 'KnifeRarityLine', hittest='false')
        node(rarity, 'Panel', 'KnifeRarityDot', hittest='false')
        label(rarity, 'KnifeRarityText', f'Rarity{slot}')
        label(node(content, 'Panel', 'KnifeRowAction', hittest='false'), None, f'Action{slot}')
        node(row, 'Button', 'KnifeRowHit', f'Preview{slot}')
    pager = node(left, 'Panel', 'KnifePager')
    label(node(pager, 'Button', 'KnifePageButton', 'PreviousPage'), None, value='‹')
    label(pager, 'KnifePageLabel', 'PageLabel')
    label(node(pager, 'Button', 'KnifePageButton', 'NextPage'), None, value='›')
    right = node(body, 'Panel', 'KnifeRight')
    preview = node(right, 'Panel', 'KnifePreview', 'Preview')
    art = node(preview, 'Panel', 'KnifePreviewArt')
    node(art, 'Panel', 'KnifePreviewGlow', hittest='false')
    node(art, 'Panel', 'KnifeHero KnifeImage', 'PreviewImage', hittest='false')
    rarity = node(art, 'Panel', 'KnifePreviewRarity', hittest='false')
    node(rarity, 'Panel', 'KnifeRarityDot', hittest='false')
    label(rarity, 'KnifeRarityText', 'PreviewRarity')
    caption = node(preview, 'Panel', 'KnifeCaption')
    label(caption, 'KnifePreviewName', 'PreviewName')
    label(caption, 'KnifePreviewSubtitle', 'PreviewSubtitle')
    details = node(right, 'Panel', 'KnifeDetails')
    benefits = node(details, 'Panel', 'KnifeBenefits')
    label(benefits, 'KnifeSectionLabel', 'BenefitsTitle')
    for index in range(4):
        row = node(benefits, 'Panel', 'KnifeBenefitRow', f'BenefitRow{index}')
        label(row, 'KnifePlus', value='+')
        label(row, 'KnifeBenefit', f'Benefit{index}')
    description = node(details, 'Panel', 'KnifeDescriptionPanel')
    label(description, 'KnifeSectionLabel', 'DescriptionTitle')
    label(node(description, 'Panel', 'KnifeDescriptionScroll'), 'KnifeDescription', 'Description')
    label(right, 'KnifeEmpty', 'Empty')
    footer = node(window, 'Panel', 'KnifeFooter', 'Footer')
    node(footer, 'Panel', 'KnifeStatusDot')
    label(footer, 'KnifeStatus', 'FooterStatus')
    confirm = node(footer, 'Panel', 'KnifeConfirm', 'Confirm')
    label(confirm, 'KnifeEquipLabel', 'EquipLabel')
    for slot in range(7):
        node(confirm, 'Button', f'KnifeEquipHit EquipSlot{slot}', f'Equip{slot}')
    ET.indent(root, space='    ')
    return ET.tostring(root, encoding='unicode') + '\n'


def images():
    assets = json.loads((RESOURCE / 'knife-hud-assets.json').read_text())
    if 'knife' not in assets: raise ValueError('Обязателен fallback knife')
    for key, path in assets.items():
        if not re.fullmatch(r'[a-z][a-z0-9_]{0,63}', key): raise ValueError(f'Неверный ключ: {key}')
        if not re.fullmatch(r's2r://panorama/images/[a-z0-9_/.-]+\.(?:vsvg|vtex)', path) or '..' in path:
            raise ValueError(f'Неверный путь: {path}')
    css = '/* Создано scripts/generate-knife-hud.py из knife-hud-assets.json */\n'
    css += ''.join(f'.KnifeImage.Image_{key} {{ background-image: url("{path}"); }}\n' for key, path in sorted(assets.items()))
    names = ', '.join(json.dumps(key) for key in sorted(assets))
    cs = '''// Создано scripts/generate-knife-hud.py; редактируйте knife-hud-assets.json.
namespace CustomKnife.Hud;

internal static class KnifeHudImages
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal) { NAMES };
    public static string Resolve(string? image) => image is not null && Names.Contains(image) ? image : "knife";
}
'''.replace('NAMES', names)
    return css, cs


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    css, cs = images()
    output = {
        PANORAMA / 'layout/custom_game/elysium_knife_selector_v4.xml': layout(),
        PANORAMA / 'styles/custom_game/elysium_knife_images_v4.css': css,
        ROOT / 'CustomKnife.Core/src/Hud/KnifeHudImages.cs': cs,
    }
    for path, value in output.items():
        if args.check:
            if not path.exists() or path.read_text() != value: raise SystemExit(f'Устарел файл: {path}')
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(value)
    print('Knife HUD v4: файлы синхронизированы')


if __name__ == '__main__':
    main()
