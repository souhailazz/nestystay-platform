import { expect, test } from "@playwright/test";

test.describe("public privacy and consent surfaces", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => window.localStorage.removeItem("nesty-cookie-consent-v1"));
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
    await expect(page.getByRole("dialog", { name: "Your cookie choices" })).toBeVisible();
    await page.getByRole("button", { name: "Reject optional" }).click();
    await expect(page.getByRole("dialog", { name: "Your cookie choices" })).toHaveCount(0);
    await page.getByRole("link", { name: "Explore", exact: true }).click();
    await page.getByRole("button", { name: "Cookie settings", exact: true }).click();
    await expect(page.getByRole("dialog", { name: "Your cookie choices" })).toBeVisible();
  });

  test("registration consent is explicit and initially unchecked", async ({ page }) => {
    await page.goto("/register", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("checkbox")).toHaveCount(2);
    for (const checkbox of await page.getByRole("checkbox").all()) await expect(checkbox).not.toBeChecked();
    await expect(page.getByRole("link", { name: "Terms of Service", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Privacy Policy", exact: true })).toBeVisible();
  });
});
