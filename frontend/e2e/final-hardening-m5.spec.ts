import AxeBuilder from "@axe-core/playwright";
import { expect, request as playwrightRequest, test, type APIRequestContext, type APIResponse, type BrowserContext, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";

type Session = { userId: string; email: string; displayName: string; accessToken: string; expiresAt: string; roles: string[]; permissions: string[] };
type Cookie = { name: string; value: string; domain: string; path: string; httpOnly?: boolean; secure?: boolean; sameSite?: "Lax" | "Strict" | "None" };
const evidenceRoot = path.resolve(process.cwd(), "..", "testing-evidence", "final-hardening");
const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("M5 property-manager journey uses cookie auth, persisted API data, responsive UI, and guard QR", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const managerEmail = `m5-manager-${suffix}@nestystay.local`;
  const ownerEmail = `m5-owner-${suffix}@nestystay.local`;
  await register(api, managerEmail, "M5 Manager", "PropertyManager");
  await register(api, ownerEmail, "M5 Owner", "Owner");
  const manager = await loginBearer(api, managerEmail);
  const owner = await loginBearer(api, ownerEmail);

  const invited = await postJson(api, "/api/property-manager/owners", manager, { email: ownerEmail, displayName: "M5 Owner" });
  const ownerId = (invited as { ownerUserId: string }).ownerUserId;
  const property = await postJson(api, "/api/property-manager/properties", manager, { ownerUserId: ownerId, title: "M5 Harbour Villa", unitNumber: "M5-1", address: "Montego Bay" }) as { id: string };
  const invoice = await postJson(api, "/api/property-manager/invoices", manager, { ownerUserId: ownerId, propertyId: property.id, dueDate: "2099-01-01", tax: 5, lines: [{ description: "M5 community fee", quantity: 1, unitAmount: 125 }] }) as { id: string; invoiceNumber: string };
  await postJson(api, "/api/property-manager/utilities", manager, { ownerUserId: ownerId, propertyId: property.id, utilityType: "Water", billingPeriod: "2098-12", usage: 10, rate: 2 });
  await postJson(api, "/api/property-manager/vendors", manager, { name: "M5 Island Electric", category: "Electrical", contact: "555-0105", notes: "M5 vendor" });
  const maintenance = await postJson(api, "/api/property-manager/maintenance", manager, { ownerUserId: ownerId, propertyId: property.id, title: "M5 maintenance task", description: "UI workflow fixture", category: "General", urgency: "NORMAL" }) as { id: string };
  await postJson(api, "/api/property-manager/notices", manager, { title: "M5 water notice", body: "A persisted community announcement.", isPinned: true });
  const proposal = await postJson(api, "/api/property-manager/governance/proposals", manager, { title: "M5 reserve vote", description: "Approve reserve works.", opensAt: new Date(Date.now() - 60_000).toISOString(), closesAt: new Date(Date.now() + 86_400_000).toISOString(), isAnonymous: true, quorum: 1 }) as { id: string };
  const bytes = Buffer.from("%PDF-1.7\nM5 evidence\n");
  await postJson(api, "/api/property-manager/documents", manager, { ownerUserId: ownerId, propertyId: property.id, title: "M5 statement", category: "Finance", fileName: "m5-statement.pdf", contentType: "application/pdf", sizeBytes: bytes.length, contentBase64: bytes.toString("base64") });
  const issued = await postJson(api, "/api/property-manager/qr", manager, { ownerUserId: ownerId, propertyId: property.id, subjectType: "OWNER", validFrom: new Date(Date.now() - 60_000).toISOString(), validUntil: new Date(Date.now() + 86_400_000).toISOString() }) as { token: string };

  const managerCookie = await loginCookie(managerEmail);
  const ownerCookie = await loginCookie(ownerEmail);
  const consoleErrors: string[] = [];
  const serverErrors: string[] = [];
  const onConsole = (message: { type(): string; text(): string }) => { if (message.type() === "error" && !/favicon/i.test(message.text())) consoleErrors.push(message.text()); };
  const onResponse = (response: { status(): number; url(): string }) => { if (response.status() >= 500) serverErrors.push(`${response.status()} ${response.url()}`); };
  page.on("console", onConsole); page.on("response", onResponse);

  await seedCookieSession(page, managerCookie);
  await page.goto("/pm/dashboard", { waitUntil: "networkidle" });
  await expect(page.getByText("One portfolio. Every owner, unit, and obligation.")).toBeVisible();
  await expect(page.getByRole("option", { name: "M5 Harbour Villa · M5-1" })).toHaveCount(1);
  await expect(page.getByText(invoice.invoiceNumber).first()).toBeVisible();
  await expect(page.getByText("M5 maintenance task")).toBeVisible();
  await expect(page.getByText("M5 reserve vote")).toBeVisible();
  await expect(page.getByText("M5 statement").first()).toBeVisible();
  await page.getByRole("button", { name: "Start work" }).first().click();
  await expect(page.getByText("IN_PROGRESS").first()).toBeVisible();
  await page.getByRole("button", { name: "Issue QR" }).click();
  await expect(page.getByText("QR issued")).toBeVisible();
  const uiToken = await page.locator("code").filter({ hasText: /^[A-Za-z0-9_-]{20,}$/ }).first().innerText();
  expect(uiToken.length).toBeGreaterThan(20);
  await expect(page.getByRole("button", { name: "Revoke QR" })).toBeVisible();
  await page.getByRole("button", { name: "Revoke QR" }).click();
  await expect(page.getByText("QR revoked · access denied")).toBeVisible();
  const axeManager = await new AxeBuilder({ page }).analyze();
  expect(axeManager.violations, "M5 manager accessibility").toEqual([]);

  await seedCookieSession(page, ownerCookie);
  await page.goto("/owner/dashboard", { waitUntil: "networkidle" });
  await expect(page.getByText("Your property account, in one place.")).toBeVisible();
  await expect(page.getByText("M5 Harbour Villa · M5-1")).toBeVisible();
  await expect(page.getByText(invoice.invoiceNumber).first()).toBeVisible();
  const ownerMaintenanceResponse = page.waitForResponse((response) => response.url().includes("/api/property-manager/maintenance") && response.request().method() === "POST");
  await page.getByRole("button", { name: "Report maintenance" }).click();
  const ownerMaintenance = await ownerMaintenanceResponse;
  expect(ownerMaintenance.status(), await ownerMaintenance.text()).toBe(200);
  await expect(page.getByText("Maintenance").first()).toBeVisible();
  const axeOwner = await new AxeBuilder({ page }).analyze();
  expect(axeOwner.violations, "M5 owner accessibility").toEqual([]);

  await page.goto(`/gate?token=${encodeURIComponent(uiToken)}&propertyId=${encodeURIComponent(property.id)}`, { waitUntil: "networkidle" });
  await expect(page.getByText("Camera scan uses the browser QR decoder")).toBeVisible();
  await page.getByRole("button", { name: "Validate access" }).click();
  await expect(page.getByRole("status").filter({ hasText: /ACCESS (APPROVED|DENIED)/ })).toContainText("ACCESS DENIED");
  // The manager UI revoked its issued token; the gate must expose that state.
  expect(await page.locator("body").innerText()).toMatch(/REVOKED|ACCESS DENIED/i);

  const overflow = await page.evaluate(() => ({ scrollWidth: document.documentElement.scrollWidth, innerWidth: window.innerWidth, buttons: [...document.querySelectorAll("button")].filter((button) => !button.getBoundingClientRect().width).length }));
  expect(overflow.scrollWidth).toBeLessThanOrEqual(overflow.innerWidth + 1);
  expect(overflow.buttons).toBe(0);
  page.off("console", onConsole); page.off("response", onResponse);
  writeEvidence(`07-browser/m5-${testInfo.project.name}.json`, { generatedAt: new Date().toISOString(), project: testInfo.project.name, workflows: ["manager cookie session", "owner portal persisted records", "manager maintenance update", "UI QR issue/revoke", "gate revoked validation", "mobile overflow/accessibility"], consoleErrors, unexpected5xx: serverErrors, persisted: { propertyId: property.id, invoiceId: invoice.id, maintenanceId: maintenance.id, proposalId: proposal.id }, axeViolations: 0, responsive: overflow, passed: consoleErrors.length === 0 && serverErrors.length === 0 });
  await api.dispose();
});

async function register(api: APIRequestContext, email: string, displayName: string, role: string) {
  const response = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: "+15550105005", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(response.ok(), await response.text()).toBeTruthy();
}

async function loginBearer(api: APIRequestContext, email: string): Promise<Session> {
  const response = await api.post("/api/auth/login", { data: { email, password } });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = await response.json() as Session & { requiresTwoFactor?: boolean; challengeId?: string; challengeExpiresAt?: string };
  if (!body.requiresTwoFactor) return body;
  const challenge = await api.get(`/api/auth/development/challenges/${body.challengeId}`);
  const code = (await challenge.json() as { code: string }).code;
  const verified = await api.post("/api/auth/2fa/verify", { data: { challengeId: body.challengeId, code } });
  expect(verified.ok(), await verified.text()).toBeTruthy();
  return { ...body, ...await verified.json() as Session };
}

async function loginCookie(email: string): Promise<{ session: Session; cookies: Cookie[] }> {
  const api = await playwrightRequest.newContext({ baseURL: "http://127.0.0.1:4173" });
  const response = await api.post("/api/auth/login", { data: { email, password }, headers: { "X-Session-Mode": "cookie" } });
  expect(response.ok(), await response.text()).toBeTruthy();
  let body = await response.json() as Session & { requiresTwoFactor?: boolean; challengeId?: string };
  let setCookies = parseCookies(response);
  if (body.requiresTwoFactor) {
    const challenge = await api.get(`/api/auth/development/challenges/${body.challengeId}`);
    const code = (await challenge.json() as { code: string }).code;
    const verified = await api.post("/api/auth/2fa/verify", { data: { challengeId: body.challengeId, code }, headers: { "X-Session-Mode": "cookie" } });
    expect(verified.ok(), await verified.text()).toBeTruthy();
    body = { ...body, ...await verified.json() as Session };
    setCookies = [...setCookies, ...parseCookies(verified)];
  }
  await api.dispose();
  return { session: { ...body, accessToken: "" }, cookies: setCookies };
}

async function seedCookieSession(page: Page, auth: { session: Session; cookies: Cookie[] }) {
  await page.context().clearCookies();
  await page.context().addCookies(auth.cookies);
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate(value => window.localStorage.setItem("nestyStay.session", JSON.stringify(value)), auth.session);
}

function parseCookies(response: APIResponse): Cookie[] {
  return response.headersArray().filter((header) => header.name.toLowerCase() === "set-cookie").map((header) => {
    const [pair, ...attributes] = header.value.split(";");
    const [name, ...rest] = pair.split("=");
    const attributeMap = new Map(attributes.map((attribute) => { const [key, ...value] = attribute.trim().split("="); return [key.toLowerCase(), value.join("=")]; }));
    const sameSiteValue = attributeMap.get("samesite")?.toLowerCase();
    const sameSite: Cookie["sameSite"] = sameSiteValue === "strict" ? "Strict" : sameSiteValue === "none" ? "None" : "Lax";
    return { name, value: rest.join("="), domain: "127.0.0.1", path: attributeMap.get("path") || "/", httpOnly: attributes.some((attribute) => attribute.trim().toLowerCase() === "httponly"), secure: attributes.some((attribute) => attribute.trim().toLowerCase() === "secure"), sameSite };
  });
}

async function postJson(api: APIRequestContext, url: string, session: Session, data: unknown) {
  const response = await api.post(url, { headers: { Authorization: `Bearer ${session.accessToken}` }, data });
  expect(response.ok(), `${url}: ${await response.text()}`).toBeTruthy();
  return await response.json();
}

function writeEvidence(relativePath: string, value: unknown) {
  const target = path.join(evidenceRoot, relativePath);
  mkdirSync(path.dirname(target), { recursive: true });
  writeFileSync(target, JSON.stringify(value, null, 2));
}
