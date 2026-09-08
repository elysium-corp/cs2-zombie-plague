"""Rebuild the static v4 banner layout and effects; no client scripts or dynamic paths."""
from pathlib import Path
import re
import json
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1] / 'CustomHud.Core/resources/hud/messages/content/panorama'
icons = {
    'info': '<circle cx="32" cy="32" r="25"/><path d="M32 29v18M32 18v2"/>',
    'warning': '<path d="M32 7 59 55H5ZM32 25v13M32 45v2"/>',
    'infection': '<circle cx="32" cy="32" r="12"/><path d="M32 4v12M32 48v12M4 32h12M48 32h12M12 12l9 9M43 43l9 9M12 52l9-9M43 21l9-9"/><circle cx="28" cy="30" r="1"/><circle cx="36" cy="36" r="1"/>',
    'skull': '<path d="M20 47C3 43 5 8 32 8s29 35 12 39v10H20Z"/><circle cx="22" cy="30" r="5"/><circle cx="42" cy="30" r="5"/><path d="m29 43 3-5 3 5M28 50v7M36 50v7"/>',
    'shield': '<path d="M32 6 54 15v16c0 13-11 22-22 27C21 53 10 44 10 31V15ZM21 31l8 8 16-18"/>',
    'trophy': '<path d="M18 7h28v19c0 20-28 20-28 0ZM18 13H7v10c0 9 8 13 14 13M46 13h11v10c0 9-8 13-14 13M32 41v15M20 56h24"/>',
    'star': '<path d="m32 5 8 18 20 2-15 14 4 20-17-10-17 10 4-20L4 25l20-2Z"/>',
    'gift': '<path d="M8 25h48v12H8ZM12 37v21h40V37M32 25v33"/><path d="M32 25C8 26 13 2 25 9c6 3 7 16 7 16Zm0 0C56 26 51 2 39 9c-6 3-7 16-7 16Z"/>',
    'megaphone': '<path d="M7 26h14l34-16v42L21 37H7ZM21 37l7 18H16l-5-18M21 26v11"/>',
    'lightning': '<path d="M36 4 10 37h20l-3 23 27-35H34Z"/>',
    'clock': '<circle cx="32" cy="32" r="25"/><path d="M32 16v17l13 8"/>',
    'heart': '<path d="M32 56 10 34C-8 13 18-5 32 16 46-5 72 13 54 34Z"/>',
}
image_dir = root / 'images/custom_game/elysium/banners'
image_dir.mkdir(parents=True, exist_ok=True)
for name, body in icons.items():
    (image_dir / f'{name}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64"><g fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" stroke-linejoin="round">{body}</g></svg>\n')

layout = ET.Element('root')
styles = ET.SubElement(layout, 'styles')
ET.SubElement(styles, 'include', src='s2r://panorama/styles/custom_game/elysium_messages_v4_r2.vcss_c')
canvas = ET.SubElement(layout, 'Panel', id='ElysiumMessageCanvas', attrib={'class': 'ElysiumMessageCanvas', 'hittest': 'false'})
positions = ['TopLeft', 'TopCenter', 'TopRight', 'MiddleLeft', 'Center', 'MiddleRight', 'BottomLeft', 'BottomCenter', 'BottomRight']
region = ET.SubElement(canvas, 'Panel', id='MessageRegion', attrib={'class': 'MessageRegion'})
for i in range(3):
    slot = f'Message{i}'
    panel = ET.SubElement(region, 'Panel', id=slot, attrib={'class': 'Message', 'hittest': 'false'})
    surface = ET.SubElement(panel, 'Panel', attrib={'class': 'BannerSurface'})
    ET.SubElement(surface, 'Panel', attrib={'class': 'MessageAccent'})
    ET.SubElement(surface, 'Label', attrib={'class': 'MessageBrand', 'text': 'ELYSIUM'})
    content = ET.SubElement(surface, 'Panel', attrib={'class': 'BannerContent'})
    icon_panel = ET.SubElement(content, 'Panel', attrib={'class': 'BannerIcons'})
    for name in icons:
        ET.SubElement(icon_panel, 'Image', attrib={'class': f'BannerIcon Icon_{name}', 'src': f's2r://panorama/images/custom_game/elysium/banners/{name}.vsvg', 'texturewidth': '64', 'textureheight': '64'})
    texts = ET.SubElement(content, 'Panel', attrib={'class': 'BannerTexts'})
    for suffix in ['Header', 'Title'] + [f'Line{n}' for n in range(4)]:
        row = ET.SubElement(texts, 'Panel', id=slot+suffix, attrib={'class': 'MessageLine ' + ('Banner'+suffix if suffix in ['Header','Title'] else 'BannerDescription')})
        for n in range(12):
            ET.SubElement(row, 'Label', id=f'{slot}{suffix}Run{n}', attrib={'class': 'MessageRun', 'text': '{s:value}'})
ET.indent(layout, space='    ')
(root / 'layout/custom_game/elysium_messages_v4_r2.xml').write_text(ET.tostring(layout, encoding='unicode') + '\n')
css_path = root / 'styles/custom_game/elysium_messages_v4_r2.css'
css = css_path.read_text().split('/* Banner constructor */')[0].rstrip()
palette = sorted(set(re.findall(r'\.MessageRun\.C(\d+) \{ color: (#[A-F0-9]+); \}', css)))
css += '''

/* Banner constructor */
.MessageRegion { width: 960px; min-width: 960px; flow-children: down; overflow: noclip; }
.MessageRegion .Message { margin-bottom: 10px; }
.PositionTopCenter .Message, .PositionCenter .Message, .PositionBottomCenter .Message { horizontal-align: center; }
.PositionTopRight .Message, .PositionMiddleRight .Message, .PositionBottomRight .Message { horizontal-align: right; }
.BannerContent, .BannerTexts, .MessageLine { overflow: noclip; }

.BannerContent { width: 100%; flow-children: right; }
.BannerTexts { flow-children: down; width: fill-parent-flow(1); }
.BannerIcons { visibility: collapse; width: 64px; height: 64px; margin-right: 16px; vertical-align: center; }
.BannerIcon { width: 64px; height: 64px; visibility: collapse; }
.CustomBanner .MessageBrand, .CustomBanner .MessageAccent { visibility: collapse; }
.CustomBanner { padding: 18px 22px; }
.CustomBanner.Shown { animation-name: none; animation-duration: 0.4s; }
.CustomBanner .BannerIcons { visibility: visible; }
.CustomBanner.Icon_none .BannerIcons { visibility: collapse; }
.CustomBanner.IconPosition_top .BannerContent { flow-children: down; }
.CustomBanner.IconPosition_top .BannerIcons { horizontal-align: center; margin: 0px 0px 12px 0px; }
.CustomBanner.IconPosition_top .BannerTexts { width: 100%; }
.CustomBanner.Width_small { width: 440px; min-width: 440px; }
.CustomBanner.Width_medium { width: 600px; min-width: 600px; }
.CustomBanner.Width_large { width: 760px; min-width: 760px; }
.CustomBanner .MessageRun { font-size: 22px; }
.CustomBanner .BannerHeader .MessageRun { font-size: 14px; font-weight: bold; letter-spacing: 2px; }
.CustomBanner .BannerTitle .MessageRun { font-size: 30px; font-weight: bold; }
.CustomBanner .BannerHeader { margin-bottom: 6px; min-height: 18px; }
.CustomBanner .BannerTitle { margin-bottom: 8px; }
.CustomBanner.Size_small .MessageRun { font-size: 18px; }
.CustomBanner.Size_small .BannerTitle .MessageRun { font-size: 24px; }
.CustomBanner.Size_small .BannerHeader .MessageRun { font-size: 12px; }
.CustomBanner.Size_large .MessageRun { font-size: 26px; }
.CustomBanner.Size_large .BannerTitle .MessageRun { font-size: 36px; }
.CustomBanner.Size_large .BannerHeader .MessageRun { font-size: 16px; }
.CustomBanner.Align_left .MessageLine { horizontal-align: left; }
.CustomBanner.Align_center .MessageLine { horizontal-align: center; }
.CustomBanner.Align_right .MessageLine { horizontal-align: right; }
.CustomBanner.Theme_glass { background-color: #11242cb8; }
.CustomBanner.Theme_solid { background-color: #15232aff; }
.CustomBanner.Theme_light { background-color: #e9f0f5f5; }
.CustomBanner.Theme_light .MessageRun { text-shadow: none; }
.CustomBanner.Theme_light .MessageRun { color: #15232a; }
.CustomBanner.Theme_danger { background-color: gradient(linear, 0% 0%, 100% 100%, from(#320e16f5), to(#170c16f0)); }
.CustomBanner.Theme_transparent { background-color: transparent; box-shadow: none; }
.CustomBanner.Corners_square { border-radius: 0px; }
.CustomBanner.Corners_soft { border-radius: 8px; }
.CustomBanner.Corners_round { border-radius: 20px; }
.CustomBanner.Border_none { border: 0px solid transparent; }
.CustomBanner.Border_line { border-width: 0px 0px 0px 3px; }
.CustomBanner.Border_frame { border-width: 2px; }
.CustomBanner.Speed_fast { animation-duration: 0.2s; }
.CustomBanner.Speed_normal { animation-duration: 0.4s; }
.CustomBanner.Speed_slow { animation-duration: 0.8s; }
.CustomBanner.Leaving { opacity: 0; animation-timing-function: ease-in; }
'''
for name in icons:
    css += f'.CustomBanner.Icon_{name} .Icon_{name} {{ visibility: visible; }}\n'
for idx, color in palette:
    css += f'.CustomBanner.A{idx} {{ border-color: {color}; }}\n.CustomBanner.A{idx} .BannerIcon {{ wash-color: {color}; }}\n'
    # Preserve explicit inline colors in the light theme.
    css += f'.CustomBanner.Theme_light .MessageRun.C{idx} {{ color: {color}; }}\n'
effects = json.loads((Path(__file__).resolve().parents[1] / 'CustomHud.Api/Resources/banner-effects.json').read_text())
def frames(values):
    rules = []
    for index, value in enumerate(values):
        offset = value.get('offset', index / max(1, len(values) - 1)) * 100
        properties = []
        for key, item in value.items():
            if key == 'offset': continue
            if key == 'transform':
                item = item.lower()
                item = re.sub(r'scale\(([^)]+)\)', lambda match: f'scale3d({match[1]}, {match[1]}, 1)', item)
            properties.append(f'{key}: {item};')
        rules.append(f'{offset:g}% {{ {" ".join(properties)} }}')
    return ' '.join(rules)
for name, values in effects['motion'].items():
    if name == 'none': continue
    for generation in ['A', 'B']:
        css += f".CustomBanner.Enter_{name}.Entry{generation} {{ animation-name: banner-in-{name}-{generation}; }}\n@keyframes 'banner-in-{name}-{generation}' {{ {frames(values)} }}\n"
    reverse = [{**value, 'offset': 1 - value.get('offset', index / (len(values) - 1))} for index, value in reversed(list(enumerate(values)))]
    css += f".CustomBanner.Shown.Leaving.Exit_{name} {{ animation-name: banner-out-{name}; }}\n@keyframes 'banner-out-{name}' {{ {frames(reverse)} }}\n"
for name, values in effects['loops'].items():
    css += f"@keyframes 'banner-loop-{name}' {{ {frames(values)} }}\n"
    for field, target in {'Container': '.BannerSurface', 'Header': '.BannerHeader', 'Title': '.BannerTitle', 'Description': '.BannerDescription', 'Parameter': '.MessageRun.Parameter', 'Icon': '.BannerIcon'}.items():
        if name.startswith('spin') and field != 'Icon': continue
        css += f'.CustomBanner.{field}Animation_{name} {target} {{ animation-name: banner-loop-{name}; animation-duration: 2.4s; animation-iteration-count: infinite; animation-timing-function: ease-in-out; }}\n'
loop_targets = ['.BannerSurface', '.BannerHeader', '.BannerTitle', '.BannerDescription', '.MessageRun.Parameter', '.BannerIcon']
for name, duration in {'fast': 1.2, 'normal': 2.4, 'slow': 3.6}.items():
    css += ', '.join(f'.CustomBanner.LoopSpeed_{name} {target}' for target in loop_targets) + f' {{ animation-duration: {duration}s; }}\n'
for delay in range(0, 2001, 100):
    css += ', '.join(f'.CustomBanner.EffectDelay_{delay} {target}' for target in loop_targets) + f' {{ animation-delay: {delay / 1000:g}s; }}\n'
# Непрозрачность задаётся альфа-каналом фона; текст и иконка не становятся прозрачными.
def alpha(color, opacity):
    rgb = color[:7]
    original = int(color[7:9], 16) if len(color) == 9 else 255
    return rgb + format(round(original * opacity / 100), '02x')
themes = {'glass': '#11242cb8', 'solid': '#15232aff', 'light': '#e9f0f5f5', 'transparent': '#00000000'}
backgrounds = {'slate': '#15232a', 'black': '#090d12', 'blue': '#10233f', 'purple': '#271735', 'red': '#35121c', 'green': '#112e26', 'gold': '#342a13', 'white': '#eef3f5'}
for opacity in range(0, 101, 10):
    for name, color in themes.items():
        css += f'.CustomBanner.Theme_{name}.Background_theme.BackgroundOpacity_{opacity} {{ background-color: {alpha(color, opacity)}; }}\n'
    css += f'.CustomBanner.Theme_midnight.Background_theme.BackgroundOpacity_{opacity} {{ background-color: gradient(linear, 0% 0%, 100% 100%, from({alpha("#0c171cf2", opacity)}), to({alpha("#14252ae8", opacity)})); }}\n'
    css += f'.CustomBanner.Theme_danger.Background_theme.BackgroundOpacity_{opacity} {{ background-color: gradient(linear, 0% 0%, 100% 100%, from({alpha("#320e16f5", opacity)}), to({alpha("#170c16f0", opacity)})); }}\n'
    for name, color in backgrounds.items():
        css += f'.CustomBanner.Background_{name}.BackgroundOpacity_{opacity} {{ background-color: {alpha(color, opacity)}; }}\n'
for name, shadow in {'none': 'none', 'soft': '#00000050 0px 3px 12px 0px', 'strong': '#000000b0 0px 6px 24px 0px'}.items():
    css += f'.CustomBanner.Shadow_{name} {{ box-shadow: {shadow}; }}\n'
for width in range(320, 961, 40):
    css += f'.CustomBanner.WidthPixels_{width} {{ width: {width}px; min-width: {width}px; }}\n'
for padding in range(0, 41, 4):
    css += f'.CustomBanner.Padding_{padding} {{ padding: {padding}px; }}\n'
for gap in range(0, 25, 2):
    css += f'.CustomBanner.Gap_{gap} .BannerHeader, .CustomBanner.Gap_{gap} .BannerTitle {{ margin-bottom: {gap}px; }}\n'
for field, limits in {'Header': (10, 24), 'Title': (16, 48), 'Description': (12, 32)}.items():
    for size in range(limits[0], limits[1] + 1, 2):
        css += f'.CustomBanner.{field}Size_{size} .Banner{field} .MessageRun {{ font-size: {size}px; }}\n'
for size in range(24, 97, 8):
    css += f'.CustomBanner.IconSize_{size} .BannerIcons, .CustomBanner.IconSize_{size} .BannerIcon {{ width: {size}px; height: {size}px; }}\n'
colors = {'white': '#ffffff', 'muted': '#adc2ce', 'mint': '#85dcb1', 'gold': '#ffd36a', 'red': '#ff4d6d', 'green': '#69d98b', 'blue': '#73a7ff', 'cyan': '#66e0eb', 'purple': '#bda0ff', 'pink': '#ffa3d3', 'black': '#15232a'}
for field in ['Header', 'Title', 'Description']:
    for name, color in colors.items():
        css += f'.CustomBanner.{field}Color_{name} .Banner{field} .MessageRun {{ color: {color}; }}\n'
# Разметка Localization имеет больший приоритет, чем общий цвет текстового блока.
for idx, color in palette:
    css += f'.CustomBanner .BannerTexts .MessageLine .MessageRun.C{idx} {{ color: {color}; }}\n'
css += '.CustomBanner.NoDescription .BannerTitle { margin-bottom: 0px; }\n'
css += '.CustomBanner.NoDescription.NoTitle .BannerHeader { margin-bottom: 0px; }\n'
# Перенос визуальных свойств на отдельную поверхность предотвращает конфликт transform-анимаций.
css = re.sub(r'^(\.CustomBanner(?:\.(?:A\d+|Theme_[a-z]+|Corners_[a-z]+|Border_[a-z]+|Background_[a-z]+|BackgroundOpacity_\d+|Padding_\d+|Shadow_[a-z]+))+) (\{[^\n]+)',
             r'\1 .BannerSurface \2', css, flags=re.M)
css += '.BannerSurface { width: 100%; flow-children: down; }\n'
css += '.Message.CustomBanner { padding: 0px; background-color: transparent; border: 0px solid transparent; box-shadow: none; overflow: noclip; }\n'
# Начальные свойства стоят перед вариантами дизайна, чтобы выбранные фон и отступы имели приоритет.
css = css.replace('/* Banner constructor */', "/* Banner constructor */\n.CustomBanner .BannerSurface { padding: 18px 22px; background-color: gradient(linear, 0% 0%, 100% 100%, from(#0c171cf2), to(#14252ae8)); border: 1px solid #85dcb140; border-radius: 7px; box-shadow: #00000080 0px 4px 14px 0px; overflow: noclip; }")
for idx, color in palette:
    css += f'.CustomBanner.A{idx}.ParameterColor_accent .MessageRun.Parameter {{ color: {color}; }}\n'
    for glow, radius, strength in [('soft', 6, 1), ('strong', 12, 2)]:
        css += f'.CustomBanner.A{idx}.IconGlow_{glow} .BannerIcon {{ img-shadow: 0px 0px {radius}px {strength}.0 {color}; }}\n'
        css += f'.CustomBanner.A{idx}.TextGlow_{glow} .MessageRun {{ text-shadow: 0px 0px {radius}px {strength}.0 {color}; }}\n'
    for effect in ['glow', 'neon']:
        css += f'.CustomBanner.A{idx}.ContainerAnimation_{effect} .BannerSurface {{ box-shadow: {color} 0px 0px 14px 0px; }}\n'
        css += f'.CustomBanner.A{idx}.IconAnimation_{effect} .BannerIcon {{ img-shadow: 0px 0px 8px 1.0 {color}; }}\n'
        for field, target in {'Header': '.BannerHeader .MessageRun', 'Title': '.BannerTitle .MessageRun', 'Description': '.BannerDescription .MessageRun', 'Parameter': '.MessageRun.Parameter'}.items():
            css += f'.CustomBanner.A{idx}.{field}Animation_{effect} {target} {{ text-shadow: 0px 0px 8px 1.0 {color}; }}\n'
for name, color in colors.items():
    css += f'.CustomBanner.ParameterColor_{name} .BannerTexts .MessageLine .MessageRun.Parameter {{ color: {color}; }}\n'
# Класс акцента должен иметь тот же приоритет, что и явный HTML-цвет параметра.
for idx, color in palette:
    css += f'.CustomBanner.A{idx}.ParameterColor_accent .BannerTexts .MessageLine .MessageRun.Parameter {{ color: {color}; }}\n'
for width in range(128, 961, 8):
    css += f'.CustomBanner.TextWidth_{width} .BannerTexts {{ width: {width}px; min-width: {width}px; }}\n'
assert len(layout.findall('.//*[@id]')) < 1024
classes = set(re.findall(r'\.([A-Za-z_][A-Za-z_0-9]*)', css))
assert len(classes) < 1024, f'Panorama CSS class limit: {len(classes)}'
css_path.write_text(css)
print(f'Generated v4: {len(layout.findall(".//*[@id]"))} panel IDs, {len(icons)} icons')
