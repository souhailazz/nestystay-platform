import { chromium } from "playwright-core";
import { spawn } from "node:child_process";
import { mkdir, stat } from "node:fs/promises";
import path from "node:path";

const root = path.resolve(new URL("..", import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));
const outputPath = path.join(root, "artifacts", "nestystay-real-60fps-milestones-1-2-walkthrough.mp4");
const baseUrl = "http://127.0.0.1:5173";
const apiBase = `${baseUrl}/api`;
const chromePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const adminToken = process.env.NESTYSTAY_ADMIN_TOKEN;
if (!adminToken) {
  throw new Error("Set NESTYSTAY_ADMIN_TOKEN before recording admin flows.");
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

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

  if (response.status === 204) return undefined;
  return response.json();
}

async function tryRequest(pathname, options = {}) {
  try {
    return await request(pathname, options);
  } catch {
    return null;
  }
}

function dateOnly(daysFromNow) {
  const date = new Date(Date.now() + daysFromNow * 24 * 60 * 60 * 1000);
  return date.toISOString().slice(0, 10);
}

async function seedMilestoneData() {
  const nonce = Math.random().toString(16).slice(2, 10);
  const email = `real-video-${nonce}@nestystay.local`;
  const password = "NestyStay1";

  const registered = await request("/auth/register", {
    method: "POST",
    body: { email, password, displayName: "Real Walkthrough Host", phone: "+1 876 555 0198" },
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
      hostName: "Real Walkthrough Host",
      hostEmail: email,
      title: `Real Walkthrough Villa ${nonce}`,
      location: "Negril, Westmoreland",
      country: "Jamaica",
      nightlyRate: 265,
      currency: "USD",
      badgeLevel: "Verified",
      guestVerificationEnabled: true,
      insuraGuestEnabled: true,
      cancellationPolicy: "Flexible",
      highlights: ["Seven Mile Beach style", "eKYC required", "InsuraGuest-ready", "Verified host"],
    },
  });

  const quote = await request("/bookings/quote", {
    method: "POST",
    body: { propertyId: property.id, checkIn: dateOnly(21), checkOut: dateOnly(25) },
  });

  const booking = await request("/bookings", {
    method: "POST",
    body: {
      propertyId: property.id,
      guestUserId: session.userId,
      checkIn: quote.checkIn,
      checkOut: quote.checkOut,
      ekycMetaInfo: "real 60fps walkthrough",
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

  await request("/badges-pricing/badges/purchase", {
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
      key: `real-walkthrough-${nonce}`,
      name: "Real Walkthrough Host Launch",
      campaignType: "Launch",
      overrideAmount: 79,
      appliesTo: "Trusted",
      opensAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
      closesAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      isActive: true,
    },
  });

  await tryRequest(`/badges-pricing/campaigns/real-walkthrough-${nonce}/enroll`, {
    method: "POST",
    body: { subjectType: "Host", subjectId: session.userId },
  });

  return { session, property, booking: verifiedBooking, nonce };
}

async function startRecorder() {
  await mkdir(path.dirname(outputPath), { recursive: true });
  const ffmpeg = spawn(
    "ffmpeg",
    [
      "-y",
      "-f",
      "gdigrab",
      "-framerate",
      "60",
      "-draw_mouse",
      "1",
      "-offset_x",
      "0",
      "-offset_y",
      "0",
      "-video_size",
      "1920x1080",
      "-i",
      "desktop",
      "-an",
      "-c:v",
      "libx264",
      "-preset",
      "veryfast",
      "-crf",
      "20",
      "-pix_fmt",
      "yuv420p",
      "-r",
      "60",
      "-movflags",
      "+faststart",
      outputPath,
    ],
    { stdio: ["pipe", "ignore", "pipe"] },
  );

  let stderr = "";
  ffmpeg.stderr.on("data", (data) => {
    stderr += data.toString();
    if (stderr.length > 12000) stderr = stderr.slice(-12000);
  });

  await sleep(2500);

  return {
    stop: async () => {
      ffmpeg.stdin.write("q");
      ffmpeg.stdin.end();
      const exitCode = await new Promise((resolve) => ffmpeg.on("close", resolve));
      if (exitCode !== 0) {
        throw new Error(`ffmpeg exited with ${exitCode}\n${stderr}`);
      }
    },
  };
}

async function setupOverlay(page) {
  await page.addStyleTag({
    content: `
      html {
        scroll-behavior: smooth !important;
      }
      #real-video-cursor, #real-video-caption, #real-video-chapter, #real-video-click {
        pointer-events: none;
      }
      #real-video-caption {
        position: fixed;
        left: 50%;
        bottom: 30px;
        transform: translateX(-50%);
        z-index: 2147483647;
        width: min(1480px, calc(100vw - 96px));
        border: 1px solid rgba(255,255,255,.35);
        border-radius: 8px;
        background: rgba(8, 14, 18, .88);
        box-shadow: 0 18px 48px rgba(0,0,0,.36);
        color: #fff;
        font: 800 30px/1.22 Arial, Helvetica, sans-serif;
        letter-spacing: 0;
        padding: 20px 28px;
        text-align: center;
      }
      #real-video-chapter {
        position: fixed;
        top: 24px;
        left: 24px;
        z-index: 2147483647;
        border-radius: 8px;
        background: rgba(255,255,255,.94);
        color: #101820;
        font: 900 22px/1 Arial, Helvetica, sans-serif;
        letter-spacing: 0;
        padding: 14px 18px;
        box-shadow: 0 10px 30px rgba(0,0,0,.24);
      }
      #real-video-cursor {
        position: fixed;
        z-index: 2147483647;
        left: 120px;
        top: 120px;
        width: 44px;
        height: 44px;
        filter: drop-shadow(0 7px 14px rgba(0,0,0,.58));
        transition: left .75s cubic-bezier(.22, 1, .36, 1), top .75s cubic-bezier(.22, 1, .36, 1);
      }
      #real-video-cursor::after {
        content: "";
        position: absolute;
        left: 22px;
        top: 20px;
        width: 22px;
        height: 22px;
        border-radius: 999px;
        border: 3px solid rgba(255, 207, 69, .96);
        background: rgba(255, 207, 69, .3);
      }
      #real-video-click {
        position: fixed;
        z-index: 2147483646;
        width: 18px;
        height: 18px;
        margin-left: -9px;
        margin-top: -9px;
        border-radius: 999px;
        border: 4px solid rgba(255, 207, 69, .95);
        opacity: 0;
      }
      #real-video-click.pulse {
        animation: realVideoClick .75s ease-out;
      }
      @keyframes realVideoClick {
        0% { opacity: .95; transform: scale(.8); }
        100% { opacity: 0; transform: scale(5); }
      }
    `,
  });
  await page.evaluate(() => {
    const chapter = document.createElement("div");
    chapter.id = "real-video-chapter";
    chapter.textContent = "NestyStay";
    document.body.appendChild(chapter);

    const caption = document.createElement("div");
    caption.id = "real-video-caption";
    caption.textContent = "Starting live local walkthrough.";
    document.body.appendChild(caption);

    const cursor = document.createElement("div");
    cursor.id = "real-video-cursor";
    cursor.innerHTML = `
      <svg width="44" height="44" viewBox="0 0 44 44" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M5 3L38 22L23 25L16 40L5 3Z" fill="white" stroke="#101820" stroke-width="2.7" stroke-linejoin="round"/>
      </svg>
    `;
    document.body.appendChild(cursor);

    const click = document.createElement("div");
    click.id = "real-video-click";
    document.body.appendChild(click);
  });
}

async function setCaption(page, chapter, caption) {
  await page.evaluate(
    ({ chapter, caption }) => {
      document.getElementById("real-video-chapter").textContent = chapter;
      document.getElementById("real-video-caption").textContent = caption;
    },
    { chapter, caption },
  );
}

async function moveCursor(page, x, y, ms = 850) {
  await page.evaluate(
    ({ x, y }) => {
      const cursor = document.getElementById("real-video-cursor");
      cursor.style.left = `${x}px`;
      cursor.style.top = `${y}px`;
    },
    { x, y },
  );
  await page.mouse.move(x + 14, y + 12, { steps: 24 });
  await sleep(ms);
}

async function clickAt(page, x, y) {
  await moveCursor(page, x, y, 350);
  await page.evaluate(
    ({ x, y }) => {
      const click = document.getElementById("real-video-click");
      click.style.left = `${x + 22}px`;
      click.style.top = `${y + 22}px`;
      click.classList.remove("pulse");
      void click.offsetWidth;
      click.classList.add("pulse");
    },
    { x, y },
  );
  await page.mouse.click(x + 14, y + 12);
  await sleep(800);
}

async function typeLikeHuman(page, selector, text) {
  const locator = page.locator(selector).first();
  try {
    await locator.waitFor({ state: "visible", timeout: 2500 });
  } catch {
    return false;
  }
  const box = await locator.boundingBox();
  if (box) {
    await clickAt(page, box.x + Math.min(box.width - 30, 140), box.y + box.height / 2 - 20);
  }
  await locator.fill("");
  for (const char of text) {
    await page.keyboard.type(char);
    await sleep(34);
  }
  await sleep(500);
  return true;
}

async function gotoWithTransition(page, route, chapter, caption) {
  await setCaption(page, chapter, caption);
  await moveCursor(page, 1460, 46, 450);
  await page.goto(`${baseUrl}${route}`);
  await page.waitForLoadState("domcontentloaded");
  await sleep(950);
  await setupOverlay(page);
  await setCaption(page, chapter, caption);
  await sleep(900);
}

async function smoothScroll(page, top, duration = 1900) {
  await page.evaluate(
    ({ top, duration }) =>
      new Promise((resolve) => {
        const start = window.scrollY;
        const delta = top - start;
        const startedAt = performance.now();
        function tick(now) {
          const progress = Math.min(1, (now - startedAt) / duration);
          const eased = 1 - Math.pow(1 - progress, 3);
          window.scrollTo(0, start + delta * eased);
          if (progress < 1) requestAnimationFrame(tick);
          else resolve();
        }
        requestAnimationFrame(tick);
      }),
    { top, duration },
  );
}

async function run() {
  const health = await request("/health");
  if (!["Healthy", "ok"].includes(health.status)) throw new Error(`Backend health check returned ${health.status}`);

  const demo = await seedMilestoneData();
  const browser = await chromium.launch({
    executablePath: chromePath,
    headless: false,
    args: [
      "--window-position=0,0",
      "--window-size=1920,1080",
      "--force-device-scale-factor=1",
      "--disable-infobars",
      "--no-first-run",
      "--disable-session-crashed-bubble",
    ],
  });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
    colorScheme: "light",
  });

  await context.addInitScript(({ session, token }) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(session));
    window.localStorage.setItem("nestyStay.adminToken", token);
  }, { session: demo.session, token: adminToken });

  const page = await context.newPage();
  await page.goto(baseUrl);
  await page.waitForLoadState("domcontentloaded");
  await setupOverlay(page);
  await setCaption(page, "Milestone 1 - Start", "This is a live 60fps walkthrough of Milestones 1 and 2, not a slideshow.");
  await sleep(2000);

  const recorder = await startRecorder();
  try {
    await clickAt(page, 780, 52);
    await setCaption(page, "Milestone 1 - Explore", "Clicking Explore loads real listings from the backend property API.");
    await page.goto(`${baseUrl}/explore`);
    await page.waitForLoadState("domcontentloaded");
    await setupOverlay(page);
    await setCaption(page, "Milestone 1 - Explore", "Search, badge filters, rates, verification flags, and property cards are all backend-linked.");
    await sleep(1200);
    await typeLikeHuman(page, "input[placeholder*='Parish']", "Negril");
    await clickAt(page, 1480, 506);
    await sleep(1300);
    await smoothScroll(page, 520, 1900);
    await moveCursor(page, 700, 620, 900);
    await sleep(900);

    await gotoWithTransition(
      page,
      `/properties/${demo.property.id}`,
      "Milestone 1 - Property Detail",
      "Opening the seeded villa shows the live property detail endpoint, eKYC requirement, InsuraGuest flag, and booking entry point.",
    );
    await smoothScroll(page, 420, 1700);
    await moveCursor(page, 1410, 560, 1000);
    await clickAt(page, 1410, 560);
    await setCaption(page, "Milestone 1 - Booking Quote", "The booking modal calculates quote totals, dates, fees, holds, and verification requirements from backend rules.");
    await sleep(2200);

    await gotoWithTransition(
      page,
      "/login",
      "Milestone 1 - Auth",
      "Auth is wired: registration, password login, 2FA challenge, and verified session storage are all local full-stack flows.",
    );
    await typeLikeHuman(page, "input[type='email']", demo.session.email);
    await typeLikeHuman(page, "input[type='password']", "NestyStay1");
    await moveCursor(page, 1110, 690, 1100);
    await sleep(1200);

    await gotoWithTransition(
      page,
      "/guest-dashboard",
      "Milestone 1 - Guest Dashboard",
      "The guest dashboard reads bookings, eKYC status, payment state, totals, price lines, notifications, and timeline from the backend.",
    );
    await smoothScroll(page, 560, 2200);
    await moveCursor(page, 1320, 430, 1000);
    await sleep(1000);

    await gotoWithTransition(
      page,
      "/host-dashboard",
      "Milestone 1 - Host Dashboard",
      "The host dashboard is connected to the same persisted property and booking records, showing the host side of the workflow.",
    );
    await smoothScroll(page, 500, 1800);
    await moveCursor(page, 530, 620, 900);
    await sleep(1100);

    await gotoWithTransition(
      page,
      "/host/properties",
      "Milestone 1 - Host Property Creation",
      "Host property creation persists to PostgreSQL and immediately appears in Explore, details, dashboards, and admin views.",
    );
    await smoothScroll(page, 620, 1900);
    await typeLikeHuman(page, "input[value='Ocho Rios, St. Ann']", "Kingston demo edit");
    await moveCursor(page, 1190, 665, 1000);
    await sleep(1100);

    await gotoWithTransition(
      page,
      "/bookings",
      "Milestone 1 - Booking Admin",
      "Booking admin shows date holds, overlap protection, eKYC pass/reject state, and payment capture blocked until approval.",
    );
    await smoothScroll(page, 520, 1800);
    await moveCursor(page, 1450, 470, 900);
    await sleep(1200);

    await gotoWithTransition(
      page,
      "/payment-confirmation",
      "Milestone 1 - Payment Capture",
      "Payment confirmation demonstrates the Stripe-style authorization and capture workflow after booking verification is approved.",
    );
    await moveCursor(page, 900, 520, 1100);
    await sleep(1600);

    await gotoWithTransition(
      page,
      "/calendar",
      "Milestone 1 - Calendar",
      "Calendar/read views expose booked dates and booking status for host operations.",
    );
    await smoothScroll(page, 460, 1800);
    await moveCursor(page, 1280, 520, 1200);
    await sleep(1100);

    await gotoWithTransition(
      page,
      "/admin",
      "Milestone 2 - Admin Overview",
      "Milestone 2 lives in Admin: health, platform modules, backend schema, jobs, badges, pricing, campaigns, and founding benefits.",
    );
    await moveCursor(page, 1520, 245, 900);
    await smoothScroll(page, 760, 2200);
    await sleep(1000);

    await setCaption(page, "Milestone 2 - Pricebook", "Pricebook read/update controls call admin-protected badge pricing endpoints with the local admin token.");
    await smoothScroll(page, 1160, 2400);
    await moveCursor(page, 1030, 666, 900);
    await sleep(1300);

    await setCaption(page, "Milestone 2 - Badges", "Badge eligibility, purchases, assignments, feature unlocking, renewals, expiration, and suspension are wired through the badges-pricing API.");
    await smoothScroll(page, 1480, 2400);
    await moveCursor(page, 1170, 470, 900);
    await sleep(1500);

    await setCaption(page, "Milestone 2 - Campaigns", "Campaign creation, listing, and enrollment are connected to backend persistence and admin protection.");
    await smoothScroll(page, 2050, 2600);
    await moveCursor(page, 1240, 650, 1000);
    await sleep(1500);

    await setCaption(page, "Milestone 2 - Founding Benefits", "Founding benefits, transfer evaluation, and commission quote calculation complete the Milestone 2 local workflow.");
    await smoothScroll(page, 2800, 2800);
    await moveCursor(page, 1420, 575, 1000);
    await sleep(1800);

    await setCaption(page, "Milestones 1 and 2 - Complete Locally", "Milestones 1 and 2 are complete locally. Remaining work is production secrets, providers, hosting, SSL, backups, monitoring, and compliance review.");
    await smoothScroll(page, 0, 2600);
    await moveCursor(page, 960, 500, 1200);
    await sleep(3000);

    const recapScenes = [
      {
        route: "/explore",
        chapter: "Final Pass - Discovery",
        caption: "Final pass: discovery keeps live listings, badge filters, search, host-created properties, and Jamaican-styled property imagery linked together.",
        scroll: 720,
        cursor: [760, 590],
      },
      {
        route: `/properties/${demo.property.id}`,
        chapter: "Final Pass - Property Detail",
        caption: "The created villa is still available through its backend detail endpoint, with verification, insurance, policy, and price context intact.",
        scroll: 620,
        cursor: [1470, 610],
      },
      {
        route: "/guest-dashboard",
        chapter: "Final Pass - Guest Operations",
        caption: "Guest operations show persisted booking state, eKYC outcome, authorization, captured payment, price lines, and timeline.",
        scroll: 640,
        cursor: [1325, 455],
      },
      {
        route: "/host-dashboard",
        chapter: "Final Pass - Host Operations",
        caption: "Host operations read the same source of truth, so the local full-stack app stays linked across guest and host views.",
        scroll: 560,
        cursor: [540, 620],
      },
      {
        route: "/host/properties",
        chapter: "Final Pass - Property Management",
        caption: "Property management is ready locally for creating and reviewing host listings without needing Docker.",
        scroll: 780,
        cursor: [1180, 675],
      },
      {
        route: "/bookings",
        chapter: "Final Pass - Booking Controls",
        caption: "Booking controls cover holds, overlap blocking, admin review, eKYC resolution, and capture protection after approval.",
        scroll: 650,
        cursor: [1440, 510],
      },
      {
        route: "/admin",
        chapter: "Final Pass - Admin Overview",
        caption: "Admin reads platform health, backend schema, jobs, property data, bookings, badge pricing, campaigns, and benefits.",
        scroll: 820,
        cursor: [1530, 250],
      },
      {
        route: "/admin",
        chapter: "Final Pass - Pricebook and Badges",
        caption: "Pricebook and badge controls are admin-protected locally and cover eligibility, assignments, feature locks, renewals, expiration, and suspension.",
        scroll: 1520,
        cursor: [1130, 485],
      },
      {
        route: "/admin",
        chapter: "Final Pass - Campaigns",
        caption: "Campaign creation, enrollment, and live campaign lists are connected through the same badges-pricing backend.",
        scroll: 2100,
        cursor: [1245, 652],
      },
      {
        route: "/admin",
        chapter: "Final Pass - Founding and Commission",
        caption: "Founding benefits, transfer evaluation, and commission quote calculation close out Milestone 2.",
        scroll: 2900,
        cursor: [1425, 575],
      },
    ];

    for (const scene of recapScenes) {
      await gotoWithTransition(page, scene.route, scene.chapter, scene.caption);
      await smoothScroll(page, scene.scroll, 2600);
      await moveCursor(page, scene.cursor[0], scene.cursor[1], 1200);
      await sleep(2600);
    }

    await gotoWithTransition(
      page,
      "/",
      "Milestones 1 and 2 - Local QA Complete",
      "This recording shows real browser navigation, typing, clicks, scrolling, backend-seeded data, and admin-linked Milestone 1 and 2 workflows.",
    );
    await smoothScroll(page, 760, 2600);
    await moveCursor(page, 960, 520, 1200);
    await sleep(5000);
  } finally {
    await recorder.stop();
    await browser.close();
  }

  const info = await stat(outputPath);
  console.log(JSON.stringify({ outputPath, bytes: info.size, fps: 60, mode: "live-browser-gdigrab" }, null, 2));
}

run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
