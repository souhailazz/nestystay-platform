import { chromium } from "playwright-core";
import { mkdir, writeFile, rm, stat } from "node:fs/promises";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import path from "node:path";

const execFileAsync = promisify(execFile);
const root = path.resolve(new URL("..", import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));
const framesDir = path.join(root, "artifacts", "full-hd-video-frames");
const listPath = path.join(root, "artifacts", "full-hd-video-frames.txt");
const outputPath = path.join(root, "artifacts", "nestystay-full-hd-milestones-1-3-demo.mp4");
const baseUrl = "http://127.0.0.1:5173";
const apiBase = `${baseUrl}/api`;
const chromePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const adminToken = "dev-admin-token";

async function request(pathname, options = {}) {
  const response = await fetch(`${apiBase}${pathname}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {}),
    },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });
  if (!response.ok) {
    const body = await response.text();
    throw new Error(`${pathname} failed with ${response.status}: ${body}`);
  }
  return response.json();
}

async function seedDemoData() {
  const nonce = Math.random().toString(16).slice(2, 10);
  const email = `video-${nonce}@nestystay.local`;
  const password = "NestyStay1";
  const registered = await request("/auth/register", {
    method: "POST",
    body: { email, password, displayName: "Video Demo Host", phone: "+1 876 555 0119" },
  });
  const login = await request("/auth/login", { method: "POST", body: { email, password } });
  const verified = await request("/auth/2fa/verify", {
    method: "POST",
    body: { challengeId: login.challengeId, code: login.twoFactorCode },
  });
  const session = {
    userId: verified.userId,
    email,
    displayName: registered.displayName,
    accessToken: verified.accessToken,
    expiresAt: verified.expiresAt,
    roles: verified.roles,
  };
  const property = await request("/properties", {
    method: "POST",
    body: {
      hostUserId: session.userId,
      hostName: "Video Demo Host",
      hostEmail: email,
      title: `Video Demo Wellness Villa ${nonce}`,
      location: "Ocho Rios, St. Ann",
      country: "Jamaica",
      nightlyRate: 210,
      currency: "USD",
      badgeLevel: "Wellness",
      guestVerificationEnabled: true,
      insuraGuestEnabled: true,
      cancellationPolicy: "Flexible",
      highlights: ["Emergency 119 displayed", "Wellness visits", "Jamaican seaview style"],
    },
  });
  const officer = await request("/wellness/officers", {
    method: "POST",
    body: {
      badgeNumber: `JCF-VID-${nonce}`.toUpperCase(),
      parish: "St. Ann",
      coverageArea: "Ocho Rios",
      isActiveOffDuty: true,
      isRetired: false,
    },
  });
  const approved = await request(`/wellness/officers/${officer.id}/approve`, {
    method: "POST",
    headers: { Authorization: `Bearer ${adminToken}` },
    body: { reason: "Video demo approval" },
  });
  const scheduledAt = new Date(Date.now() + 24 * 60 * 60 * 1000);
  scheduledAt.setHours(10, 30, 0, 0);
  const visit = await request("/wellness/visits", {
    method: "POST",
    body: {
      hostUserId: session.userId,
      propertyId: property.id,
      visitType: "StandardWellnessCheck",
      scheduledAt: scheduledAt.toISOString(),
      parish: "St. Ann",
      area: "Ocho Rios",
    },
  });
  await request(`/wellness/visits/${visit.id}/assign`, {
    method: "POST",
    headers: { Authorization: `Bearer ${adminToken}` },
    body: { officerId: approved.id },
  });
  return { session, property, officer: approved, visit };
}

async function waitReady(page) {
  await page.waitForLoadState("domcontentloaded");
  await page.waitForTimeout(900);
}

async function capture(page, frames, name, duration = 2.4) {
  const index = String(frames.length + 1).padStart(3, "0");
  const file = path.join(framesDir, `${index}-${name}.png`);
  await page.screenshot({ path: file, fullPage: false });
  frames.push({ file, duration });
}

async function gotoCapture(page, frames, route, name, duration = 2.4) {
  await page.goto(`${baseUrl}${route}`);
  await waitReady(page);
  await capture(page, frames, name, duration);
}

async function run() {
  await rm(framesDir, { recursive: true, force: true });
  await mkdir(framesDir, { recursive: true });
  const demo = await seedDemoData();
  const frames = [];
  const browser = await chromium.launch({
    executablePath: chromePath,
    headless: true,
    args: ["--window-size=1920,1080", "--force-device-scale-factor=1"],
  });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
  });
  await context.addInitScript((session) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(session));
  }, demo.session);
  const page = await context.newPage();

  await gotoCapture(page, frames, "/", "home-hero", 2.8);
  await page.evaluate(() => window.scrollTo({ top: 840, behavior: "instant" }));
  await page.waitForTimeout(800);
  await capture(page, frames, "home-story", 2.2);
  await gotoCapture(page, frames, "/explore", "explore-listings", 2.4);
  await gotoCapture(page, frames, `/properties/${demo.property.id}`, "property-details", 2.6);
  await gotoCapture(page, frames, "/guest-dashboard", "guest-dashboard", 2.2);
  await gotoCapture(page, frames, "/host-dashboard", "host-dashboard", 2.2);
  await gotoCapture(page, frames, "/host/properties", "host-property-management", 2.4);
  await gotoCapture(page, frames, "/host/wellness", "host-wellness", 2.4);
  const quoteButton = page.getByRole("button", { name: "Quote" });
  if (await quoteButton.count()) {
    await quoteButton.click();
    await page.waitForTimeout(1200);
    await capture(page, frames, "host-wellness-quote", 2.4);
  }
  await gotoCapture(page, frames, "/officer/wellness", "officer-wellness", 2.4);
  await gotoCapture(page, frames, "/calendar", "calendar", 2.0);
  await gotoCapture(page, frames, "/bookings", "booking-admin", 2.2);
  await gotoCapture(page, frames, "/payment-confirmation", "payment-confirmation", 2.0);
  await gotoCapture(page, frames, "/admin", "admin-overview", 2.5);
  await page.evaluate(() => window.scrollTo({ top: 980, behavior: "instant" }));
  await page.waitForTimeout(900);
  await capture(page, frames, "admin-wellness-controls", 3.0);
  await page.evaluate(() => window.scrollTo({ top: document.body.scrollHeight, behavior: "instant" }));
  await page.waitForTimeout(900);
  await capture(page, frames, "admin-foundation", 2.4);

  await browser.close();

  const concatLines = [];
  for (const frame of frames) {
    concatLines.push(`file '${frame.file.replaceAll("\\", "/")}'`);
    concatLines.push(`duration ${frame.duration}`);
  }
  const last = frames.at(-1);
  concatLines.push(`file '${last.file.replaceAll("\\", "/")}'`);
  await writeFile(listPath, `${concatLines.join("\n")}\n`, "utf8");

  await execFileAsync("ffmpeg", [
    "-y",
    "-f",
    "concat",
    "-safe",
    "0",
    "-i",
    listPath,
    "-vf",
    "fps=30,scale=1920:1080,format=yuv420p",
    "-movflags",
    "+faststart",
    outputPath,
  ]);
  const info = await stat(outputPath);
  console.log(JSON.stringify({ outputPath, bytes: info.size, frames: frames.length }, null, 2));
}

run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
