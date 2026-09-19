import { expect, request as playwrightRequest, test, type APIRequestContext } from "@playwright/test";
import { installCookieSession } from "./helpers/session";

test("professional completion center persists scoped records and remains usable on mobile", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const suffix = `${testInfo.project.name}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
  const password = "NestyStay1";
  const managerEmail = `completion-manager-${suffix}@nestystay.local`;
  const ownerEmail = `completion-owner-${suffix}@nestystay.local`;
  await register(api, managerEmail, "Completion manager", password, "PropertyManager", suffix);
  const owner = await register(api, ownerEmail, "Completion owner", password, "Owner", `${suffix}-owner`);
  const manager = await login(api, managerEmail, password);
  const headers = { Authorization: `Bearer ${manager.accessToken}` };
  const invited = await api.post("/api/property-manager/owners", { headers, data: { email: ownerEmail, displayName: "Completion owner" } });
  expect(invited.ok(), await invited.text()).toBeTruthy();
  const managed = await api.post("/api/property-manager/properties", { headers, data: { ownerUserId: owner.userId, title: "Completion Villa", unitNumber: `C-${suffix.slice(0, 4)}`, address: "Kingston" } });
  expect(managed.ok(), await managed.text()).toBeTruthy();
  const property = await managed.json() as { id: string };
  const created = await api.post("/api/property-manager/professional-completion/utilities", { headers, data: { resourceType: "UTILITY_BILL", status: "PENDING", ownerUserId: owner.userId, propertyId: property.id, currency: "JMD", idempotencyKey: `e2e-${suffix}`, payloadJson: JSON.stringify({ utilityType: "WATER", amount: 1250, evidence: ["meter-photo"] }) } });
  expect(created.ok(), await created.text()).toBeTruthy();
  await installCookieSession(page, manager);
  await page.goto("/pm/professional-completion", { waitUntil: "networkidle" });
  await expect(page.getByRole("heading", { name: "One workspace for every operational record" })).toBeVisible();
  await expect(page.getByText("Persisted Utilities & evidence records")).toBeVisible();
  await expect(page.getByText("UTILITY_BILL")).toBeVisible();
  for (const label of ["Documents", "Governance & proxy", "Team & RBAC", "Reporting & KPIs", "Unified audit", "Vendors", "Assets", "Inventory", "Incidents", "Community & gate", "Bulk operations", "Notifications"]) {
    await page.getByRole("tab", { name: label, exact: true }).click();
    await expect(page.getByRole("heading", { name: label, exact: true })).toBeVisible();
  }
  await api.dispose();
});

async function register(api: APIRequestContext, email: string, displayName: string, password: string, role: string, phoneSeed: string) {
  const hash = [...phoneSeed].reduce((value, character) => ((value * 33) + character.charCodeAt(0)) >>> 0, 5381);
  const digits = String(hash % 100_000_000).padStart(8, "0");
  const response = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: `+1555${digits}`, acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(response.ok(), await response.text()).toBeTruthy();
  return await response.json() as { userId: string };
}

async function login(api: APIRequestContext, email: string, password: string) {
  const response = await api.post("/api/auth/login", { data: { email, password } });
  expect(response.ok(), await response.text()).toBeTruthy();
  const loginBody = await response.json() as { accessToken: string; requiresTwoFactor: boolean; challengeId?: string };
  if (!loginBody.requiresTwoFactor) return loginBody;
  const challenge = await api.get(`/api/auth/development/challenges/${loginBody.challengeId}`);
  const code = (await challenge.json() as { code: string }).code;
  const verified = await api.post("/api/auth/2fa/verify", { data: { challengeId: loginBody.challengeId, code } });
  expect(verified.ok(), await verified.text()).toBeTruthy();
  return { ...loginBody, ...await verified.json() as { accessToken: string } };
}
