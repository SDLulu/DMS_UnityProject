const { chromium } = require("playwright");

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const errors = [];
  page.on("console", (msg) => {
    if (msg.type() === "error") errors.push(msg.text());
  });
  page.on("pageerror", (err) => errors.push(String(err)));
  await page.goto("http://127.0.0.1:5173", { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.__deepDiveVisualReel && window.render_game_to_text, null, { timeout: 10000 });
  await page.evaluate(() => window.__deepDiveVisualReel.pause());

  for (let i = 1; i <= 7; i++) {
    await page.evaluate((index) => window.__deepDiveVisualReel.goto(index), i);
    await page.waitForTimeout(900);
    await page.screenshot({ path: `output/web-game/reel-${i}.png`, fullPage: true });
  }

  const state = await page.evaluate(() => window.render_game_to_text());
  console.log(JSON.stringify({ ok: true, state: JSON.parse(state), errors }, null, 2));
  await browser.close();
})();
