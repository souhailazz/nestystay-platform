import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test.describe("public privacy and consent surfaces", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => window.localStorage.removeItem("nesty-cookie-consent-v2"));
  });

  test("policy routes are readable and linked from the footer", async ({ page }) => {
    for (const [path, heading] of [["/privacy", "Privacy Policy"], ["/terms", "Terms of Service"], ["/cookies", "Cookie Policy"], ["/refund-policy", "Refund Policy"]] as const) {
      await page.goto(path, { waitUntil: "domcontentloaded" });
      await expect(page.getByRole("heading", { name: heading, exact: true })).toBeVisible();
      await expect(page.getByRole("link", { name: "Privacy", exact: true })).toBeVisible();
      await expect(page.getByRole("link", { name: "Terms", exact: true })).toBeVisible();
      await expect(page.getByRole("link", { name: "Cookies", exact: true })).toBeVisible();
      await expect(page.getByRole("link", { name: "Refunds", exact: true })).toBeVisible();
    }
  });

  test("optional cookies require an explicit choice and settings can be reopened", async ({ page }) => {
    await page.goto("/", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("region", { name: "Your cookie choices" })).toBeVisible();
    await page.getByRole("button", { name: "Reject optional" }).click();
    await expect(page.getByRole("region", { name: "Your cookie choices" })).toHaveCount(0);
    // Desktop exposes the compact Explore nav item; mobile exposes it inside
    // the responsive menu. Both are the same public destination.
    const menuButton = page.getByRole("button", { name: "Open menu", exact: true });
    if (await menuButton.isVisible()) await menuButton.click();
    await page.getByRole("link", { name: "Explore", exact: true }).click();
    await page.getByRole("button", { name: "Cookie settings", exact: true }).click();
    await expect(page.getByRole("dialog", { name: "Manage cookie preferences" })).toBeVisible();
    await expect(page.getByRole("checkbox", { name: /Essential storage/i })).toBeDisabled();
    await expect(page.getByRole("checkbox", { name: /Analytics/i })).not.toBeChecked();
    await expect(page.getByRole("checkbox", { name: /Marketing/i })).not.toBeChecked();
  });

  test("preference dialog supports keyboard focus and preserves optional tracking denial", async ({ page }) => {
    const requests: string[] = [];
    page.on("request", (request) => requests.push(request.url()));
    await page.goto("/", { waitUntil: "networkidle" });
    await page.getByRole("button", { name: "Manage preferences" }).click();
    const dialog = page.getByRole("dialog", { name: "Manage cookie preferences" });
    await expect(dialog.locator("[tabindex='-1']")).toBeFocused();
    await page.keyboard.press("Tab");
    await expect.poll(() => page.evaluate(() => Boolean(document.activeElement?.closest('[role="dialog"]')))).toBe(true);
    await page.keyboard.press("Shift+Tab");
    await expect.poll(() => page.evaluate(() => Boolean(document.activeElement?.closest('[role="dialog"]')))).toBe(true);
    await page.keyboard.press("Escape");
    await expect(page.getByRole("region", { name: "Your cookie choices" })).toBeVisible();
    expect(requests.some((url) => /googletagmanager|google-analytics/i.test(url))).toBe(false);
  });

  test("consent surfaces expose accessible names and have no axe violations", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle" });
    const bannerAxe = await new AxeBuilder({ page }).include('[role="region"]').analyze();
    expect(bannerAxe.violations).toEqual([]);

    await page.getByRole("button", { name: "Manage preferences" }).click();
    await expect(page.getByRole("dialog", { name: "Manage cookie preferences" })).toBeVisible();
    const dialogAxe = await new AxeBuilder({ page }).include('[role="dialog"]').analyze();
    expect(dialogAxe.violations).toEqual([]);
  });

  test("registration consent is explicit and initially unchecked", async ({ page }) => {
    await page.goto("/register", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("checkbox")).toHaveCount(2);
    for (const checkbox of await page.getByRole("checkbox").all()) await expect(checkbox).not.toBeChecked();
    await expect(page.getByRole("link", { name: "Terms of Service", exact: true })).toBeVisible();
    await expect(page.locator("#AUTH-01").getByRole("link", { name: "Privacy Policy", exact: true })).toBeVisible();
  });
});
