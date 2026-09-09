import { randomUUID } from "node:crypto";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type APIRequestContext, type Page } from "@playwright/test";

async function fixture(api: APIRequestContext) {
  const suffix = randomUUID();
  const password = "NestyStay1";
  const managerEmail = `pm-browser-${suffix}@nestystay.local`;
  const ownerEmail = `owner-browser-${suffix}@nestystay.local`;
  const ids: string[] = [];
  for (const [email, role] of [[managerEmail, "PropertyManager"], [ownerEmail, "Owner"]]) {
    const response = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName: role === "Owner" ? "PM browser owner" : "PM browser manager", phone: "+15550104101", acceptedTerms: true, acceptedPrivacy: true, role } });
    expect(response.ok(), await response.text()).toBeTruthy();
    ids.push((await response.json()).userId);
  }
  const login = await api.post("/api/auth/login", { data: { email: managerEmail, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const credentials = await login.json();
  expect(credentials.requiresTwoFactor).toBe(false);
  const headers = { Authorization: `Bearer ${credentials.accessToken}` };
  const invited = await api.post("/api/property-manager/owners", { headers, data: { email: ownerEmail, displayName: "PM browser owner" } });
  expect(invited.ok(), await invited.text()).toBeTruthy();
  const propertyResponse = await api.post("/api/property-manager/properties", { headers, data: { ownerUserId: ids[1], title: "PM browser villa", unitNumber: "PM-B", address: "Kingston" } });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const propertyId = (await propertyResponse.json()).id as string;
  const invoiceResponse = await api.post("/api/property-manager/invoices", { headers, data: { ownerUserId: ids[1], propertyId, dueDate: "2030-01-01", tax: 5, lines: [{ description: "Browser service", quantity: 1, unitAmount: 120 }] } });
  expect(invoiceResponse.ok(), await invoiceResponse.text()).toBeTruthy();
  const invoice = await invoiceResponse.json();
  return { managerEmail, ownerEmail, password, headers, propertyId, invoice };
}

async function loginUi(page: Page, email: string, password: string) {
  await page.goto("/login");
  const form = page.locator("form").filter({ has: page.getByRole("heading", { name: "Log in", exact: true }) });
  await form.getByLabel("Email", { exact: true }).fill(email);
  await form.getByLabel("Password", { exact: true }).fill(password);
  const response = page.waitForResponse(r => r.url().endsWith("/api/auth/login") && r.request().method() === "POST");
  await form.getByRole("button", { name: "Log in", exact: false }).click();
  expect((await response).ok()).toBeTruthy();
  await expect.poll(async () => (await page.context().cookies()).some(cookie => cookie.httpOnly && cookie.name.includes("session"))).toBe(true);
}

test("PM cookie login, invoice loading, utility persistence and owner portal", async ({ page, request }, testInfo) => {
  const data = await fixture(request);
  await loginUi(page, data.managerEmail, data.password);
  await page.goto("/pm/invoices");
  await expect(page.getByRole("heading", { name: "Invoices", exact: true })).toBeVisible();
  await expect(page.getByText("Calculating report…")).toHaveCount(0);
  await expect(page.locator("strong").filter({ hasText: data.invoice.invoiceNumber }).first()).toBeVisible();
  await expect(page.getByText("Invoice revenue", { exact: false })).toHaveText("Invoice revenue$125");
  await expect(page.getByRole("alert")).toHaveCount(0);
  await page.goto("/pm/dashboard");
  const communityBoard = page.locator("article").filter({ has: page.getByRole("heading", { name: "Community board", exact: true }) }).first();
  await communityBoard.getByLabel("Title").fill("Scheduled water maintenance");
  await communityBoard.getByLabel("Notice").fill("Water service will pause during the maintenance window.");
  await communityBoard.getByLabel("Category").selectOption("MAINTENANCE");
  await communityBoard.getByLabel("Publish at (optional)").fill(new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString().slice(0, 16));
  const scheduledNotice = page.waitForResponse(r => r.url().endsWith("/api/property-manager/notices") && r.request().method() === "POST");
  await communityBoard.getByRole("button", { name: "Schedule notice", exact: true }).click();
  expect((await scheduledNotice).ok()).toBeTruthy();
  await expect(communityBoard.getByText("Scheduled water maintenance", { exact: true })).toBeVisible();
  await expect(communityBoard.getByText("SCHEDULED", { exact: true })).toBeVisible();
  await page.getByRole("navigation", { name: "Property manager sections" }).getByRole("link", { name: "Utilities", exact: true }).click();
  await page.getByLabel("Previous", { exact: true }).fill("0");
  await page.getByLabel("Current", { exact: true }).fill("10");
  await page.getByLabel("Rate", { exact: true }).fill("2");
  const saved = page.waitForResponse(r => r.url().endsWith("/api/property-manager/utilities/readings") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Record reading", exact: true }).click();
  expect((await saved).ok()).toBeTruthy();
  await expect(page.getByText("10 × $2 = $20", { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText("10 × $2 = $20", { exact: true })).toBeVisible();
  const persisted = await request.get(`/api/property-manager/utilities/${data.propertyId}/readings`, { headers: data.headers });
  expect(persisted.ok()).toBeTruthy();
  expect(await persisted.json()).toMatchObject([{ currentReading: 10, usage: 10 }]);
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await expect(page.getByRole("heading", { name: "Utilities", exact: true })).toHaveCSS("opacity", "1");
  await expect(page.getByText("Property Manager · real API workspace", { exact: true })).toHaveCSS("opacity", "1");
  const accessibility = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"]).analyze();
  expect(accessibility.violations).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath("manager-persisted-utility.png"), fullPage: true });

  await page.context().clearCookies();
  await page.evaluate(() => localStorage.removeItem("nestyStay.session"));
  await loginUi(page, data.ownerEmail, data.password);
  await page.goto("/owner/dashboard");
  await expect(page.getByText("Your property account, in one place.")).toBeVisible();
  await expect(page.locator("strong").filter({ hasText: data.invoice.invoiceNumber }).first()).toBeVisible();
  await expect(page.getByText("PM browser villa · PM-B", { exact: true })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("owner-persisted-portfolio.png"), fullPage: true });
});
