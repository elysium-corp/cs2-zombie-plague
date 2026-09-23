// Снимки HTML-макетов создаются после адаптации разметки Panorama.
import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
const require = createRequire(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES ? resolve(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES, 'package.json') : import.meta.url);
const { chromium } = require('playwright');
const root = resolve(import.meta.dirname, '../../docs/map-rotation/previews');
const browser = await chromium.launch({headless:true,args:['--no-sandbox'],...(process.env.HUD_PREVIEW_CHROMIUM ? {executablePath:process.env.HUD_PREVIEW_CHROMIUM} : {})});
const page = await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
for (const name of ['nomination','vote','result','cards','overview']) {
  await page.goto(pathToFileURL(resolve(root,name+'.html')).href);
  await page.screenshot({path:resolve(root,name+'.png')});
  console.log(name, await page.locator(name==='cards'?'.cards-board':name==='overview'?'.screens':'.MenuWindow').boundingBox());
}
await browser.close();
