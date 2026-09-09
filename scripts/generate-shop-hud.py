#!/usr/bin/env python3
"""Генерирует макет Shop Custom HUD с размером пула и путями из серверного кода."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / 'Shop.Core/resources/hud/shop/content'
HUD = ROOT / 'Shop.Core/src/Hud'
CATALOG = (HUD / 'ShopHudCatalog.cs').read_text(encoding='utf-8')
RUNTIME = (HUD / 'ShopHudRuntime.cs').read_text(encoding='utf-8')
COLUMNS = int(re.search(r'const int ColumnCount = (\d+);', CATALOG)[1])
ROWS = int(re.search(r'const int RowCount = (\d+);', CATALOG)[1])
LAYOUT = re.search(r'const string Layout = "([^"]+)";', RUNTIME)[1]
STYLE = re.search(r'const string Style = "([^"]+)";', RUNTIME)[1]
ICONS = re.findall(r'"([a-z0-9_]+)"',
                   (HUD / 'ShopHudIcons.cs').read_text(encoding='utf-8').split('internal static string Normalize')[0])
COLORS = dict(Common='#b0c3d9', Uncommon='#5e98d9', Rare='#4b69ff', Restricted='#8847ff',
              Classified='#d32ce6', Elite='#eb4b4b', Prototype='#ff8c00', Legendary='#e4ae39')


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
    hit = ET.SubElement(node, 'Button', {'id': button, 'class': 'NavHit'})
    label(hit, 'NavGlyph', text=glyph)


root = ET.Element('root')
styles = ET.SubElement(root, 'styles')
ET.SubElement(styles, 'include', {'src': 's2r://' + STYLE})
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
close = ET.SubElement(header, 'Button', {'id': 'Close', 'class': 'Close'})
label(close, 'CloseLabel', text='×')
body = panel(window, 'Body')
columns = panel(body, 'Columns')
for col in range(COLUMNS):
    column = panel(columns, 'Column', f'Column{col}')
    category = panel(column, 'CategoryHeader')
    panel(category, 'CategoryAccent')
    label(category, 'Category', f'Category{col}')
    cards = panel(column, 'Cards')
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
        panel(icon, 'WeaponLayer IconHalo').set('hittest', 'false')
        panel(icon, 'WeaponLayer IconSilhouette').set('hittest', 'false')
        details = panel(content, 'CardDetails')
        panel(details, 'RarityMark')
        label(details, 'Status', f'Status{slot}')
        label(details, 'Price', f'Price{slot}')
        ET.SubElement(surface, 'Button', {'id': f'Buy{slot}', 'class': 'BuyHit'})
    pager_space = panel(column, 'PagerSpace')
    pager = panel(pager_space, 'ItemPager', f'ItemPager{col}')
    nav(pager, f'Prev{col}', f'Previous{col}', '‹')
    label(pager, 'PageText', f'Page{col}')
    nav(pager, f'Next{col}', f'NextItems{col}', '›')
label(body, 'Empty', 'Empty')
footer = panel(window, 'Footer')
label(footer, 'Hint', 'Hint')
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
.ShopRoot Label { font-family: Stratum2, Arial; }
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
.Body { width: 100%; height: fill-parent-flow(1); padding: 18px 18px 10px; }
.Columns { width: 100%; height: 100%; flow-children: right; }
.Columns1 .Columns { width: 340px; horizontal-align: center; }
.Column { width: fill-parent-flow(1); height: 100%; margin: 0px 6px; flow-children: down; visibility: collapse; }
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
.IconHalo { visibility: collapse; blur: gaussian(3); opacity: 0.85; }
.IconSilhouette { wash-color: #64707c; opacity: 0.45; }
.Card.Available .IconHalo { visibility: visible; }
.Card.Available .IconSilhouette { wash-color: #dfebf2; opacity: 1; }
.CardDetails { width: 100%; height: 22px; flow-children: right; }
.RarityMark { width: 13px; height: 3px; vertical-align: center; margin-right: 6px; background-color: #52606c; }
.Status { width: fill-parent-flow(1); height: 22px; vertical-align: center; font-size: 11px; color: #7b8996; text-overflow: ellipsis; }
.Price { vertical-align: center; color: #687582; font-size: 17px; font-weight: bold; margin-left: 4px; }
.Card.Available .Price { color: #f2d38d; }
.BuyHit { width: 100%; height: 100%; visibility: collapse; }
.Card.Available .BuyHit { visibility: visible; border: 1px solid #00000000; border-radius: 4px; }
.BuyHit:hover { background-color: #a2e5e20a; border-color: #a7e7e080; }
.BuyHit:active { background-color: #bdece51c; border-color: #d9fff3; }
.PagerSpace { width: 100%; height: 0px; }
.HasItemPages .PagerSpace { height: 28px; }
.ItemPager { width: 100%; height: 28px; flow-children: right; visibility: collapse; }
.ItemPager.Visible { visibility: visible; }
.Nav { width: 32px; height: 28px; }
.NavGlyph { color: #435464; font-size: 24px; horizontal-align: center; vertical-align: center; }
.NavHit { width: 100%; height: 100%; visibility: collapse; }
.Nav.Available .NavHit { visibility: visible; background-color: #76d9cc12; border: 1px solid #76d9cc33; border-radius: 3px; }
.Nav.Available .NavHit .NavGlyph { color: #a6dad6; }
.NavHit:hover { background-color: #76d9cc2d; }
.PageText { width: fill-parent-flow(1); vertical-align: center; text-align: center; color: #718b9c; font-size: 13px; }
.Empty { visibility: collapse; horizontal-align: center; vertical-align: center; color: #9bacbb; font-size: 20px; }
.Empty.Visible { visibility: visible; }
.Footer { width: 100%; height: 48px; padding: 10px 24px; background-color: #09111ae8; border-top: 1px solid #7a9eae22; flow-children: right; }
.Hint { width: fill-parent-flow(1); vertical-align: center; font-size: 13px; color: #7d96a7; text-overflow: ellipsis; }
.CategoryPager { width: 144px; height: 28px; margin-left: 18px; flow-children: right; visibility: collapse; }
.CategoryPager.Visible { visibility: visible; }
'''
for columns in range(1, COLUMNS + 1):
    width = min(1720, max(720, columns * 224 + 48))
    css += f'.Columns{columns} .ShopWindow {{ width: {width}px; }}\n'
for rows in range(1, ROWS + 1):
    height = 236 + rows * 124
    css += f'.Rows{rows} .ShopWindow {{ height: {height}px; }}\n'
    css += f'.Rows{rows} .Card {{ height: {100 / rows:.6f}%; }}\n'
for name, color in COLORS.items():
    css += f'.Card.Available.Rarity{name} .IconHalo {{ wash-color: {color}; }}\n'
    css += f'.Card.Available.Rarity{name} .RarityMark {{ background-color: {color}; }}\n'
    css += f'.Card.Available.Rarity{name} .BuyHit:hover {{ border-color: {color}; box-shadow: {color}33 0px 0px 9px 0px; }}\n'
for icon in sorted(set(ICONS)):
    asset = 'kevlar' if icon == 'equipment' else icon
    css += f'.Icon_{icon} .WeaponLayer {{ background-image: url("s2r://panorama/images/icons/equipment/{asset}.vsvg"); }}\n'
for file, data in [(LAYOUT.replace('.vxml_c', '.xml'), xml), (STYLE.replace('.vcss_c', '.css'), css)]:
    path = CONTENT / file
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(data, encoding='utf-8')
print(f'Shop HUD: {COLUMNS} columns, {COLUMNS * ROWS} cards, contour rarity glow')
