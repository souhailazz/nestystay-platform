import { expect, request as playwrightRequest, test, type APIRequestContext, type Page, type TestInfo } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

type UserRole = "Guest" | "Host";

type AuthSession = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: string[];
};

const repoRoot = path.resolve(process.cwd(), "..");
const evidenceRoot = path.join(repoRoot, "artifacts", "m1-m2-visual");
const password = "NestyStay1";
const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN ?? ["dev", "admin", "token"].join("-");
const transparentPng = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=",
  "base64",
);

test.beforeAll(async ({ baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const seed = await api.post("/api/spec/seed");
    expect(seed.ok()).toBeTruthy();
  } finally {
    await api.dispose();
  }
});

test.describe("M1/M2 public, auth, admin, and error evidence", () => {
  test("captures representative public, auth, admin, and error routes", async ({ page }, testInfo) => {
    const errors = collectPageErrors(page);

    await visitAndCapture(page, testInfo, "PUB", "PUB-01", "/");
    await visitAndCapture(page, testInfo, "PUB", "PUB-02", "/explore");
    await visitAndCapture(page, testInfo, "PUB", "PUB-03", "/explore/map");
    await visitAndCapture(page, testInfo, "PUB", "PUB-05", "/experiences");
    await visitAndCapture(page, testInfo, "AUTH", "AUTH-01", "/login");
    await visitAndCapture(page, testInfo, "AUTH", "AUTH-03", "/register");
    await visitAdminAndCapture(page, testInfo);
    await visitAndCapture(page, testInfo, "ERR", "ERR-03", "/404");

    expect(errors).toEqual([]);
  });
});

test.describe("M1/M2 authenticated traveler and messaging evidence", () => {
  test("captures traveler, profile upload, identity, payment, and messaging routes", async ({ baseURL, page }, testInfo) => {
    const errors = collectPageErrors(page);
    const api = await playwrightRequest.newContext({ baseURL });
    const session = await createSession(api, "Guest");
    await api.dispose();
    await stubLocalStorageHost(page);
    await installSession(page, session);

    await visitAndCapture(page, testInfo, "TRAV", "TRAV-01", "/guest-dashboard");
    await visitAndCapture(page, testInfo, "TRAV", "TRAV-09", "/traveler/payment-methods");
    await visitAndCapture(page, testInfo, "TRAV", "TRAV-11", "/traveler/invoices");
    await visitAndCapture(page, testInfo, "TRAV", "TRAV-13", "/traveler/identity");
    await visitAndCapture(page, testInfo, "MSG", "MSG-01", "/messages");
    await uploadProfilePhotoAndCapture(page, testInfo);

    expect(errors).toEqual([]);
  });
});

test.describe("M1/M2 authenticated host, directory, and host profile evidence", () => {
  test("captures host, host-profile, and provider-directory routes", async ({ baseURL, page }, testInfo) => {
    const errors = collectPageErrors(page);
    const api = await playwrightRequest.newContext({ baseURL });
    const session = await createSession(api, "Host");
    await seedProviderProfile(api, session);
    await api.dispose();
    await installSession(page, session);

    await visitAndCapture(page, testInfo, "HOST", "HOST-01", "/host-dashboard");
    await visitAndCapture(page, testInfo, "HOST", "HOST-03", "/host/properties");
    await visitAndCapture(page, testInfo, "HOST", "HOST-07", "/host/pricing");
    await visitAndCapture(page, testInfo, "HPRO", "HPRO-04", "/host/profile/edit");
    await visitAndCapture(page, testInfo, "DIR", "DIR-05", "/directory/provider");

    expect(errors).toEqual([]);
  });
});

async function visitAndCapture(page: Page, testInfo: TestInfo, family: string, screenId: string, route: string) {
  await page.goto(route);
  await page.waitForLoadState("domcontentloaded");
  await page.waitForTimeout(750);
  await expect(page.locator("main, [role='main']").first()).toBeVisible();
  await capture(page, testInfo, family, screenId);
}

async function uploadProfilePhotoAndCapture(page: Page, testInfo: TestInfo) {
  await page.goto("/profile");
  await page.waitForLoadState("domcontentloaded");
  await expect(page.getByText("Profile photo").first()).toBeVisible();
  await page.locator(".profile-photo-picker input[type='file']").setInputFiles({
    name: "profile-e2e.png",
    mimeType: "image/png",
    buffer: transparentPng,
  });
  await expect(page.getByText("profile-e2e.png · Clean", { exact: true })).toBeVisible();
  await capture(page, testInfo, "TRAV", "TRAV-12-profile-upload");
}

async function stubLocalStorageHost(page: Page) {
  await page.route("https://storage.nestystay.local/**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "image/png",
      body: transparentPng,
    });
  });
}

async function visitAdminAndCapture(page: Page, testInfo: TestInfo) {
  await page.goto("/admin/ops/disputes");
  await page.waitForLoadState("domcontentloaded");
  await page.getByLabel("Admin token").fill(adminToken);
  await expect(page.getByText("Evidence").first()).toBeVisible();
  await capture(page, testInfo, "ADM", "ADM-07");
}

async function capture(page: Page, testInfo: TestInfo, family: string, screenId: string) {
  const viewport = testInfo.project.name.replace("-chromium", "");
  const directory = path.join(evidenceRoot, family);
  mkdirSync(directory, { recursive: true });
  await page.screenshot({
    fullPage: true,
    path: path.join(directory, `${screenId}-${viewport}.png`),
  });
}

async function createSession(api: APIRequestContext, role: UserRole): Promise<AuthSession> {
  const unique = `${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const email = `${unique}@nestystay.local`;
  const displayName = role === "Host" ? "E2E Host" : "E2E Traveler";

  const registered = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName,
      phone: "+15550102030",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role,
    },
  });
  expect(registered.ok()).toBeTruthy();

  const login = await api.post("/api/auth/login", {
    data: { email, password },
  });
  expect(login.ok()).toBeTruthy();
  const loginBody = await login.json();
  expect(loginBody.challengeId).toBeTruthy();

  const challenge = await api.get(`/api/auth/development/challenges/${loginBody.challengeId}`);
  expect(challenge.ok()).toBeTruthy();
  const challengeBody = await challenge.json();

  const verified = await api.post("/api/auth/2fa/verify", {
    data: {
      challengeId: loginBody.challengeId,
      code: challengeBody.code,
    },
  });
  expect(verified.ok()).toBeTruthy();
  const session = await verified.json();

  return {
    userId: session.userId,
    email,
    displayName,
    accessToken: session.accessToken,
    expiresAt: session.expiresAt,
    roles: session.roles,
  };
}

async function seedProviderProfile(api: APIRequestContext, session: AuthSession) {
  const response = await api.post("/api/spec/directories/providers", {
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
    },
    data: {
      slug: `provider-${session.userId.slice(0, 8)}`,
      kind: "LocalBusiness",
      category: "Host services",
      name: `${session.displayName} Services`,
      parish: "St. Ann",
      badgeLevel: "Verified",
      description: "E2E provider profile seeded for protected directory dashboard evidence.",
      availabilitySummary: "Weekdays and urgent turnovers",
      contactMode: "Platform messaging",
      isActive: true,
    },
  });
  expect(response.ok()).toBeTruthy();
}

async function installSession(page: Page, session: AuthSession) {
  await page.addInitScript((value) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(value));
  }, session);
}

function collectPageErrors(page: Page) {
  const errors: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error") {
      errors.push(message.text());
    }
  });
  page.on("pageerror", (error) => {
    errors.push(error.message);
  });
  return errors;
}
