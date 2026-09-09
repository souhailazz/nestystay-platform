import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";

type Session = { userId: string; email: string; displayName: string; accessToken: string; expiresAt?: string; roles: string[]; permissions: string[] };
type RouteResult = { role: string; route: string; status: number; consoleErrors: string[]; failedRequests: string[]; bodyHasServerError: boolean };

const evidenceDir = process.env.NESTYSTAY_ROUTE_INVENTORY_DIR
  ? path.resolve(process.env.NESTYSTAY_ROUTE_INVENTORY_DIR)
  : path.resolve(process.cwd(), "..", "testing-evidence", "milestones-1-4", "browser");
const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("M1-M4 route inventory loads contractual routes without browser crashes", async ({ baseURL, page }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const sessions = {
    anonymous: null,
    guest: await createSession(api, "Guest", "Route Guest"),
    host: await createSession(api, "Host", "Route Host"),
    officer: await createSession(api, "Officer", "Route Officer"),
  } as const;
  const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
  test.skip(!adminToken, "NESTYSTAY_E2E_ADMIN_TOKEN is required for the privileged route inventory.");
  const officerApplication = await api.post("/api/wellness/officers", {
    headers: { Authorization: `Bearer ${sessions.officer.accessToken}` },
    data: { userId: sessions.officer.userId, badgeNumber: `ROUTE-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, parish: "St. Ann", coverageArea: "Ocho Rios", isActiveOffDuty: true, isRetired: false },
  });
  expect(officerApplication.ok(), await officerApplication.text()).toBeTruthy();
  const officer = await officerApplication.json() as { id: string };
  const officerApproval = await api.post(`/api/wellness/officers/${officer.id}/approve`, { headers: { Authorization: `Bearer ${adminToken}` }, data: { reason: "Route inventory" } });
  expect(officerApproval.ok(), await officerApproval.text()).toBeTruthy();
  const routes: Array<{ role: keyof typeof sessions; route: string }> = [
    { role: "anonymous", route: "/" },
    { role: "anonymous", route: "/explore" },
    { role: "anonymous", route: "/explore/map" },
    { role: "anonymous", route: "/directory/custodians" },
    { role: "anonymous", route: "/directory/trades" },
    { role: "anonymous", route: "/directory/businesses" },
    { role: "anonymous", route: "/directory/police" },
    { role: "anonymous", route: "/gate/qr" },
    { role: "guest", route: "/guest-dashboard" },
    { role: "guest", route: "/traveler/reservations" },
    { role: "guest", route: "/traveler/qr" },
    { role: "guest", route: "/messages" },
    { role: "host", route: "/host-dashboard" },
    { role: "host", route: "/host/properties" },
    { role: "host", route: "/host/wellness" },
    { role: "host", route: "/host/wellness/book" },
    { role: "host", route: "/host/wellness/directory" },
    { role: "host", route: "/directory/provider" },
    { role: "officer", route: "/officer/wellness" },
  ];
  const results: RouteResult[] = [];
  for (const item of routes) {
    const session = sessions[item.role];
    await page.goto("/", { waitUntil: "domcontentloaded" });
    if (session) {
      await page.evaluate((value) => localStorage.setItem("nestyStay.session", JSON.stringify(value)), session);
    } else {
      await page.evaluate(() => localStorage.removeItem("nestyStay.session"));
    }
    const consoleErrors: string[] = [];
    const failedRequests: string[] = [];
    const onConsole = (message: { type(): string; text(): string }) => { if (message.type() === "error") consoleErrors.push(message.text()); };
    const onResponse = (response: { status(): number; url(): string }) => { if (response.status() >= 500) failedRequests.push(`${response.status()} ${response.url()}`); };
    page.on("console", onConsole);
    page.on("response", onResponse);
    const response = await page.goto(item.route, { waitUntil: "networkidle" });
    const body = await page.locator("body").innerText();
    results.push({ role: item.role, route: item.route, status: response?.status() ?? 0, consoleErrors, failedRequests, bodyHasServerError: /Unexpected backend error|Application Error|Cannot read properties/i.test(body) });
    page.off("console", onConsole);
    page.off("response", onResponse);
  }
  await api.dispose();
  mkdirSync(evidenceDir, { recursive: true });
  // Each responsive project runs in parallel; keep evidence files isolated so
  // tablet/mobile runs cannot truncate the desktop report.
  const projectFile = `route-inventory-${test.info().project.name}.json`;
  writeFileSync(path.join(evidenceDir, projectFile), JSON.stringify({ generatedAt: new Date().toISOString(), project: test.info().project.name, routeCount: results.length, results }, null, 2));
  expect(results.filter((result) => result.status >= 500 || result.failedRequests.length > 0 || result.bodyHasServerError)).toEqual([]);
  expect(results).toHaveLength(routes.length);
});

async function createSession(api: APIRequestContext, role: "Guest" | "Host" | "Officer", displayName: string): Promise<Session> {
  const email = `route-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: "+15550102032", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as Session;
  return { ...body, email, displayName, accessToken: body.accessToken };
}
