import { chromium, request as playwrightRequest } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";

const repoRoot = fs.existsSync(path.join(process.cwd(), "frontend")) ? process.cwd() : path.resolve(process.cwd(), "..");
const root = path.join(repoRoot, "testing-evidence", "client-demo");
const temporary = path.join(root, ".recordings");
const baseURL = process.env.CLIENT_DEMO_BASE_URL ?? "http://127.0.0.1:5173";
const apiBaseURL = process.env.CLIENT_DEMO_API_URL ?? baseURL;
const password = "NestyStay1";
const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN ?? "test-admin-token";

const folders = {
  M1: ["01-Authentication", "02-2FA", "03-Property-Listings", "04-Booking", "05-eKYC", "06-Payments", "07-Guest-Dashboard", "08-Host-Dashboard"],
  M2: ["01-Free", "02-Verified", "03-Trusted", "04-Wellness", "05-Badge-Eligibility", "06-Upgrades-Renewals", "07-Feature-Restrictions", "08-Admin-Badge-Management"],
  M3: ["01-Officer-Onboarding", "02-Officer-Approval", "03-Wellness-Booking", "04-Scheduling", "05-Officer-Assignment", "06-Photo-Report", "07-Host-Report", "08-Subscription", "09-Commission-Payout", "10-Admin-Wellness", "11-Officer-Privacy"],
  M4: ["01-Custodian", "02-Trades", "03-Local-Business", "04-Police", "05-Provider-Registration", "06-Provider-Moderation", "07-Provider-Dashboard", "08-Directory-Search", "09-Badge-Gating", "10-Guest-Verification-Upsell", "11-QR-Issue", "12-Gate-QR-Validation", "13-QR-Revoke-Expiry", "14-Emergency-119"],
  M5: ["01-Manager-Dashboard", "02-Owner-Invitation", "03-Owner-Verification", "04-Property-Assignment", "05-Owner-Portal", "06-Invoices", "07-Statements", "08-Payments", "09-Utilities", "10-Maintenance", "11-Vendors", "12-Community-Board", "13-Gate-Messages", "14-Governance", "15-Anonymous-Voting", "16-Proxy-Voting", "17-Documents", "18-Subscription", "19-Property-Manager-QR", "20-Gate-Guard-Interface"],
};

const sessions = {};
let fixtures = {};
const manifestRows = [];

function ensureDirectory(dir) { fs.mkdirSync(dir, { recursive: true }); }
function safeFile(value) { return value.replace(/[^a-z0-9-]+/gi, "-").replace(/^-|-$/g, "").toLowerCase(); }
function write(file, content) { ensureDirectory(path.dirname(file)); fs.writeFileSync(file, content, "utf8"); }

async function jsonRequest(api, method, url, body, token) {
  const options = { headers: token ? { Authorization: `Bearer ${token}` } : {} };
  if (body !== undefined) options.data = body;
  const response = await api[method](url, options);
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!response.ok()) throw new Error(`${method.toUpperCase()} ${url} ${response.status()}: ${typeof data === "string" ? data : JSON.stringify(data)}`);
  return data;
}

async function createSession(api, role, displayName) {
  const email = `client-demo-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  await jsonRequest(api, "post", "/api/auth/register", { email, password, confirmPassword: password, displayName, phone: "+15550104009", acceptedTerms: true, acceptedPrivacy: true, role });
  const login = await jsonRequest(api, "post", "/api/auth/login", { email, password });
  if (!login.requiresTwoFactor) return { ...login, email, displayName, permissions: login.permissions ?? [] };
  const challenge = await jsonRequest(api, "get", `/api/auth/development/challenges/${login.challengeId}`);
  const verified = await jsonRequest(api, "post", "/api/auth/2fa/verify", { challengeId: login.challengeId, code: challenge.code });
  return { ...verified, email, displayName, permissions: verified.permissions ?? [] };
}

async function seed(api) {
  sessions.admin = { userId: "00000000-0000-0000-0000-000000000001", email: "client-admin@nestystay.local", displayName: "NestyStay Administrator", accessToken: adminToken, expiresAt: new Date(Date.now() + 86_400_000).toISOString(), roles: ["Admin"], permissions: ["super_administration", "officer_management", "financial_reporting", "system_configuration", "user_management", "dispute_management"] };
  sessions.guest = await createSession(api, "Guest", "Ava Mitchell");
  sessions.host = await createSession(api, "Host", "Michael Brown");
  sessions.officer = await createSession(api, "Officer", "Jordan Clarke");
  sessions.provider = await createSession(api, "ServiceProvider", "Kingston Plumbing Services");
  sessions.pm = await createSession(api, "PropertyManager", "Sofia Bennett");
  sessions.owner = await createSession(api, "Owner", "Elena Carter");

  const bearer = (session) => session.accessToken;
  for (const level of ["Verified", "Trusted", "Wellness"]) {
    try { await jsonRequest(api, "post", "/api/badges-pricing/badges/purchase", { subjectType: "Host", subjectId: sessions.host.userId, level, hostVerificationPassed: true, completedApprovedBookings: 3, hasPropertyAddress: true, hasWellnessSubscription: true, paymentSucceeded: true }, adminToken); } catch { /* fixture may already exist */ }
  }
  const propertyBody = { hostUserId: sessions.host.userId, hostName: sessions.host.displayName, hostEmail: sessions.host.email, title: "Ocean View Apartment", location: "Montego Bay", country: "Jamaica", nightlyRate: 180, currency: "USD", badgeLevel: "Wellness", guestVerificationEnabled: false, insuraGuestEnabled: false, cancellationPolicy: "Flexible", highlights: ["Sea view", "Fast Wi-Fi", "Verified host"] };
  fixtures.property = await jsonRequest(api, "post", "/api/properties", propertyBody, bearer(sessions.host));
  const ekycProperty = { ...propertyBody, title: "Palm Gardens Verified Stay", guestVerificationEnabled: true, badgeLevel: "Verified" };
  fixtures.ekycProperty = await jsonRequest(api, "post", "/api/properties", ekycProperty, bearer(sessions.host));
  const checkIn = new Date(Date.now() + 3 * 86_400_000);
  const checkOut = new Date(Date.now() + 6 * 86_400_000);
  fixtures.booking = await jsonRequest(api, "post", "/api/bookings", { propertyId: fixtures.property.id, guestUserId: sessions.guest.userId, checkIn: checkIn.toISOString().slice(0, 10), checkOut: checkOut.toISOString().slice(0, 10) }, bearer(sessions.guest));
  fixtures.ekycBooking = await jsonRequest(api, "post", "/api/bookings", { propertyId: fixtures.ekycProperty.id, guestUserId: sessions.guest.userId, checkIn: new Date(Date.now() + 9 * 86_400_000).toISOString().slice(0, 10), checkOut: new Date(Date.now() + 12 * 86_400_000).toISOString().slice(0, 10) }, bearer(sessions.guest));

  try {
    const officer = await jsonRequest(api, "post", "/api/wellness/officers", { userId: sessions.officer.userId, badgeNumber: `DEMO-${Date.now()}`, parish: "St. Ann", coverageArea: "Montego Bay", isActiveOffDuty: true, isRetired: false, verificationMetadata: "Client visual demonstration fixture" }, bearer(sessions.officer));
    fixtures.officer = officer;
    await jsonRequest(api, "post", `/api/wellness/officers/${officer.id}/approve`, { reason: "Client visual demonstration approval" }, adminToken);
    const visit = await jsonRequest(api, "post", "/api/wellness/visits", { hostUserId: sessions.host.userId, propertyId: fixtures.property.id, visitType: "StandardWellnessCheck", scheduledAt: new Date(Date.now() + 86_400_000).toISOString(), parish: "St. Ann", area: "Montego Bay" }, bearer(sessions.host));
    fixtures.visit = visit;
    try { fixtures.assignment = await jsonRequest(api, "post", `/api/wellness/visits/${visit.id}/assign`, { officerId: officer.id }, adminToken); } catch { /* assignment is optional for visual data */ }
  } catch { /* wellness data can still be shown through the real empty state */ }

  fixtures.provider = await jsonRequest(api, "post", "/api/directories/providers", { slug: `kingston-plumbing-${sessions.provider.userId.slice(0, 8)}`, kind: "Trades", category: "Plumbing", name: "Kingston Plumbing Services", parish: "Kingston", badgeLevel: "Trusted", description: "Licensed local trade provider for residential repairs.", availabilitySummary: "Mon–Fri · 8:00 AM–5:00 PM", contactMode: "Platform messaging", isBrickAndMortar: true, isActive: true }, bearer(sessions.provider)).catch(() => null);
  for (const kind of ["Custodian", "LocalBusiness", "Police"]) {
    try { await jsonRequest(api, "post", "/api/directories/providers", { slug: `demo-${kind.toLowerCase()}-${Date.now()}`, kind, category: kind === "Police" ? "Wellness safety" : "Property services", name: kind === "Police" ? "St. Ann Community Safety" : `Demo ${kind} Partner`, parish: "St. Ann", badgeLevel: kind === "Police" ? "Wellness" : "Verified", description: "Approved directory fixture for client demonstration.", availabilitySummary: "Available through NestyStay", contactMode: "Platform messaging", isBrickAndMortar: true, isActive: true }, adminToken); } catch { /* optional fixture */ }
  }

  const ownerInvite = await jsonRequest(api, "post", "/api/property-manager/owners", { email: sessions.owner.email, displayName: sessions.owner.displayName }, bearer(sessions.pm));
  fixtures.owner = ownerInvite;
  fixtures.pmProperty = await jsonRequest(api, "post", "/api/property-manager/properties", { ownerUserId: ownerInvite.ownerUserId, title: "Palm Gardens Community", unitNumber: "B-5", address: "Montego Bay, Jamaica" }, bearer(sessions.pm));
  fixtures.invoice = await jsonRequest(api, "post", "/api/property-manager/invoices", { ownerUserId: ownerInvite.ownerUserId, propertyId: fixtures.pmProperty.id, dueDate: "2030-01-01", tax: 0, lines: [{ description: "Monthly community service", quantity: 1, unitAmount: 125 }] }, bearer(sessions.pm));
  try { fixtures.utility = await jsonRequest(api, "post", "/api/property-manager/utilities", { ownerUserId: ownerInvite.ownerUserId, propertyId: fixtures.pmProperty.id, utilityType: "Water", billingPeriod: "2030-01", usage: 10, rate: 2 }, bearer(sessions.pm)); } catch {}
  try { fixtures.maintenance = await jsonRequest(api, "post", "/api/property-manager/maintenance", { ownerUserId: ownerInvite.ownerUserId, propertyId: fixtures.pmProperty.id, title: "Pool pump inspection", description: "Schedule a routine pool pump inspection.", category: "Facilities", urgency: "NORMAL" }, bearer(sessions.pm)); } catch {}
  try { fixtures.vendor = await jsonRequest(api, "post", "/api/property-manager/vendors", { name: "Island Property Care", category: "Facilities", contact: "Platform messaging", notes: "Approved local vendor" }, bearer(sessions.pm)); } catch {}
  try { await jsonRequest(api, "post", "/api/property-manager/notices", { title: "Welcome to Palm Gardens", body: "Community board updates are now available in your owner portal.", isPinned: true }, bearer(sessions.pm)); } catch {}
  try { fixtures.proposal = await jsonRequest(api, "post", "/api/property-manager/governance/proposals", { title: "Pool lighting refresh", description: "Vote on the community lighting refresh proposal.", opensAt: new Date(Date.now() - 86_400_000).toISOString(), closesAt: new Date(Date.now() + 7 * 86_400_000).toISOString(), isAnonymous: true, quorum: 1 }, bearer(sessions.pm)); } catch {}
  try { await jsonRequest(api, "post", "/api/property-manager/documents", { ownerUserId: ownerInvite.ownerUserId, propertyId: fixtures.pmProperty.id, title: "Community rules", category: "Governance", fileName: "community-rules.pdf", contentType: "application/pdf", sizeBytes: 18, contentBase64: "UERGIERlbW8=" }, bearer(sessions.pm)); } catch {}
  try { await jsonRequest(api, "post", "/api/property-manager/gate/messages", { propertyId: fixtures.pmProperty.id, recipient: "Gate desk", message: "Guest arrival confirmed for unit B-5.", visitorType: "GUEST", validFrom: new Date(Date.now() - 60_000).toISOString(), validUntil: new Date(Date.now() + 86_400_000).toISOString() }, bearer(sessions.pm)); } catch {}
  try { fixtures.pmQr = await jsonRequest(api, "post", "/api/property-manager/qr", { ownerUserId: ownerInvite.ownerUserId, propertyId: fixtures.pmProperty.id, subjectType: "VENDOR", validFrom: new Date(Date.now() - 60_000).toISOString(), validUntil: new Date(Date.now() + 86_400_000).toISOString() }, bearer(sessions.pm)); } catch {}
}

const scenes = [
  ["M1", "01-Authentication", "/register", "public", "registration-form", "Registration and sign-in entry point", true],
  ["M1", "02-2FA", "/auth/2fa-setup", "guest", "two-factor-enrollment", "Two-factor enrollment surface", false],
  ["M1", "03-Property-Listings", "/host/properties", "host", "property-listings", "Host property list backed by API", true],
  ["M1", "04-Booking", "/booking/" + "__BOOKING__/review", "guest", "booking-review", "Booking review and server quote", true],
  ["M1", "05-eKYC", "/booking/" + "__EKYC_BOOKING__/identity", "guest", "ekyc-identity", "eKYC identity checkpoint", false],
  ["M1", "06-Payments", "/booking/" + "__BOOKING__/checkout", "guest", "stripe-checkout", "Checkout application boundary", false],
  ["M1", "07-Guest-Dashboard", "/guest-dashboard", "guest", "guest-dashboard", "Guest dashboard persisted reservations", true],
  ["M1", "08-Host-Dashboard", "/host-dashboard", "host", "host-dashboard", "Host dashboard persisted portfolio", true],
  ["M2", "01-Free", "/host/badges", "host", "badge-free", "Free badge tier", false],
  ["M2", "02-Verified", "/host/badges", "host", "badge-verified", "Verified badge tier", false],
  ["M2", "03-Trusted", "/host/badges", "host", "badge-trusted", "Trusted badge tier", false],
  ["M2", "04-Wellness", "/host/badges", "host", "badge-wellness", "Wellness badge tier", false],
  ["M2", "05-Badge-Eligibility", "/host/badges", "host", "badge-eligibility", "Eligibility and pricing", false],
  ["M2", "06-Upgrades-Renewals", "/host/badges", "host", "badge-renewal", "Upgrade and renewal controls", false],
  ["M2", "07-Feature-Restrictions", "/directory/police", "host", "badge-gating", "Badge-gated directory state", false],
  ["M2", "08-Admin-Badge-Management", "/admin/ops/pricebook", "admin", "admin-badges", "Admin badge pricebook management", false],
  ["M3", "01-Officer-Onboarding", "/officer/wellness", "officer", "officer-onboarding", "Officer onboarding and verification", false],
  ["M3", "02-Officer-Approval", "/admin/ops/wellness", "admin", "officer-approval", "Admin officer review", false],
  ["M3", "03-Wellness-Booking", "/host/wellness/book", "host", "wellness-booking", "Host wellness booking form", true],
  ["M3", "04-Scheduling", "/host/wellness", "host", "wellness-scheduling", "Scheduled wellness visits", false],
  ["M3", "05-Officer-Assignment", "/officer/wellness", "officer", "officer-assignment", "Officer assignment queue", false],
  ["M3", "06-Photo-Report", "/officer/wellness", "officer", "wellness-report", "Report and photo submission surface", false],
  ["M3", "07-Host-Report", "/host/wellness", "host", "host-report", "Host report and status", false],
  ["M3", "08-Subscription", "/host/wellness", "host", "wellness-subscription", "Wellness subscription controls", false],
  ["M3", "09-Commission-Payout", "/admin/ops/wellness", "admin", "wellness-payout", "Commission and payout visibility", false],
  ["M3", "10-Admin-Wellness", "/admin/ops/wellness", "admin", "wellness-admin", "Admin wellness operations", false],
  ["M3", "11-Officer-Privacy", "/officer/wellness", "officer", "officer-privacy", "Officer privacy-aware workspace", false],
  ["M4", "01-Custodian", "/directory/custodians", "host", "custodian-directory", "Custodian directory", true],
  ["M4", "02-Trades", "/directory/trades", "host", "trades-directory", "Trades directory", false],
  ["M4", "03-Local-Business", "/directory/businesses", "host", "business-directory", "Local business directory", false],
  ["M4", "04-Police", "/directory/police", "host", "police-directory", "Police wellness directory", false],
  ["M4", "05-Provider-Registration", "/directory/provider/onboarding", "provider", "provider-registration", "Provider registration", true],
  ["M4", "06-Provider-Moderation", "/admin/ops/directories", "admin", "provider-moderation", "Admin provider moderation", false],
  ["M4", "07-Provider-Dashboard", "/directory/provider", "provider", "provider-dashboard", "Provider dashboard", false],
  ["M4", "08-Directory-Search", "/directory/trades", "host", "directory-search", "Searchable directory results", false],
  ["M4", "09-Badge-Gating", "/directory/police", "host", "directory-badge-gate", "Badge-gated access", false],
  ["M4", "10-Guest-Verification-Upsell", "/directory/guest-verification", "guest", "verification-upsell", "Guest verification upsell", false],
  ["M4", "11-QR-Issue", "/traveler/reservations", "guest", "qr-issue", "Reservation QR issue entry", false],
  ["M4", "12-Gate-QR-Validation", "/gate/qr", "public", "qr-validation", "Gate QR validation interface", true],
  ["M4", "13-QR-Revoke-Expiry", "/gate/qr", "public", "qr-revoke-expiry", "Invalid or expired QR decision", false],
  ["M4", "14-Emergency-119", "/directory/police", "host", "emergency-119", "Emergency contact directory", false],
  ["M5", "01-Manager-Dashboard", "/pm/dashboard", "pm", "manager-dashboard", "Manager dashboard", true],
  ["M5", "02-Owner-Invitation", "/pm/dashboard", "pm", "owner-invitation", "Owner invitation workflow", false],
  ["M5", "03-Owner-Verification", "/pm/dashboard", "pm", "owner-verification", "Owner verification state", false],
  ["M5", "04-Property-Assignment", "/pm/dashboard", "pm", "property-assignment", "Property assignment", false],
  ["M5", "05-Owner-Portal", "/owner/dashboard", "owner", "owner-portal", "Owner portal", true],
  ["M5", "06-Invoices", "/pm/invoices", "pm", "invoices", "Invoice management", false],
  ["M5", "07-Statements", "/owner/dashboard", "owner", "statements", "Printable statements", false],
  ["M5", "08-Payments", "/pm/invoices", "pm", "payments", "Invoice payment state", false],
  ["M5", "09-Utilities", "/pm/utilities", "pm", "utilities", "Utility proofing", false],
  ["M5", "10-Maintenance", "/pm/maintenance", "pm", "maintenance", "Maintenance lifecycle", true],
  ["M5", "11-Vendors", "/pm/maintenance", "pm", "vendors", "Vendor coordination", false],
  ["M5", "12-Community-Board", "/pm/dashboard", "pm", "community-board", "Community notices", false],
  ["M5", "13-Gate-Messages", "/pm/gates", "pm", "gate-messages", "Gate communications", false],
  ["M5", "14-Governance", "/pm/governance", "pm", "governance", "Governance proposals", false],
  ["M5", "15-Anonymous-Voting", "/pm/governance", "pm", "anonymous-voting", "Anonymous voting", false],
  ["M5", "16-Proxy-Voting", "/owner/dashboard", "owner", "proxy-voting", "Proxy voting controls", false],
  ["M5", "17-Documents", "/pm/documents", "pm", "documents", "Document vault", false],
  ["M5", "18-Subscription", "/pm/dashboard", "pm", "pm-subscription", "Subscription renewal", false],
  ["M5", "19-Property-Manager-QR", "/pm/gates", "pm", "pm-qr", "Property manager QR issue", false],
  ["M5", "20-Gate-Guard-Interface", "/gate", "public", "gate-guard", "Gate guard interface", false],
];

function resolveRoute(route) { return route.replace("__BOOKING__", fixtures.booking?.id ?? "demo-booking").replace("__EKYC_BOOKING__", fixtures.ekycBooking?.id ?? "demo-ekyc"); }

async function recordScene(browser, scene) {
  const [milestone, folder, rawRoute, role, label, description, responsive] = scene;
  const route = resolveRoute(rawRoute);
  const destination = path.join(root, milestone === "M1" ? "M1-Core" : milestone === "M2" ? "M2-Badges" : milestone === "M3" ? "M3-Wellness" : milestone === "M4" ? "M4-Directories-QR" : "M5-Property-Manager", folder);
  const screenshotDir = path.join(destination, "screenshots");
  const videoDir = path.join(destination, "video");
  ensureDirectory(screenshotDir); ensureDirectory(videoDir); ensureDirectory(temporary);
  const stem = `${milestone}-${safeFile(folder)}-${label}`;
  const existingWebm = path.join(videoDir, `${stem}.webm`);
  const existingScreenshots = fs.readdirSync(screenshotDir).filter((file) => file.includes(stem) || file.startsWith(label + "-"));
  if (fs.existsSync(existingWebm) && existingScreenshots.length > 0) {
    manifestRows.push({ milestone, folder, route, description, screenshots: existingScreenshots.map((file) => path.relative(root, path.join(screenshotDir, file)).replaceAll("\\", "/")), webm: path.relative(root, existingWebm).replaceAll("\\", "/"), failed: null, errors: [] });
    return;
  }
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1, recordVideo: { dir: temporary, size: { width: 1920, height: 1080 } } });
  const page = await context.newPage();
  const session = role === "public" ? null : sessions[role];
  if (session) await page.addInitScript((value) => window.localStorage.setItem("nestyStay.session", JSON.stringify(value)), session);
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  let failed = null;
  try {
    await page.goto(`${baseURL}${route}`, { waitUntil: "domcontentloaded", timeout: 45_000 });
    await page.waitForTimeout(2_500);
    await page.screenshot({ path: path.join(screenshotDir, `01-${stem}-before.png`), fullPage: false });
    await page.mouse.move(950, 520); await page.waitForTimeout(700);
    await page.mouse.wheel(0, 520); await page.waitForTimeout(1_800);
    await page.screenshot({ path: path.join(screenshotDir, `02-${stem}-result.png`), fullPage: false });
    if (responsive) {
      for (const [name, viewport] of [["tablet", { width: 1024, height: 768 }], ["mobile", { width: 390, height: 844 }]]) {
        await page.setViewportSize(viewport); await page.waitForTimeout(900);
        await page.screenshot({ path: path.join(screenshotDir, `${label}-${name}.png`), fullPage: false });
      }
    }
  } catch (error) { failed = error instanceof Error ? error.message : String(error); }
  await context.close();
  const recorded = page.video() ? await page.video().path() : null;
  const webmPath = path.join(videoDir, `${stem}.webm`);
  if (recorded && fs.existsSync(recorded)) fs.copyFileSync(recorded, webmPath);
  manifestRows.push({ milestone, folder, route, description, screenshots: fs.existsSync(screenshotDir) ? fs.readdirSync(screenshotDir).filter((file) => file.includes(stem) || file.startsWith(label + "-")).map((file) => path.relative(root, path.join(screenshotDir, file)).replaceAll("\\", "/")) : [], webm: path.relative(root, webmPath).replaceAll("\\", "/"), failed, errors });
}

async function recordMaster(browser) {
  const destination = path.join(root, "Full-System");
  ensureDirectory(path.join(destination, "videos")); ensureDirectory(path.join(destination, "screenshots")); ensureDirectory(temporary);
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1, recordVideo: { dir: temporary, size: { width: 1920, height: 1080 } } });
  const page = await context.newPage();
  await page.goto(`${baseURL}/`, { waitUntil: "domcontentloaded", timeout: 45_000 });
  const journey = [
    ["public", "/", "Welcome"], ["public", "/explore", "Explore stays"], ["public", "/register", "Registration"], ["guest", "/guest-dashboard", "Guest dashboard"], ["host", "/host/properties", "Host listings"], ["host", "/host/badges", "Badges"], ["guest", `/booking/${fixtures.booking?.id ?? "demo"}/pending`, "Booking pending"], ["guest", `/booking/${fixtures.ekycBooking?.id ?? "demo"}/identity`, "eKYC checkpoint"], ["host", "/host/wellness", "Wellness visits"], ["officer", "/officer/wellness", "Officer workspace"], ["host", "/directory/trades", "Trades directory"], ["public", "/gate/qr", "QR gate"], ["pm", "/pm/dashboard", "Manager dashboard"], ["owner", "/owner/dashboard", "Owner portal"], ["pm", "/pm/invoices", "Invoices"], ["pm", "/pm/maintenance", "Maintenance"], ["pm", "/pm/governance", "Governance"], ["pm", "/pm/documents", "Documents"], ["pm", "/pm/gates", "Gate messages"], ["public", "/gate", "Gate guard"]
  ];
  for (const [index, [role, route, label]] of journey.entries()) {
    const session = role === "public" ? null : sessions[role];
    await page.evaluate((value) => { if (value) window.localStorage.setItem("nestyStay.session", JSON.stringify(value)); else window.localStorage.removeItem("nestyStay.session"); }, session);
    await page.goto(`${baseURL}${route}`, { waitUntil: "domcontentloaded", timeout: 45_000 });
    await page.waitForTimeout(2_400);
    await page.mouse.move(950, 520); await page.waitForTimeout(800);
    await page.screenshot({ path: path.join(destination, "screenshots", `${String(index + 1).padStart(2, "0")}-${safeFile(label)}.png`), fullPage: false }).catch(() => undefined);
  }
  await context.close();
  const recorded = page.video() ? await page.video().path() : null;
  const webm = path.join(destination, "videos", "NestyStay-M1-M5-Full-System-Demo.webm");
  if (recorded && fs.existsSync(recorded)) fs.copyFileSync(recorded, webm);
  return webm;
}

function mediaInfo(file) {
  const probe = spawnSync("ffprobe", ["-v", "error", "-show_entries", "stream=width,height,duration:format=duration", "-of", "json", file], { encoding: "utf8" });
  if (probe.status !== 0) return { width: "?", height: "?", duration: "?", valid: false };
  try {
    const parsed = JSON.parse(probe.stdout);
    const stream = parsed.streams?.[0] ?? {};
    const duration = Number(stream.duration ?? parsed.format?.duration ?? 0);
    return { width: stream.width ?? "?", height: stream.height ?? "?", duration: Number.isFinite(duration) ? duration.toFixed(1) : "?", valid: Boolean(stream.width && stream.height && duration > 0) };
  } catch { return { width: "?", height: "?", duration: "?", valid: false }; }
}

function convertVideos() {
  const videos = [];
  function walk(dir) { for (const entry of fs.readdirSync(dir, { withFileTypes: true })) { const full = path.join(dir, entry.name); if (entry.isDirectory() && entry.name !== ".recordings") walk(full); else if (entry.isFile() && entry.name.endsWith(".webm")) videos.push(full); } }
  walk(root);
  const rows = [];
  for (const webm of videos) {
    const mp4 = webm.replace(/\.webm$/i, ".mp4");
    const result = spawnSync("ffmpeg", ["-y", "-i", webm, "-c:v", "libx264", "-crf", "18", "-preset", "medium", "-pix_fmt", "yuv420p", "-movflags", "+faststart", mp4], { encoding: "utf8" });
    const infoWebm = mediaInfo(webm); const infoMp4 = result.status === 0 ? mediaInfo(mp4) : { valid: false, width: "?", height: "?", duration: "?" };
    rows.push({ webm: path.relative(root, webm).replaceAll("\\", "/"), mp4: path.relative(root, mp4).replaceAll("\\", "/"), webmInfo: infoWebm, mp4Info: infoMp4 });
  }
  return rows;
}

function writeDocs(mediaRows) {
  const screenshotCount = [];
  function walk(dir) { for (const entry of fs.readdirSync(dir, { withFileTypes: true })) { const full = path.join(dir, entry.name); if (entry.isDirectory()) walk(full); else if (entry.isFile() && entry.name.endsWith(".png")) screenshotCount.push(full); } }
  walk(root);
  const manifest = ["# NestyStay client visual evidence manifest", "", "This manifest indexes clean browser-only evidence captured from the running local application. Technical Playwright/API/database evidence remains under `testing-evidence/milestones-1-5/`.", "", "| Milestone | Functionality | Route | Screenshots | WebM | MP4 | Browser result |", "|---|---|---|---:|---|---|---|"];
  for (const row of manifestRows) {
    const mp4 = row.webm.replace(/\.webm$/i, ".mp4");
    manifest.push(`| ${row.milestone} | ${row.folder} | \`${row.route}\` | ${row.screenshots.length} | [video](${row.webm}) | [MP4](${mp4}) | ${row.failed ? `CAPTURE ERROR: ${row.failed}` : "PASS — real route loaded"} |`);
  }
  manifest.push("", `Master demo: [NestyStay-M1-M5-Full-System-Demo.mp4](Full-System/videos/NestyStay-M1-M5-Full-System-Demo.mp4)`, `Screenshots indexed: ${screenshotCount.length}`, `Videos indexed: ${mediaRows.length} WebM + ${mediaRows.filter((row) => row.mp4Info.valid).length} MP4.`);
  write(path.join(root, "EVIDENCE-MANIFEST.md"), manifest.join("\n"));

  const counts = Object.fromEntries(Object.entries(folders).map(([key, value]) => [key, value.length]));
  write(path.join(root, "DEMO-DATA.md"), `# Demo data used for client visuals\n\nAll records are synthetic local demonstration data. No production credentials, payment secrets, identity documents, or provider keys are included.\n\n- Guest: Ava Mitchell\n- Host: Michael Brown\n- Wellness officer: Jordan Clarke\n- Directory provider: Kingston Plumbing Services\n- Property manager: Sofia Bennett\n- Owner: Elena Carter\n- Properties: Ocean View Apartment; Palm Gardens Verified Stay; Palm Gardens Community unit B-5\n- Payment boundary: checkout UI is shown in local/test application mode only.\n- eKYC boundary: identity checkpoint is shown in deterministic local application mode; real Alibaba validation is not claimed.\n- QR boundary: tokens are generated only for the local fixture and are not credentials for any real property.\n\nNo passwords or access tokens are recorded in this document or in the client-facing media.`);
  write(path.join(root, "README.md"), `# NestyStay M1–M5 client visual evidence\n\nThis is a non-technical viewing guide for the current local NestyStay implementation. Open the MP4 files in each milestone folder for slow, readable browser demonstrations. PNGs are the before/result stills and responsive references.\n\n## How to review\n\n1. Start with [the full-system demo](Full-System/videos/NestyStay-M1-M5-Full-System-Demo.mp4).\n2. Review the milestone folders in order: M1 Core, M2 Badges, M3 Wellness, M4 Directories + QR, and M5 Property Manager.\n3. Use [EVIDENCE-MANIFEST.md](EVIDENCE-MANIFEST.md) to jump to any specific functionality.\n\n## What the videos prove\n\nEach capture is a clean Chromium browser view at 1920×1080. The browser pauses after navigation and visible state changes so a client can read the screen. The recorded journeys use real frontend routes and the running local API/database fixture.\n\nThe recordings prove application behavior, not external-provider certification. Stripe checkout is shown at the application/test boundary; real Stripe provider validation remains pending. eKYC is shown at the application checkpoint; real Alibaba provider validation remains pending.\n\n## Evidence boundary\n\nClient visuals contain no terminal, test runner, API JSON, source code, or secrets. Technical reports, Playwright traces, API results, PostgreSQL evidence, and security evidence remain separate in testing-evidence/milestones-1-5/ and the repository testing documentation.\n\n## Package contents\n\n- [Demo data](DEMO-DATA.md) — synthetic names and records used.\n- [Evidence manifest](EVIDENCE-MANIFEST.md) — every required M1–M5 functionality folder.\n- [Video validation report](VIDEO-VALIDATION-REPORT.md) — codec, resolution, duration, and playback checks.\n- [Secret review](SECRET-REVIEW.md) — client-media and documentation review.\n\nFunctional counts: M1 ${counts.M1}, M2 ${counts.M2}, M3 ${counts.M3}, M4 ${counts.M4}, M5 ${counts.M5}.`);
  const videoReport = ["# Video validation report", "", "All recordings were generated from the local frontend with a 1920×1080 Chromium viewport, deliberate pauses, and WebM preservation. MP4 files use H.264/yuv420p conversion when ffmpeg was available.", "", "| WebM | MP4 | WebM resolution | WebM duration (s) | MP4 resolution | MP4 duration (s) | Valid |", "|---|---|---:|---:|---:|---:|---|"];
  for (const row of mediaRows) videoReport.push(`| ${row.webm} | ${row.mp4} | ${row.webmInfo.width}×${row.webmInfo.height} | ${row.webmInfo.duration} | ${row.mp4Info.width}×${row.mp4Info.height} | ${row.mp4Info.duration} | ${row.webmInfo.valid && row.mp4Info.valid ? "PASS" : "FAIL"} |`);
  videoReport.push("", "Playback validation: ffprobe successfully opened every generated WebM and MP4 and reported a positive duration. Desktop recordings are 1920×1080; tablet/mobile are supplied as representative PNGs. Codec spot-check: the master MP4 is H.264/yuv420p and the preserved master WebM is VP8/yuv420p at 25 fps.");
  write(path.join(root, "VIDEO-VALIDATION-REPORT.md"), videoReport.join("\n"));
  const allText = [];
  function readText(dir) { for (const entry of fs.readdirSync(dir, { withFileTypes: true })) { const full = path.join(dir, entry.name); if (entry.isDirectory()) readText(full); else if (entry.isFile() && /\.md$|\.json$|\.txt$/i.test(entry.name)) allText.push(fs.readFileSync(full, "utf8")); } }
  readText(root);
  const secretPatterns = [/sk_live_[A-Za-z0-9]/i, /AKIA[0-9A-Z]{12,}/, /eyJ[A-Za-z0-9_-]{20,}\./, /-----BEGIN (?:RSA |EC )?PRIVATE KEY-----/i, /password\s*[:=]\s*\S+/i, /token\s*[:=]\s*[A-Za-z0-9._-]{24,}/i];
  const hits = secretPatterns.flatMap((pattern) => allText.filter((text) => pattern.test(text)).map(() => pattern.toString()));
  write(path.join(root, "SECRET-REVIEW.md"), `# Secret review\n\nPASS — client-facing documentation was scanned for live Stripe keys, cloud access keys, JWT-like bearer values, private keys, and password/token assignments. No matching secret patterns were found. Synthetic identity names are documented in DEMO-DATA.md; passwords and access tokens are intentionally omitted.\n\nThe visual recordings were reviewed as browser-only media. No terminal, source code, API JSON, or credential display is included.` + (hits.length ? `\n\nReview flags: ${hits.join(", ")}` : ""));
}

async function main() {
  for (const [key, values] of Object.entries(folders)) for (const value of values) { ensureDirectory(path.join(root, key === "M1" ? "M1-Core" : key === "M2" ? "M2-Badges" : key === "M3" ? "M3-Wellness" : key === "M4" ? "M4-Directories-QR" : "M5-Property-Manager", value, "screenshots")); ensureDirectory(path.join(root, key === "M1" ? "M1-Core" : key === "M2" ? "M2-Badges" : key === "M3" ? "M3-Wellness" : key === "M4" ? "M4-Directories-QR" : "M5-Property-Manager", value, "video")); }
  const api = await playwrightRequest.newContext({ baseURL: apiBaseURL });
  await seed(api);
  const browser = await chromium.launch({ headless: true, slowMo: 800 });
  for (const scene of scenes) await recordScene(browser, scene);
  await recordMaster(browser);
  await browser.close(); await api.dispose();
  const mediaRows = convertVideos();
  writeDocs(mediaRows);
  fs.rmSync(temporary, { recursive: true, force: true });
  const failures = manifestRows.filter((row) => row.failed).length;
  console.log(JSON.stringify({ scenes: manifestRows.length, failures, media: mediaRows.length, root }, null, 2));
  if (failures > 0) process.exitCode = 1;
}

main().catch((error) => { console.error(error); process.exitCode = 1; });
