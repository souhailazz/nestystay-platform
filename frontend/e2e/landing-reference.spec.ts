import { expect, test } from "@playwright/test";

test("landing search preserves the new popovers and sends selections to Explore", async ({ page }, testInfo) => {
  if (testInfo.project.name === "desktop-chromium") {
    await page.setViewportSize({ width: 1672, height: 941 });
  }
  const pageErrors: string[] = [];
  page.on("pageerror", error => pageErrors.push(error.message));
  await page.goto("/", { waitUntil: "networkidle" });
  await page.evaluate(() => document.fonts.ready);
  await expect(page.getByRole("heading", { name: /More than.*a place to stay.*It's yaad/i })).toBeVisible();
  await expect(page.locator(".reference-hero__photo")).toBeVisible();
  await expect(page.getByRole("form", { name: "Find a stay" })).toBeVisible();
  await page.screenshot({ path: `../testing-evidence/landing-match/${testInfo.project.name}.png`, scale: "css" });
  const imageLoaded = await page.locator(".reference-hero__photo").evaluate(node => (node as HTMLImageElement).naturalWidth > 0);
  expect(imageLoaded).toBe(true);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);

  await page.getByRole("button", { name: "Where to?", exact: true }).click();
  await page.getByPlaceholder("Search destination").fill("Kingston");
  await page.getByRole("button", { name: "Kingston Kingston", exact: true }).click();
  await expect(page.getByText("When yuh staying?", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Next month", exact: true }).click();
  const dates = await page.evaluate(() => {
    const next = new Date();
    next.setMonth(next.getMonth() + 1, 1);
    const prefix = `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, "0")}`;
    return { checkIn: `${prefix}-12`, checkOut: `${prefix}-16` };
  });
  await page.getByRole("button", { name: dates.checkIn, exact: true }).click();
  await page.getByRole("button", { name: dates.checkOut, exact: true }).click();
  await page.getByRole("button", { name: "Done", exact: true }).click();
  await page.getByRole("button", { name: "Increase adults", exact: true }).click();
  await page.getByRole("button", { name: "Done", exact: true }).click();
  await expect(page.locator(".nesty-popover")).toHaveCount(0);

  if (testInfo.project.name === "mobile-chromium") {
    await page.getByRole("form", { name: "Find a stay" }).scrollIntoViewIfNeeded();
    await page.screenshot({ path: "../testing-evidence/landing-match/mobile-search.png", scale: "css" });
  }
  await page.getByRole("button", { name: "Search stays", exact: true }).click();
  await expect(page).toHaveURL(/\/explore\?/);
  await expect(page.getByLabel("Check-in date", { exact: true })).toHaveValue(dates.checkIn);
  await expect(page.getByLabel("Check-out date", { exact: true })).toHaveValue(dates.checkOut);
  await expect(page.getByLabel("Guest count", { exact: true })).toHaveValue("3");
  expect(new URL(page.url()).searchParams.get("search")).toBe("Kingston");
  expect(pageErrors).toEqual([]);
});
