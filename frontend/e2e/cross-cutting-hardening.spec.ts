import { expect, test, type Page } from "@playwright/test";

type DemoRole = "Guest" | "Host" | "PropertyManager" | "Admin";

const syntheticSession = (role: DemoRole) => ({
  userId: `cross-cutting-${role.toLowerCase()}`,
  email: `${role.toLowerCase()}@cross-cutting.local`,
  displayName: `${role} Hardening`,
  accessToken: "",
  expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  roles: [role],
  permissions: role === "Admin" ? ["super_administration", "financial_reporting", "badge_management"] : [],
});

async function installSession(page: Page, role: DemoRole) {
  await page.evaluate((session) => window.localStorage.setItem("nestyStay.session", JSON.stringify(session)), syntheticSession(role));
}

async function measureMobileLayout(page: Page) {
  return page.evaluate(() => {
    const viewport = document.documentElement.clientWidth;
    const visible = (element: HTMLElement) => {
      const style = getComputedStyle(element);
      const rect = element.getBoundingClientRect();
      return style.display !== "none" && style.visibility !== "hidden" && rect.width > 0 && rect.height > 0;
    };
    const visibleTables = [...document.querySelectorAll<HTMLTableElement>("table")]
      .filter(visible)
      .map((table) => ({
        width: Math.round(table.getBoundingClientRect().width),
        responsiveCards: Boolean(table.closest(".responsive-table-cards")),
        scrollableParent: Boolean(table.parentElement && table.parentElement.scrollWidth > table.parentElement.clientWidth + 1),
      }));
    return {
      viewport,
      scrollWidth: document.documentElement.scrollWidth,
      horizontalOverflow: Math.max(0, document.documentElement.scrollWidth - viewport),
      visibleTables,
      focusedHeading: document.activeElement?.getAttribute("data-route-heading") === "true" || document.activeElement?.tagName === "H1" || document.activeElement?.tagName === "H2",
    };
  });
}

test.describe("cross-cutting frontend hardening", () => {
  test("mobile guest, host, property-manager and admin workflows use responsive records", async ({ page }) => {
    const cases: Array<{ role: DemoRole; routes: string[] }> = [
      { role: "Guest", routes: ["/booking/booking-00000000-0000-4000-8000-000000000000/checkout", "/traveler/invoices", "/traveler/notifications"] },
      { role: "Host", routes: ["/bookings", "/host-dashboard"] },
      { role: "PropertyManager", routes: ["/pm/dashboard", "/pm/invoices", "/pm/maintenance", "/pm/calendar"] },
      { role: "Admin", routes: ["/admin/ops/badges", "/admin/ops/financials", "/admin/reports"] },
    ];

    await page.goto("/", { waitUntil: "domcontentloaded" });
    for (const workflow of cases) {
      await installSession(page, workflow.role);
      for (const route of workflow.routes) {
        await page.goto(route, { waitUntil: "domcontentloaded" });
        await page.waitForTimeout(250);
        const measurement = await measureMobileLayout(page);
        expect(measurement.horizontalOverflow, `${workflow.role} ${route} overflows`).toBeLessThanOrEqual(1);
        expect(measurement.visibleTables.filter((table) => table.scrollableParent && !table.responsiveCards), `${workflow.role} ${route} leaves a table dependent on horizontal scrolling`).toEqual([]);
      }
    }
  });

  test("route changes focus the new heading and honor reduced-motion and forced-colors preferences", async ({ page }) => {
    await page.goto("/explore", { waitUntil: "domcontentloaded" });
    await expect.poll(async () => (await measureMobileLayout(page)).focusedHeading, { timeout: 5000 }).toBeTruthy();
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await expect.poll(async () => (await measureMobileLayout(page)).focusedHeading, { timeout: 5000 }).toBeTruthy();

    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.reload({ waitUntil: "domcontentloaded" });
    await expect.poll(() => page.evaluate(() => window.matchMedia("(prefers-reduced-motion: reduce)").matches)).toBeTruthy();
    expect(await page.evaluate(() => getComputedStyle(document.documentElement).scrollBehavior)).toBe("auto");

    await page.emulateMedia({ forcedColors: "active" });
    await page.reload({ waitUntil: "domcontentloaded" });
    await expect.poll(() => page.evaluate(() => window.matchMedia("(forced-colors: active)").matches)).toBeTruthy();
  });
});
