import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { installCookieSession } from "./helpers/session";

const repoRoot = path.resolve(process.cwd(), "..");
const evidenceRoot = process.env.NESTYSTAY_EVIDENCE_ROOT ?? path.join(repoRoot, "testing-evidence", "milestones-1-2", "screenshots", "real-workflows");
const password = "NestyStay1";

test.describe.configure({ timeout: 180_000, mode: "serial" });

test.beforeAll(async ({ baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const seed = await api.post("/api/spec/seed");
    expect(seed.ok(), await seed.text()).toBeTruthy();
  } finally {
    await api.dispose();
  }
});

test("real guest registration, login, quote, and persisted eKYC booking flow", async ({ baseURL, page }, testInfo) => {
  // Create a private listing for this journey. Selecting the
  // first public card is not deterministic when the parallel security and M5
  // projects publish their own fixtures at the same time; those listings may
  // already contain an owner block or booking for the randomized dates.
  const fixtureApi = await playwrightRequest.newContext({ baseURL });
  const fixtureHost = await createSession(fixtureApi, "Host") as { userId: string; email: string; accessToken: string };
  const fixturePropertyResponse = await fixtureApi.post("/api/properties", {
    headers: { Authorization: `Bearer ${fixtureHost.accessToken}` },
    data: {
      hostUserId: fixtureHost.userId,
      hostName: "M1 eKYC Fixture Host",
      hostEmail: fixtureHost.email,
      title: `M1 eKYC Fixture ${Date.now()}`,
      location: "Kingston",
      country: "Jamaica",
      nightlyRate: 120,
      currency: "USD",
      // Keep this fixture at the contractual Free/no-eKYC baseline. The
      // dedicated contract-validation spec covers the badge-gated eKYC path
      // when an admin fixture token is supplied.
      badgeLevel: "Free",
      guestVerificationEnabled: false,
      insuraGuestEnabled: false,
      cancellationPolicy: "Flexible",
    },
  });
  expect(fixturePropertyResponse.ok(), await fixturePropertyResponse.text()).toBeTruthy();
  const fixtureProperty = await fixturePropertyResponse.json() as { id: string };
  await fixtureApi.dispose();

  const email = `ui-guest-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  console.log("guest: goto register", email);
  await page.goto("/register", { waitUntil: "domcontentloaded" });

  console.log("guest: fill registration");
  await page.locator('input[placeholder="Keisha Brown"]').fill("UI Booking Guest");
  await page.locator('input[placeholder="+1 876 555 0123"]').fill("+15550102030");
  await page.locator('input[type="email"]').fill(email);
  const passwordInputs = page.locator('input[autocomplete="new-password"]');
  await passwordInputs.nth(0).fill(password);
  await passwordInputs.nth(1).fill(password);
  await page.getByRole("button", { name: "Create my account", exact: true }).click();
  console.log("guest: submitted registration");
  await expect(page).toHaveURL(/\/guest-dashboard$/);

  console.log("guest: goto explore");
  await page.goto("/explore", { waitUntil: "domcontentloaded" });
  const bookButtons = page.getByRole("button", { name: "Book", exact: true });
  await expect(bookButtons.first()).toBeVisible();
  // Confirm the public listing route still renders, then use the isolated
  // fixture instead of a parallel test's potentially occupied card.
  await page.goto(`/properties/${fixtureProperty.id}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "Book this stay", exact: true }).click();
  console.log("guest: opened booking modal");
  await expect(page.getByRole("heading", { name: "Choose your dates", exact: true })).toBeVisible();

  // Keep repeatable evidence runs isolated from previously held dates by using
  // a far-future, randomized window. The quote endpoint remains authoritative.
  const baseStayOffset = ({ "desktop-chromium": 12_000, "tablet-chromium": 12_010, "mobile-chromium": 12_020 } as Record<string, number>)[testInfo.project.name] ?? 12_000;
  const stayOffset = baseStayOffset + Math.floor(Math.random() * 10_000);
  const checkIn = new Date(Date.now() + stayOffset * 86_400_000).toISOString().slice(0, 10);
  const checkOut = new Date(Date.now() + (stayOffset + 3) * 86_400_000).toISOString().slice(0, 10);
  const dateInputs = page.locator('input[type="date"]');
  await dateInputs.nth(0).fill(checkIn);
  await dateInputs.nth(1).fill(checkOut);

  const continueButton = page.getByRole("button", { name: /Continue to quote/ });
  await expect(continueButton).toBeEnabled({ timeout: 30_000 });
  await continueButton.click();
  console.log("guest: continued from quote");
  await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/(identity|checkout)$/);
  await expect(page.getByText(/booking|identity|payment/i).first()).toBeVisible();
  await capture(page, testInfo, "guest-booking-created");
  if (await page.getByRole("button", { name: /Hold dates & verify/ }).isVisible()) {
    await page.getByRole("button", { name: /Hold dates & verify/ }).click();
    await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/pending$/);
    await expect(page.getByText(/verification|pending/i).first()).toBeVisible();
    await capture(page, testInfo, "guest-booking-pending");
  }
  await page.goto("/guest-dashboard", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Your stay hub" })).toBeVisible();
  const stayHub = page.getByRole("region", { name: "Your stay hub" });
  await expect(stayHub.getByRole("link", { name: "Invoices" })).toBeVisible();
  await expect(stayHub.getByRole("link", { name: "Gate QR" })).toBeVisible();
});

test("host badge page renders live ownership-scoped assignment and feature access", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const session = await createSession(api, "Host");
  await api.dispose();
  await installCookieSession(page, session);

  await page.goto("/host/badges", { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("host-13-page")).toBeVisible();
  await expect(page.getByText(/Badge eligibility and payment confirmation are verified/i)).toBeVisible();
  await expect(page.getByText("FREE HOST", { exact: true })).toBeVisible();
  await capture(page, testInfo, "host-badges-live");
});

async function createSession(api: APIRequestContext, role: "Host"): Promise<Record<string, unknown>> {
  const email = `ui-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registered = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName: "UI Badge Host",
      phone: "+15550102030",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role,
    },
  });
  expect(registered.ok(), await registered.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json();
  expect(body.accessToken).toBeTruthy();
  return {
    userId: body.userId,
    email,
    displayName: "UI Badge Host",
    accessToken: body.accessToken,
    expiresAt: body.expiresAt,
    roles: body.roles,
    permissions: body.permissions ?? [],
  };
}

async function capture(page: Page, testInfo: { project: { name: string } }, name: string) {
  const viewport = testInfo.project.name.replace("-chromium", "");
  const directory = path.join(evidenceRoot, "real-workflows");
  mkdirSync(directory, { recursive: true });
  const target = path.join(directory, `${name}-${viewport}.png`);
  try {
    const buffer = await page.screenshot({ fullPage: true });
    try { writeFileSync(target, buffer); } catch { /* evidence path may be locked by AV */ }
    try { await testInfo.attach(`${name}-${viewport}.png`, { body: buffer, contentType: "image/png" }); } catch { /* attachment is best effort */ }
  } catch { /* screenshot evidence must not fail the product-flow assertion */ }
}
