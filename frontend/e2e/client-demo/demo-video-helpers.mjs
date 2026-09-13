import { chromium, request as playwrightRequest } from "@playwright/test";
import { createHash, createHmac } from "node:crypto";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { copyFile, mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const execFileAsync = promisify(execFile);

const HERE = path.dirname(fileURLToPath(import.meta.url));
export const REPO_ROOT = path.resolve(HERE, "../../..");
export const VIDEO_OUTPUT_DIR = path.join(REPO_ROOT, "testing-evidence", "client-demo");
export const RAW_VIDEO_DIR = path.join(VIDEO_OUTPUT_DIR, "_recordings");
export const FRONTEND_URL = process.env.CLIENT_DEMO_FRONTEND_URL ?? "http://127.0.0.1:5173";
export const API_URL = process.env.CLIENT_DEMO_API_URL ?? "http://127.0.0.1:5019";
export const GUEST_EMAIL = process.env.CLIENT_DEMO_GUEST_EMAIL ?? "demo.video.guest@nestystay.local";
export const GUEST_PASSWORD = process.env.CLIENT_DEMO_GUEST_PASSWORD ?? "NestyStay1";
export const ADMIN_EMAIL = process.env.CLIENT_DEMO_ADMIN_EMAIL ?? "client-demo-admin@nestystay.local";
export const ADMIN_PASSWORD = process.env.CLIENT_DEMO_ADMIN_PASSWORD ?? "NestyDemoAdmin9!";
export const HOST_PASSWORD = process.env.CLIENT_DEMO_HOST_PASSWORD ?? "NestyStay1";

const cursorPositions = new WeakMap();

export async function apiContext() {
  return playwrightRequest.newContext({
    baseURL: API_URL,
    extraHTTPHeaders: { Accept: "application/json", "Content-Type": "application/json" },
  });
}

export async function apiJson(context, method, endpoint, body, token) {
  const options = { method, headers: {} };
  if (body !== undefined) options.data = body;
  if (token) options.headers.Authorization = `Bearer ${token}`;
  const response = await context.fetch(endpoint, options);
  const text = await response.text();
  let payload = null;
  if (text) {
    try { payload = JSON.parse(text); } catch { payload = text; }
  }
  if (!response.ok()) {
    const detail = typeof payload === "string" ? payload : JSON.stringify(payload);
    throw new Error(`${method} ${endpoint} failed (${response.status()}): ${detail.slice(0, 900)}`);
  }
  return payload;
}

export async function registerOrLogin(context, details) {
  let registered = null;
  try {
    registered = await apiJson(context, "POST", "/api/auth/register", {
      email: details.email,
      password: details.password,
      displayName: details.displayName,
      phone: details.phone ?? "+18765550123",
      confirmPassword: details.password,
      acceptedTerms: true,
      acceptedPrivacy: true,
      role: details.role ?? "Guest",
    });
  } catch (error) {
    if (!String(error?.message ?? error).includes("already registered")) throw error;
  }

  const login = await apiJson(context, "POST", "/api/auth/login", {
    email: details.email,
    password: details.password,
    deviceName: "NestyStay client demo preparation",
    rememberDevice: false,
  });
  return {
    userId: registered?.userId ?? login.userId,
    email: details.email,
    displayName: details.displayName,
    accessToken: login.accessToken ?? null,
    requiresTwoFactor: Boolean(login.requiresTwoFactor),
  };
}

const BASE32_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function decodeBase32(value) {
  const normalized = value.replace(/=+$/g, "").replace(/\s+/g, "").toUpperCase();
  let buffer = 0;
  let bits = 0;
  const output = [];
  for (const character of normalized) {
    const index = BASE32_ALPHABET.indexOf(character);
    if (index < 0) throw new Error(`Invalid TOTP secret character: ${character}`);
    buffer = (buffer << 5) | index;
    bits += 5;
    if (bits >= 8) {
      bits -= 8;
      output.push((buffer >>> bits) & 0xff);
    }
  }
  return Buffer.from(output);
}

export function totpCode(secret, counter = Math.floor(Date.now() / 1000 / 30)) {
  const key = decodeBase32(secret);
  const counterBuffer = Buffer.alloc(8);
  counterBuffer.writeBigUInt64BE(BigInt(counter));
  const digest = createHmac("sha1", key).update(counterBuffer).digest();
  const offset = digest[digest.length - 1] & 0x0f;
  const binary = ((digest[offset] & 0x7f) << 24) |
    ((digest[offset + 1] & 0xff) << 16) |
    ((digest[offset + 2] & 0xff) << 8) |
    (digest[offset + 3] & 0xff);
  return String(binary % 1_000_000).padStart(6, "0");
}

async function ensureGuestTwoFactor(context, guest) {
  if (guest.requiresTwoFactor) return guest;
  if (!guest.accessToken) throw new Error("Guest preparation did not return an access token.");
  const enrollment = await apiJson(context, "POST", "/api/auth/2fa/enrollments", undefined, guest.accessToken);
  const counter = Math.floor(Date.now() / 1000 / 30);
  // The backend accepts the previous 30-second window first. This prevents
  // the enrollment confirmation from consuming the same code used at login.
  const code = totpCode(enrollment.manualKey, counter - 1);
  await apiJson(context, "POST", "/api/auth/2fa/enrollments/confirm", {
    enrollmentId: enrollment.enrollmentId,
    code,
  }, guest.accessToken);
  return { ...guest, requiresTwoFactor: true };
}

async function ensureBadge(context, adminToken, subjectId, level, requirements) {
  const assignments = await apiJson(context, "GET", "/api/badges-pricing/badges/assignments", undefined, adminToken);
  const existing = assignments.find((item) => item.subjectId === subjectId && item.level === level && item.status.toLowerCase() === "active");
  if (existing) return existing;
  return apiJson(context, "POST", "/api/badges-pricing/badges/purchase", {
    subjectType: "Host",
    subjectId,
    level,
    hostVerificationPassed: requirements.hostVerificationPassed ?? false,
    completedApprovedBookings: requirements.completedApprovedBookings ?? 0,
    hasPropertyAddress: requirements.hasPropertyAddress ?? false,
    hasWellnessSubscription: requirements.hasWellnessSubscription ?? false,
    paymentSucceeded: true,
  }, adminToken);
}

const HOST_FIXTURES = [
  {
    key: "free",
    email: "demo.free.host@nestystay.local",
    displayName: "Maya Free",
    slug: "demo-free-host",
    parish: "St. James",
    bio: "A welcoming local host with a simple, comfortable base for exploring Jamaica.",
    badge: "Free",
    title: "Montego Bay Starter Home",
    location: "Montego Bay, St. James",
    rate: 110,
    guestVerificationEnabled: false,
    highlights: ["Free host access", "Calendar and messaging", "QR gate access"],
  },
  {
    key: "verified",
    email: "demo.verified.host@nestystay.local",
    displayName: "Naomi Verified",
    slug: "demo-verified-host",
    parish: "St. Ann",
    bio: "Identity-reviewed host offering a calm retreat close to Ocho Rios adventures.",
    badge: "Verified",
    title: "Ocho Rios Verified Retreat",
    location: "Ocho Rios, St. Ann",
    rate: 165,
    guestVerificationEnabled: true,
    highlights: ["Verified host", "Guest verification", "Local business directory"],
  },
  {
    key: "trusted",
    email: "demo.trusted.host@nestystay.local",
    displayName: "Andre Trusted",
    slug: "demo-trusted-host",
    parish: "Kingston",
    bio: "An established host with a proven booking history and trusted local partners.",
    badge: "Trusted",
    title: "Kingston Trusted Townhouse",
    location: "Kingston, Jamaica",
    rate: 190,
    guestVerificationEnabled: true,
    highlights: ["Trusted host", "Trades directory", "Search boost"],
  },
  {
    key: "wellness",
    email: "demo.wellness.host@nestystay.local",
    displayName: "Priya Wellness",
    slug: "demo-wellness-host",
    parish: "Westmoreland",
    bio: "A fully qualified host focused on calm stays, safety, and wellness-linked access.",
    badge: "Wellness",
    title: "Negril Wellness Villa",
    location: "Negril, Westmoreland",
    rate: 240,
    guestVerificationEnabled: true,
    highlights: ["Wellness host", "Police directory", "Wellness visits"],
  },
];

export async function prepareDemoData(context) {
  await apiJson(context, "POST", "/api/spec/seed");
  const adminLogin = await apiJson(context, "POST", "/api/auth/login", {
    email: ADMIN_EMAIL,
    password: ADMIN_PASSWORD,
    deviceName: "NestyStay client demo administrator",
  });
  if (!adminLogin.accessToken) throw new Error("Demo administrator could not be authenticated.");
  const admin = { userId: adminLogin.userId, accessToken: adminLogin.accessToken };

  let guest = await registerOrLogin(context, {
    email: GUEST_EMAIL,
    password: GUEST_PASSWORD,
    displayName: "Jordan Demo",
    role: "Guest",
  });
  guest = await ensureGuestTwoFactor(context, guest);

  const hosts = {};
  for (const fixture of HOST_FIXTURES) {
    const account = await registerOrLogin(context, {
      email: fixture.email,
      password: HOST_PASSWORD,
      displayName: fixture.displayName,
      role: "Host",
    });
    const requirements = { hostVerificationPassed: true, completedApprovedBookings: 3, hasPropertyAddress: true, hasWellnessSubscription: true };
    if (fixture.badge !== "Free") await ensureBadge(context, admin.accessToken, account.userId, "Verified", requirements);
    if (["Trusted", "Wellness"].includes(fixture.badge)) await ensureBadge(context, admin.accessToken, account.userId, "Trusted", requirements);
    if (fixture.badge === "Wellness") await ensureBadge(context, admin.accessToken, account.userId, "Wellness", requirements);

    let properties = await apiJson(context, "GET", "/api/properties");
    let property = properties.find((item) => item.title === fixture.title);
    if (!property) {
      property = await apiJson(context, "POST", "/api/properties", {
        hostUserId: account.userId,
        hostName: fixture.displayName,
        hostEmail: fixture.email,
        title: fixture.title,
        location: fixture.location,
        country: "Jamaica",
        nightlyRate: fixture.rate,
        currency: "USD",
        badgeLevel: fixture.badge,
        guestVerificationEnabled: fixture.guestVerificationEnabled,
        insuraGuestEnabled: fixture.badge !== "Free",
        cancellationPolicy: "Moderate",
        highlights: fixture.highlights,
      }, account.accessToken);
    }
    await apiJson(context, "PUT", `/api/spec/host-profiles/${fixture.slug}`, {
      hostUserId: account.userId,
      displayName: fixture.displayName,
      parish: fixture.parish,
      bio: fixture.bio,
      responseTime: "Replies in 10 minutes",
      badges: [fixture.badge],
      listingIds: [property.id],
      isPublic: true,
      highlights: fixture.highlights,
    }, account.accessToken);
    hosts[fixture.key] = { ...fixture, ...account, propertyId: property.id };
  }

  return { admin, guest, hosts, verifiedPropertyId: (await apiJson(context, "GET", "/api/properties")).find((item) => item.title === "Ocho Rios Verified Villa")?.id };
}

export async function installDemoCursor(page) {
  await page.addInitScript(() => {
    const install = () => {
      if (document.getElementById("nesty-demo-cursor")) return;
      const cursor = document.createElement("div");
      cursor.id = "nesty-demo-cursor";
      cursor.setAttribute("aria-hidden", "true");
      cursor.innerHTML = `<svg viewBox="0 0 24 32" xmlns="http://www.w3.org/2000/svg"><path d="M3 2.2v25.1l6.3-6.1 4 9.1 4-1.9-4.1-9.2H22L3 2.2Z"/></svg>`;
      const style = document.createElement("style");
      style.textContent = `
        #nesty-demo-cursor { position: fixed; left: 72px; top: 72px; width: 28px; height: 36px; z-index: 2147483647; pointer-events: none; transform: translate(-2px,-2px); filter: drop-shadow(0 2px 3px rgba(0,0,0,.45)); transition: transform 90ms ease-out; }
        #nesty-demo-cursor svg { display:block; width:100%; height:100%; overflow:visible; }
        #nesty-demo-cursor path { fill:#fff; stroke:#102f2e; stroke-width:2.2; stroke-linejoin:round; }
        #nesty-demo-cursor.is-pressed { transform: translate(-2px,-2px) scale(.82); }
        #nesty-demo-cursor .demo-click-ring { position:absolute; left:-8px; top:-8px; width:38px; height:38px; border:2px solid rgba(251,193,53,.96); border-radius:50%; animation: demo-click-ring 520ms ease-out forwards; }
        @keyframes demo-click-ring { from { opacity:.95; transform:scale(.35); } to { opacity:0; transform:scale(1.45); } }
      `;
      document.documentElement.append(style, cursor);
      window.__NESTYSTAY_DEMO_CURSOR__ = cursor;
      window.addEventListener("mousemove", (event) => { cursor.style.left = `${event.clientX}px`; cursor.style.top = `${event.clientY}px`; }, true);
      window.addEventListener("mousedown", () => { cursor.classList.add("is-pressed"); const ring = document.createElement("span"); ring.className = "demo-click-ring"; cursor.append(ring); window.setTimeout(() => ring.remove(), 560); }, true);
      window.addEventListener("mouseup", () => cursor.classList.remove("is-pressed"), true);
    };
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", install, { once: true }); else install();
  });
}

export function easeInOutCubic(value) {
  return value < 0.5 ? 4 * value * value * value : 1 - Math.pow(-2 * value + 2, 3) / 2;
}

export async function demoPause(page, milliseconds = 900) {
  await page.waitForTimeout(milliseconds);
}

export async function smoothMove(page, target, options = {}) {
  const box = typeof target === "object" && "boundingBox" in target ? await target.boundingBox() : target;
  if (!box) throw new Error("Cannot move cursor to a hidden or missing target.");
  const x = box.x + box.width * (options.xRatio ?? 0.5);
  const y = box.y + box.height * (options.yRatio ?? 0.5);
  const start = cursorPositions.get(page) ?? { x: 72, y: 72 };
  const steps = options.steps ?? 30;
  const pause = options.pause ?? 18;
  for (let index = 1; index <= steps; index += 1) {
    const progress = easeInOutCubic(index / steps);
    await page.mouse.move(start.x + (x - start.x) * progress, start.y + (y - start.y) * progress);
    await page.waitForTimeout(pause);
  }
  cursorPositions.set(page, { x, y });
}

export async function humanClick(page, locator, options = {}) {
  await locator.waitFor({ state: "visible", timeout: options.timeout ?? 15000 });
  await locator.scrollIntoViewIfNeeded();
  await smoothMove(page, locator, { steps: options.steps ?? 30, pause: options.movePause ?? 18 });
  await demoPause(page, options.hoverPause ?? 420);
  await page.mouse.down();
  await demoPause(page, 95);
  await page.mouse.up();
  await demoPause(page, options.afterPause ?? 850);
}

export async function humanType(page, locator, value, options = {}) {
  await humanClick(page, locator, { afterPause: 180, hoverPause: 300 });
  await page.keyboard.press("Control+A");
  await page.keyboard.press("Backspace");
  await page.keyboard.type(value, { delay: options.delay ?? 62 });
  await demoPause(page, options.afterPause ?? 260);
}

export async function humanSelect(page, locator, value) {
  await locator.waitFor({ state: "visible" });
  await locator.scrollIntoViewIfNeeded();
  await smoothMove(page, locator);
  await demoPause(page, 350);
  await locator.selectOption(value);
  await demoPause(page, 550);
}

export async function smoothScroll(page, distance, options = {}) {
  const direction = Math.sign(distance) || 1;
  let remaining = Math.abs(distance);
  while (remaining > 0) {
    const chunk = Math.min(options.chunk ?? 115, remaining);
    await page.mouse.wheel(0, direction * chunk);
    remaining -= chunk;
    await demoPause(page, options.pause ?? 55);
  }
  await demoPause(page, options.afterPause ?? 500);
}

export async function waitForApp(page, locatorOrText, options = {}) {
  const locator = typeof locatorOrText === "string" ? page.getByText(locatorOrText, { exact: false }).first() : locatorOrText;
  await locator.waitFor({ state: "visible", timeout: options.timeout ?? 20000 });
  await page.waitForLoadState("domcontentloaded").catch(() => undefined);
  await demoPause(page, options.pause ?? 950);
  return locator;
}

export async function loginViaUi(page, context, email, password, { expectTwoFactor = false } = {}) {
  await page.goto(`${FRONTEND_URL}/login`);
  await waitForApp(page, page.getByRole("heading", { name: "Log in", exact: true }), { pause: 800 });
  await humanType(page, page.locator('input[type="email"]').first(), email, { delay: 62 });
  await humanType(page, page.locator('input[type="password"]').first(), password, { delay: 68 });
  const responsePromise = page.waitForResponse((response) => response.url().includes("/api/auth/login") && response.request().method() === "POST");
  await humanClick(page, page.locator('button[type="submit"]').filter({ hasText: "Log in" }).first(), { hoverPause: 450, afterPause: 1050 });
  const loginResponse = await responsePromise;
  const loginResult = await loginResponse.json();
  if (loginResult.requiresTwoFactor) {
    if (!loginResult.challengeId) throw new Error("The UI login returned a 2FA response without a challenge id.");
    await waitForApp(page, page.getByRole("heading", { name: "Two-factor check", exact: true }), { pause: 650 });
    const developmentChallenge = await apiJson(context, "GET", `/api/auth/development/challenges/${loginResult.challengeId}`);
    await humanType(page, page.getByLabel("Digit 1", { exact: true }), developmentChallenge.code, { delay: 72, afterPause: 360 });
    await humanClick(page, page.getByRole("button", { name: "Verify code", exact: true }), { hoverPause: 480, afterPause: 1250 });
  } else if (expectTwoFactor) {
    throw new Error("The UI login did not present the expected 2FA challenge.");
  }
  await page.waitForFunction(() => Boolean(window.localStorage.getItem("nestyStay.session")), undefined, { timeout: 15000 });
  await demoPause(page, 1200);
  return loginResult;
}

export async function openRecordedBrowser() {
  await mkdir(RAW_VIDEO_DIR, { recursive: true });
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext({
    bypassCSP: true,
    viewport: { width: 1920, height: 1080 },
    screen: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
    colorScheme: "light",
    locale: "en-US",
    timezoneId: "America/Jamaica",
    recordVideo: { dir: RAW_VIDEO_DIR, size: { width: 1920, height: 1080 } },
  });
  const page = await context.newPage();
  await installDemoCursor(page);
  const consoleErrors = [];
  const pageErrors = [];
  page.on("console", (message) => {
    if (message.type() === "error" && !/Failed to load resource:.*\b401\b/i.test(message.text())) consoleErrors.push(message.text());
  });
  page.on("pageerror", (error) => pageErrors.push(error.message));
  return { browser, context, page, consoleErrors, pageErrors };
}

export async function closeRecordedBrowser(recording) {
  const video = recording.page.video();
  await recording.context.close();
  const videoPath = video ? await video.path() : null;
  await recording.browser.close();
  return { videoPath, consoleErrors: recording.consoleErrors, pageErrors: recording.pageErrors };
}

export async function finalizeVideo(videoPath, outputStem) {
  if (!videoPath) throw new Error(`No Playwright video was produced for ${outputStem}.`);
  await mkdir(VIDEO_OUTPUT_DIR, { recursive: true });
  const webmPath = path.join(VIDEO_OUTPUT_DIR, `${outputStem}.webm`);
  const mp4Path = path.join(VIDEO_OUTPUT_DIR, `${outputStem}.mp4`);
  await copyFile(videoPath, webmPath);
  try {
    await execFileAsync("ffmpeg", ["-y", "-i", webmPath, "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-an", mp4Path], { windowsHide: true });
  } catch (error) {
    throw new Error(`ffmpeg H.264 conversion failed. The WebM was retained at ${webmPath}. ${error.message}`);
  }
  return { webmPath, mp4Path };
}

export async function sessionFromPage(page) {
  return page.evaluate(() => {
    const raw = window.localStorage.getItem("nestyStay.session");
    return raw ? JSON.parse(raw) : null;
  });
}

export function deterministicDateRange(startOffset, nights) {
  const start = new Date();
  start.setHours(12, 0, 0, 0);
  start.setDate(start.getDate() + startOffset);
  const end = new Date(start);
  end.setDate(end.getDate() + nights);
  return { checkIn: start.toISOString().slice(0, 10), checkOut: end.toISOString().slice(0, 10) };
}

export async function createAndResolveBooking(context, guestToken, adminToken, propertyId, range, passed) {
  const booking = await apiJson(context, "POST", "/api/bookings", {
    propertyId,
    guestUserId: "00000000-0000-0000-0000-000000000000",
    checkIn: range.checkIn,
    checkOut: range.checkOut,
    documentType: "GLB03002",
    ekycMetaInfo: passed ? "Client demo approval path" : "Client demo rejection path",
  }, guestToken);
  const reference = booking.ekycTransactionId;
  if (!reference) throw new Error("Booking did not return a deterministic eKYC transaction reference.");
  const resolved = await apiJson(context, "POST", `/api/bookings/${booking.id}/verification-result`, {
    passed,
    providerReference: reference,
  }, adminToken);
  return { created: booking, resolved };
}

export function sha256File(buffer) {
  return createHash("sha256").update(buffer).digest("hex");
}

export async function writeJson(filePath, value) {
  await writeFile(filePath, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

export async function readJson(filePath) {
  return JSON.parse(await readFile(filePath, "utf8"));
}

export { HOST_FIXTURES };
