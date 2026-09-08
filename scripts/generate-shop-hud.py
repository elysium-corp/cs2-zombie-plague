#!/usr/bin/env python3
"""Генерирует статический пул Custom HUD: 8 колонок по 6 карточек без клиентских скриптов"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / 'Shop.Core/resources/hud/shop/content/panorama'
ICON_SOURCE = ROOT / 'Shop.Core/src/Hud/ShopHudIcons.cs'
ICONS = re.findall(r'"([a-z0-9_]+)"', ICON_SOURCE.read_text().split('internal static string Normalize')[0])
COLORS = dict(Common='#b0c3d9', Uncommon='#5e98d9', Rare='#4b69ff', Restricted='#8847ff',
              Classified='#d32ce6', Elite='#eb4b4b', Prototype='#ff8c00', Legendary='#e4ae39')


def label(parent, id=None, text='{s:value}', cls=None):
    attrs = {'text': text, 'hittest': 'false'}
    if id: attrs['id'] = id
    if cls: attrs['class'] = cls
    return ET.SubElement(parent, 'Label', attrs)


def nav(parent, panel, button, text):
    node = ET.SubElement(parent, 'Panel', {'id': panel, 'class': 'Nav'})
    label(node, text=text, cls='NavGlyph')
    hit = ET.SubElement(node, 'Button', {'id': button, 'class': 'NavHit'})
    label(hit, text=text, cls='NavGlyph')


root = ET.Element('root')
styles = ET.SubElement(root, 'styles')
ET.SubElement(styles, 'include', {'src': 's2r://panorama/styles/custom_game/elysium_shop_v1.vcss_c'})
screen = ET.SubElement(root, 'Panel', {'id': 'ShopRoot', 'class': 'ShopRoot'})
window = ET.SubElement(screen, 'Panel', {'class': 'ShopWindow'})
header = ET.SubElement(window, 'Panel', {'class': 'Header'})
brand = ET.SubElement(header, 'Panel', {'class': 'Brand'})
label(brand, text='ELYSIUM', cls='BrandName')
label(brand, id='StoreTitle', cls='StoreTitle')
label(header, id='Balance', cls='Balance')
close = ET.SubElement(header, 'Button', {'id': 'Close', 'class': 'Close'})
label(close, text='×', cls='CloseLabel')
body = ET.SubElement(window, 'Panel', {'class': 'Body'})
columns = ET.SubElement(body, 'Panel', {'class': 'Columns'})
for col in range(8):
    column = ET.SubElement(columns, 'Panel', {'id': f'Column{col}', 'class': 'Column'})
    label(column, id=f'Category{col}', cls='Category')
    cards = ET.SubElement(column, 'Panel', {'class': 'Cards'})
    for row in range(6):
        slot = col * 6 + row
        card = ET.SubElement(cards, 'Panel', {'id': f'Card{slot}', 'class': 'Card'})
        label(card, id=f'Name{slot}', cls='ItemName')
        ET.SubElement(card, 'Panel', {'id': f'Icon{slot}', 'class': 'ItemIcon', 'hittest': 'false'})
        label(card, id=f'Price{slot}', cls='Price')
        label(card, id=f'Status{slot}', cls='Status')
        ET.SubElement(card, 'Button', {'id': f'Buy{slot}', 'class': 'BuyHit'})
    pager = ET.SubElement(column, 'Panel', {'class': 'ItemPager'})
    nav(pager, f'Prev{col}', f'Previous{col}', '‹')
    label(pager, id=f'Page{col}', cls='PageText')
    nav(pager, f'Next{col}', f'NextItems{col}', '›')
label(body, id='Empty', cls='Empty')
footer = ET.SubElement(window, 'Panel', {'class': 'Footer'})
label(footer, id='Hint', cls='Hint')
pages = ET.SubElement(footer, 'Panel', {'class': 'CategoryPager'})
nav(pages, 'CategoryPrev', 'CategoriesPrevious', '‹')
label(pages, id='CategoryPage', cls='PageText')
nav(pages, 'CategoryNext', 'CategoriesNext', '›')
ET.indent(root, space='    ')
xml = ET.tostring(root, encoding='unicode') + '\n'
css = '''/* Создано scripts/generate-shop-hud.py */
.ShopRoot { width: 100%; height: 100%; visibility: collapse; background-color: #07101855; }
.ShopRoot.Visible { visibility: visible; }
.ShopWindow { width: 94%; max-width: 1720px; height: 920px; max-height: 94%; horizontal-align: center; vertical-align: center; flow-children: down; background-color: #10171feF; border: 1px solid #b3c5d42b; box-shadow: 0px 8px 40px #00000099; }
.Header { width: 100%; height: 100px; padding: 18px 24px; flow-children: right; background-color: #0b0f15f8; border-bottom: 2px solid #75dbcd; }
.Brand { width: fill-parent-flow(1); height: 100%; flow-children: down; }
Label { font-family: Stratum2, Arial; }
.BrandName { font-size: 30px; font-weight: bold; letter-spacing: 6px; color: #ebf4fa; }
.StoreTitle { font-size: 18px; color: #889cac; text-overflow: ellipsis; }
.Balance { font-size: 28px; font-weight: bold; color: #f2de9c; vertical-align: center; margin-right: 30px; }
.Close { width: 56px; height: 56px; background-color: #ffffff08; border: 1px solid #ffffff12; }
.Close:hover { background-color: #e55d5d44; }
.CloseLabel { font-family: Arial; font-size: 38px; color: #d2dbe4; horizontal-align: center; vertical-align: center; }
.Body { width: 100%; height: fill-parent-flow(1); padding: 12px 16px; }
.Columns { width: 100%; height: 100%; flow-children: right; }
.Column { width: 12.5%; height: 100%; padding: 0px 5px; flow-children: down; visibility: collapse; }
.Column.Visible { visibility: visible; }
.Category { width: 100%; height: 38px; font-size: 18px; font-weight: bold; text-align: center; text-overflow: ellipsis; color: #b0becb; padding-top: 7px; }
.Cards { width: 100%; height: fill-parent-flow(1); flow-children: down; }
.Card { width: 100%; height: 15.5%; margin-bottom: 6px; background-color: #ffffff03; border: 1px solid #ffffff08; visibility: collapse; }
.Card.Visible { visibility: visible; }
.Card.Available { background-color: #ffffff08; border-bottom: 2px solid #b0c3d944; }
.ItemName { width: 100%; height: 25px; margin: 8px 8px 0px; font-size: 17px; font-weight: medium; color: #626d78; text-align: right; text-overflow: ellipsis; }
.ItemIcon { width: 80%; height: 43px; horizontal-align: center; vertical-align: center; margin-bottom: 4px; background-size: contain; background-position: center; background-repeat: no-repeat; wash-color: #5b626b; opacity: 0.5; }
.Price { horizontal-align: right; vertical-align: bottom; margin: 0px 9px 8px 0px; color: #646c75; font-size: 17px; font-weight: bold; }
.Card.Available .Price { color: #f3cf79; }
.Card.Available .ItemIcon { opacity: 1; }
.Status { vertical-align: bottom; width: 64%; max-height: 30px; margin: 0px 0px 7px 7px; font-size: 11px; color: #7b8590; text-overflow: ellipsis; }
.BuyHit { width: 100%; height: 100%; visibility: collapse; }
.Card.Available .BuyHit { visibility: visible; }
.BuyHit:hover { background-color: #ffffff0b; border: 1px solid #ffffff60; }
.BuyHit:active { background-color: #ffffff22; }
.ItemPager { width: 100%; height: 32px; flow-children: right; }
.Nav { width: 34px; height: 32px; }
.NavGlyph { color: #44515e; font-size: 26px; horizontal-align: center; vertical-align: center; }
.NavHit { width: 100%; height: 100%; visibility: collapse; }
.Nav.Available .NavHit { visibility: visible; background-color: #202d39; }
.Nav.Available .NavHit .NavGlyph { color: #c1d8e7; }
.NavHit:hover { background-color: #34495c; }
.PageText { width: fill-parent-flow(1); vertical-align: center; text-align: center; color: #7c91a4; font-size: 14px; }
.Empty { visibility: collapse; horizontal-align: center; vertical-align: center; color: #9bacbb; font-size: 24px; }
.Empty.Visible { visibility: visible; }
.Footer { width: 100%; height: 56px; padding: 10px 22px; background-color: #0b1017ed; flow-children: right; }
.Hint { width: fill-parent-flow(1); vertical-align: center; font-size: 15px; color: #7e94a4; }
.CategoryPager { width: 160px; height: 32px; flow-children: right; }
'''
for name, color in COLORS.items():
    css += f'.Card.Available.Rarity{name} {{ border-bottom-color: {color}; }}\n'
    css += f'.Card.Available.Rarity{name} .ItemName {{ color: {color}; }}\n'
    css += f'.Card.Available.Rarity{name} .ItemIcon {{ wash-color: {color}; }}\n'
for icon in sorted(set(ICONS)):
    asset = 'kevlar' if icon == 'equipment' else icon
    css += f'.Icon_{icon} {{ background-image: url("s2r://panorama/images/icons/equipment/{asset}.vsvg"); }}\n'
for file, data in [('layout/custom_game/elysium_shop_v1.xml', xml), ('styles/custom_game/elysium_shop_v1.css', css)]:
    path = CONTENT / file
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(data, encoding='utf-8')
print('Shop HUD: 8 columns, 48 cards, native icons')
