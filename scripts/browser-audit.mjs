// End-to-end checks of the main user flows in a real browser.
// Setup: Node.js 22+, then `npm ci --prefix scripts`. Start the app, then:
//   node scripts/browser-audit.mjs                      (Google Chrome)
//   CHROME_PATH=/usr/bin/chromium node scripts/browser-audit.mjs
//   DWELLIO_BROWSER=firefox node scripts/browser-audit.mjs   (after: npx playwright install firefox)
//   DWELLIO_URL=http://localhost:5180 DWELLIO_SCREENSHOTS=docs/screenshots node scripts/browser-audit.mjs
import { chromium, firefox, webkit } from 'playwright';
import fs from 'node:fs/promises';
const base = process.env.DWELLIO_URL || 'http://localhost:5180';
const out = process.env.DWELLIO_SCREENSHOTS;
if (out) await fs.mkdir(out, { recursive: true });
// DWELLIO_BROWSER=firefox | webkit | chrome (default). Playwright browsers are installed with `npx playwright install`.
const engine = { firefox, webkit, chrome: chromium }[process.env.DWELLIO_BROWSER || 'chrome'] ?? chromium;
const browser = await engine.launch(process.env.CHROME_PATH
    ? { executablePath: process.env.CHROME_PATH }
    : engine === chromium ? { channel: 'chrome' } : {});
// DWELLIO_MOBILE=1 runs the same flows with a 390 px phone profile.
const page = await browser.newPage(process.env.DWELLIO_MOBILE
    ? { viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true }
    : { viewport: { width: 1440, height: 1000 } });
const shot = async name => { if (out) await page.screenshot({ path: `${out}/${name}.png`, fullPage: true }); };
page.setDefaultTimeout(30000);
const errors = [];
page.on('pageerror', e => errors.push(e.message));
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
const pass = n => console.log('PASS', n);
// Every navigation waits until Blazor has connected, so clicks are never sent before the circuit exists.
async function open(path) {
  await page.goto(base + path);
  await page.waitForFunction(() => !!window.Blazor);
  await page.waitForTimeout(1000);
}

await open('/plan'); await page.locator('h1#results-title').waitFor();
await page.getByRole('button', { name: '⭐ Areas' }).click();
await page.locator('.area-card').first().waitFor();
await page.waitForTimeout(1500);
await shot('areas');
pass(`areas tab: ${await page.locator('.area-card').count()} areas`);

// Need picker: gym want -> must
await page.getByRole('button', { name: '🏠 Homes' }).click();
const before = await page.locator('#results-title').innerText();
await page.getByRole('button', { name: /^Spa & sauna$/i }).click();
await page.getByRole('button', { name: /^Spa & sauna want/i }).click();
await page.getByRole('button', { name: /^Spa & sauna must/i }).waitFor();
await page.waitForTimeout(800);
const after = await page.locator('#results-title').innerText();
pass(`must-have spa changes results: "${before}" -> "${after}"`);
await page.getByRole('button', { name: /^Spa & sauna must/i }).click();
await page.waitForTimeout(800); // let the plan update settle before typing

// Invalid budget shows error and keeps state
await page.getByLabel('Max rent, ₸ / month').fill('0'); await page.getByLabel('Max rent, ₸ / month').blur();
await page.getByText('Enter a monthly rent budget above zero.').waitFor();
pass('invalid budget rejected with message');
await page.getByLabel('Max rent, ₸ / month').fill('350000'); await page.getByLabel('Max rent, ₸ / month').blur();

// Compare
await page.locator('.home-card .icon-button').first().click();
await page.locator('.home-card .icon-button').nth(1).click();
await page.waitForTimeout(300);
await page.reload(); await page.locator('h1#results-title').waitFor();
await open('/compare'); await page.locator('.compare-grid article').nth(1).waitFor();
pass('compare 2 homes survives reload');

// Plan persists across reload
await open('/plan'); await page.locator('h1#results-title').waitFor();
// On phones the plan panel starts collapsed behind "Edit plan"; open it when that button is visible.
const editPlan = page.getByRole('button', { name: /Edit plan/ });
if (await editPlan.isVisible()) await editPlan.click();
await page.getByRole('combobox', { name: 'Change your work or study place' }).fill('Khan');
await page.getByRole('option', { name: /Khan Shatyr/ }).first().click();
await page.getByText('Your plan · Khan Shatyr').waitFor();
await page.reload();
await page.getByText('Your plan · Khan Shatyr').waitFor();
pass('destination change persists in localStorage');

// Commute
await open('/commute'); await page.getByRole('button', { name: 'Comfort Town' }).waitFor();
await page.getByRole('button', { name: 'Comfort Town' }).click();
await page.locator('.option').first().waitFor();
await page.locator('.option details summary').first().click();
await page.locator('.option .chart svg').first().waitFor();
await page.waitForTimeout(1200);
await shot('commute');
pass(`commute options: ${await page.locator('.option').count()}`);

// Home details
await open('/homes/7'); await page.getByText('Your everyday life here').waitFor();
await page.waitForTimeout(1500);
await shot('home7');
pass('home details render');
await open('/homes/9999'); await page.getByText('This home is not available').waitFor();
pass('missing home shows empty state');
await open('/area?lat=abc'); await page.getByText('Pick a spot in Astana').waitFor();
pass('invalid area link handled');

await browser.close();
if (errors.length) { console.error('Browser errors:\n' + errors.join('\n')); process.exit(1); }
console.log('All browser checks passed, no console errors');
