"""Создаёт Web-макет из тех же XML/CSS, которые компилируются для Panorama."""
from pathlib import Path
import hashlib
import json
import re
import xml.etree.ElementTree as ET


def rules(css):
    css = re.sub(r'/\*.*?\*/', '', css, flags=re.S)
    cursor = 0
    while cursor < len(css):
        start = css.find('{', cursor)
        if start < 0:
            break
        depth, end = 1, start + 1
        while depth:
            if css[end] == '{':
                depth += 1
            elif css[end] == '}':
                depth -= 1
            end += 1
        yield css[cursor:start].strip(), css[start + 1:end - 1]
        cursor = end


def preview_css(css):
    scope = '.elysium-shop-game'
    # Нативный Panel без flow-children располагает потомков в одной области.
    result = [f'''/* Создано из Panorama CSS; ручные изменения будут перезаписаны. */
{scope} {{ position: absolute; left: 0; top: 0; transform-origin: top left; font: 16px Arial, sans-serif; }}
{scope} * {{ box-sizing: border-box; min-width: 0; min-height: 0; flex-shrink: 0; }}
{scope} [data-panel] {{ display: grid; grid-template: minmax(0,1fr) / minmax(0,1fr); position: relative; overflow: hidden; }}
{scope} [data-panel] > * {{ grid-area: 1 / 1; }}
{scope} [data-label] {{ display: block; white-space: nowrap; overflow: hidden; font-family: Arial, sans-serif; line-height: normal; }}
{scope} button {{ display: grid; position: relative; border: 0; padding: 0; margin: 0; background: transparent; color: inherit; font: inherit; cursor: pointer; }}
{scope} button > * {{ grid-area: 1 / 1; }}
{scope} [data-hittest="false"] {{ pointer-events: none; }}
{scope} .WeaponLayer {{ mask: var(--icon) center / contain no-repeat; background-color: white; }}
{scope} .Closing button {{ pointer-events: none; }}
''']
    flows = set()
    for selector, body in rules(css):
        if 'flow-children:' in body:
            for part in selector.split(','):
                flows.update(re.findall(r'\.([\w]+)', part.split()[-1]))

    def declarations(body, selector=''):
        values = []
        visibility = None
        for declaration in body.split(';'):
            if ':' not in declaration:
                continue
            key, value = [item.strip() for item in declaration.split(':', 1)]
            if key == 'flow-children':
                values += ['display: flex', 'flex-direction: ' + ('column' if value == 'down' else 'row')]
                continue
            if key == 'visibility':
                names = re.findall(r'\.([\w]+)', selector.split()[-1]) if selector else []
                value = 'none' if value == 'collapse' else ('flex' if flows.intersection(names) else 'grid')
                visibility = 'display: ' + value
                continue
            if value.startswith('fill-parent-flow'):
                values += ['flex: 1 1 0px', f'{key}: auto']
                continue
            if key == 'horizontal-align':
                values.append('justify-self: ' + {'left': 'start', 'right': 'end'}.get(value, value))
                continue
            if key == 'vertical-align':
                values.append('align-self: ' + {'top': 'start', 'bottom': 'end'}.get(value, value))
                continue
            if key == 'wash-color':
                key = 'background-color'
            if key == 'brightness':
                key, value = 'filter', f'brightness({value})'
            if key == 'blur':
                key, value = 'filter', re.sub(r'gaussian\(([^)]+)\)', r'blur(\1px)', value)
            if key == 'ui-scale':
                key, value = 'zoom', str(float(value.rstrip('%')) / 100)
            if key == 'font-weight' and value == 'medium':
                value = '500'
            if value.startswith('gradient('):
                colors = re.search(r'from\(([^)]+)\),\s*to\(([^)]+)\)', value)
                value = f'linear-gradient(135deg, {colors[1]}, {colors[2]})'
                key = 'background-image'
            if key == 'background-image' and 's2r://' in value:
                # Реальный ресурс выбран на сервере и передан в --icon; URL задаётся безопасно в JS.
                continue
            if key in ('background-size', 'background-position', 'background-repeat') and '.WeaponLayer' in selector:
                key = key.replace('background', 'mask')
            values.append(f'{key}: {value}')
        if visibility is not None:
            values.append(visibility)
        return '; '.join(values)

    for selector, body in rules(css):
        if selector.startswith('@keyframes'):
            name = selector.removeprefix('@keyframes').strip().strip("'")
            result.append('@keyframes ' + name + ' { ' + ' '.join(
                frame + ' { ' + declarations(style) + '; }' for frame, style in rules(body)) + ' }')
            continue
        for part in selector.split(','):
            part = part.strip().replace(' Label', ' [data-label]')
            result.append(f'{scope} {part} {{ {declarations(body, part)}; }}')
    return '\n'.join(result) + '\n'


def build_preview(directory, xml, css, specification):
    tree = ET.fromstring(xml).find('Panel')

    def node(element):
        return dict(tag=element.tag, attributes=dict(element.attrib), children=[node(child) for child in element])

    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    columns = [element for element in tree.iter('Panel') if 'Column' in element.get('class', '').split()]
    rows = sum('Card' in element.get('class', '').split() for element in columns[0].iter('Panel'))
    payload = dict(sourceSha256=hashlib.sha256((xml + css).encode()).hexdigest(),
                   columns=len(columns), rows=rows, template=node(tree), specification=specification)
    (directory / 'shop-hud-template.json').write_text(json.dumps(payload, ensure_ascii=False, separators=(',', ':')) + '\n')
    (directory / 'shop-hud.css').write_text(preview_css(css))
