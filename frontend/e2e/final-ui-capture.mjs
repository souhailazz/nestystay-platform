import { chromium, request as playwrightRequest } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(process.cwd(), "..", "testing-evidence", "final-ui");
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:5173";
const apiURL = process.env.PLAYWRIGHT_API_URL?.replace(/\/api\/health\/?$/, "") ?? "http://127.0.0.1:5019";
const password = "NestyStay1";
const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN ?? "test-admin-token";

const routes = [
  ["M1", "01-explore", "/explore", "public"],
  ["M1", "02-property-detail", "/properties/11111111-1111-4111-8111-111111111111", "public"],
  ["M1", "03-registration", "/register", "public"],
  ["M1", "04-guest-dashboard", "/guest-dashboard", "guest"],
  ["M1", "05-host-properties", "/host/properties", "host"],
  ["M1", "06-host-calendar", "/calendar", "host"],
  ["M1", "07-booking-pending", "/booking/11111111-1111-4111-8111-111111111111/pending", "guest"],
  ["M2", "01-badge-levels", "/host/badges", "host"],
  ["M2", "02-badge-admin", "/admin/ops/pricebook", "admin"],
  ["M3", "01-wellness-host", "/host/wellness", "host"],
  ["M3", "02-wellness-officer", "/officer/wellness", "officer"],
  ["M3", "03-wellness-admin", "/admin/ops/wellness", "admin"],
  ["M4", "01-trades-directory", "/directory/trades", "public"],
  ["M4", "02-provider-workspace", "/directory/provider", "provider"],
  ["M4", "03-directory-admin", "/admin/ops/directories", "admin"],
  ["M4", "04-gate-qr", "/gate/qr", "public"],
  ["M5", "01-manager-dashboard", "/pm/dashboard", "pm"],
  ["M5", "02-owner-portal", "/owner/dashboard", "owner"],
  ["M5", "03-invoices", "/pm/invoices", "pm"],
  ["M5", "04-utilities", "/pm/utilities", "pm"],
  ["M5", "05-maintenance", "/pm/maintenance", "pm"],
  ["M5", "06-vendors", "/pm/maintenance", "pm"],
  ["M5", "07-community", "/pm/dashboard", "pm"],
  ["M5", "08-gate-messages", "/pm/gates", "pm"],
  ["M5", "09-governance", "/pm/governance", "pm"],
  ["M5", "10-documents", "/pm/documents", "pm"],
  ["M5", "11-calendar", "/pm/calendar", "pm"],
];

const sessions = { admin: {
  userId: "00000000-0000-0000-0000-000000000001",
  email: "client-admin@nestystay.local",
  displayName: "NestyStay Administrator",
  accessToken: adminToken,
  expiresAt: new Date(Date.now() + 86400000).toISOString(),
  roles: ["Admin"],
  permissions: ["super_administration", "officer_management", "financial_reporting", "system_configuration", "user_management", "dispute_management"],
} };

function ensureDirectory(dir) { fs.mkdirSync(dir, { recursive: true }); }

async function jsonRequest(api, method, url, body, token) {
  const options = { headers: token ? { Authorization: `Bearer ${token}` } : {} };
  if (body !== undefined) options.data = body;
  const response = await api[method](url, options);
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok()) throw new Error(`${method.toUpperCase()} ${url} ${response.status()}`);
  return data;
}

async function createSession(api, role, displayName) {
  const email = `final-ui-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  await jsonRequest(api, "post", "/api/auth/register", { email, password, confirmPassword: password, displayName, phone: "+15550104009", acceptedTerms: true, acceptedPrivacy: true, role });
  const login = await jsonRequest(api, "post", "/api/auth/login", { email, password });
  if (!login.requiresTwoFactor) return { ...login, email, displayName, permissions: login.permissions ?? [] };
  const challenge = await jsonRequest(api, "get", `/api/auth/development/challenges/${login.challengeId}`);
  const verified = await jsonRequest(api, "post", "/api/auth/2fa/verify", { challengeId: login.challengeId, code: challenge.code });
  return { ...verified, email, displayName, permissions: verified.permissions ?? [] };
}

async function capture(browser, route) {
  const [milestone, name, url, role] = route;
  for (const [viewportName, viewport] of [["desktop", { width: 1440, height: 1000 }], ["mobile", { width: 390, height: 844 }], ["tablet", { width: 768, height: 1024 }]]) {
    const context = await browser.newContext({ viewport, deviceScaleFactor: 1 });
    const page = await context.newPage();
    const session = role === "public" ? null : sessions[role];
    if (session) {
      // Browser auth is intentionally cookie-backed. Keep the non-secret
      // session metadata in localStorage and transfer only the local test
      // session cookie into this isolated browser context.
      await context.addCookies([{ name: "nestyStay.session", value: session.accessToken ?? "", domain: "127.0.0.1", path: "/" }]);
      await page.addInitScript((value) => window.localStorage.setItem("nestyStay.session", JSON.stringify(value)), { ...session, accessToken: "" });
    }
    const errors = [];
    page.on("pageerror", (error) => errors.push(error.message));
    try {
      await page.goto(`${baseURL}${url}`, { waitUntil: "domcontentloaded", timeout: 45000 });
      await page.waitForTimeout(2200);
      const destination = path.join(root, milestone, viewportName);
      ensureDirectory(destination);
      await page.screenshot({ path: path.join(destination, `${name}.png`), fullPage: false });
      console.log(JSON.stringify({ milestone, name, viewport: viewportName, url, role, result: "PASS", pageErrors: errors }));
    } catch (error) {
      console.log(JSON.stringify({ milestone, name, viewport: viewportName, url, role, result: "FAIL", error: error instanceof Error ? error.message : String(error), pageErrors: errors }));
      process.exitCode = 1;
    } finally {
      await context.close();
    }
  }
}

async function main() {
  ensureDirectory(root);
  const api = await playwrightRequest.newContext({ baseURL: apiURL });
  sessions.guest = await createSession(api, "Guest", "Ava Mitchell");
  sessions.host = await createSession(api, "Host", "Michael Brown");
  sessions.officer = await createSession(api, "Officer", "Jordan Clarke");
  sessions.provider = await createSession(api, "ServiceProvider", "Kingston Plumbing Services");
  sessions.pm = await createSession(api, "PropertyManager", "Sofia Bennett");
  sessions.owner = await createSession(api, "Owner", "Elena Carter");
  const hostToken = sessions.host.accessToken;
  await jsonRequest(api, "post", "/api/properties", {
    hostUserId: sessions.host.userId,
    hostName: sessions.host.displayName,
    hostEmail: sessions.host.email,
    title: "Palm Gardens Demo Stay",
    location: "Montego Bay",
    country: "Jamaica",
    nightlyRate: 180,
    currency: "USD",
    badgeLevel: "Verified",
    guestVerificationEnabled: false,
    insuraGuestEnabled: false,
    cancellationPolicy: "Flexible",
    highlights: ["Sea view", "Fast Wi-Fi", "Verified host"],
    parish: "St. James",
    description: "A clean local demonstration property.",
    bedrooms: 2,
    bathrooms: 2,
    maxGuests: 4,
    amenities: ["Wi-Fi", "Air conditioning"],
    sleepingArrangements: ["1 king bed", "1 sofa bed"],
    houseRules: ["No smoking"],
    cleaningFee: 30,
    serviceFee: 18,
    latitude: 18.4762,
    longitude: -77.8939,
  }, hostToken).catch(() => undefined);
  const ownerInvite = await jsonRequest(api, "post", "/api/property-manager/owners", { email: sessions.owner.email, displayName: sessions.owner.displayName }, sessions.pm.accessToken).catch(() => null);
  if (ownerInvite?.ownerUserId) {
    await jsonRequest(api, "post", "/api/property-manager/properties", { ownerUserId: ownerInvite.ownerUserId, title: "Palm Gardens Community", unitNumber: "B-5", address: "Montego Bay, Jamaica" }, sessions.pm.accessToken).catch(() => undefined);
  }
  const browser = await chromium.launch({ headless: true });
  for (const route of routes) await capture(browser, route);
  await browser.close();
  await api.dispose();
}

main().catch((error) => { console.error(error); process.exitCode = 1; });
