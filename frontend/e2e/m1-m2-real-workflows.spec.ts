import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

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

test("real guest registration, login, quote, and persisted eKYC booking flow", async ({ page }, testInfo) => {
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
  await bookButtons.first().click();
  console.log("guest: opened booking modal");
  await expect(page.getByRole("heading", { name: "Choose your dates", exact: true })).toBeVisible();

  const baseStayOffset = ({ "desktop-chromium": 2500, "tablet-chromium": 2510, "mobile-chromium": 2520 } as Record<string, number>)[testInfo.project.name] ?? 2500;
  const stayOffset = baseStayOffset + Math.floor(Math.random() * 1000);
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
  await page.addInitScript((value) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(value));
  }, session);

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
  await page.screenshot({ fullPage: true, path: path.join(directory, `${name}-${viewport}.png`) });
}
