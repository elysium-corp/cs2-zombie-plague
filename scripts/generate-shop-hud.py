#!/usr/bin/env python3
"""Генерирует макет Shop Custom HUD с размером пула и путями из серверного кода."""
from pathlib import Path
import re
import json
import argparse
import shutil
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / 'Shop.Core/resources/hud/shop/content'
HUD = ROOT / 'Shop.Core/src/Hud'
APPEARANCE = json.loads((CONTENT.parent / 'hud-appearance.json').read_text())
CATALOG = (HUD / 'ShopHudCatalog.cs').read_text(encoding='utf-8')
RUNTIME = (HUD / 'ShopHudRuntime.cs').read_text(encoding='utf-8')
COLUMNS = int(re.search(r'const int ColumnCount = (\d+);', CATALOG)[1])
ROWS = int(re.search(r'const int RowCount = (\d+);', CATALOG)[1])
LAYOUT = re.search(r'const string Layout = "([^"]+)";', RUNTIME)[1]
STYLE = re.search(r'const string Style = "([^"]+)";', RUNTIME)[1]
ICONS_STYLE = re.search(r'const string IconsStyle = "([^"]+)";', RUNTIME)[1]
ICONS = re.findall(r'"([a-z0-9_]+)"',
                   (HUD / 'ShopHudIcons.cs').read_text(encoding='utf-8').split('internal static string? Override')[0])
COLORS = dict(Common='#b0c3d9', Uncommon='#5e98d9', Rare='#4b69ff', Restricted='#8847ff',
              Classified='#d32ce6', Elite='#eb4b4b', Prototype='#ff8c00', Legendary='#e4ae39')
SCALES = [int(x) for x in re.search(r'Scales = \[([^]]+)\]',
          (HUD / 'ShopHudPreferences.cs').read_text(encoding='utf-8'))[1].split(',')]


def page_buttons(parent, id, cls):
    return [ET.SubElement(parent, 'Button', {'id': bank + '_' + id, 'class': cls + ' Hit' + bank})
            for bank in ('A', 'B')]


def panel(parent, cls, id=None):
    attrs = {'class': cls}
    if id:
        attrs['id'] = id
    return ET.SubElement(parent, 'Panel', attrs)


def label(parent, cls, id=None, text='{s:value}'):
    attrs = {'class': cls, 'text': text, 'hittest': 'false'}
    if id:
        attrs['id'] = id
    return ET.SubElement(parent, 'Label', attrs)


def nav(parent, id, button, glyph):
    node = panel(parent, 'Nav', id)
    label(node, 'NavGlyph', text=glyph)
    for hit in page_buttons(node, button, 'NavHit'):
        label(hit, 'NavGlyph', text=glyph)


root = ET.Element('root')
styles = ET.SubElement(root, 'styles')
ET.SubElement(styles, 'include', {'src': 's2r://' + STYLE})
ET.SubElement(styles, 'include', {'src': 's2r://' + ICONS_STYLE})
# Корень без id; персональная видимость задаётся дочернему ShopRoot.
viewport = panel(root, 'ShopViewport')
screen = panel(viewport, 'ShopRoot', 'ShopRoot')
window = panel(screen, 'ShopWindow')
header = panel(window, 'Header')
mark = panel(header, 'BrandMark')
label(mark, 'BrandGlyph', text='E')
brand = panel(header, 'Brand')
label(brand, 'BrandName', text='ELYSIUM')
label(brand, 'StoreTitle', 'StoreTitle')
wallet = panel(header, 'Wallet')
label(wallet, 'Balance', 'Balance')
settings = ET.SubElement(header, 'Button', {'id': 'Settings', 'class': 'SettingsButton'})
gear = panel(settings, 'Gear')
gear.set('hittest', 'false')
close = ET.SubElement(header, 'Button', {'id': 'Close', 'class': 'Close'})
label(close, 'CloseLabel', text='×')
stack = panel(window, 'ContentStack')
body = panel(stack, 'Body')
columns = panel(body, 'Columns', 'Columns')
for col in range(COLUMNS):
    column = panel(columns, 'Column', f'Column{col}')
    category = panel(column, 'CategoryHeader')
    panel(category, 'CategoryAccent')
    label(category, 'Category', f'Category{col}')
    cards = panel(column, 'Cards', f'Cards{col}')
    for row in range(ROWS):
        slot = col * ROWS + row
        card = panel(cards, 'Card', f'Card{slot}')
        surface = panel(card, 'CardSurface')
        content = panel(surface, 'CardContent')
        label(content, 'ItemName', f'Name{slot}')
        icon = panel(content, 'ItemIcon', f'Icon{slot}')
        icon.set('hittest', 'false')
        # Два одинаковых силуэта: размытый цветной контур под нейтральной иконкой.
        # img-shadow применяется только к Image; здесь фон выбирается CSS-классом.
        halo = panel(icon, 'IconHalo')
        halo.set('hittest', 'false')
        panel(halo, 'WeaponLayer HaloShape').set('hittest', 'false')
        panel(icon, 'WeaponLayer IconSilhouette').set('hittest', 'false')
        details = panel(content, 'CardDetails')
        panel(details, 'RarityMark')
        label(details, 'Status', f'Status{slot}')
        label(details, 'Price', f'Price{slot}')
        page_buttons(surface, f'Buy{slot}', 'BuyHit')
        cooldown = panel(surface, 'Cooldown', f'Cooldown{slot}')
        cooldown.set('hittest', 'false')
        label(cooldown, 'Countdown', f'Countdown{slot}')
    pager_space = panel(column, 'PagerSpace')
    pager = panel(pager_space, 'ItemPager', f'ItemPager{col}')
    nav(pager, f'Prev{col}', f'Previous{col}', '‹')
    label(pager, 'PageText', f'Page{col}')
    nav(pager, f'Next{col}', f'NextItems{col}', '›')
label(body, 'Empty', 'Empty')
settings_panel = panel(stack, 'SettingsPanel', 'SettingsPanel')
label(settings_panel, 'SettingsTitle', 'SettingsTitle')
scale_options = panel(settings_panel, 'ScaleOptions')
for index, scale in enumerate(SCALES):
    option = panel(scale_options, 'ScaleOption', f'ScaleOption{index}')
    label(option, 'ScaleLabel', text=f'{scale}%')
    ET.SubElement(option, 'Button', {'id': f'SetScale{index}', 'class': 'ScaleHit'})
label(settings_panel, 'SettingsStatus', 'SettingsStatus')
footer = panel(window, 'Footer')
selection = panel(footer, 'Selection')
label(selection, 'SelectionName', 'SelectionName')
confirm = panel(selection, 'Confirm', 'Confirm')
label(confirm, 'ConfirmLabel', 'ConfirmLabel')
for slot in range(COLUMNS * ROWS):
    page_buttons(confirm, f'Confirm{slot}', f'ConfirmHit ConfirmHit{slot}')
panel(footer, 'FooterSpace')
pages = panel(footer, 'CategoryPager', 'CategoryPager')
nav(pages, 'CategoryPrev', 'CategoriesPrevious', '‹')
label(pages, 'PageText', 'CategoryPage')
nav(pages, 'CategoryNext', 'CategoriesNext', '›')
ET.indent(root, space='    ')
xml = ET.tostring(root, encoding='unicode') + '\n'
css = '''/* Создано scripts/generate-shop-hud.py */
.ShopViewport { width: 100%; height: 100%; }
.ShopRoot { width: 100%; height: 100%; visibility: collapse; background-color: #050b1266; }
.ShopRoot.Visible { visibility: visible; }
.ShopWindow { width: 1720px; max-width: 94%; height: 980px; max-height: 96%; horizontal-align: center; vertical-align: center; flow-children: down; background-color: gradient(linear, 0% 0%, 100% 100%, from(#12222bf5), to(#080f17fa)); border: 1px solid #698fa144; border-radius: 8px; box-shadow: #000000bb 0px 10px 36px 0px; }
.ShopRoot Label { font-family: Arial; }
.Header { width: 100%; height: 88px; padding: 16px 22px; flow-children: right; background-color: #0a131deb; border-bottom: 1px solid #76d9cc88; }
.BrandMark { width: 48px; height: 48px; vertical-align: center; margin-right: 14px; border: 1px solid #75daceaa; border-radius: 6px; background-color: #69d9c912; }
.BrandGlyph { horizontal-align: center; vertical-align: center; color: #a4f3e7; font-size: 34px; font-weight: bold; }
.Brand { width: fill-parent-flow(1); height: 56px; flow-children: down; vertical-align: center; }
.BrandName { width: 100%; height: 32px; font-size: 27px; font-weight: bold; letter-spacing: 5px; color: #effaf9; }
.StoreTitle { width: 100%; height: 22px; font-size: 16px; color: #88a4b2; text-overflow: ellipsis; }
.Wallet { height: 42px; vertical-align: center; margin: 0px 16px; padding: 8px 14px; border: 1px solid #dccb8c33; border-radius: 4px; background-color: #e8cb7810; }
.Balance { vertical-align: center; font-size: 23px; font-weight: bold; color: #f3d992; }
.Close { width: 44px; height: 44px; vertical-align: center; border: 1px solid #6b839455; border-radius: 4px; background-color: #15212a; }
.Close:hover { background-color: #553038; border-color: #d07e84; }
.CloseLabel { horizontal-align: center; vertical-align: center; color: #b9cbd4; font-family: Arial; font-size: 30px; }
.SettingsButton { width: 44px; height: 44px; vertical-align: center; margin-right: 8px; border: 1px solid #6b839455; border-radius: 4px; background-color: #15212a; }
.SettingsButton:hover { background-color: #243e48; border-color: #76d9cc; }
.SettingsOpen .SettingsButton { border-color: #76d9cc; }
.Gear { width: 28px; height: 28px; horizontal-align: center; vertical-align: center; background-image: url("s2r://panorama/images/custom_game/shop/gear.vsvg"); background-size: contain; background-position: center; background-repeat: no-repeat; wash-color: #b9cbd4; }
.ContentStack { width: 100%; height: fill-parent-flow(1); }
.Body { width: 100%; height: 100%; padding: 18px 18px 10px; }
.SettingsPanel { width: 376px; max-width: 96%; height: 150px; horizontal-align: right; margin: 12px 24px; padding: 16px; visibility: collapse; flow-children: down; background-color: #0d1d29fe; border: 1px solid #76d9cc88; border-radius: 6px; box-shadow: #000000aa 0px 6px 18px 0px; }
.SettingsOpen .SettingsPanel { visibility: visible; }
.SettingsTitle { width: 100%; height: 28px; color: #dcefed; font-size: 19px; }
.ScaleOptions { width: 100%; height: 38px; flow-children: right; margin-top: 8px; }
.ScaleOption { width: fill-parent-flow(1); height: 36px; margin-right: 4px; background-color: #1d303e; border: 1px solid #6c8b9a44; border-radius: 3px; }
.ScaleOption.Selected { border-color: #76d9cc; background-color: #76d9cc20; }
.ScaleLabel { horizontal-align: center; vertical-align: center; font-size: 17px; color: #b7cbd6; }
.ScaleOption.Selected .ScaleLabel { color: #bbfff0; }
.ScaleHit { width: 100%; height: 100%; visibility: collapse; }
.CanEdit .ScaleHit { visibility: visible; }
.ScaleHit:hover { background-color: #76d9cc22; }
.SettingsStatus { width: 100%; height: 24px; margin-top: 8px; font-size: 13px; color: #8cabb9; text-overflow: ellipsis; }
.SettingsStatus.Failed { color: #eab5a3; }
.Columns { width: 100%; height: 100%; flow-children: right; }
.Column { height: 100%; padding: 0px 6px; flow-children: down; visibility: collapse; }
.Column.Visible { visibility: visible; }
.CategoryHeader { width: 100%; height: 40px; flow-children: right; }
.CategoryAccent { width: 3px; height: 15px; margin: 4px 9px 0px 1px; background-color: #76d9cc; }
.Category { width: fill-parent-flow(1); height: 32px; font-size: 16px; font-weight: bold; color: #b7cbd6; text-overflow: ellipsis; }
.Cards { width: 100%; height: fill-parent-flow(1); flow-children: down; }
.Card { width: 100%; padding-bottom: 8px; visibility: collapse; }
.Card.Visible { visibility: visible; }
.CardSurface { width: 100%; height: 100%; border: 1px solid #62717e22; border-radius: 5px; background-color: #141c25bc; }
.Card.Available .CardSurface { border-color: #7896a444; background-color: gradient(linear, 0% 0%, 100% 100%, from(#263843dc), to(#14222df0)); }
.CardContent { width: 100%; height: 100%; padding: 8px 10px 6px; flow-children: down; }
.ItemName { width: 100%; height: 32px; font-size: 15px; font-weight: medium; color: #65727e; text-overflow: ellipsis; white-space: normal; }
.Card.Available .ItemName { color: #dfebf0; }
.ItemIcon { width: 100%; height: fill-parent-flow(1); margin: 2px 0px 5px; }
.WeaponLayer { width: 86%; max-width: 190px; height: 100%; horizontal-align: center; vertical-align: center; background-size: contain; background-position: center; background-repeat: no-repeat; }
.IconHalo { width: 100%; height: 100%; visibility: collapse; blur: gaussian(5); opacity: 1; transform: scale3d(1.08, 1.08, 1); }
.IconSilhouette { wash-color: #64707c; opacity: 0.45; }
.Card.Available .IconHalo { visibility: visible; }
.Card.Available .IconSilhouette { wash-color: #dfebf2; opacity: 1; }
.Cooldown { visibility: collapse; horizontal-align: right; vertical-align: center; margin-right: 10px; padding: 4px 8px; background-color: #081019ed; border: 1px solid #a4c3d955; border-radius: 4px; }
.Cooldown.Visible { visibility: visible; }
.Countdown { color: #eaf5ff; font-size: 18px; font-weight: bold; }
.CardDetails { width: 100%; height: 22px; flow-children: right; }
.RarityMark { width: 13px; height: 3px; vertical-align: center; margin-right: 6px; background-color: #52606c; }
.Status { width: fill-parent-flow(1); height: 22px; vertical-align: center; font-size: 11px; color: #7b8996; text-overflow: ellipsis; }
.Price { vertical-align: center; color: #687582; font-size: 17px; font-weight: bold; margin-left: 4px; }
.Card.Available .Price { color: #f2d38d; }
.BuyHit { width: 100%; height: 100%; visibility: collapse; }
.BankA .Card.Available .BuyHit.HitA, .BankB .Card.Available .BuyHit.HitB { visibility: visible; border: 1px solid #00000000; border-radius: 4px; }
.BuyHit:hover { background-color: #a2e5e20a; border-color: #a7e7e080; }
.BuyHit:active { background-color: #bdece51c; border-color: #d9fff3; }
.PagerSpace { width: 100%; height: 0px; }
.HasItemPages .PagerSpace { height: 28px; }
.ItemPager { width: 100%; height: 28px; flow-children: right; visibility: collapse; }
.ItemPager.Visible { visibility: visible; }
.Nav { width: 32px; height: 28px; }
.NavGlyph { color: #435464; font-size: 24px; horizontal-align: center; vertical-align: center; }
.NavHit { width: 100%; height: 100%; visibility: collapse; }
.BankA .Nav.Available .NavHit.HitA, .BankB .Nav.Available .NavHit.HitB { visibility: visible; background-color: #76d9cc12; border: 1px solid #76d9cc33; border-radius: 3px; }
.Nav.Available .NavHit .NavGlyph { color: #a6dad6; }
.NavHit:hover { background-color: #76d9cc2d; }
.PageText { width: fill-parent-flow(1); vertical-align: center; text-align: center; color: #718b9c; font-size: 13px; }
.Empty { visibility: collapse; horizontal-align: center; vertical-align: center; color: #9bacbb; font-size: 20px; }
.Empty.Visible { visibility: visible; }
.Footer { width: 100%; height: 48px; padding: 10px 24px; background-color: #09111ae8; border-top: 1px solid #7a9eae22; flow-children: right; }
.FooterSpace { width: fill-parent-flow(1); height: 1px; }
.CategoryPager { width: 144px; height: 28px; margin-left: 18px; flow-children: right; visibility: collapse; }
.CategoryPager.Visible { visibility: visible; }
.ShopRoot.SettingsOpen .Card.Available .BuyHit, .ShopRoot.SettingsOpen .Nav.Available .NavHit { visibility: collapse; }
'''
css += """
.Selection { width: 400px; height: 28px; margin-right: 18px; flow-children: right; visibility: collapse; }
.HasSelection .Selection { visibility: visible; }
.SelectionName { width: fill-parent-flow(1); vertical-align: center; font-size: 13px; color: #dfebf2; text-overflow: ellipsis; }
.Confirm { width: 112px; height: 28px; margin-left: 12px; background-color: #34424b; border-radius: 3px; }
.Confirm.Available { background-color: #285b51; }
.ConfirmLabel { horizontal-align: center; vertical-align: center; font-size: 14px; color: #ffffff; }
.ConfirmHit { width: 100%; height: 100%; visibility: collapse; }
.ConfirmHit:hover { background-color: #ffffff22; }
.SettingsOpen .ConfirmHit { visibility: collapse; }
.Card.Selected .CardSurface { border-color: #d7f8ee; }
.Selection_none .Card.Selected .CardSurface { animation-name: none; }
"""
for bank in ('A', 'B'):
    for effect, start in [('pulse', 'brightness: 1.25;'), ('lift', 'transform: translateY(-4px);'), ('press', 'transform: scale3d(0.97, 0.97, 1);')]:
        end = 'brightness: 1;' if effect == 'pulse' else 'transform: scale3d(1, 1, 1);' if effect == 'press' else 'transform: translateY(0px);'
        css += f'.Selection_{effect}.Pulse{bank} .Card.Selected .CardSurface {{ animation-name: shop-selection-{effect}-{bank}; animation-timing-function: ease-out; }}\n'
        css += f"@keyframes 'shop-selection-{effect}-{bank}' {{ 0% {{ {start} }} 100% {{ {end} }} }}\n"
for slot in range(COLUMNS * ROWS):
    css += f'.Confirm.Available.ConfirmSlot{slot} .ConfirmHit{slot} {{ visibility: visible; }}\n'
for columns in range(1, COLUMNS + 1):
    css += f'.Columns{columns} .Column {{ width: {100 / columns:.6f}%; }}\n'
for width in APPEARANCE['options']['width']:
    css += f'.Width{width} .ShopWindow {{ width: {width}px; }}\n'
for height in APPEARANCE['options']['height']:
    css += f'.Height{height} .ShopWindow {{ height: {height}px; }}\n'
for scale in SCALES:
    css += f'.Scale{scale} .ShopWindow {{ ui-scale: {scale}%; }}\n'
for rows in range(1, ROWS + 1):
    css += f'.Rows{rows} .Card {{ height: {100 / rows:.6f}%; }}\n'
for name, color in COLORS.items():
    css += f'.Card.Available.Rarity{name} .HaloShape {{ wash-color: {color}; }}\n'
    css += f'.Card.Available.Rarity{name} .RarityMark {{ background-color: {color}; }}\n'
    css += f'.Hover_rarity .Card.Available.Rarity{name} .BuyHit:hover {{ border-color: {color}; box-shadow: {color}33 0px 0px 9px 0px; }}\n'
for icon in sorted(set(ICONS)):
    asset = 'kevlar' if icon == 'equipment' else icon
    css += f'.Icon_{icon} .WeaponLayer {{ background-image: url("s2r://panorama/images/icons/equipment/{asset}.vsvg"); }}\n'
# Варианты оформления используют те же значения, что и Web-превью.
css += """
.Theme_minimal .ShopWindow { background-color: #10151bf7; border-radius: 2px; box-shadow: none; }
.Theme_minimal .Card.Available .CardSurface { background-color: #19222a; border-color: #ffffff18; }
.Theme_minimal .BrandMark { visibility: collapse; }
.Theme_tactical .ShopWindow { background-color: #101d1bf7; border-radius: 0px; border: 2px solid #6d8a7855; }
.Theme_tactical .CardSurface, .Theme_tactical .BrandMark { border-radius: 0px; }
.Theme_tactical .Card.Available .CardSurface { background-color: #1b2b25; }
.Names_left .ItemName { text-align: left; }
.Names_center .ItemName { text-align: center; }
.Names_right .ItemName { text-align: right; }
.Icons_silhouette .Card .IconHalo, .Icons_hidden .Card .ItemIcon { visibility: collapse; }
.Highlight_none .Card .IconHalo, .Highlight_none .Card .RarityMark { visibility: collapse; }
.Hover_none .Card.Available .BuyHit:hover { border-color: #00000000; box-shadow: none; }
.Open_none.Visible .ShopWindow { animation-name: none; }
"""
for accent, color in APPEARANCE['accents'].items():
    css += f'.Accent_{accent} .Header {{ border-bottom-color: {color}88; }}\n'
    css += f'.Accent_{accent} .CategoryAccent {{ background-color: {color}; }}\n'
    css += f'.Accent_{accent} .BrandMark {{ border-color: {color}aa; }}\n'
    css += f'.Accent_{accent}.Highlight_accent .Card.Available .HaloShape {{ wash-color: {color}; }}\n'
    css += f'.Accent_{accent}.Highlight_accent .Card.Available .RarityMark {{ background-color: {color}; }}\n'
    css += f'.Accent_{accent}.Hover_accent .Card.Available .BuyHit:hover {{ border-color: {color}; box-shadow: {color}33 0px 0px 9px 0px; }}\n'
for accent, color in APPEARANCE['accents'].items():
    css += f'.Hover_{accent} .Card.Available .BuyHit:hover {{ border-color: {color}; box-shadow: {color}33 0px 0px 9px 0px; }}\n'
frames = dict(fade=('opacity: 0;', 'opacity: 1;'),
              slide=('opacity: 0; transform: translateY(18px);', 'opacity: 1; transform: translateY(0px);'),
              zoom=('opacity: 0; transform: scale3d(0.96, 0.96, 1);', 'opacity: 1; transform: scale3d(1, 1, 1);'),
              rise=('opacity: 0; transform: translateY(60px);', 'opacity: 1; transform: translateY(0px);'),
              unfold=('opacity: 0; transform: scale3d(1, 0.85, 1);', 'opacity: 1; transform: scale3d(1, 1, 1);'),
              drift=('opacity: 0; transform: translateX(64px);', 'opacity: 1; transform: translateX(0px);'))
for effect, (start, end) in frames.items():
    css += f".Open_{effect}.Visible .ShopWindow {{ animation-name: shop-open-{effect}; animation-timing-function: ease-out; }}\n"
    css += f"@keyframes 'shop-open-{effect}' {{ 0% {{ {start} }} 100% {{ {end} }} }}\n"
for effect, (start, end) in frames.items():
    css += f".Close_{effect}.Closing .ShopWindow {{ animation-name: shop-close-{effect}; animation-timing-function: ease-in; animation-fill-mode: forwards; }}\n"
    css += f"@keyframes 'shop-close-{effect}' {{ 0% {{ {end} }} 100% {{ {start} }} }}\n"
css += '.Close_none.Closing .ShopWindow { animation-name: none; }\n'
css += '.Closing .BuyHit, .Closing .NavHit, .Closing .ConfirmHit, .Closing .ScaleHit { visibility: collapse; }\n'
# Выход и вход применяются только к Columns или Cards выбранной колонки.
# В scroll нет прозрачности: элементы действительно уезжают за границу области.
for direction, sign in [('Next', 1), ('Previous', -1)]:
    for effect in APPEARANCE['options']['pageAnimation']:
        if effect == 'none':
            continue
        for phase in ('Out', 'In'):
            offset = -sign if phase == 'Out' else sign
            if effect in ('scroll_x', 'scroll_y', 'slide'):
                axis = 'Y' if effect == 'scroll_y' else 'X'
                distance = f'{offset * 100}%' if effect.startswith('scroll') else f'{offset * 28}px'
                moved = f'transform: translate{axis}({distance});'
                rest = f'transform: translate{axis}(0px);'
                if effect == 'slide':
                    moved += ' opacity: 0;'
                    rest += ' opacity: 1;'
            else:
                moved, rest = frames[effect]
            start, end = (rest, moved) if phase == 'Out' else (moved, rest)
            name = f'shop-page-{effect}-{phase}-{direction}'
            css += f'.Page_{effect} .Motion{phase}{direction} {{ animation-name: {name}; animation-timing-function: ease-in-out; animation-fill-mode: both; }}\n'
            css += f"@keyframes '{name}' {{ 0% {{ {start} }} 100% {{ {end} }} }}\n"
css += '.ShopRoot.MotionBusy .Card.Available .BuyHit, .ShopRoot.MotionBusy .Nav.Available .NavHit, .ShopRoot.MotionBusy .Confirm .ConfirmHit { visibility: collapse; }\n'
for speed, duration in APPEARANCE['duration'].items():
    css += f'.Speed_{speed} .ShopWindow, .Speed_{speed} .CardSurface {{ animation-duration: {duration}s; animation-iteration-count: 1; }}\n'
    css += f'.Speed_{speed} .Cards, .Speed_{speed} .Columns {{ animation-duration: {duration / 2}s; animation-iteration-count: 1; }}\n'

css += '.ShopRoot.BankA .Confirm .HitB, .ShopRoot.BankB .Confirm .HitA, .ShopRoot.Closing .Confirm .ConfirmHit, .ShopRoot.SettingsOpen .Confirm .ConfirmHit { visibility: collapse; }\n'

for file, data in [(LAYOUT.replace('.vxml_c', '.xml'), xml), (STYLE.replace('.vcss_c', '.css'), css)]:
    path = CONTENT / file
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(data, encoding='utf-8')
print(f'Shop HUD: {COLUMNS} columns, {COLUMNS * ROWS} cards, contour rarity glow')

from shop_hud_web import build_preview
build_preview(CONTENT.parent / 'web', xml, css, APPEARANCE)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--cms-assets', type=Path, help='Каталог ElysiumShop/Resources/assets для синхронизации Web-превью')
arguments = parser.parse_args()
if arguments.cms_assets:
    arguments.cms_assets.mkdir(parents=True, exist_ok=True)
    for source in [CONTENT.parent / 'hud-appearance.json', *(CONTENT.parent / 'web').glob('shop-hud*')]:
        shutil.copyfile(source, arguments.cms_assets / source.name)
    controls = arguments.cms_assets / 'controls'
    controls.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(CONTENT / 'panorama/images/custom_game/shop/gear.svg', controls / 'gear.svg')
