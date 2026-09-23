"""Статический браузерный макет из XML/CSS Panorama; не заменяет Workshop Tools."""
from pathlib import Path
import re, json, html, base64, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/map-rotation/previews'
OUT.mkdir(parents=True,exist_ok=True)
RESOURCE=ROOT/'CustomHud.Core/resources/hud/menus/content/panorama'
CSS=(RESOURCE/'styles/custom_game/elysium_menu_v1.css').read_text()
XML=ET.parse(RESOURCE/'layout/custom_game/elysium_menu_v1.xml').getroot().find('Panel')

def convert_css(css):
    css=re.sub(r'background-color:\s*gradient\(linear, ([^,]+), ([^,]+), from\(([^)]+)\), to\(([^)]+)\)\)',lambda m:'background: linear-gradient('+('90deg' if m[2].strip()=='100% 0%' else '135deg')+','+m[3]+','+m[4]+')',css)
    css=re.sub(r'box-shadow:\s*(#[a-fA-F0-9]+)\s+([^;]+)',r'box-shadow: \2 \1',css)
    css=re.sub(r'flow-children:\s*(down|right)',lambda m:'--flow: '+m[1]+';--panel-display:flex;display:flex;flex-direction:'+('column' if m[1]=='down' else 'row'),css)
    css=re.sub(r'width:\s*fill-parent-flow\(1\)',r'width:0;flex:1 1 0%;min-width:0',css)
    css=css.replace('visibility: collapse','display:none!important').replace('visibility: visible','display:var(--panel-display,block)!important')
    css=re.sub(r'wash-color:\s*([^;]+)',r'--wash: \1',css)
    css=re.sub(r'horizontal-align:\s*(\w+)',r'--halign: \1',css)
    css=re.sub(r'vertical-align:\s*(\w+)',r'--valign: \1',css)
    css=css.replace('text-overflow: ellipsis','text-overflow:ellipsis;overflow:hidden;white-space:nowrap')
    css=re.sub(r'(?<![.\w-])(Label|Panel|Button|Image)\b',lambda m:{'Label':'span','Panel':'div','Button':'button','Image':'img'}[m[1]],css)
    return css

def node(el,texts,classes):
    tag={'Panel':'div','Label':'span','Button':'button','Image':'div'}[el.tag]
    eid=el.get('id','')
    cl=' '.join([el.get('class','')]+classes.get(eid,[]))
    attrs=f' class="{html.escape(cl)}" data-panel="{el.tag}"'+(f' id="{eid}"' if eid else '')
    if el.tag=='Image':
        source=ROOT/'CustomHud.Core/resources/hud/messages/content'/el.get('src').replace('s2r://','').replace('.vsvg','.svg')
        data=base64.b64encode(source.read_bytes()).decode()
        attrs+=f''' style="mask:url('data:image/svg+xml;base64,{data}') center / contain no-repeat;background-color:var(--wash,#fff)"'''
    text=el.get('text','')
    if text=='{s:value}':text=texts.get(eid,'')
    return f'<{tag}{attrs}>'+html.escape(text).replace('\n','<br>')+''.join(node(child,texts,classes) for child in el)+f'</{tag}>'

base='''*{box-sizing:border-box}html,body{margin:0;width:100%;height:100%;font-family:Arial,sans-serif}body{color:#eef2ff;background:radial-gradient(ellipse at 24% 14%,#253749 0,transparent 52%),radial-gradient(ellipse at 80% 80%,#2a253f 0,transparent 48%),#09121c}button{appearance:none;font:inherit;color:inherit;text-align:left;cursor:pointer}button,div,span{position:relative;flex-shrink:0}span{display:block}button{margin:0;padding:0;border:0;background:none}span{--flow:none;--halign:left;--valign:top;--panel-display:block}div,button{--flow:none;--halign:left;--valign:top;--panel-display:block}.page-meta{position:absolute;left:60px;top:40px;z-index:20}.page-meta b{display:block;letter-spacing:4px;font-size:14px;color:#b699f3;margin-bottom:9px}.page-meta span{font-size:16px;color:#90a0b8}.page-note{position:absolute;bottom:32px;left:60px;color:#72829b;font-size:13px;z-index:20}.frame-edge{position:absolute;inset:18px;border:1px solid #ffffff08;pointer-events:none}'''
js='''function layout(){for(const e of document.querySelectorAll('[data-panel]')){const s=getComputedStyle(e), p=e.parentElement, ps=getComputedStyle(p);const flow=ps.getPropertyValue('--flow').trim();const h=s.getPropertyValue('--halign').trim(),v=s.getPropertyValue('--valign').trim();if(s.display==='none')continue;if(flow==='right'){if(v==='center')e.style.alignSelf='center';if(h==='right')e.style.marginLeft='auto'}else if(flow==='down'){if(h==='center')e.style.alignSelf='center';if(h==='right')e.style.alignSelf='flex-end'}else if(h==='center'||v==='center'||h==='right'||v==='bottom'){e.style.position='absolute';let x='0',y='0';if(h==='center'){e.style.left='50%';x='-50%'}else if(h==='right')e.style.right='0';if(v==='center'){e.style.top='50%';y='-50%'}else if(v==='bottom')e.style.bottom='0';e.style.transform='translate('+x+','+y+')'}}}layout();'''
fragments=[]
names=['Gorodok','City','Laboratory','Castle','Dust World','Outpost']
for mode,label in [('nomination','Номинация карты'),('vote','Голосование'),('result','Результат голосования')]:
    texts={'Title':{'nomination':'Номинация карты','vote':'Выберите следующую карту','result':'Голосование завершено'}[mode],'Subtitle':{'nomination':'Выберите карту для следующего голосования','vote':'Голосование за следующую карту','result':'Следующая карта'}[mode],'Status':'00:14','Page':'1 / 2','CloseText':'Закрыть','Footer':'Текущий раунд — последний\nКарта сменится после завершения раунда'}
    classes={'MenuRoot':['Visible','Modal']+(['Result'] if mode=='result' else []),'Back':['Hidden'],'Item5':['Hidden']}
    if mode!='vote':classes['Status']=['Hidden']
    if mode!='result':classes['Footer']=['Hidden']
    else:classes['Pagination']=['Hidden']
    classes['PreviousPage']=['Disabled']
    for i,name in enumerate(names):
        texts['Name'+str(i)]=name
        texts['Description'+str(i)]='Ваш голос' if mode=='vote' and i==2 else ('Голосов: 12' if mode=='result' else '')
        texts['Badge'+str(i)]='Голосов: '+str([7,4,2,1,0,0][i]) if mode=='vote' else ''
        if mode=='result' and i>0:classes['Item'+str(i)]=['Hidden']
        if not texts['Description'+str(i)]:classes['Description'+str(i)]=['Hidden']
    if mode=='vote':classes['Item2']=['Selected']
    if mode=='nomination':classes['Item0']=['Selected']
    doc='<!doctype html><meta charset="utf-8"><style>'+base+convert_css(CSS)+'</style>'
    doc+=f'<div class="page-meta"><b>ELYSIUM / MAP ROTATION</b><span>{label}</span></div>'
    fragment=node(XML,texts,classes)
    fragments.append((label,fragment))
    doc+=fragment
    doc+='<div class="page-note">Макет по ресурсам HUD · 1920 × 1080 · без обязательных изображений карт</div><div class="frame-edge"></div><script>'+js+'</script>'
    (OUT/(mode+'.html')).write_text(doc)
print(OUT)

# Снимки классов/текста получены из HudBannerDesign.Parse/Classes с теми же шаблонами, что у RotationCoordinator.Card.
fixtures=json.loads((OUT/'cards-fixtures.json').read_text())
message_resource=ROOT/'CustomHud.Core/resources/hud/messages/content/panorama'
message_css=(message_resource/'styles/custom_game/elysium_messages_v4_r3.css').read_text()
message_xml=ET.parse(message_resource/'layout/custom_game/elysium_messages_v4_r3.xml').getroot().find('Panel')
board_css='.cards-board{position:absolute;inset:200px 300px;display:grid;grid-template-columns:1fr 1fr;gap:60px}.card-frame{position:relative;border:1px solid #ffffff0a;border-radius:8px}.card-frame h3{position:absolute;left:30px;top:20px;font-size:15px;font-weight:normal;color:#99abc6;margin:0}.card-frame .MessageRegion{min-width:0;width:100%}.card-frame .Message{animation:none!important}'
doc='<!doctype html><meta charset="utf-8"><style>'+base+convert_css(message_css)+board_css+'</style><div class="page-meta"><b>ELYSIUM / MAP ROTATION</b><span>Компактные HUD-карточки</span></div><div class="cards-board">'
for card in fixtures:
    classes={'MessageRegion':['PositionCenter'],'Message0':['Shown']+card['Classes']}
    texts={}
    for suffix,value in [('Header',card['Header']),('Title',card['Title'])]+[('Line'+str(i),line) for i,line in enumerate(card['Lines'])]:
        if value:
            classes['Message0'+suffix]=['Shown']
            classes['Message0'+suffix+'Run0']=['Shown']
            texts['Message0'+suffix+'Run0']=value
    doc+='<div class="card-frame"><h3>'+html.escape('!rtv · задержка' if card['Id']=='rtv-delay' else '!'+card['Id'])+'</h3>'+node(message_xml,texts,classes)+'</div>'
doc+='</div><div class="page-note">Карточки существующего Custom HUD · строки и классы из серверного рендера</div><script>'+js+'</script>'
(OUT/'cards.html').write_text(doc)

overview_css='.screens{position:absolute;top:130px;left:65px;display:flex;gap:55px}.screen{position:relative;width:560px;height:830px}.screen-caption{position:absolute;top:10px;left:0;color:#9cacca;font-size:17px}.screens .MenuRoot{background:transparent}.screens .MenuWindow{--valign:top;margin-top:65px}.screen:nth-child(3) .MenuWindow{margin-top:65px}'
overview='<!doctype html><meta charset="utf-8"><style>'+base+convert_css(CSS)+overview_css+'</style><div class="page-meta"><b>ELYSIUM / MAP ROTATION</b><span>HUD номинаций, голосования и результата</span></div><div class="screens">'
for label,fragment in fragments:
    overview+='<div class="screen"><div class="screen-caption">'+label+'</div>'+fragment+'</div>'
overview+='</div><div class="page-note">Макеты по XML/CSS · изображения карт необязательны · финальная проверка в Workshop Tools</div><script>'+js+'</script>'
(OUT/'overview.html').write_text(overview)
