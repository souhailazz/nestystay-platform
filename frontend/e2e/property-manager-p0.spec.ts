import { expect, test } from "@playwright/test";

/**
 * Route-level acceptance for the P0 money and authority workspace.  The
 * authenticated lifecycle is covered by the API/PostgreSQL tests because a
 * browser test cannot safely invent a manager session.  These checks still
 * ensure the real production routes are reachable, show the correct access
 * boundary, and do not fall through to an unrelated screen.
 */
test("P0 manager workspace has an explicit sign-in boundary", async ({ page }) => {
  await page.goto("/pm/p0");
  await expect(page.getByRole("heading", { name: "Sign in required", exact: true })).toBeVisible();
  await expect(page.getByText("owner money and authority controls", { exact: false })).toBeVisible();
});

test("P0 owner portal has an explicit sign-in boundary", async ({ page }) => {
  await page.goto("/owner/p0");
  await expect(page.getByRole("heading", { name: "Owner portal requires a session.", exact: true })).toBeVisible();
  await expect(page.getByText("owner identity", { exact: false })).toHaveCount(0);
});
