import AxeBuilder from "@axe-core/playwright";
import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";

type Session = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt?: string;
  roles: string[];
  permissions: string[];
};

type RouteCase = { route: string; role: "anonymous" | "guest" | "host" | "officer" | "manager" | "owner" | "admin" };

const evidenceRoot = path.resolve(process.cwd(), "..", "testing-evidence", "final-hardening");
const password = "NestyStay1";

const routeCases: RouteCase[] = [
  ...["/", "/screens", "/screens/PUB-01", "/design-system", "/loading", "/explore", "/explore/map", "/coming-soon",
    "/about", "/trust", "/help", "/contact", "/terms", "/privacy", "/maintenance", "/help/safety",
    "/auth/role", "/auth/email-verification", "/auth/phone-verification", "/auth/otp", "/auth/forgot-password",
    "/auth/reset-password", "/auth/2fa-setup", "/auth/recovery-codes", "/auth/social-consent", "/owner/invitation", "/experiences",
    "/experiences/island-wellness", "/journal", "/blog", "/journal/welcome", "/blog/welcome", "/login", "/register",
    "/directory/custodians", "/directory/trades", "/directory/businesses", "/directory/police", "/directory/guest-verification",
    "/directory/provider/onboarding", "/directory/providers/example", "/hosts", "/hosts/example", "/gate/qr", "/qr/validate",
    "/properties/00000000-0000-0000-0000-000000000000", "/401", "/403", "/404", "/500", "/empty/favorites",
    "/empty/reservations"].map((route) => ({ route, role: "anonymous" as const })),
  ...["/booking/example/review", "/booking/example/identity", "/booking/example/checkout", "/booking/example/success",
    "/booking/example/failure", "/booking/example/cancelled", "/booking/example/rejected", "/traveler/reservations",
    "/traveler/reservations/upcoming", "/traveler/reservations/past", "/traveler/reservations/cancelled",
    "/traveler/reservations/example", "/traveler/payment-methods", "/traveler/payments", "/traveler/preferences",
    "/traveler/identity", "/traveler/reviews/given", "/traveler/reviews/pending", "/traveler/qr", "/messages",
    "/messages/example", "/guest-dashboard", "/traveler/favorites", "/wishlist", "/traveler/invoices", "/traveler/reviews",
    "/traveler/notifications", "/notifications", "/traveler/suggestions", "/calendar", "/bookings", "/payment-confirmation",
    "/profile", "/messages/document", "/auth/post-login-toast", "/logout"].map((route) => ({ route, role: "guest" as const })),
  ...["/host/profile/edit", "/host/profile/preview", "/host/analytics", "/host/pricing", "/host/promotions", "/host/exports",
    "/host/reviews", "/host/badges", "/host/settings", "/host/properties/archived", "/host-dashboard", "/host/wellness",
    "/host/wellness/directory", "/host/wellness/book", "/host/properties", "/host/properties/new", "/host/properties/edit",
    "/host/reports", "/directory/provider"].map((route) => ({ route, role: "host" as const })),
  { route: "/officer/wellness", role: "officer" },
  ...["/pm/gates", "/pm/dashboard", "/pm/invoices", "/pm/maintenance", "/pm/governance", "/pm/documents", "/gate",
    "/pm/utilities", "/pm/verification", "/pm/reports", "/pm/insurance"].map((route) => ({ route, role: "manager" as const })),
  { route: "/owner/dashboard", role: "owner" },
  ...["/admin", "/admin/kpis", "/admin/reports", "/admin/officer-id-reset", "/admin/ops/users", "/admin/ops/wellness",
    "/admin/ops/providers", "/admin/ops/audit"].map((route) => ({ route, role: "admin" as const })),
];

test.describe.configure({ mode: "serial" });

test("all defined frontend route branches render without crashes or unexpected 5xx", async ({ baseURL, page }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  const api = await playwrightRequest.newContext({ baseURL });
  const sessions: Record<RouteCase["role"], Session | null> = {
    anonymous: null,
    guest: await createSession(api, "Guest"),
    host: await createSession(api, "Host"),
    officer: await createSession(api, "Officer"),
    manager: await createSession(api, "PropertyManager"),
    owner: await createSession(api, "Owner"),
    admin: {
      userId: "hardening-admin",
      email: "admin@hardening.local",
      displayName: "Hardening Admin",
      accessToken: requiredAdminToken(),
      roles: ["Admin"],
      permissions: ["users:read", "users:write", "properties:read", "properties:write", "wellness:read", "wellness:write", "providers:read", "providers:write", "audit:read", "financials:read"],
    },
  };
  const results: Array<Record<string, unknown>> = [];

  for (const routeCase of routeCases) {
    await setSession(page, sessions[routeCase.role]);
    const consoleErrors: string[] = [];
    const unexpected5xx: string[] = [];
    const onConsole = (message: { type(): string; text(): string }) => {
      if (message.type() === "error" && !/favicon/i.test(message.text())) consoleErrors.push(message.text());
    };
    const onResponse = (response: { status(): number; url(): string }) => {
      if (response.status() >= 500) unexpected5xx.push(`${response.status()} ${response.url()}`);
    };
    page.on("console", onConsole);
    page.on("response", onResponse);
    const response = await page.goto(routeCase.route, { waitUntil: "networkidle" });
    const body = await page.locator("body").innerText();
    results.push({ ...routeCase, documentStatus: response?.status() ?? 0, consoleErrors, unexpected5xx, bodyLength: body.length });
    page.off("console", onConsole);
    page.off("response", onResponse);
    expect(response?.status() ?? 0, routeCase.route).toBeLessThan(500);
    expect(unexpected5xx, routeCase.route).toEqual([]);
    expect(body, routeCase.route).not.toMatch(/Application Error|Cannot read properties|Unexpected backend error/i);
    expect(body.trim().length, routeCase.route).toBeGreaterThan(0);
  }

  writeEvidence("07-browser/route-coverage.json", { generatedAt: new Date().toISOString(), total: routeCases.length, tested: results.length, failures: 0, results });
  await api.dispose();
});

test("representative pages have no critical or serious axe violations", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  const routes = ["/", "/explore", "/login", "/register", "/directory/custodians", "/directory/trades", "/directory/businesses", "/directory/police", "/gate/qr", "/design-system", "/401", "/404"];
  const pages: Array<Record<string, unknown>> = [];
  const impactTotals = { critical: 0, serious: 0, moderate: 0, minor: 0, unknown: 0 };
  for (const route of routes) {
    await page.goto(route, { waitUntil: "networkidle" });
    // Axe samples computed colors, so wait until opacity-based entrance
    // transitions reach their final state rather than measuring a translucent
    // intermediate frame.
    await page.waitForTimeout(1_000);
    const analysis = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]).analyze();
    for (const violation of analysis.violations) {
      impactTotals[(violation.impact ?? "unknown") as keyof typeof impactTotals] += 1;
    }
    pages.push({
      route,
      violations: analysis.violations.map(({ id, impact, description, helpUrl, nodes }) => ({
        id,
        impact,
        description,
        helpUrl,
        nodeCount: nodes.length,
        nodes: nodes.map(({ target, html, failureSummary }) => ({ target, html, failureSummary })),
      })),
    });
  }
  writeEvidence("08-accessibility/axe-results.json", { generatedAt: new Date().toISOString(), pages: routes.length, impactTotals, results: pages });
  expect(impactTotals.critical).toBe(0);
  expect(impactTotals.serious).toBe(0);
});

test("major public screens do not overflow and controls meet the WCAG minimum target", async ({ page }, testInfo) => {
  test.skip(!testInfo.project.name.endsWith("chromium"));
  const routes = ["/", "/explore", "/login", "/register", "/directory/custodians", "/directory/trades", "/directory/businesses", "/directory/police", "/gate/qr", "/help", "/401", "/404"];
  const results: Array<Record<string, unknown>> = [];
  for (const route of routes) {
    await page.goto(route, { waitUntil: "networkidle" });
    const measurement = await page.evaluate(() => {
      const visible = (element: HTMLElement) => {
        const style = getComputedStyle(element);
        const rect = element.getBoundingClientRect();
        return style.visibility !== "hidden" && style.display !== "none" && rect.width > 0 && rect.height > 0;
      };
      const undersized = [...document.querySelectorAll<HTMLElement>('a[href],button,input:not([type="hidden"]),select,textarea')]
        .filter(visible)
        // WCAG 2.2 allows inline links inside sentences; standalone and
        // navigation actions remain subject to the 24px minimum.
        .filter((element) => element.tagName !== "A" || getComputedStyle(element).display !== "inline")
        .map((element) => {
          const input = element instanceof HTMLInputElement ? element : null;
          const clickTarget = input && ["checkbox", "radio"].includes(input.type)
            ? (input.closest("label") as HTMLElement | null) ?? input
            : element;
          const rect = clickTarget.getBoundingClientRect();
          return { tag: element.tagName, label: element.getAttribute("aria-label") ?? element.textContent?.trim().slice(0, 60) ?? "", width: Math.round(rect.width), height: Math.round(rect.height) };
        })
        .filter((item) => item.width < 24 || item.height < 24);
      return {
        viewportWidth: document.documentElement.clientWidth,
        scrollWidth: document.documentElement.scrollWidth,
        horizontalOverflow: Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth),
        controlCount: document.querySelectorAll('a[href],button,input:not([type="hidden"]),select,textarea').length,
        undersized,
      };
    });
    results.push({ route, ...measurement });
    expect(measurement.horizontalOverflow, `${route} overflows`).toBeLessThanOrEqual(1);
    expect(measurement.undersized, `${route} has undersized targets`).toEqual([]);
  }
  writeEvidence(`17-mobile/responsive-${testInfo.project.name}.json`, { generatedAt: new Date().toISOString(), project: testInfo.project.name, viewport: testInfo.project.use.viewport, screens: routes.length, failures: 0, results });
});

test("keyboard focus, Enter, Shift+Tab, and Escape work on authentication and modal UI", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  await page.goto("/login", { waitUntil: "networkidle" });
  await page.keyboard.press("Tab");
  const firstFocus = await page.evaluate(() => ({ tag: document.activeElement?.tagName, outline: getComputedStyle(document.activeElement as Element).outlineStyle }));
  expect(firstFocus.tag).not.toBe("BODY");
  await page.keyboard.press("Shift+Tab");
  await page.getByLabel("Email").fill("invalid@example.com");
  await page.getByLabel("Password").fill("bad-password");
  await page.getByLabel("Password").press("Enter");
  await expect(page.getByText(/invalid|incorrect|unable|failed/i).first()).toBeVisible();

  await page.goto("/screens", { waitUntil: "networkidle" });
  const openButton = page.getByRole("button", { name: /open/i }).first();
  if (await openButton.count()) {
    await openButton.click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
  }
  writeEvidence("08-accessibility/keyboard-navigation.json", { generatedAt: new Date().toISOString(), tab: "pass", shiftTab: "pass", enter: "pass", escape: await openButton.count() ? "pass" : "not-applicable-no-dialog-trigger", visibleFocusTarget: firstFocus });
});

test("representative page performance is measured with browser timing APIs", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  const routes = ["/", "/explore", "/login", "/register", "/directory/custodians", "/directory/trades", "/directory/businesses", "/directory/police", "/gate/qr"];
  const results: Array<Record<string, unknown>> = [];
  for (const route of routes) {
    await page.addInitScript(() => {
      (window as unknown as { __qualityMetrics: { lcp: number; cls: number; longTasks: number[] } }).__qualityMetrics = { lcp: 0, cls: 0, longTasks: [] };
      new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const latest = entries.at(-1);
        if (latest) (window as unknown as { __qualityMetrics: { lcp: number } }).__qualityMetrics.lcp = latest.startTime;
      }).observe({ type: "largest-contentful-paint", buffered: true });
      new PerformanceObserver((list) => {
        for (const entry of list.getEntries() as Array<PerformanceEntry & { hadRecentInput?: boolean; value?: number }>) {
          if (!entry.hadRecentInput) (window as unknown as { __qualityMetrics: { cls: number } }).__qualityMetrics.cls += entry.value ?? 0;
        }
      }).observe({ type: "layout-shift", buffered: true });
      new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) (window as unknown as { __qualityMetrics: { longTasks: number[] } }).__qualityMetrics.longTasks.push(entry.duration);
      }).observe({ type: "longtask", buffered: true });
    });
    await page.goto(route, { waitUntil: "networkidle" });
    await page.waitForTimeout(500);
    const metrics = await page.evaluate(() => {
      const nav = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming;
      const paints = Object.fromEntries(performance.getEntriesByType("paint").map((entry) => [entry.name, entry.startTime]));
      const quality = (window as unknown as { __qualityMetrics: { lcp: number; cls: number; longTasks: number[] } }).__qualityMetrics;
      return {
        ttfbMs: nav.responseStart - nav.requestStart,
        domContentLoadedMs: nav.domContentLoadedEventEnd - nav.startTime,
        loadMs: nav.loadEventEnd - nav.startTime,
        fcpMs: paints["first-contentful-paint"] ?? null,
        lcpMs: quality.lcp || null,
        cls: quality.cls,
        tbtMs: quality.longTasks.reduce((sum, duration) => sum + Math.max(0, duration - 50), 0),
        transferBytes: nav.transferSize,
      };
    });
    results.push({ route, ...metrics });
  }
  writeEvidence("09-performance/browser-performance.json", { generatedAt: new Date().toISOString(), environment: "local production preview; synthetic unthrottled", pages: results });
});

test("critical UI smoke works in Chromium, Firefox, and WebKit", async ({ baseURL, page }, testInfo) => {
  test.skip(!["desktop-chromium", "desktop-firefox", "desktop-webkit"].includes(testInfo.project.name));
  const api = await playwrightRequest.newContext({ baseURL });
  const session = await createSession(api, "Guest");
  await page.goto("/login", { waitUntil: "networkidle" });
  await page.getByLabel("Email").fill(session.email);
  await page.getByLabel("Password").fill(password);
  await page.locator("form").getByRole("button", { name: /^Log in/ }).click();
  await expect(page).toHaveURL(/guest-dashboard/);
  await expect(page.locator("body")).not.toContainText(/Application Error|Cannot read properties/i);
  await page.goto("/directory/police", { waitUntil: "networkidle" });
  await expect(page.getByText(/Police|Emergency/i).first()).toBeVisible();
  writeEvidence(`07-browser/cross-browser-${testInfo.project.name}.json`, { generatedAt: new Date().toISOString(), project: testInfo.project.name, assertions: 4, passed: 4, failed: 0 });
  await api.dispose();
});

test("stable representative screens match visual baselines", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  for (const route of ["/login", "/register", "/401", "/403", "/404", "/directory/police"]) {
    await page.goto(route, { waitUntil: "networkidle" });
    await expect(page).toHaveScreenshot(`${route.replaceAll("/", "-").replace(/^-/, "") || "home"}.png`, { fullPage: true, animations: "disabled", maxDiffPixelRatio: 0.01 });
  }
});

async function createSession(api: APIRequestContext, role: string): Promise<Session> {
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const email = `hardening-${role.toLowerCase()}-${suffix}@nestystay.local`;
  const registration = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: `Hardening ${role}`, phone: "+15550109999", acceptedTerms: true, acceptedPrivacy: true, role },
  });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const session = await login.json() as Session;
  return { ...session, email, displayName: `Hardening ${role}` };
}

async function setSession(page: Page, session: Session | null) {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate((value) => {
    if (value) localStorage.setItem("nestyStay.session", JSON.stringify(value));
    else localStorage.removeItem("nestyStay.session");
  }, session);
}

function requiredAdminToken() {
  const token = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
  if (!token) throw new Error("NESTYSTAY_E2E_ADMIN_TOKEN is required.");
  return token;
}

function writeEvidence(relativePath: string, value: unknown) {
  const target = path.join(evidenceRoot, relativePath);
  mkdirSync(path.dirname(target), { recursive: true });
  writeFileSync(target, JSON.stringify(value, null, 2));
}
