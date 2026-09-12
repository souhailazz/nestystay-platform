import { expect, test, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { SCREEN_MANIFEST, USER_ROLES, type ScreenDefinition } from "../src/app/routeManifest";

type BrowserRole = "anonymous" | (typeof USER_ROLES)[number];
type InventoryResult = {
  screen: string;
  route: string;
  role: BrowserRole;
  desktop: boolean;
  tablet: boolean;
  mobile: boolean;
  status: "passed" | "failed";
  documentStatus: number;
  shellRendered: boolean;
  consoleErrors: string[];
  pageErrors: string[];
  failedRequests: string[];
};

const TEST_VALUE_BY_PARAM: Record<string, string> = {
  bookingId: "booking-00000000-0000-4000-8000-000000000000",
  propertyId: "22222222-2222-4222-8222-222222222222",
  reservationId: "reservation-00000000-0000-4000-8000-000000000000",
  screenId: "PUB-01",
  slug: "sample",
  state: "other-state",
};
const supportedProjects = ["desktop-chromium", "tablet-chromium", "mobile-chromium"] as const;
const evidenceDir = path.resolve(process.cwd(), "..", "testing-evidence", "final-hardening", "07-browser");
const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 600_000 });

test("95 canonical screens and 51 aliases render from the canonical manifest", async ({ page }, testInfo) => {
  test.skip(!supportedProjects.includes(testInfo.project.name as (typeof supportedProjects)[number]), "The inventory is the Chromium responsive certification matrix.");
  const cases = [
    ...SCREEN_MANIFEST.map((screen) => ({ kind: "canonical" as const, screen, route: materialize(screen.canonicalPath), role: roleFor(screen) })),
    ...SCREEN_MANIFEST.flatMap((screen) => screen.patterns.slice(1).map((pattern) => ({ kind: "alias" as const, screen, route: materialize(pattern), role: roleFor(screen) }))),
  ];
  expect(SCREEN_MANIFEST).toHaveLength(95);
  expect(cases.filter((item) => item.kind === "canonical")).toHaveLength(95);
  expect(cases.filter((item) => item.kind === "alias")).toHaveLength(51);

  const results: InventoryResult[] = [];
  await page.goto("/", { waitUntil: "domcontentloaded" });
  let activeRole: BrowserRole | null = null;
  let activeSession: Session | null = null;
  for (const item of cases) {
    if (item.role !== activeRole) {
      activeRole = item.role;
      activeSession = await provisionSession(page, item.role);
    }
    await setSyntheticSession(page, activeSession);
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    const failedRequests: string[] = [];
    const onConsole = (message: { type(): string; text(): string }) => {
      if (message.type() === "error" && !/favicon/i.test(message.text()) && !/Failed to load resource: the server responded with a status of 4\d\d \(/i.test(message.text())) {
        consoleErrors.push(message.text());
      }
    };
    const onPageError = (error: Error) => pageErrors.push(error.message);
    const onResponse = (response: { status(): number; url(): string }) => {
      if (response.status() >= 500) failedRequests.push(`${response.status()} ${response.url()}`);
    };
    page.on("console", onConsole);
    page.on("pageerror", onPageError);
    page.on("response", onResponse);
    let documentStatus = 0;
    let shellRendered = false;
    try {
      const response = await page.goto(item.route, { waitUntil: "networkidle" });
      documentStatus = response?.status() ?? 0;
      await page.waitForTimeout(125);
      const shell = page.locator("[data-route-name]");
      if (item.screen.shell === "workspace") {
        await expect(shell).toHaveCount(1);
        shellRendered = true;
      } else {
        shellRendered = await shell.count() === 1;
      }
      const body = await page.locator("body").innerText();
      if (/Application Error|Cannot read properties|Unexpected backend error|Rental-Agreement-Dec\.pdf|InsuraGuest's \$50/i.test(body)) {
        consoleErrors.push("Unexpected application or placeholder content detected in the rendered body.");
      }
      expect(documentStatus, `${item.kind} ${item.screen.id} ${item.route}`).toBeLessThan(500);
      expect(pageErrors, `${item.kind} ${item.screen.id} ${item.route} page errors`).toEqual([]);
      expect(consoleErrors, `${item.kind} ${item.screen.id} ${item.route} console errors`).toEqual([]);
      expect(failedRequests, `${item.kind} ${item.screen.id} ${item.route} API failures`).toEqual([]);
      expect(body.trim().length, `${item.kind} ${item.screen.id} ${item.route} is blank`).toBeGreaterThan(0);
    } finally {
      page.off("console", onConsole);
      page.off("pageerror", onPageError);
      page.off("response", onResponse);
    }
    results.push({
      screen: item.screen.id,
      route: item.route,
      role: item.role,
      desktop: testInfo.project.name === "desktop-chromium",
      tablet: testInfo.project.name === "tablet-chromium",
      mobile: testInfo.project.name === "mobile-chromium",
      status: "passed",
      documentStatus,
      shellRendered,
      consoleErrors,
      pageErrors,
      failedRequests,
    });
  }

  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(path.join(evidenceDir, `route-manifest-${testInfo.project.name}.json`), JSON.stringify({
    generatedAt: new Date().toISOString(),
    canonicalScreens: 95,
    routePatterns: 146,
    aliases: 51,
    project: testInfo.project.name,
    results,
  }, null, 2));
});

test("every configured role reaches its landing route and is denied a foreign workspace", async ({ page }, testInfo) => {
  test.skip(!["desktop-chromium", "mobile-chromium"].includes(testInfo.project.name), "Role authorization is certified in the desktop and mobile Chromium projects.");
  const isMobile = testInfo.project.name === "mobile-chromium";
  const landingRoutes: Record<(typeof USER_ROLES)[number], string> = {
    Guest: "/guest-dashboard",
    Host: "/host-dashboard",
    PropertyManager: "/pm/dashboard",
    Owner: "/owner/dashboard",
    ServiceProvider: "/directory/provider",
    LocalBusiness: "/directory/provider",
    Officer: "/officer/wellness",
    Admin: "/admin",
  };
  const checks: Array<Record<string, unknown>> = [];
  await page.goto("/", { waitUntil: "domcontentloaded" });
  for (const role of USER_ROLES) {
    await setSyntheticSession(page, syntheticSessionForRole(role));
    await page.goto(landingRoutes[role], { waitUntil: "domcontentloaded" });
    await expect(page.locator("[data-route-name]")).toHaveCount(1);
    await expect(page.getByRole("navigation", { name: "Workspace navigation", exact: true })).toBeVisible();
    const mobileNavigation = page.locator('nav[aria-label="Mobile workspace navigation"]');
    await expect(mobileNavigation).toHaveCount(1);
    if (isMobile) await expect(mobileNavigation).toBeVisible();
    const mobileItems = await mobileNavigation.locator("a,button").count();
    expect(mobileItems, `${role} mobile navigation`).toBe(5);
    checks.push({ role, landing: landingRoutes[role], landingStatus: "passed", mobileNavigationItems: mobileItems });
  }

  for (const role of USER_ROLES.filter((candidate) => candidate !== "PropertyManager")) {
    await setSyntheticSession(page, syntheticSessionForRole(role));
    await page.goto("/pm/dashboard", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: /Access is restricted/i })).toBeVisible();
    checks.push({ role, deniedRoute: "/pm/dashboard", forbiddenStatus: "passed" });
  }

  await setSyntheticSession(page, syntheticSessionForRole("PropertyManager"));
  await page.goto("/pm/dashboard", { waitUntil: "domcontentloaded" });
  await expect(page.locator("[data-route-name]")).toHaveCount(1);
  checks.push({ role: "PropertyManager", authorizedRoute: "/pm/dashboard", authorizedStatus: "passed" });

  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(path.join(evidenceDir, `role-authorization-${testInfo.project.name}.json`), JSON.stringify({ generatedAt: new Date().toISOString(), roles: 8, project: testInfo.project.name, checks }, null, 2));
});

function roleFor(screen: ScreenDefinition): BrowserRole {
  if (screen.auth === "public") return "anonymous";
  return screen.auth === "authenticated" ? "Guest" : screen.roleAccess[0];
}

function materialize(pattern: string) {
  return pattern.replace(/:([A-Za-z]+)/g, (_, key: string) => TEST_VALUE_BY_PARAM[key] ?? "sample");
}

async function setSyntheticSession(page: Page, session: Session | null) {
  await page.evaluate((value) => {
    if (!value) window.localStorage.removeItem("nestyStay.session");
    else window.localStorage.setItem("nestyStay.session", JSON.stringify(value));
  }, session);
}

type Session = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: (typeof USER_ROLES)[number][];
  permissions: string[];
};

function syntheticSessionForRole(role: (typeof USER_ROLES)[number]): Session {
  return {
    userId: `route-authorization-${role.toLowerCase()}`,
    email: `${role.toLowerCase()}@route-authorization.local`,
    displayName: `${role} Authorization`,
    accessToken: "",
    expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    roles: [role],
    permissions: role === "Admin" ? ["super_administration"] : [],
  };
}

async function provisionSession(page: Page, role: BrowserRole): Promise<Session | null> {
  await page.context().clearCookies();
  if (role === "anonymous") return null;
  if (role === "Admin") {
    return {
      userId: "route-inventory-admin",
      email: "admin@route-inventory.local",
      displayName: "Admin Inventory",
      accessToken: "",
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      roles: ["Admin"],
      permissions: ["super_administration"],
    };
  }

  const email = `route-inventory-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@nestystay.local`;
  const registration = await page.request.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: `${role} Inventory`, phone: "+15550102032", acceptedTerms: true, acceptedPrivacy: true, role },
  });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const registered = await registration.json() as { userId: string; displayName: string };
  const login = await page.request.post("/api/auth/login", {
    headers: { "X-Session-Mode": "cookie" },
    data: { email, password },
  });
  expect(login.ok(), await login.text()).toBeTruthy();
  const authenticated = await login.json() as { expiresAt?: string; roles?: (typeof USER_ROLES)[number][]; permissions?: string[] };
  return {
    userId: registered.userId,
    email,
    displayName: registered.displayName,
    accessToken: "",
    expiresAt: authenticated.expiresAt ?? new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    roles: authenticated.roles ?? [role],
    permissions: authenticated.permissions ?? [],
  };
}
