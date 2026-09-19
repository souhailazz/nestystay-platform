import { expect, test } from "@playwright/test";

const publicJourneys = [
  ["homepage", "/"],
  ["login", "/login"],
  ["property listing", "/explore"],
  ["booking quote", "/booking/quote"],
  ["badges", "/host/badges"],
  ["wellness", "/host/wellness"],
  ["custodian directory", "/directory/custodians"],
  ["trades directory", "/directory/trades"],
  ["local business directory", "/directory/businesses"],
  ["police directory", "/directory/police"],
  ["property manager dashboard", "/pm/dashboard"],
  ["owner portal", "/owner/dashboard"],
  ["gate QR", "/gate/qr"],
] as const;

test("deployed public journeys load without server errors", async ({ page }) => {
  const serverErrors: string[] = [];
  page.on("response", (response) => {
    if (response.status() >= 500) serverErrors.push(`${response.status()} ${response.url()}`);
  });

  for (const [name, path] of publicJourneys) {
    const response = await page.goto(path, { waitUntil: "domcontentloaded" });
    expect(response, `${name} did not return a response`).not.toBeNull();
    expect(response!.status(), `${name} returned a server error`).toBeLessThan(500);
    await expect(page.locator("body")).toBeVisible();
  }

  expect(serverErrors, "deployed journey responses").toEqual([]);
});

test("deployed health endpoints report liveness and readiness", async ({ request }) => {
  for (const path of ["/api/health/live", "/api/health/ready", "/api/health"]) {
    const response = await request.get(path);
    expect(response.status(), `${path} health check`).toBe(200);
  }
});

test("deployed login journey works with an explicitly supplied smoke account", async ({ page }) => {
  test.skip(!process.env.SMOKE_EMAIL || !process.env.SMOKE_PASSWORD, "Set SMOKE_EMAIL and SMOKE_PASSWORD for the non-destructive authenticated smoke journey.");

  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.getByLabel("Email").fill(process.env.SMOKE_EMAIL!);
  await page.getByLabel("Password").fill(process.env.SMOKE_PASSWORD!);
  await page.getByRole("button", { name: /^Log in/ }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
});
