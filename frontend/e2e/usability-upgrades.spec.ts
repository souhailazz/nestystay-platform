import { expect, test } from "@playwright/test";

const hostSession = {
  userId: "usability-host",
  email: "usability-host@nestystay.local",
  displayName: "Usability Host",
  accessToken: "local-usability-token",
  expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  roles: ["Host"],
  permissions: [],
};

test.describe("global usability upgrades", () => {
  test("public search is keyboard reachable and deep-links into filtered stays", async ({ page }) => {
    await page.goto("/explore", { waitUntil: "domcontentloaded" });
    if (test.info().project.name === "mobile-chromium") {
      await page.getByRole("button", { name: "Open menu" }).click();
    }
    const globalSearch = page.getByRole("searchbox", { name: "Search stays and workspaces" });
    await expect(globalSearch).toBeVisible();
    await globalSearch.fill("Montego");
    await globalSearch.press("Enter");
    await expect(page).toHaveURL(/\/explore\?search=Montego/);
    await expect(page.getByRole("searchbox", { name: "Filter visible stays" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Export" })).toBeVisible();
  });

  test("role workspace exposes shortcuts, breadcrumbs, back action, and keyboard search", async ({ page }) => {
    await page.goto("/", { waitUntil: "domcontentloaded" });
    await page.evaluate((session) => window.localStorage.setItem("nestyStay.session", JSON.stringify(session)), hostSession);
    await page.goto("/host-dashboard", { waitUntil: "domcontentloaded" });

    await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toBeVisible();
    await expect(page.getByRole("region", { name: "Quick actions" })).toContainText("Add property");
    await expect(page.getByRole("button", { name: "Go back" })).toBeVisible();

    await page.keyboard.press("/");
    const workspaceSearch = page.getByRole("searchbox", { name: "Search workspace" });
    await expect(workspaceSearch).toBeFocused();
    await workspaceSearch.fill("Reservations");
    await expect(page.getByRole("listbox").getByText("Reservations", { exact: true })).toBeVisible();
    await workspaceSearch.press("Escape");
    await expect(workspaceSearch).toHaveValue("");
  });

  test("notification preferences provide accessible channel controls and persistence", async ({ page }) => {
    await page.goto("/traveler/notifications", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Notification preferences" })).toBeVisible();
    const sms = page.getByRole("checkbox", { name: "SMS notifications" });
    await sms.check();
    await page.getByRole("button", { name: "Save preferences" }).click();
    await expect(page.getByRole("status")).toContainText("Notification preferences saved.");
    await page.reload({ waitUntil: "domcontentloaded" });
    await expect(page.getByRole("checkbox", { name: "SMS notifications" })).toBeChecked();
  });

  test("mobile workspace navigation stays visible with large touch targets", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "mobile-chromium", "Mobile navigation is only visible below the md breakpoint.");
    await page.goto("/", { waitUntil: "domcontentloaded" });
    await page.evaluate((session) => window.localStorage.setItem("nestyStay.session", JSON.stringify(session)), hostSession);
    await page.goto("/host-dashboard", { waitUntil: "domcontentloaded" });
    const mobileNav = page.getByRole("navigation", { name: "Mobile workspace navigation" });
    await expect(mobileNav).toBeVisible();
    await expect(mobileNav.locator("a").first()).toHaveCSS("min-height", "48px");
  });
});
