import { chromium } from "playwright-core";

const executablePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const browser = await chromium.launch({ executablePath, headless: true });

const desktop = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
const consoleErrors = [];
desktop.on("console", (message) => {
  if (message.type() === "error") consoleErrors.push(message.text());
});
await desktop.goto("http://127.0.0.1:5173/", { waitUntil: "networkidle" });
await desktop.waitForTimeout(1500);

const desktopMetrics = await desktop.evaluate(() => ({
  scrollWidth: document.documentElement.scrollWidth,
  clientWidth: document.documentElement.clientWidth,
  scrollHeight: document.documentElement.scrollHeight,
  title: document.title,
}));

await desktop.screenshot({ path: "qa-full-desktop.png", fullPage: true });
await desktop.locator(".feature-section").scrollIntoViewIfNeeded();
await desktop.waitForTimeout(600);
await desktop.screenshot({ path: "qa-features-desktop.png" });
await desktop.locator("#stays").scrollIntoViewIfNeeded();
await desktop.waitForTimeout(600);
await desktop.screenshot({ path: "qa-stays-desktop.png" });
await desktop.locator("#how-it-works").scrollIntoViewIfNeeded();
await desktop.waitForTimeout(600);
await desktop.screenshot({ path: "qa-how-desktop.png" });
await desktop.locator("#trust").scrollIntoViewIfNeeded();
await desktop.waitForTimeout(600);
await desktop.screenshot({ path: "qa-trust-desktop.png" });
await desktop.locator(".final-section").scrollIntoViewIfNeeded();
await desktop.waitForTimeout(600);
await desktop.screenshot({ path: "qa-final-desktop.png" });

const mobile = await browser.newPage({
  viewport: { width: 390, height: 844 },
  isMobile: true,
  deviceScaleFactor: 1,
});
await mobile.goto("http://127.0.0.1:5173/", { waitUntil: "networkidle" });
await mobile.waitForTimeout(1500);

const mobileMetrics = await mobile.evaluate(() => ({
  scrollWidth: document.documentElement.scrollWidth,
  clientWidth: document.documentElement.clientWidth,
  scrollHeight: document.documentElement.scrollHeight,
  heroWidth: document.querySelector(".hero")?.getBoundingClientRect().width,
  copyWidth: document.querySelector(".hero-copy")?.getBoundingClientRect().width,
}));

await mobile.screenshot({ path: "qa-full-mobile.png", fullPage: true });
await mobile.locator(".feature-section").scrollIntoViewIfNeeded();
await mobile.waitForTimeout(500);
await mobile.screenshot({ path: "qa-features-mobile.png" });
await mobile.locator("#stays").scrollIntoViewIfNeeded();
await mobile.waitForTimeout(500);
await mobile.screenshot({ path: "qa-stays-mobile.png" });

console.log(JSON.stringify({ desktopMetrics, mobileMetrics, consoleErrors }, null, 2));
await browser.close();
