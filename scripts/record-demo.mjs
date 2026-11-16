// Records a ~5 minute captioned demo of Dwellio by driving the real app in a browser.
// Setup: Node.js 22+, `npm ci --prefix scripts`, start the app (dotnet run ... --launch-profile http), then:
//   CHROME_PATH=/usr/bin/chromium node scripts/record-demo.mjs
// Output: demo/raw/*.webm (convert to MP4 with ffmpeg, see README of this script's author).
import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';

const base = process.env.DWELLIO_URL || 'http://localhost:5180';
const outDir = process.env.DEMO_OUT || 'demo/raw';
const shots = path.resolve('docs/screenshots');
await fs.rm(outDir, { recursive: true, force: true });
await fs.mkdir(outDir, { recursive: true });

const W = 1440, H = 900;
const browser = await chromium.launch(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : { channel: 'chrome' });
const context = await browser.newContext({ viewport: { width: W, height: H }, recordVideo: { dir: outDir, size: { width: W, height: H } } });
const page = await context.newPage();
page.setDefaultTimeout(30000);

// Caption bar and visible cursor, re-created on every page load.
await context.addInitScript(() => {
  const setup = () => {
    if (document.getElementById('demo-caption')) return;
    const cap = document.createElement('div');
    cap.id = 'demo-caption';
    cap.style.cssText = 'position:fixed;left:50%;bottom:28px;transform:translateX(-50%);max-width:1100px;padding:14px 26px;border-radius:14px;background:rgba(17,24,39,.9);color:#fff;font:600 24px/1.35 system-ui,sans-serif;text-align:center;z-index:2147483646;pointer-events:none;display:none;box-shadow:0 8px 30px rgba(0,0,0,.35)';
    document.body.appendChild(cap);
    const dot = document.createElement('div');
    dot.style.cssText = 'position:fixed;width:22px;height:22px;margin:-11px 0 0 -11px;border-radius:50%;background:rgba(239,68,68,.55);border:2px solid #fff;z-index:2147483647;pointer-events:none;left:-50px;top:-50px;transition:transform .08s';
    document.body.appendChild(dot);
    try { const p = JSON.parse(sessionStorage.getItem('demoMouse') || 'null'); if (p) { dot.style.left = p.x + 'px'; dot.style.top = p.y + 'px'; } } catch {}
    document.addEventListener('mousemove', e => { dot.style.left = e.clientX + 'px'; dot.style.top = e.clientY + 'px'; sessionStorage.setItem('demoMouse', JSON.stringify({ x: e.clientX, y: e.clientY })); }, true);
    document.addEventListener('mousedown', () => dot.style.transform = 'scale(.6)', true);
    document.addEventListener('mouseup', () => dot.style.transform = 'scale(1)', true);
    const refresh = () => { const t = localStorage.getItem('demoCaption') || ''; cap.textContent = t; cap.style.display = t ? 'block' : 'none'; };
    refresh(); setInterval(refresh, 150);
  };
  if (document.body) setup(); else document.addEventListener('DOMContentLoaded', setup);
});

// Cards (title, code, phones, outro) are served from the app's own origin so the same init script works.
const cards = new Map();
await page.route(base + '/__card/**', async route => {
  const html = cards.get(new URL(route.request().url()).pathname.split('/').pop());
  await route.fulfill({ contentType: 'text/html; charset=utf-8', body: html ?? '' });
});
await page.route(base + '/__shots/**', async route => {
  const file = path.join(shots, new URL(route.request().url()).pathname.split('/').pop());
  await route.fulfill({ contentType: 'image/jpeg', body: await fs.readFile(file) });
});

const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const cardStyle = `<style>*{box-sizing:border-box}body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:linear-gradient(135deg,#312e81,#6d28d9);color:#fff;font-family:system-ui,sans-serif}
.wrap{max-width:1200px;padding:40px;text-align:center}h1{font-size:84px;margin:0 0 16px}h2{font-size:46px;margin:0 0 24px}p,li{font-size:28px;line-height:1.5;opacity:.95}
pre{background:#0f172a;color:#e2e8f0;text-align:left;padding:24px;border-radius:14px;font-size:21px;line-height:1.45;overflow:hidden}ul{text-align:left;display:inline-block}
.cols{display:flex;gap:34px;align-items:center}.cols>*{flex:1}.phones{display:flex;gap:30px;justify-content:center}.phones img{width:300px;height:640px;border-radius:26px;border:6px solid #fff;object-fit:cover;object-position:top}
code{background:rgba(255,255,255,.15);padding:2px 10px;border-radius:8px}</style>`;

async function showCard(name, body) {
  cards.set(name, `<!doctype html><meta charset=utf-8>${cardStyle}<body>${body}</body>`);
  await page.goto(`${base}/__card/${name}`);
  await page.evaluate(() => localStorage.setItem('demoCaption', ''));
}
const caption = text => page.evaluate(t => localStorage.setItem('demoCaption', t), text);
const pause = ms => page.waitForTimeout(ms);

async function open(p) {
  await page.goto(base + p);
  await page.waitForFunction(() => !!window.Blazor);
  await pause(1200);
}
async function moveTo(locator, steps = 30) {
  await locator.scrollIntoViewIfNeeded();
  const b = await locator.boundingBox();
  if (b) await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2, { steps });
}
async function click(locator) { await moveTo(locator); await pause(350); await locator.click(); }
async function scrollBy(px, ms) {
  const steps = Math.max(1, Math.round(ms / 50));
  for (let i = 0; i < steps; i++) { await page.mouse.wheel(0, px / steps); await pause(50); }
}
async function scrollTop() { await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' })); await pause(900); }

let clock = Date.now();
const t0 = clock;
async function scene(name, seconds, fn) {
  const start = Date.now();
  console.log(`[${((start - t0) / 1000).toFixed(0)}s] ${name}`);
  await fn();
  const left = seconds * 1000 - (Date.now() - start);
  if (left > 0) await pause(left); else console.warn(`  scene "${name}" overran by ${(-left / 1000).toFixed(1)}s`);
}

// 1. Title
await scene('Title', 10, async () => {
  await showCard('title', `<div class="wrap"><h1>Dwellio</h1><h2>Live where your day is easy.</h2>
  <p>Choose where to live in Astana around the place you go every day.</p>
  <p style="margin-top:50px">Anuarbek Nurkadyr · Daulet Ulgaisyn<br>IT-2502 · Introduction to Functional Programming<br>C# / .NET 10 · Blazor · EF Core · SQLite · Leaflet</p></div>`);
});

// 2. Home page
await scene('Home page', 25, async () => {
  await open('/');
  await caption('Rent sites show rent and little else. Dwellio starts from where you study or work.');
  await pause(5000);
  await scrollBy(560, 4000);
  await caption('Three steps: pick your daily place, choose what you need nearby, get homes, areas and a seat plan.');
  await pause(4500);
  await scrollBy(700, 4000);
  await caption('Everything is built on free, open data: OpenStreetMap and an open bus GPS dataset. No API keys.');
  await pause(3500);
  await scrollTop();
});

// 3. Planner: homes
await scene('Planner - homes', 50, async () => {
  await click(page.getByRole('link', { name: /Plan my move/ }).first());
  await page.locator('h1#results-title').waitFor();
  await pause(1500);
  await caption('The planner ranks homes by how well daily life works around your destination.');
  await moveTo(page.locator('.home-card').first(), 40);
  await pause(4500);
  await caption('Tap a place type once for “want”, twice for “must-have”. Results update instantly.');
  const before = await page.locator('#results-title').innerText();
  await click(page.getByRole('button', { name: /^Cafés want/i }));
  await page.getByRole('button', { name: /^Cafés must/i }).waitFor();
  await pause(2000);
  const after = await page.locator('#results-title').innerText();
  console.log(`  must-have cafés: "${before}" -> "${after}"`);
  await caption(`Cafés as a must-have: “${before}” becomes “${after}”.`);
  await pause(4500);
  await click(page.getByRole('button', { name: /^Cafés must/i }));   // remove
  await pause(700);
  await click(page.getByRole('button', { name: /^Cafés$/i }));       // back to want
  await page.getByRole('button', { name: /^Cafés want/i }).waitFor();
  await pause(1500);
  await caption('Set a budget and Dwellio drops homes that cost more. Every exclusion has a reason.');
  const budget = page.getByLabel('Max rent, ₸ / month');
  await moveTo(budget);
  await budget.fill('200000'); await budget.blur();
  await pause(2500);
  await click(page.locator('summary', { hasText: /don.t fit/ }).first());
  await pause(4000);
  await budget.fill('350000'); await budget.blur();
  await pause(1000);
  await caption('Add homes to compare with the scale button. Your plan is remembered in the browser.');
  await click(page.locator('.home-card .icon-button').first());
  await pause(800);
  await click(page.locator('.home-card .icon-button').nth(1));
  await pause(2500);
});

// 4. Areas
await scene('Planner - areas', 30, async () => {
  await scrollTop();
  await click(page.getByRole('button', { name: '⭐ Areas' }));
  await page.locator('.area-card').first().waitFor();
  await pause(2000);
  await caption('The Areas tab finds the best neighbourhoods on the map, even where no home is listed.');
  await pause(5000);
  await moveTo(page.locator('.area-card').first(), 40);
  await pause(3500);
  await caption('Each area shows how far the places you want are and how close the nearest bus stop is.');
  await moveTo(page.locator('.area-card').nth(1), 40);
  await pause(4000);
  await moveTo(page.locator('.area-card').nth(2), 40);
  await pause(3000);
  await click(page.getByRole('button', { name: '🏠 Homes' }));
});

// 5. Change destination
await scene('Change destination', 22, async () => {
  await scrollTop();
  await caption('Change the destination and every result is recalculated, here for Khan Shatyr.');
  const box = page.getByRole('combobox', { name: 'Change your work or study place' });
  await moveTo(box);
  await box.click();
  await box.pressSequentially('Khan', { delay: 220 });
  await pause(1500);
  await click(page.getByRole('option', { name: /Khan Shatyr/ }).first());
  await page.getByText('Your plan · Khan Shatyr').waitFor();
  await pause(5000);
  await caption('Back to Astana IT University for the rest of the tour.');
  await box.click();
  await box.pressSequentially('Astana IT', { delay: 200 });
  await pause(1200);
  await click(page.getByRole('option', { name: /Astana IT University/ }).first());
  await page.getByText('Your plan · Astana IT University').waitFor();
  await pause(2000);
});

// 6. Home details
await scene('Home details', 40, async () => {
  await open('/homes/7');
  await page.getByText('Your everyday life here').waitFor();
  await caption('A home page: monthly rent plus utilities, bus fares and gym, so you see the real monthly cost.');
  await pause(6000);
  await scrollBy(450, 3500);
  await caption('“Why this home fits” lists what works and what does not, with a score for price, neighbourhood and commute.');
  await pause(6000);
  await scrollBy(500, 3500);
  await caption('Nearest real places from OpenStreetMap, with walking minutes. Walking time = straight line × 1.3 at 80 m/min.');
  await pause(6000);
  await scrollBy(500, 3500);
  await caption('And the bus plan to your destination with a likely-seat estimate.');
  await pause(3000);
});

// 7. Commute
await scene('Commute and seat planner', 46, async () => {
  await open('/commute');
  await page.getByRole('button', { name: 'Comfort Town' }).waitFor();
  await caption('The commute planner compares tactics: nearest stop, walking back to an emptier stop, leaving earlier.');
  await pause(3500);
  await click(page.getByRole('button', { name: 'Comfort Town' }));
  await page.locator('.option').first().waitFor();
  await pause(3000);
  await caption('Each option shows walking, waiting and riding time and the chance of getting a seat.');
  await pause(4000);
  await click(page.locator('.option details summary').first());
  await page.locator('.option .chart svg').first().waitFor();
  await scrollBy(420, 2500);
  await caption('The seat model comes from real GPS dwell times: longer door-open time means more people boarding.');
  await pause(6000);
  const slider = page.locator('input[type=range]').last();
  await scrollBy(900, 3000);
  await caption('Route load explorer: drag the time of day to see how full buses 10, 12 and 46 get along their route.');
  await moveTo(slider);
  await slider.focus();
  await pause(2000);
  for (let i = 0; i < 12; i++) { await page.keyboard.press('ArrowRight'); await pause(450); }
  await pause(2000);
});

// 8. Compare
await scene('Compare', 20, async () => {
  await scrollTop();
  await click(page.getByRole('link', { name: /Compare/ }).first());
  await page.locator('.compare-grid article').nth(1).waitFor();
  await caption('Compare up to three homes side by side: cost, commute and everyday places.');
  await pause(6000);
  await scrollBy(400, 3000);
  await pause(4000);
});

// 9. Mobile
await scene('Phone layout', 16, async () => {
  await showCard('phones', `<div class="wrap"><h2>Responsive on a phone</h2>
  <div class="phones"><img src="/__shots/home-mobile.jpg"><img src="/__shots/plan-mobile.jpg"><img src="/__shots/commute-mobile.jpg"></div></div>`);
  await caption('The same app adapts to a 390 px phone screen, with a bottom tab bar.');
});

// 10. Under the hood
await scene('Pure functions', 20, async () => {
  const code = `public static double Activity(double dwellSeconds, BusAssumptions a) =>
    Math.Max(0, dwellSeconds - a.DoorSeconds) / a.SecondsPerPassenger;

public static double BoardingShare(int index, int stopCount) =>
    stopCount <= 1 ? 1 : 1 - (double)index / (stopCount - 1);

public sealed record BusAssumptions(
    int Seats = 30, int Capacity = 95, double DoorSeconds = 8,
    double SecondsPerPassenger = 2.5, int HeadwayMinutes = 10);`;
  await showCard('code', `<div class="wrap"><h2>Functional core: pure functions over immutable records</h2><pre>${esc(code)}</pre>
  <p>Dwellio.Core · SeatModel.cs: no database, no web calls, easy to test</p></div>`);
});
await scene('Architecture and tests', 16, async () => {
  await showCard('arch', `<div class="wrap"><h2>Architecture and quality</h2><ul>
  <li><b>Dwellio.Core</b>: geo maths, places, area analysis, seat model, journey planner, home matching</li>
  <li><b>Dwellio.Web</b>: Blazor components, <code>PlannerState</code> container, open-data clients, accounts, SQLite</li>
  <li>Reusable components: LiveMap, PlaceSearch, NeedPicker, HomeCard, LoadChart</li>
  <li><b>98 of 98</b> automated tests pass (xUnit); accessibility score 1.0 on the planner page</li></ul></div>`);
});

// 11. Outro
await scene('Outro', 10, async () => {
  await showCard('outro', `<div class="wrap"><h1>Thank you</h1>
  <p>Run it locally:<br><code>dotnet run --project src/Dwellio.Web --launch-profile http</code></p>
  <p>github.com/Nkdr21/final-project</p></div>`);
});

await context.close();
await browser.close();
console.log(`Done in ${((Date.now() - t0) / 1000).toFixed(0)}s. Video in ${outDir}`);
