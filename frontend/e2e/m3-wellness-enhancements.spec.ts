import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { installCookieSession } from "./helpers/session";

type Session = { userId: string; email: string; displayName: string; accessToken: string; expiresAt?: string; roles: string[]; permissions: string[] };
const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 120_000 });

test("M3 host wellness UX: plan comparison, service comparison, live quote journey", async ({ baseURL, page }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const host = await createSession(api, "Host", "M3 Wellness UX Host");
  const propertyResponse = await api.post("/api/properties", { headers: { Authorization: `Bearer ${host.accessToken}` }, data: { hostUserId: host.userId, hostName: host.displayName, hostEmail: host.email, title: `M3 UX Villa ${Date.now()}`, location: "Ocho Rios", country: "Jamaica", nightlyRate: 150, currency: "USD", badgeLevel: "Free", guestVerificationEnabled: false, insuraGuestEnabled: false, cancellationPolicy: "Flexible" } });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  await installSession(page, host);
  await page.goto("/host/wellness", { waitUntil: "networkidle" });
  await expect(page.getByRole("heading", { name: "Wellness visits", exact: true })).toBeVisible();
  await page.getByRole("button", { name: /Compare plans/i }).click();
  await expect(page.getByTestId("wellness-plan-comparison")).toBeVisible();
  await expect(page.getByText(/Pay as you go/i)).toBeVisible();
  await expect(page.getByTestId("wellness-service-comparison")).toBeVisible();
  await page.getByRole("button", { name: /Drive-by patrol/i }).click();
  await expect(page.getByText(/Step 1 of 3/i)).toBeVisible();
  await page.getByRole("button", { name: "Get quote" }).click();
  await expect(page.getByText(/Wellness quote is eligible|Wellness is locked/i)).toBeVisible({ timeout: 30_000 });
  await api.dispose();
});

test("M3 officer UX: onboarding wizard saves and resumes a privacy-aware draft", async ({ baseURL, page }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const officer = await createSession(api, "Officer", "M3 Wellness UX Officer");
  await installSession(page, officer);
  await page.goto("/officer/wellness", { waitUntil: "networkidle" });
  await expect(page.getByRole("heading", { name: "Officer wellness", exact: true })).toBeVisible();
  await page.getByRole("textbox", { name: "JCF badge number" }).fill(`UX-${Date.now()}`);
  await page.getByRole("textbox", { name: "Coverage zone" }).fill("Montego Bay north");
  await page.getByRole("button", { name: /Continue/i }).click();
  await expect(page.getByText(/Step 2 of 3/i)).toBeVisible();
  // Required evidence is uploaded after the application is created. The
  // wizard must still retain its step and draft state while the upload
  // control remains intentionally disabled until an officer record exists.
  await expect(page.getByRole("button", { name: "Upload Government-issued ID" })).toBeDisabled();
  await page.reload({ waitUntil: "networkidle" });
  await expect(page.getByText(/Step 2 of 3/i)).toBeVisible();
  await expect(page.getByRole("button", { name: "Upload Government-issued ID" })).toBeDisabled();
  await page.getByRole("button", { name: /Continue/i }).click();
  await expect(page.getByText(/Step 3 of 3/i)).toBeVisible();
  await expect(page.getByText(/consent to NestyStay securely processing/i)).toBeVisible();
  await expect(page.getByText(/Hosts see badge ID only/i)).toBeVisible();
  await page.getByRole("checkbox", { name: /I consent to NestyStay/i }).check();
  await page.getByRole("button", { name: /Submit for verification/i }).click();
  await expect(page.getByText(/onboarding is Pending/i)).toBeVisible({ timeout: 30_000 });
  await page.getByRole("button", { name: "Back", exact: true }).click();
  await page.getByRole("button", { name: "Back", exact: true }).click();
  await page.getByRole("button", { name: /Continue/i }).click();
  await expect(page.getByRole("button", { name: "Upload Government-issued ID" })).toBeEnabled({ timeout: 30_000 });
  await page.getByLabel("Upload Government-issued ID").setInputFiles({ name: "government-id.png", mimeType: "image/png", buffer: Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=", "base64") });
  await expect(page.getByText(/uploaded securely/i)).toBeVisible({ timeout: 30_000 });
  await api.dispose();
});

test("M3 admin UX: live wellness operations route exposes review, KPI, and payout controls", async ({ page }) => {
  const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
  test.skip(!adminToken, "NESTYSTAY_E2E_ADMIN_TOKEN is required for the authenticated admin browser journey.");
  await installCookieSession(page, {
    userId: "00000000-0000-0000-0000-000000000001",
    email: "local-admin@nestystay.local",
    displayName: "Local admin",
    accessToken: adminToken,
    expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    roles: ["Admin"],
    permissions: ["super_administration", "officer_management", "financial_reporting", "system_configuration", "user_management", "audit_log_access"],
  });
  await page.goto("/admin/ops/wellness", { waitUntil: "networkidle" });
  await expect(page.getByRole("heading", { name: "Platform operations", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Wellness operations", exact: true })).toBeVisible();
  await expect(page.getByText("Side-by-side application review", { exact: true })).toBeVisible();
  await expect(page.getByText("Commission & payout desk", { exact: true })).toBeVisible();
});

async function createSession(api: APIRequestContext, role: "Host" | "Officer", displayName: string): Promise<Session> {
  const email = `m3-enhancement-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const phone = `+1555${String(Date.now()).slice(-7)}${Math.floor(Math.random() * 10)}`;
  const registration = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone, acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as Session;
  return { ...body, email, displayName, accessToken: body.accessToken };
}

async function installSession(page: Page, session: Session) {
  await installCookieSession(page, session);
}
