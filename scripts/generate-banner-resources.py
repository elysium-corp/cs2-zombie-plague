"""Rebuild the static v4 banner layout and effects; no client scripts or dynamic paths."""
from pathlib import Path
import re
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
ET.SubElement(styles, 'include', src='s2r://panorama/styles/custom_game/elysium_messages_v4.vcss_c')
canvas = ET.SubElement(layout, 'Panel', id='ElysiumMessageCanvas', attrib={'class': 'ElysiumMessageCanvas', 'hittest': 'false'})
positions = ['TopLeft', 'TopCenter', 'TopRight', 'MiddleLeft', 'Center', 'MiddleRight', 'BottomLeft', 'BottomCenter', 'BottomRight']
for i, position in enumerate(positions):
    slot = f'Message{i}'
    panel = ET.SubElement(canvas, 'Panel', id=slot, attrib={'class': f'Message Position{position}', 'hittest': 'false'})
    ET.SubElement(panel, 'Panel', attrib={'class': 'MessageAccent'})
    ET.SubElement(panel, 'Label', attrib={'class': 'MessageBrand', 'text': 'ELYSIUM'})
    content = ET.SubElement(panel, 'Panel', attrib={'class': 'BannerContent'})
    icon_panel = ET.SubElement(content, 'Panel', attrib={'class': 'BannerIcons'})
    for name in icons:
        ET.SubElement(icon_panel, 'Image', attrib={'class': f'BannerIcon Icon_{name}', 'src': f'file://{{images}}/custom_game/elysium/banners/{name}.svg', 'texturewidth': '64', 'textureheight': '64'})
    texts = ET.SubElement(content, 'Panel', attrib={'class': 'BannerTexts'})
    for suffix in ['Header', 'Title'] + [f'Line{n}' for n in range(4)]:
        row = ET.SubElement(texts, 'Panel', id=slot+suffix, attrib={'class': 'MessageLine ' + ('Banner'+suffix if suffix in ['Header','Title'] else 'BannerDescription')})
        for n in range(12):
            ET.SubElement(row, 'Label', id=f'{slot}{suffix}Run{n}', attrib={'class': 'MessageRun', 'text': '{s:value}'})
ET.indent(layout, space='    ')
(root / 'layout/custom_game/elysium_messages_v4.xml').write_text(ET.tostring(layout, encoding='unicode') + '\n')
css_path = root / 'styles/custom_game/elysium_messages_v4.css'
css = css_path.read_text().split('/* Banner constructor */')[0].rstrip()
css += '''

/* Banner constructor */
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
for idx, color in re.findall(r'\.MessageRun\.C(\d+) \{ color: (#[A-F0-9]+); \}', css):
    css += f'.CustomBanner.A{idx} {{ border-color: {color}; }}\n.CustomBanner.A{idx} .BannerIcon {{ wash-color: {color}; }}\n'
    # Preserve explicit inline colors in the light theme.
    css += f'.CustomBanner.Theme_light .MessageRun.C{idx} {{ color: {color}; }}\n'
transforms = {'fade': 'translatey(0px)', 'slide_up': 'translatey(24px)', 'slide_down': 'translatey(-24px)', 'slide_left': 'translatex(32px)', 'slide_right': 'translatex(-32px)', 'zoom': 'scale3d(0.82, 0.82, 1)'}
for name, transform in transforms.items():
    for generation in ['A','B']:
        css += f".CustomBanner.Enter_{name}.Entry{generation} {{ animation-name: banner-in-{name}-{generation}; }}\n@keyframes 'banner-in-{name}-{generation}' {{ 0% {{ opacity: 0; transform: {transform}; }} 100% {{ opacity: 1; transform: translatey(0px); }} }}\n"
    css += f".CustomBanner.Shown.Leaving.Exit_{name} {{ animation-name: banner-out-{name}; }}\n@keyframes 'banner-out-{name}' {{ 0% {{ opacity: 1; transform: translatey(0px); }} 100% {{ opacity: 0; transform: {transform}; }} }}\n"
for name, keyframes in {'pulse': '0% { opacity: 0.55; } 50% { opacity: 1; } 100% { opacity: 0.55; }', 'spin': '0% { transform: rotatez(0deg); } 100% { transform: rotatez(360deg); }', 'bounce': '0% { transform: translatey(0px); } 50% { transform: translatey(-6px); } 100% { transform: translatey(0px); }', 'shake': '0% { transform: rotatez(-8deg); } 50% { transform: rotatez(8deg); } 100% { transform: rotatez(-8deg); }'}.items():
    css += f".CustomBanner.IconAnimation_{name} .BannerIcon {{ animation-name: banner-icon-{name}; animation-duration: 1.8s; animation-iteration-count: infinite; animation-timing-function: ease-in-out; }}\n@keyframes 'banner-icon-{name}' {{ {keyframes} }}\n"
css_path.write_text(css)
print(f'Generated v4: {len(layout.findall(".//*[@id]"))} panel IDs, {len(icons)} icons')
