import { chromium } from "playwright-core";
import { mkdir, writeFile, rm, stat } from "node:fs/promises";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import path from "node:path";

const execFileAsync = promisify(execFile);
const root = path.resolve(new URL("..", import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));
const framesDir = path.join(root, "artifacts", "full-hd-60fps-milestones-1-2-frames");
const listPath = path.join(root, "artifacts", "full-hd-60fps-milestones-1-2-frames.txt");
const outputPath = path.join(root, "artifacts", "nestystay-full-hd-60fps-milestones-1-2-demo.mp4");
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

  if (response.status === 204) {
    return undefined;
  }

  return response.json();
}

function dateOnly(daysFromNow) {
  const date = new Date(Date.now() + daysFromNow * 24 * 60 * 60 * 1000);
  return date.toISOString().slice(0, 10);
}

async function tryRequest(pathname, options = {}) {
  try {
    return await request(pathname, options);
  } catch (error) {
    return { error: String(error) };
  }
}

async function seedMilestoneData() {
  const nonce = Math.random().toString(16).slice(2, 10);
  const email = `m12-video-${nonce}@nestystay.local`;
  const password = "NestyStay1";

  const registered = await request("/auth/register", {
    method: "POST",
    body: { email, password, displayName: "Milestone Demo Host", phone: "+1 876 555 0144" },
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
      hostName: "Milestone Demo Host",
      hostEmail: email,
      title: `Milestone Villa ${nonce}`,
      location: "Montego Bay, St. James",
      country: "Jamaica",
      nightlyRate: 245,
      currency: "USD",
      badgeLevel: "Verified",
      guestVerificationEnabled: true,
      insuraGuestEnabled: true,
      cancellationPolicy: "Flexible",
      highlights: ["Ocean view", "eKYC required", "InsuraGuest-ready", "Verified host"],
    },
  });

  const quote = await request("/bookings/quote", {
    method: "POST",
    body: { propertyId: property.id, checkIn: dateOnly(18), checkOut: dateOnly(22) },
  });

  const booking = await request("/bookings", {
    method: "POST",
    body: {
      propertyId: property.id,
      guestUserId: session.userId,
      checkIn: quote.checkIn,
      checkOut: quote.checkOut,
      ekycMetaInfo: "60fps milestone demo",
      documentType: "Passport",
      ekycCallbackUrl: "https://example.test/ekyc",
    },
  });

  await tryRequest(`/bookings/${booking.id}/capture-payment`, { method: "POST" });
  const verifiedBooking = await request(`/bookings/${booking.id}/verification-result`, {
    method: "POST",
    body: { passed: true, providerReference: booking.ekycTransactionId },
  });
  await request(`/bookings/${verifiedBooking.id}/capture-payment`, { method: "POST" });

  await request("/badges-pricing/badges/purchase", {
    method: "POST",
    body: {
      subjectType: "Host",
      subjectId: session.userId,
      level: "Verified",
      hostVerificationPassed: true,
      completedApprovedBookings: 1,
      hasPropertyAddress: true,
      hasWellnessSubscription: false,
      paymentSucceeded: true,
    },
  });

  const trustedAssignment = await request("/badges-pricing/badges/purchase", {
    method: "POST",
    body: {
      subjectType: "Host",
      subjectId: session.userId,
      level: "Trusted",
      hostVerificationPassed: true,
      completedApprovedBookings: 8,
      hasPropertyAddress: true,
      hasWellnessSubscription: false,
      paymentSucceeded: true,
    },
  });

  await tryRequest("/badges-pricing/founding-benefits", {
    method: "POST",
    headers: { Authorization: `Bearer ${adminToken}` },
    body: { propertyId: property.id, tier: "Gold", isEligible: true },
  });

  await tryRequest("/badges-pricing/campaigns", {
    method: "POST",
    headers: { Authorization: `Bearer ${adminToken}` },
    body: {
      key: `video-gold-${nonce}`,
      name: "Milestone Video Gold Host Launch",
      campaignType: "Launch",
      overrideAmount: 79,
      appliesTo: "Trusted",
      opensAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
      closesAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      isActive: true,
    },
  });

  await tryRequest(`/badges-pricing/campaigns/video-gold-${nonce}/enroll`, {
    method: "POST",
    body: { subjectType: "Host", subjectId: session.userId },
  });

  await tryRequest("/badges-pricing/commission-quote", {
    method: "POST",
    body: { bookingValue: 980, nights: 4, tier: "Gold" },
  });

  return { session, property, booking: verifiedBooking, trustedAssignment, nonce };
}

async function waitReady(page) {
  await page.waitForLoadState("domcontentloaded");
  await page.waitForTimeout(1100);
}

async function installOverlay(page, caption, cursor, chapter = "NestyStay local QA demo") {
  await page.evaluate(
    ({ caption: captionText, cursor: cursorPosition, chapter: chapterText }) => {
      let style = document.getElementById("demo-video-overlay-style");
      if (!style) {
        style = document.createElement("style");
        style.id = "demo-video-overlay-style";
        style.textContent = `
          #demo-video-caption {
            position: fixed;
            left: 50%;
            bottom: 34px;
            transform: translateX(-50%);
            z-index: 2147483647;
            width: min(1460px, calc(100vw - 96px));
            border: 1px solid rgba(255,255,255,.32);
            border-radius: 8px;
            background: rgba(9, 15, 18, .88);
            box-shadow: 0 18px 48px rgba(0,0,0,.36);
            color: #fff;
            font: 700 30px/1.22 Arial, Helvetica, sans-serif;
            letter-spacing: 0;
            padding: 20px 28px;
            text-align: center;
            pointer-events: none;
          }
          #demo-video-chapter {
            position: fixed;
            top: 24px;
            left: 24px;
            z-index: 2147483647;
            border-radius: 8px;
            background: rgba(255,255,255,.92);
            color: #101820;
            font: 800 22px/1 Arial, Helvetica, sans-serif;
            letter-spacing: 0;
            padding: 14px 18px;
            box-shadow: 0 10px 30px rgba(0,0,0,.24);
            pointer-events: none;
          }
          #demo-video-cursor {
            position: fixed;
            z-index: 2147483647;
            width: 42px;
            height: 42px;
            pointer-events: none;
            filter: drop-shadow(0 6px 12px rgba(0,0,0,.55));
          }
          #demo-video-cursor::after {
            content: "";
            position: absolute;
            left: 22px;
            top: 20px;
            width: 22px;
            height: 22px;
            border-radius: 999px;
            border: 3px solid rgba(255, 207, 69, .95);
            background: rgba(255, 207, 69, .28);
          }
        `;
        document.head.appendChild(style);
      }

      let captionEl = document.getElementById("demo-video-caption");
      if (!captionEl) {
        captionEl = document.createElement("div");
        captionEl.id = "demo-video-caption";
        document.body.appendChild(captionEl);
      }
      captionEl.textContent = captionText;

      let chapterEl = document.getElementById("demo-video-chapter");
      if (!chapterEl) {
        chapterEl = document.createElement("div");
        chapterEl.id = "demo-video-chapter";
        document.body.appendChild(chapterEl);
      }
      chapterEl.textContent = chapterText;

      let cursorEl = document.getElementById("demo-video-cursor");
      if (!cursorEl) {
        cursorEl = document.createElement("div");
        cursorEl.id = "demo-video-cursor";
        cursorEl.innerHTML = `
          <svg width="42" height="42" viewBox="0 0 42 42" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M5 3L36 21L22 24L16 38L5 3Z" fill="white" stroke="#101820" stroke-width="2.6" stroke-linejoin="round"/>
          </svg>
        `;
        document.body.appendChild(cursorEl);
      }
      cursorEl.style.left = `${cursorPosition.x}px`;
      cursorEl.style.top = `${cursorPosition.y}px`;
    },
    { caption, cursor, chapter },
  );
}

async function capture(page, frames, name, caption, cursor, duration = 2.8, chapter = "NestyStay local QA demo") {
  await installOverlay(page, caption, cursor, chapter);
  await page.waitForTimeout(250);
  const index = String(frames.length + 1).padStart(3, "0");
  const file = path.join(framesDir, `${index}-${name}.png`);
  await page.screenshot({ path: file, fullPage: false, animations: "disabled" });
  frames.push({ file, duration });
}

async function gotoCapture(page, frames, scene) {
  await page.goto(`${baseUrl}${scene.route}`);
  await waitReady(page);
  if (scene.scrollTo !== undefined) {
    await page.evaluate((top) => window.scrollTo({ top, behavior: "instant" }), scene.scrollTo);
    await page.waitForTimeout(700);
  }
  if (scene.scrollBottom) {
    await page.evaluate(() => window.scrollTo({ top: document.body.scrollHeight, behavior: "instant" }));
    await page.waitForTimeout(700);
  }
  await capture(page, frames, scene.name, scene.caption, scene.cursor, scene.duration, scene.chapter);
}

async function run() {
  await rm(framesDir, { recursive: true, force: true });
  await mkdir(framesDir, { recursive: true });

  const health = await request("/health");
  if (!["Healthy", "ok"].includes(health.status)) {
    throw new Error(`Backend health check returned ${health.status}`);
  }

  const demo = await seedMilestoneData();
  const frames = [];
  const browser = await chromium.launch({
    executablePath: chromePath,
    headless: true,
    args: ["--window-size=1920,1080", "--force-device-scale-factor=1"],
  });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
    colorScheme: "light",
  });

  await context.addInitScript((session) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(session));
  }, demo.session);

  const page = await context.newPage();
  await page.addInitScript(() => {
    window.localStorage.setItem("nestyStay.adminToken", "dev-admin-token");
  });

  const scenes = [
    {
      route: "/",
      name: "m1-home",
      chapter: "Milestone 1 - Guest and host booking flow",
      caption: "Milestone 1 starts at the live NestyStay app: guests can browse Jamaican stays and begin a protected booking journey.",
      cursor: { x: 520, y: 430 },
      duration: 3.0,
    },
    {
      route: "/explore",
      name: "m1-explore",
      chapter: "Milestone 1 - Property discovery",
      caption: "Explore is wired to backend property listing APIs, including badges, rates, verification flags, and InsuraGuest indicators.",
      cursor: { x: 1230, y: 365 },
      duration: 3.0,
    },
    {
      route: `/properties/${demo.property.id}`,
      name: "m1-property-detail",
      chapter: "Milestone 1 - Property detail and quote",
      caption: "Property details are live for the newly seeded Montego Bay demo villa, with date-based booking quotes and verification requirements.",
      cursor: { x: 1480, y: 545 },
      duration: 3.2,
    },
    {
      route: "/login",
      name: "m1-auth",
      chapter: "Milestone 1 - Registration, login, and 2FA",
      caption: "Registration, password login, 2FA challenge verification, and Google-style sign-in are implemented in the frontend and backend.",
      cursor: { x: 1120, y: 610 },
      duration: 3.0,
    },
    {
      route: "/guest-dashboard",
      name: "m1-guest-dashboard",
      chapter: "Milestone 1 - Guest dashboard",
      caption: "The guest dashboard reads live booking data: pending holds, eKYC status, payment state, totals, and booking timeline are connected.",
      cursor: { x: 1310, y: 360 },
      duration: 3.2,
    },
    {
      route: "/host-dashboard",
      name: "m1-host-dashboard",
      chapter: "Milestone 1 - Host dashboard",
      caption: "The host dashboard is connected to property and booking APIs, giving hosts a local operating view of their portfolio.",
      cursor: { x: 500, y: 585 },
      duration: 3.0,
    },
    {
      route: "/host/properties",
      name: "m1-host-property-create",
      chapter: "Milestone 1 - Host property creation",
      caption: "Host property creation is wired end to end: new listings persist in PostgreSQL and immediately appear across the app.",
      cursor: { x: 1185, y: 635 },
      duration: 3.2,
    },
    {
      route: "/bookings",
      name: "m1-bookings-admin",
      chapter: "Milestone 1 - Booking operations",
      caption: "Booking operations include quote creation, date overlap blocking, pending holds, eKYC pass/reject handling, and payment capture rules.",
      cursor: { x: 1400, y: 420 },
      duration: 3.4,
    },
    {
      route: "/payment-confirmation",
      name: "m1-payment-confirmation",
      chapter: "Milestone 1 - Payment confirmation",
      caption: "Payment confirmation shows the Stripe-style authorization/capture workflow, including the approval gate before capture.",
      cursor: { x: 900, y: 520 },
      duration: 3.0,
    },
    {
      route: "/calendar",
      name: "m1-calendar",
      chapter: "Milestone 1 - Calendar/read view",
      caption: "Calendar and read views expose booked dates and booking status so hosts can see what is held, pending, approved, or captured.",
      cursor: { x: 1260, y: 525 },
      duration: 3.0,
    },
    {
      route: "/admin",
      name: "m2-admin-overview",
      chapter: "Milestone 2 - Admin controls",
      caption: "Milestone 2 is live inside Admin: platform health, schema views, rules, jobs, badges, pricing, campaigns, and founding benefits.",
      cursor: { x: 1540, y: 245 },
      duration: 3.4,
    },
    {
      route: "/admin",
      scrollTo: 920,
      name: "m2-pricebook",
      chapter: "Milestone 2 - Pricebook",
      caption: "Badge pricebook reads and admin-protected updates are connected with the development admin token for local QA.",
      cursor: { x: 1040, y: 670 },
      duration: 3.2,
    },
    {
      route: "/admin",
      scrollTo: 1320,
      name: "m2-badges",
      chapter: "Milestone 2 - Badge system",
      caption: "Badge definitions, eligibility checks, purchases, assignments, unlocked features, renewals, expiration, and suspension are wired.",
      cursor: { x: 1125, y: 470 },
      duration: 3.4,
    },
    {
      route: "/admin",
      scrollTo: 1840,
      name: "m2-campaigns",
      chapter: "Milestone 2 - Campaigns",
      caption: "Campaign list, creation, and enrollment flows are available from Admin and backed by the badges-pricing API.",
      cursor: { x: 1230, y: 650 },
      duration: 3.2,
    },
    {
      route: "/admin",
      scrollBottom: true,
      name: "m2-founding",
      chapter: "Milestone 2 - Founding benefits and commission",
      caption: "Founding benefits, transfer checks, and commission quote calculation are connected, completing the local Milestone 2 workflow.",
      cursor: { x: 1425, y: 575 },
      duration: 3.4,
    },
  ];

  for (const scene of scenes) {
    await gotoCapture(page, frames, scene);
  }

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
    "fps=60,scale=1920:1080,format=yuv420p",
    "-r",
    "60",
    "-movflags",
    "+faststart",
    outputPath,
  ]);

  await rm(framesDir, { recursive: true, force: true });
  await rm(listPath, { force: true });

  const info = await stat(outputPath);
  console.log(JSON.stringify({ outputPath, bytes: info.size, frames: frames.length, fps: 60 }, null, 2));
}

run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
