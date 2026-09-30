import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { randomUUID } from "node:crypto";

const viewports = [
  { width: 320, height: 568 },
  { width: 360, height: 800 },
  { width: 390, height: 844 },
  { width: 414, height: 896 },
  { width: 430, height: 932 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
] as const;

test("Property Manager mobile/tablet workflow matrix has no page overflow or clipped primary controls", async ({ page, request }, testInfo) => {
  test.skip(testInfo.project.name !== "mobile-chromium", "This focused matrix controls its own viewport sizes.");

  const fixture = await createFixture(request);
  const consoleErrors: string[] = [];
  const failedApiResponses: string[] = [];
  page.on("console", message => {
    if (message.type() === "error") consoleErrors.push(message.text());
  });
  page.on("response", response => {
    if (response.url().includes("/api/") && response.status() >= 500) failedApiResponses.push(`${response.status()} ${response.url()}`);
  });

  await loginUi(page, fixture.managerEmail, fixture.password);

  for (const [viewportIndex, viewport] of viewports.entries()) {
    await page.setViewportSize(viewport);
    await page.goto("/pm/dashboard", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "One portfolio. Every owner, unit, and obligation.", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Renew subscription", exact: true })).toBeVisible();
    const workspaceSearchBox = await page.getByLabel("Search workspace", { exact: true }).boundingBox();
    expect(workspaceSearchBox?.x ?? 0, "workspace search starts outside the viewport").toBeGreaterThanOrEqual(0);
    expect((workspaceSearchBox?.x ?? 0) + (workspaceSearchBox?.width ?? 0), "workspace search is clipped").toBeLessThanOrEqual(viewport.width + 1);
    await assertNoPageOverflow(page, viewport, "dashboard");

    await page.goto("/pm/invoices", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Invoices", exact: true })).toBeVisible();
    await expect(page.locator("strong").filter({ hasText: fixture.invoiceNumber }).first()).toBeVisible();
    await assertNoPageOverflow(page, viewport, "invoices");

    await page.goto("/pm/utilities", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Utilities", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Record reading", exact: true })).toBeVisible();
    if (viewportIndex === 0) {
      await page.getByLabel("Previous", { exact: true }).fill("0");
      await page.getByLabel("Current", { exact: true }).fill(String(viewport.width));
      await page.getByLabel("Rate", { exact: true }).fill("2");
      const save = page.waitForResponse(response => response.url().endsWith("/api/property-manager/utilities/readings") && response.request().method() === "POST");
      await page.getByRole("button", { name: "Record reading", exact: true }).click();
      expect((await save).ok()).toBeTruthy();
    }
    await expect(page.getByText("320 × $2 = $640", { exact: true })).toBeVisible();
    await assertNoPageOverflow(page, viewport, "utilities");

    for (const route of ["/pm/calendar", "/pm/documents", "/pm/community", "/pm/team", "/pm/maintenance", "/pm/vendors", "/pm/work-orders"]) {
      await page.goto(route, { waitUntil: "domcontentloaded" });
      await expect(page.locator("h1").first()).toBeVisible();
      await assertNoPageOverflow(page, viewport, route);
      const primaryControls = page.locator("main button:visible, main a:visible, main input:visible, main select:visible, main textarea:visible");
      const count = await primaryControls.count();
      for (let index = 0; index < Math.min(count, 30); index += 1) {
        const box = await primaryControls.nth(index).boundingBox();
        if (!box) continue;
        expect(box.x + box.width, `${route} control ${index} is clipped at ${viewport.width}px`).toBeLessThanOrEqual(viewport.width + 1);
      }
    }
  }

  expect(failedApiResponses, "PM mobile matrix returned server errors").toEqual([]);
  expect(consoleErrors, "PM mobile matrix emitted console errors").toEqual([]);
});

async function createFixture(api: APIRequestContext) {
  const suffix = randomUUID();
  const password = "NestyStay1";
  const managerEmail = `pm-mobile-${suffix}@nestystay.local`;
  const ownerEmail = `pm-mobile-owner-${suffix}@nestystay.local`;
  const manager = await register(api, managerEmail, "PM mobile manager", "PropertyManager", password);
  const owner = await register(api, ownerEmail, "PM mobile owner", "Owner", password);
  const login = await api.post("/api/auth/login", { data: { email: managerEmail, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const credentials = await login.json() as { accessToken: string };
  const headers = { Authorization: `Bearer ${credentials.accessToken}` };
  const invited = await api.post("/api/property-manager/owners", { headers, data: { email: ownerEmail, displayName: "PM mobile owner" } });
  expect(invited.ok(), await invited.text()).toBeTruthy();
  const property = await api.post("/api/property-manager/properties", { headers, data: { ownerUserId: owner.userId, title: "PM mobile villa", unitNumber: "M-320", address: "Kingston" } });
  expect(property.ok(), await property.text()).toBeTruthy();
  const propertyId = (await property.json()).id as string;
  const invoice = await api.post("/api/property-manager/invoices", { headers, data: { ownerUserId: owner.userId, propertyId, dueDate: "2030-01-01", tax: 5, lines: [{ description: "Mobile QA", quantity: 1, unitAmount: 120 }] } });
  expect(invoice.ok(), await invoice.text()).toBeTruthy();
  return { managerEmail, password, invoiceNumber: (await invoice.json()).invoiceNumber as string };
}

async function register(api: APIRequestContext, email: string, displayName: string, role: string, password: string) {
  const response = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: "+15550104101", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(response.ok(), await response.text()).toBeTruthy();
  return await response.json() as { userId: string };
}

async function loginUi(page: Page, email: string, password: string) {
  await page.goto("/login");
  const form = page.locator("form").filter({ has: page.getByRole("heading", { name: "Log in", exact: true }) });
  await form.getByLabel("Email", { exact: true }).fill(email);
  await form.getByLabel("Password", { exact: true }).fill(password);
  const response = page.waitForResponse(item => item.url().endsWith("/api/auth/login") && item.request().method() === "POST");
  await form.getByRole("button", { name: "Log in", exact: false }).click();
  expect((await response).ok()).toBeTruthy();
  await expect.poll(async () => (await page.context().cookies()).some(cookie => cookie.httpOnly && cookie.name.includes("session"))).toBe(true);
}

async function assertNoPageOverflow(page: Page, viewport: { width: number; height: number }, route: string) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  const bodyBox = await page.locator("body").boundingBox();
  expect(bodyBox?.width ?? 0, `${route} body width at ${viewport.width}px`).toBeLessThanOrEqual(viewport.width + 1);
}
