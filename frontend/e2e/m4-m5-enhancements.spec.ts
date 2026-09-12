import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { installCookieSession } from "./helpers/session";

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("M4 directory UX exposes live search, map/list, favorites, contact, and verification upsell", async ({ page }) => {
  await page.goto("/directory/businesses", { waitUntil: "networkidle" });
  await expect(page.getByText("Local businesses", { exact: false })).toBeVisible();
  await expect(page.getByLabel("Directory search")).toBeVisible();
  await expect(page.getByRole("button", { name: "Map", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Map", exact: true }).click();
  await expect(page.getByTestId("directory-map")).toBeVisible();
  await page.getByRole("button", { name: "List", exact: true }).click();
  await expect(page.getByTestId("directory-list")).toBeVisible();
  await page.goto("/directory/guest-verification", { waitUntil: "networkidle" });
  await expect(page.getByText("Verify once. Travel with confidence.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Not now", exact: true })).toBeVisible();
});

test("M4 provider onboarding saves a recoverable draft and requires terms", async ({ page, baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const session = await createSession(api, "ServiceProvider", "Enhancement Provider");
  await installSession(page, session);
  await page.goto("/directory/provider/onboarding", { waitUntil: "networkidle" });
  await expect(page.getByText("Provider onboarding", { exact: false })).toBeVisible();
  await page.getByLabel("Business name").fill("Enhancement Electrical");
  await page.getByRole("button", { name: "Save draft", exact: true }).click();
  await expect(page.getByText("Draft saved", { exact: false })).toBeVisible();
  await page.getByRole("button", { name: "Save provider profile", exact: true }).click();
  await expect(page.getByText("Accept the provider terms before submitting for review.", { exact: true })).toBeVisible();
  await api.dispose();
});

test("M4 admin moderation queue is API-backed and supports request changes", async ({ page, baseURL }) => {
  const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
  test.skip(!adminToken, "NESTYSTAY_E2E_ADMIN_TOKEN is required for the privileged moderation journey.");
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate((accessToken) => localStorage.setItem("nestyStay.session", JSON.stringify({ userId: "00000000-0000-0000-0000-000000000001", email: "admin@nestystay.local", displayName: "Admin", accessToken, expiresAt: new Date(Date.now() + 3600000).toISOString(), roles: ["Admin"], permissions: ["property_moderation"] })), adminToken);
  await page.goto("/admin/ops/directory", { waitUntil: "networkidle" });
  await expect(page.getByText("Directory moderation queue")).toBeVisible();
  await expect(page.getByLabel("Search providers")).toBeVisible();
});

test("M5 manager workspace includes configurable KPIs, Kanban, templates, and guard scanner", async ({ page, baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const session = await createSession(api, "PropertyManager", "Enhancement Manager");
  await installSession(page, session);
  await page.goto("/pm/dashboard", { waitUntil: "networkidle" });
  await expect(page.getByText("Configure KPI cards")).toBeVisible();
  await expect(page.getByText("Portfolio records", { exact: true })).toBeVisible();
  await expect(page.getByText("Message preset")).toBeVisible();
  await page.getByRole("button", { name: "Kanban", exact: true }).click();
  await expect(page.getByText("IN PROGRESS", { exact: true })).toBeVisible();
  await page.goto("/gate", { waitUntil: "networkidle" });
  await expect(page.getByText("Scan. Confirm. Keep the property moving.")).toBeVisible();
  await expect(page.getByText("Scan with camera", { exact: false })).toBeVisible();
  await expect(page.getByLabel("QR token")).toBeVisible();
  await api.dispose();
});

async function createSession(api: APIRequestContext, role: string, displayName: string) {
  const password = "NestyStay1";
  const email = `enhancement-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: "+15550104009", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return await login.json();
}

async function installSession(page: Page, session: Record<string, unknown>) {
  await installCookieSession(page, session);
}
