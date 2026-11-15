import { chromium } from 'playwright';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
const base = process.env.DWELLIO_URL || 'http://localhost:5180';
const screenshots = process.env.DWELLIO_SCREENSHOTS;
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
page.setDefaultTimeout(15000);
const results = [];
const pass = name => { results.push(name); console.log('PASS', name); };
async function open(path) {
  await page.goto(base + path);
  await page.waitForFunction(() => !!window.Blazor);
  // Allow the server circuit to connect before sending the first UI event.
  await page.waitForTimeout(1000);
}
async function visibleText(text) { await page.getByText(text, { exact: true }).waitFor(); }
try {
  await open('/catalog');
  await page.getByLabel('Maximum monthly rent, KZT').fill('-1');
  await visibleText('Enter a monthly rent budget greater than zero.');
  pass('Negative source-catalog budget shows an error');
  await page.getByLabel('Maximum monthly rent, KZT').fill('500000');
  await page.getByLabel('Complex or address').fill('no-such-complex');
  await visibleText('No examples within these filters');
  await page.getByLabel('Complex or address').fill('');
  await page.getByLabel('Along Mangilik El avenue').check();
  await visibleText('3 sourced examples');
  await page.getByLabel('Along Mangilik El avenue').uncheck();
  pass('Source filtering, empty state and avenue filter');
  for (const path of ['/admin', '/owner/edit/1']) {
    await page.goto(base + path);
    await page.waitForURL(/Account\/Login/);
  }
  pass('Anonymous admin and edit URLs redirect to login');
  await open('/catalog?demo=true');
  const demo = page.locator('details.demo-catalog');
  assert.equal(await demo.getAttribute('open'), '');
  await demo.getByLabel('Rent budget, KZT').fill('-1');
  await demo.getByRole('button', { name: 'Apply', exact: true }).click();
  await visibleText('Check the budget and number of rooms.');
  await demo.getByLabel('Rent budget, KZT').fill('220000');
  await demo.getByRole('button', { name: 'Apply', exact: true }).click();
  await page.getByText('Check the budget and number of rooms.', {exact:true}).waitFor({state:'hidden'});
  pass('Demo budget rejects negative input and clears corrected error');
  await demo.getByRole('button', {name:'Compare apartment',exact:true}).first().click();
  await demo.getByRole('button', {name:'Remove from comparison',exact:true}).first().waitFor();
  await demo.getByRole('button', {name:'Compare apartment',exact:true}).first().click();
  await demo.getByRole('button', {name:'Remove from comparison',exact:true}).nth(1).waitFor();
  await page.getByRole('navigation').getByRole('link', {name:'Compare',exact:true}).click();
  await page.locator('.comparison-item').nth(1).waitFor();
  await page.reload();
  await page.locator('.comparison-item').nth(1).waitFor();
  assert.equal(await page.locator('.comparison-item').count(),2);
  pass('Two selected homes survive comparison-page reload');
  await page.getByRole('link',{name:'Adjust priorities and recalculate'}).click();
  await page.getByLabel('Destination').selectOption('unavailable');
  await page.getByRole('button',{name:'Show my matches'}).click();
  await page.waitForURL(/demo=true/);
  await page.locator('details[open] .card-title').first().click();
  await page.getByText('No suitable route or data available.',{exact:true}).first().waitFor();
  pass('Unavailable route provider displays no fabricated trip');
  await page.getByRole('navigation').getByRole('link',{name:'My lifestyle'}).click();
  await page.getByLabel('Destination').selectOption('AITU');
  await page.getByRole('button',{name:'Show my matches'}).click();
  await page.waitForURL(/demo=true/);
  pass('Preferences navigate to expanded lifestyle results');
  if (screenshots) await fs.mkdir(screenshots,{recursive:true});
  for (const [size,width,height] of [['desktop',1440,1000],['mobile',390,844]]) {
    await page.setViewportSize({width,height});
    for (const [name,path] of [['home','/'],['catalog','/catalog'],['apartment','/apartment/1'],['compare','/compare']]) {
      await open(path);
      if(name==='compare') await page.locator('.comparison-item').nth(1).waitFor();
      await page.evaluate(async()=>{await Promise.all([...document.images].filter(i=>i.getBoundingClientRect().top<innerHeight).map(i=>i.decode().catch(()=>{})));});
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`${name} overflow at ${width}`);
      await page.locator('h1').first().click();
      await page.evaluate(()=>document.activeElement.blur());
      if(screenshots) await page.screenshot({fullPage:true,path:`${screenshots}/${name}-${size}.jpg`,quality:85});
    }
    pass(`Four pages fit ${width}x${height} viewport`);
  }
  await open('/catalog');
  for(let i=0;i<20 && await page.evaluate(()=>document.activeElement.textContent)!=='Skip to content';i++) await page.keyboard.press('Shift+Tab');
  assert.equal(await page.evaluate(()=>document.activeElement.textContent),'Skip to content');
  await page.keyboard.press('Enter');
  assert.equal(await page.evaluate(()=>location.hash),'#main-content');
  pass('Keyboard skip link reaches main content');
  console.log(JSON.stringify({passed:results.length,checks:results},null,2));
} finally { await browser.close(); }
