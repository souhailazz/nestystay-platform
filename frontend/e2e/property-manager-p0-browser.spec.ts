import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { randomUUID } from "node:crypto";

async function registerAndPrepare(api: APIRequestContext) {
  const suffix = randomUUID();
  const password = "NestyStay1";
  const managerEmail = `p0-manager-${suffix}@nestystay.local`;
  const ownerEmail = `p0-owner-${suffix}@nestystay.local`;
  const manager = await api.post("/api/auth/register", {
    data: {
      email: managerEmail,
      password,
      confirmPassword: password,
      displayName: "P0 Browser Manager",
      phone: "+15550104201",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role: "PropertyManager",
    },
  });
  expect(manager.ok(), await manager.text()).toBeTruthy();
  const owner = await api.post("/api/auth/register", {
    data: {
      email: ownerEmail,
      password,
      confirmPassword: password,
      displayName: "P0 Browser Owner",
      phone: "+15550104202",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role: "Owner",
    },
  });
  expect(owner.ok(), await owner.text()).toBeTruthy();
  const ownerId = (await owner.json()).userId as string;
  const login = await api.post("/api/auth/login", {
    data: { email: managerEmail, password },
  });
  expect(login.ok(), await login.text()).toBeTruthy();
  const accessToken = (await login.json()).accessToken as string;
  const headers = { Authorization: `Bearer ${accessToken}` };
  const invited = await api.post("/api/property-manager/owners", {
    headers,
    data: { email: ownerEmail, displayName: "P0 Browser Owner" },
  });
  expect(invited.ok(), await invited.text()).toBeTruthy();
  const property = await api.post("/api/property-manager/properties", {
    headers,
    data: {
      ownerUserId: ownerId,
      title: "P0 Browser Apartment",
      unitNumber: "P0-B",
      address: "Kingston",
    },
  });
  expect(property.ok(), await property.text()).toBeTruthy();
  return { managerEmail, ownerEmail, password, propertyId: (await property.json()).id as string };
}

async function loginUi(page: Page, email: string, password: string) {
  await page.goto("/login");
  const form = page.locator("form").filter({ has: page.getByRole("heading", { name: "Log in", exact: true }) });
  await form.getByLabel("Email", { exact: true }).fill(email);
  await form.getByLabel("Password", { exact: true }).fill(password);
  const response = page.waitForResponse((item) => item.url().endsWith("/api/auth/login") && item.request().method() === "POST");
  await form.getByRole("button", { name: "Log in", exact: false }).click();
  expect((await response).ok()).toBeTruthy();
  await expect.poll(async () => (await page.context().cookies()).some((cookie) => cookie.httpOnly && cookie.name.includes("session"))).toBe(true);
}

test("manager-to-owner P0 financial workspace persists through the real UI", async ({ page, request }) => {
  const data = await registerAndPrepare(request);
  await loginUi(page, data.managerEmail, data.password);
  await page.goto("/pm/p0");
  await expect(page.getByRole("heading", { name: /Run every owner relationship from one controlled workspace/ })).toBeVisible();

  const profile = page.locator("article").filter({ has: page.getByRole("heading", { name: "Owner profile & lifecycle", exact: true }) });
  await profile.getByLabel("Legal name", { exact: true }).fill("P0 Browser Owner Ltd");
  await profile.getByLabel("Billing email", { exact: true }).fill(data.ownerEmail);
  await profile.getByLabel(/Billing metadata JSON/).fill('{"invoiceDay":15}');
  await profile.getByLabel(/Payment provider customer reference/).fill("cus_local_p0_browser");
  const profileResponse = page.waitForResponse((item) => item.url().includes("/api/property-manager/p0/owners/") && item.url().endsWith("/profile") && item.request().method() === "PUT");
  await profile.getByRole("button", { name: "Save owner profile", exact: true }).click();
  expect((await profileResponse).ok()).toBeTruthy();
  await expect(page.getByRole("status")).toContainText("Owner profile saved");

  const agreements = page.locator("article").filter({ has: page.getByRole("heading", { name: "Management agreements", exact: true }) });
  const createAgreement = page.waitForResponse((item) => item.url().endsWith("/api/property-manager/p0/agreements") && item.request().method() === "POST");
  await agreements.getByRole("button", { name: "Create draft agreement", exact: true }).click();
  expect((await createAgreement).ok()).toBeTruthy();
  await expect(agreements.getByText(/v1 · DRAFT/, { exact: false })).toBeVisible();
  const activateAgreement = page.waitForResponse((item) => item.url().includes("/api/property-manager/p0/agreements/") && item.url().endsWith("/activate") && item.request().method() === "POST");
  await agreements.getByRole("button", { name: "Activate", exact: true }).click();
  const activationResponse = await activateAgreement;
  expect(activationResponse.ok(), await activationResponse.text()).toBeTruthy();
  await expect(agreements.getByText(/v1 · ACTIVE/, { exact: false })).toBeVisible();

  const ledger = page.locator("article").filter({ has: page.getByRole("heading", { name: "Auditable owner ledger", exact: true }) });
  await ledger.getByLabel("Amount", { exact: true }).fill("125");
  const journalResponse = page.waitForResponse((item) => item.url().endsWith("/api/property-manager/p0/accounting/journals") && item.request().method() === "POST");
  await ledger.getByRole("button", { name: "Post balanced entry", exact: true }).click();
  expect((await journalResponse).ok()).toBeTruthy();
  await expect(ledger.getByText("RECONCILED", { exact: true }).first()).toBeVisible();

  const statements = page.locator("article").filter({ has: page.getByRole("heading", { name: "Statements & profitability", exact: true }) });
  const statementResponse = page.waitForResponse((item) => item.url().includes("/api/property-manager/p0/statements?") && item.request().method() === "GET");
  await statements.getByRole("button", { name: "Build preview", exact: true }).click();
  expect((await statementResponse).ok()).toBeTruthy();
  await expect(statements.getByText("Income", { exact: false })).toBeVisible();

  await page.context().clearCookies();
  await page.evaluate(() => localStorage.removeItem("nestyStay.session"));
  await loginUi(page, data.ownerEmail, data.password);
  await page.goto("/owner/p0");
  await expect(page.getByRole("heading", { name: "Your agreements, approvals and money", exact: true })).toBeVisible();
  await expect(page.getByText("P0 Browser Apartment · P0-B", { exact: true })).toBeVisible();
  await expect(page.getByText("P0 Browser Owner Ltd", { exact: false })).toHaveCount(0);
});
