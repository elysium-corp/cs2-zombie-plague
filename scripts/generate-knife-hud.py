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
    # Custom HUD запрещает атрибут html даже со значением false.
    if tag == 'Label': attrs.update(hittest='false')
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
    heading = node(header, 'Panel', 'KnifeHeading')
    label(heading, 'KnifeTitle', 'Title')
    label(heading, 'KnifeSubtitle', 'Subtitle')
    node(node(header, 'Button', 'KnifeSettingsButton', 'Settings'), 'Panel', 'KnifeGear', hittest='false')
    label(node(header, 'Button', 'KnifeClose', 'Close'), None, value='×')
    stack = node(window, 'Panel', 'KnifeContentStack')
    body = node(stack, 'Panel', 'KnifeBody')
    left = node(body, 'Panel', 'KnifeList')
    top = node(left, 'Panel', 'KnifeListHeader')
    label(top, 'KnifeSectionLabel KnifeListTitle', 'AvailableTitle')
    label(top, 'KnifeCount', 'AvailableCount')
    rows = node(left, 'Panel', 'KnifeRows')
    for slot in range(7):
        row = node(rows, 'Panel', 'KnifeRow' + (' Last' if slot == 6 else ''), f'Row{slot}')
        surface = node(row, 'Panel', 'KnifeRowSurface', hittest='false')
        content = node(surface, 'Panel', 'KnifeRowContent', hittest='false')
        node(content, 'Panel', 'KnifeThumb', f'Image{slot}', hittest='false')
        info = node(content, 'Panel', 'KnifeRowText', hittest='false')
        label(info, 'KnifeName', f'Name{slot}')
        label(info, 'KnifeRowAction', f'Action{slot}')
        label(content, 'KnifeEquippedMark', value='✓')
        node(row, 'Button', 'KnifeRowHit', f'Preview{slot}')
    pager = node(left, 'Panel', 'KnifePager')
    label(node(pager, 'Button', 'KnifePageButton', 'PreviousPage'), None, value='‹')
    label(pager, 'KnifePageLabel', 'PageLabel')
    label(node(pager, 'Button', 'KnifePageButton', 'NextPage'), None, value='›')
    right = node(body, 'Panel', 'KnifeRight')
    preview = node(right, 'Panel', 'KnifePreview', 'Preview')
    art = node(preview, 'Panel', 'KnifePreviewArt')
    node(art, 'Panel', 'KnifeHero', 'PreviewImage', hittest='false')
    caption = node(preview, 'Panel', 'KnifeCaption')
    label(caption, 'KnifePreviewName', 'PreviewName')
    label(caption, 'KnifePreviewSubtitle', 'PreviewSubtitle')
    details = node(right, 'Panel', 'KnifeDetails')
    stats = node(details, 'Panel', 'KnifeStats')
    label(stats, 'KnifeSectionLabel', 'StatsTitle')
    label(stats, 'KnifeComparisonHint', 'ComparisonHint')
    for index in range(4):
        row = node(stats, 'Panel', 'KnifeStatRow', f'StatRow{index}')
        label(row, 'KnifeStatName', f'StatName{index}')
        values = node(row, 'Panel', 'KnifeStatValues')
        label(values, 'KnifeStatCurrent', f'StatCurrent{index}')
        label(values, 'KnifeStatArrow', value='→')
        label(values, 'KnifeStatSelected', f'StatSelected{index}')
        label(values, 'KnifeStatDelta', f'StatDelta{index}')
    description = node(details, 'Panel', 'KnifeDescriptionPanel')
    label(description, 'KnifeSectionLabel', 'DescriptionTitle')
    label(node(description, 'Panel', 'KnifeDescriptionScroll'), 'KnifeDescription', 'Description')
    label(right, 'KnifeEmpty', 'Empty')
    settings = node(stack, 'Panel', 'KnifeSettingsPanel')
    label(settings, 'KnifeSectionLabel', 'SettingsTitle')
    scales = node(settings, 'Panel', 'KnifeScaleOptions')
    for scale in (75, 85, 100, 115, 125):
        label(node(scales, 'Button', 'KnifeScaleOption', f'Scale{scale}'), None, value=f'{scale}%')
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
    css = '/* Создано scripts/generate-knife-hud.py из knife-hud-assets.json */\n'
    for group, panel, prefix, suffix in (('icons', 'KnifeThumb', 'Icon', '.vsvg'), ('previews', 'KnifeHero', 'Preview', '_png.vtex')):
        entries = assets.get(group)
        if not isinstance(entries, dict) or 'knife' not in entries: raise ValueError(f'Обязателен fallback {group}.knife')
        for key, path in sorted(entries.items()):
            if not re.fullmatch(r'[a-z][a-z0-9_]{0,63}', key): raise ValueError(f'Неверный ключ: {key}')
            if not isinstance(path, str) or not re.fullmatch(r's2r://panorama/images/[a-z0-9_/.-]+', path) or '..' in path or not path.endswith(suffix):
                raise ValueError(f'Неверный путь {group}.{key}: ожидается {suffix}')
            css += f'.{panel}.{prefix}_{key} {{ background-image: url("{path}"); }}\n'
    cs = '''// Создано scripts/generate-knife-hud.py; редактируйте knife-hud-assets.json.
namespace CustomKnife.Hud;

internal static class KnifeHudImages
{
    private static readonly HashSet<string> Icons = new(StringComparer.Ordinal) { ICONS };
    private static readonly HashSet<string> Previews = new(StringComparer.Ordinal) { PREVIEWS };
    public static string ResolveIcon(string? image, string knifeId) => Resolve(Icons, image, knifeId);
    public static string ResolvePreview(string? image, string knifeId) => Resolve(Previews, image, knifeId);

    private static string Resolve(HashSet<string> names, string? image, string knifeId)
    {
        if (!string.IsNullOrWhiteSpace(image)) return names.Contains(image) ? image : "knife";
        if (names.Contains(knifeId)) return knifeId;
        var shortName = knifeId.StartsWith("knife_", StringComparison.Ordinal) ? knifeId[6..] : knifeId;
        return names.Contains(shortName) ? shortName : "knife";
    }
}
'''.replace('ICONS', ', '.join(json.dumps(key) for key in sorted(assets['icons']))).replace('PREVIEWS', ', '.join(json.dumps(key) for key in sorted(assets['previews'])))
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
