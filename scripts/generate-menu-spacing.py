#!/usr/bin/env python3
"""Генерирует ограниченный набор геометрий MapRotation без клиентского JS/SetStyle."""
from pathlib import Path
import argparse
import math

ROOT = Path(__file__).resolve().parents[1]
CSS = ROOT / 'CustomHud.Core/resources/hud/menus/content/panorama/styles/custom_game/elysium_menu_v4.css'
MARKER = '/* BEGIN GENERATED MAPROTATION SPACING: scripts/generate-menu-spacing.py */'


def rules(gap: int, suffix: str) -> list[str]:
    """Все размеры — логические пиксели Panorama до применения ui-scale."""
    root = '.MenuRoot.MapRotation' + suffix
    half = math.ceil(gap / 2)
    inset = max(16, gap)
    card = 236 + 2 * gap
    result = 234 + 2 * gap
    compact = 44 + half
    output = []

    def rule(selector: str, declarations: str):
        output.append(f'{root}{selector} {{ {declarations} }}')

    rule(' .MenuWindow', f'padding-top: {inset}px; padding-bottom: {inset}px;')
    rule(' .MenuHeader', f'margin-bottom: {gap}px;')
    rule(' .Subtitle', f'height: 22px; padding: 0px; margin-bottom: {gap}px;')
    rule(' .Participation', f'height: 26px; margin: 0px 0px {gap}px;')
    rule(' .Footer', f'height: 26px; padding: 0px; margin-top: {gap}px;')
    rule(' .Pagination', f'margin-top: {gap}px;')

    rule('.Vertical.List .MenuItem', f'height: 86px; margin-bottom: {gap}px;')
    rule('.Vertical.List .Slot5', 'margin-bottom: 0px;')
    rule('.Vertical.List .ItemsViewport', f'height: {6 * 86 + 5 * gap}px;')
    rule('.Vertical.List .ItemIcon', 'height: 84px;')
    rule('.Vertical.List .ItemName', 'height: 30px; text-overflow: shrink min(20px);')
    rule('.Vertical.List .ItemDescription', f'height: 18px; font-size: 16px; margin-top: {min(half, 8)}px;')
    rule('.Vertical.List.HasSecondRow .MenuItem', f'height: 72px; margin-bottom: {gap}px;')
    rule('.Vertical.List.HasSecondRow .Slot5', 'margin-bottom: 0px;')
    rule('.Vertical.List.HasSecondRow .ItemsViewport', f'height: {6 * 72 + 5 * gap}px;')
    rule('.Vertical.List.HasSecondRow .ItemIcon', 'height: 70px;')
    rule('.Vertical.List.HasSecondRow .ItemName', 'font-size: 22px;')

    rule('.Horizontal.List .MenuItem', f'height: {card}px;')
    rule('.Horizontal.List .ItemsViewport', f'height: {card}px;')
    rule('.Horizontal.List.HasSecondRow .ItemsViewport', f'height: {2 * card + gap}px;')
    rule('.Horizontal.List .Row1', f'margin-top: {gap}px;')
    rule('.Horizontal.List .ItemText', f'padding-top: {8 + half}px; padding-bottom: {half}px;')
    rule('.Horizontal.List .ItemName', 'height: 38px;')
    rule('.Horizontal.List .ItemDescription', f'height: 20px; margin-top: {half}px;')
    rule('.Horizontal.List .Badge', f'height: {28 + half}px; padding-bottom: {half}px;')

    # При 120% уменьшается только высота строк/шапки, сами интервалы сохраняются.
    rule('.Scale120.List .MenuWindow', 'padding-top: 16px; padding-bottom: 16px;')
    rule('.Scale120.List.HasBrand .MenuHeader', 'height: 64px;')
    rule('.Scale120.List .BrandGroup', 'height: 30px;')
    rule('.Scale120.List .HeaderActions', 'height: 30px;')
    rule('.Scale120.List .BrandMark', 'width: 28px; height: 28px;')
    rule('.Scale120.List .Brand', 'font-size: 21px; letter-spacing: 4px;')
    rule('.Scale120.List .Title', 'height: 28px; font-size: 24px; margin-bottom: 4px;')
    rule('.Scale120.List .Status', 'font-size: 21px;')
    rule('.Scale120.Vertical.List .MenuItem', 'height: 72px;')
    rule('.Scale120.Vertical.List .ItemsViewport', f'height: {6 * 72 + 5 * gap}px;')
    rule('.Scale120.Vertical.List .ItemIcon', 'height: 70px;')
    rule('.Scale120.Vertical.List .ItemName', 'font-size: 22px;')

    rule('.Compact .MenuHeader', f'margin-bottom: {gap}px;')
    rule('.Compact .Participation', f'height: 22px; margin: 0px 0px {gap}px;')
    rule('.Compact .MenuItem', f'height: {compact}px; margin-bottom: {half}px;')
    rule('.Compact .Slot5', 'margin-bottom: 0px;')
    rule('.Compact .ItemsViewport', f'height: {6 * compact + 5 * half}px;')
    rule('.Compact .ItemText', 'height: 24px; vertical-align: center; padding-top: 0px; padding-bottom: 0px;')
    rule('.Compact .Badge', 'height: 22px; vertical-align: center; padding-top: 0px; padding-bottom: 0px;')
    rule('.Compact .Footer', f'height: 26px; padding: 0px; margin-top: {gap}px;')

    rule('.Result .MenuHeader', f'margin-bottom: {gap}px;')
    rule('.Result .Participation', f'height: 22px; margin: 0px 0px {gap}px;')
    rule('.Result .MenuItem', f'height: {result}px;')
    rule('.Result .ItemsViewport', f'height: {result}px;')
    rule('.Result .ItemText', f'padding-top: {8 + half}px; padding-bottom: {half}px;')
    rule('.Result .ItemName', 'height: 32px;')
    rule('.Result .ItemDescription', f'height: 20px; margin-top: {half}px;')
    rule('.Result .Badge', f'height: {28 + half}px; padding-bottom: {half}px;')
    rule('.Result .Footer', f'height: 26px; padding: 0px; margin-top: {gap}px;')
    return output


def generated() -> str:
    lines = [MARKER,
             '/* Интервал между разделами и строками равен VerticalGap; внутри карточки — половине. */',
             '/* Без класса используется 24px; это также резерв для старого состояния HUD. */']
    lines.extend(rules(24, ''))
    for gap in range(33):
        lines.extend(rules(gap, f'.VerticalGap{gap}'))
    return '\n'.join(lines) + '\n'


def validate_geometry() -> None:
    """Шесть слотов и обе строки номинаций помещаются в логическую высоту 1080."""
    for gap in range(33):
        for scale in (80, 100, 120):
            inset = 16 if scale == 120 else max(16, gap)
            header = 64 if scale == 120 else 88
            row = 72 if scale == 120 else 86
            vote = header + 22 + 26 + 26 + 2 * inset + 4 * gap + 6 * row + 5 * gap + 3
            nominations = header + 22 + 36 + 2 * inset + 3 * gap + 2 * (236 + 2 * gap) + gap + 3
            assert vote * scale / 100 <= 1080, (gap, scale, 'vote', vote)
            assert nominations * scale / 100 <= 1080, (gap, scale, 'nominations', nominations)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    validate_geometry()
    original = CSS.read_text()
    prefix = original.split(MARKER, 1)[0].rstrip() + '\n\n'
    expected = prefix + generated()
    if args.check:
        if original != expected:
            raise SystemExit('Regenerate native geometry: python3 scripts/generate-menu-spacing.py')
        print('MapRotation spacing validated: 33 variants, six fixed slots, scale 80/100/120')
    else:
        CSS.write_text(expected)


if __name__ == '__main__':
    main()
