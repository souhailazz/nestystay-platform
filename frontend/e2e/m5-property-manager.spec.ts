import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";

type Session = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: string[];
  permissions: string[];
};

test.describe("Phase 5 property manager UI", () => {
  test("manager dashboard renders persisted owner, unit, invoice, and utility data", async ({ baseURL, page }) => {
    const api = await playwrightRequest.newContext({ baseURL });
    const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const managerEmail = `browser-pm-${suffix}@nestystay.local`;
    const ownerEmail = `browser-owner-${suffix}@nestystay.local`;
    const password = "NestyStay1";
    await register(api, managerEmail, "Browser Property Manager", password, "PropertyManager");
    await register(api, ownerEmail, "Browser Owner", password, "Owner");
    const manager = await login(api, managerEmail, password);
    const owner = await login(api, ownerEmail, password);

    const invited = await api.post("/api/property-manager/owners", {
      headers: { Authorization: `Bearer ${manager.accessToken}` },
      data: { email: ownerEmail, displayName: "Browser Owner" },
    });
    expect(invited.ok(), await invited.text()).toBeTruthy();
    const ownerRecord = await invited.json() as { ownerUserId: string };
    const propertyResponse = await api.post("/api/property-manager/properties", {
      headers: { Authorization: `Bearer ${manager.accessToken}` },
      data: { ownerUserId: ownerRecord.ownerUserId, title: "Browser Villa", unitNumber: "B-5", address: "Montego Bay" },
    });
    expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
    const property = await propertyResponse.json() as { id: string };
    const invoiceResponse = await api.post("/api/property-manager/invoices", {
      headers: { Authorization: `Bearer ${manager.accessToken}` },
      data: { ownerUserId: ownerRecord.ownerUserId, propertyId: property.id, dueDate: "2030-01-01", tax: 0, lines: [{ description: "Browser rent", quantity: 1, unitAmount: 125 }] },
    });
    expect(invoiceResponse.ok(), await invoiceResponse.text()).toBeTruthy();
    const utilityResponse = await api.post("/api/property-manager/utilities", {
      headers: { Authorization: `Bearer ${manager.accessToken}` },
      data: { ownerUserId: ownerRecord.ownerUserId, propertyId: property.id, utilityType: "Water", billingPeriod: "2030-01", usage: 10, rate: 2 },
    });
    expect(utilityResponse.ok(), await utilityResponse.text()).toBeTruthy();

    await setSession(page, manager);
    await page.goto("/pm/dashboard", { waitUntil: "networkidle" });
    await expect(page.getByText("One portfolio. Every owner, unit, and obligation.")).toBeVisible();
    await expect(page.locator("strong").filter({ hasText: "Browser Owner" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Browser Villa · B-5" })).toHaveCount(1);
    await expect(page.getByText(/PM-\d{8}-\d{4}/)).toBeVisible();
    await expect(page.getByText("PENDING").first()).toBeVisible();
    await expect(page.getByRole("button", { name: "Renew subscription" })).toBeVisible();

    await setSession(page, owner);
    await page.goto("/owner/dashboard", { waitUntil: "networkidle" });
    await expect(page.getByText("Your property account, in one place.")).toBeVisible();
    await expect(page.locator("strong").filter({ hasText: "Browser Villa · B-5" })).toBeVisible();
    await expect(page.getByText("Invoices & statements")).toBeVisible();
    await api.dispose();
  });

  test("gate validator displays the real API decision for an issued QR", async ({ baseURL, page }) => {
    const api = await playwrightRequest.newContext({ baseURL });
    const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const email = `browser-qr-pm-${suffix}@nestystay.local`;
    const password = "NestyStay1";
    await register(api, email, "QR Property Manager", password, "PropertyManager");
    const session = await login(api, email, password);
    const issued = await api.post("/api/property-manager/qr", {
      headers: { Authorization: `Bearer ${session.accessToken}` },
      data: { subjectType: "VENDOR", validFrom: new Date(Date.now() - 60_000).toISOString(), validUntil: new Date(Date.now() + 3_600_000).toISOString() },
    });
    expect(issued.ok(), await issued.text()).toBeTruthy();
    const qr = await issued.json() as { token: string };
    await setSession(page, session);
    await page.goto(`/gate?token=${encodeURIComponent(qr.token)}`, { waitUntil: "networkidle" });
    await page.getByRole("button", { name: "Validate access" }).click();
    await expect(page.getByRole("status")).toContainText("ACCESS APPROVED");
    await api.dispose();
  });
});

async function register(api: APIRequestContext, email: string, displayName: string, password: string, role: string) {
  const response = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName, phone: "+15550104009", acceptedTerms: true, acceptedPrivacy: true, role },
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  return await response.json() as { userId: string };
}

async function login(api: APIRequestContext, email: string, password: string): Promise<Session> {
  const response = await api.post("/api/auth/login", { data: { email, password } });
  expect(response.ok(), await response.text()).toBeTruthy();
  const loginBody = await response.json() as Session & { requiresTwoFactor: boolean; challengeId?: string };
  if (!loginBody.requiresTwoFactor) return loginBody;
  const challenge = await api.get(`/api/auth/development/challenges/${loginBody.challengeId}`);
  expect(challenge.ok(), await challenge.text()).toBeTruthy();
  const code = (await challenge.json() as { code: string }).code;
  const verified = await api.post("/api/auth/2fa/verify", { data: { challengeId: loginBody.challengeId, code } });
  expect(verified.ok(), await verified.text()).toBeTruthy();
  return { ...loginBody, ...await verified.json() as Session };
}

async function setSession(page: Page, session: Session) {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate(value => window.localStorage.setItem("nestyStay.session", JSON.stringify(value)), session);
}
