import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { randomUUID } from "node:crypto";

async function prepare(api: APIRequestContext) {
  const suffix = randomUUID(); const password = "NestyStay1";
  const managerEmail = `pm-ops-${suffix}@nestystay.local`; const ownerEmail = `pm-ops-owner-${suffix}@nestystay.local`;
  const manager = await api.post("/api/auth/register", { data: { email: managerEmail, password, confirmPassword: password, displayName: "Operations manager", acceptedTerms: true, acceptedPrivacy: true, role: "PropertyManager" } }); expect(manager.ok(), await manager.text()).toBeTruthy();
  const owner = await api.post("/api/auth/register", { data: { email: ownerEmail, password, confirmPassword: password, displayName: "Operations owner", acceptedTerms: true, acceptedPrivacy: true, role: "Owner" } }); expect(owner.ok(), await owner.text()).toBeTruthy();
  const ownerId = (await owner.json()).userId as string; const login = await api.post("/api/auth/login", { data: { email: managerEmail, password } }); expect(login.ok(), await login.text()).toBeTruthy(); const headers = { Authorization: `Bearer ${(await login.json()).accessToken}` };
  await expect.poll(async () => (await api.post("/api/property-manager/owners", { headers, data: { email: ownerEmail, displayName: "Operations owner" } })).ok()).toBeTruthy();
  const property = await api.post("/api/property-manager/properties", { headers, data: { ownerUserId: ownerId, title: "Operations apartment", unitNumber: "OPS-1", address: "Kingston" } }); expect(property.ok(), await property.text()).toBeTruthy();
  return { managerEmail, ownerEmail, ownerId, propertyId: (await property.json()).id as string, password };
}

async function login(page: Page, email: string, password: string) {
  await page.goto("/login"); const form = page.locator("form").filter({ has: page.getByRole("heading", { name: "Log in", exact: true }) }); await form.getByLabel("Email", { exact: true }).fill(email); await form.getByLabel("Password", { exact: true }).fill(password); const response = page.waitForResponse(r => r.url().endsWith("/api/auth/login") && r.request().method() === "POST"); await form.getByRole("button", { name: /log in/i }).click(); expect((await response).ok()).toBeTruthy();
}

test("professional operations persists an owner block and an asset", async ({ page, request }) => {
  const data = await prepare(request); await login(page, data.managerEmail, data.password); await page.goto("/pm/operations"); await expect(page.getByRole("heading", { name: /Professional operations/ })).toBeVisible();
  await page.getByRole("button", { name: "Owner blocks" }).click(); await page.getByLabel("Starts").fill("2030-01-10T10:00"); await page.getByLabel("Ends").fill("2030-01-12T10:00"); await page.getByRole("button", { name: "Create owner block" }).click(); await expect(page.getByText("Owner block created", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Assets & inventory" }).click(); await page.getByLabel("Asset tag").fill("OPS-001"); await page.getByLabel("Name").fill("Water heater"); await page.getByRole("button", { name: "Add asset" }).click(); await expect(page.getByText("Asset added", { exact: true })).toBeVisible(); await expect(page.getByText("OPS-001", { exact: false })).toBeVisible();
});

test("owner operational block endpoint is ownership-scoped", async ({ request }) => {
  const data = await prepare(request); const loginResponse = await request.post("/api/auth/login", { data: { email: data.ownerEmail, password: data.password } }); expect(loginResponse.ok(), await loginResponse.text()).toBeTruthy(); const ownerToken = (await loginResponse.json()).accessToken as string; const headers = { Authorization: `Bearer ${ownerToken}` };
  const created = await request.post("/api/property-manager/owner/owner-blocks", { headers, data: { propertyId: data.propertyId, startsAt: "2030-02-10T10:00:00Z", endsAt: "2030-02-12T10:00:00Z", reason: "Owner weekend", category: "PERSONAL", timeZone: "America/Jamaica" } }); const createdBody = await created.text(); expect(created.ok(), createdBody).toBeTruthy(); const block = JSON.parse(createdBody); expect(block.category).toBe("PERSONAL");
  const list = await request.get("/api/property-manager/owner/owner-blocks", { headers }); expect(list.ok()).toBeTruthy(); expect((await list.json()).some((row: { id: string }) => row.id === block.id)).toBeTruthy();
  const cancelled = await request.post(`/api/property-manager/owner/owner-blocks/${block.id}/cancel`, { headers, data: { reason: "Plans changed", rowVersion: block.rowVersion } }); expect(cancelled.ok(), await cancelled.text()).toBeTruthy(); expect((await cancelled.json()).status).toBe("CANCELLED");
});
