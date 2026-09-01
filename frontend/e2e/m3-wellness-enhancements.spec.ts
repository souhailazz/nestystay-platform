import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";

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
  await page.getByRole("textbox", { name: "Coverage zone" }).fill("Montego Bay north");
  await page.getByRole("button", { name: /Continue/i }).click();
  await expect(page.getByText(/Step 2 of 3/i)).toBeVisible();
  await page.getByRole("checkbox", { name: "Government-issued ID" }).check();
  await page.reload({ waitUntil: "networkidle" });
  await expect(page.getByText(/Step 2 of 3/i)).toBeVisible();
  await expect(page.getByRole("checkbox", { name: "Government-issued ID" })).toBeChecked();
  await page.getByRole("button", { name: /Continue/i }).click();
  await expect(page.getByText(/Step 3 of 3/i)).toBeVisible();
  await expect(page.getByText(/consent to NestyStay securely processing/i)).toBeVisible();
  await expect(page.getByText(/Hosts see badge ID only/i)).toBeVisible();
  await api.dispose();
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
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate((value) => localStorage.setItem("nestyStay.session", JSON.stringify(value)), session);
}
