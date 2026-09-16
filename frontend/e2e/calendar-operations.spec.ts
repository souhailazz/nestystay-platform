import { expect, request as playwrightRequest, test, type APIRequestContext } from "@playwright/test";
import { installCookieSession } from "./helpers/session";

test.describe.configure({ timeout: 120_000, mode: "serial" });

test("host calendar operations persist through the real UI", async ({ baseURL, page }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const host = await createHostSession(api);
    const propertyResponse = await api.post("/api/properties", {
      headers: { Authorization: `Bearer ${host.accessToken}` },
      data: {
        hostUserId: host.userId,
        hostName: host.displayName,
        hostEmail: host.email,
        title: `Calendar operations stay ${Date.now()}`,
        location: "Montego Bay",
        parish: "St. James",
        country: "Jamaica",
        nightlyRate: 175,
        currency: "USD",
        badgeLevel: "Verified",
        cancellationPolicy: "Flexible",
        maxGuests: 4,
        guestVerificationEnabled: false,
      },
    });
    expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();

    await installCookieSession(page, host);
    await page.goto("/calendar", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Availability and date holds.", exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: "External calendar sync", exact: true })).toBeVisible();
    await expect(page.getByText("No inbound feeds connected yet.", { exact: true })).toBeVisible();

    await page.getByLabel("External ICS feed URL").fill("https://calendar.airbnb.example/ical/calendar-operations.ics");
    await page.getByRole("button", { name: "Connect feed", exact: true }).click();
    await expect(page.getByText(/Airbnb · https:\/\/calendar\.airbnb\.example/)).toBeVisible();
    await expect(page.getByText(/Connected · 0 blocked dates/)).toBeVisible();

    const from = page.getByLabel("From");
    const to = page.getByLabel("To");
    const reason = page.getByLabel("Reason");
    await from.fill(futureDate(20));
    await to.fill(futureDate(22));
    await reason.fill("Owner visit");
    await page.getByRole("button", { name: "Add hold", exact: true }).click();
    await expect(page.getByText(`${futureDate(20)} to ${futureDate(22)}`, { exact: false })).toBeVisible();
    await expect(page.getByText("Owner visit", { exact: false })).toBeVisible();

    await page.getByRole("button", { name: "Edit", exact: true }).click();
    await reason.fill("Maintenance hold");
    await page.getByRole("button", { name: "Save hold", exact: true }).click();
    await expect(page.getByText("Maintenance hold", { exact: false })).toBeVisible();
    await expect(page.getByText("Owner visit", { exact: false })).toHaveCount(0);

    await page.getByRole("button", { name: "Create private link", exact: true }).click();
    await expect(page.getByRole("status").filter({ hasText: "/api/calendar/export/" })).toBeVisible();
    await expect(page.getByText(/The link contains no guest details/)).toBeVisible();

    await page.getByRole("button", { name: "Release", exact: true }).click();
    await expect(page.getByText("Maintenance hold", { exact: false })).toHaveCount(0);
  } finally {
    await api.dispose();
  }
});

async function createHostSession(api: APIRequestContext) {
  const email = `calendar-ui-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const password = "NestyStay1";
  const registered = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName: "Calendar Operations Host",
      phone: "+15550102030",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role: "Host",
    },
  });
  expect(registered.ok(), await registered.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as { userId: string; accessToken: string; expiresAt: string; roles: string[]; permissions?: string[] };
  return {
    userId: body.userId,
    email,
    displayName: "Calendar Operations Host",
    accessToken: body.accessToken,
    expiresAt: body.expiresAt,
    roles: body.roles,
    permissions: body.permissions ?? [],
  };
}

function futureDate(days: number) {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}
