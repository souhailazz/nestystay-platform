import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.cwd(), "..");
const evidenceRoot = process.env.NESTYSTAY_EVIDENCE_ROOT ?? path.join(repoRoot, "testing-evidence", "milestones-1-2", "final-contract-validation", "screenshots");
const password = "NestyStay1";

test.describe.configure({ timeout: 180_000, mode: "serial" });

test.beforeAll(async ({ baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const seed = await api.post("/api/spec/seed");
    expect(seed.ok(), await seed.text()).toBeTruthy();
  } finally {
    await api.dispose();
  }
});

test("host creates an owned listing and guest completes the no-eKYC UI booking path", async ({ baseURL, page }, testInfo) => {
  const host = await registerViaUi(page, "Host", "Contract Host");
  await page.goto("/host/properties", { waitUntil: "domcontentloaded" });
  await expect(page.locator("#HOST-05")).toBeVisible();
  const listingTitle = `Contract disabled ${Date.now()}`;
  await page.getByLabel("Title", { exact: true }).fill(listingTitle);
  await page.getByRole("button", { name: "Publish property", exact: true }).click();
  await expect(page.locator('[role="status"]').filter({ hasText: "is live in the property API" })).toBeVisible({ timeout: 30_000 });

  const api = await playwrightRequest.newContext({ baseURL });
  const ownedResponse = await api.get("/api/properties/owned", { headers: { Authorization: `Bearer ${host.accessToken}` } });
  expect(ownedResponse.ok(), await ownedResponse.text()).toBeTruthy();
  const owned = await ownedResponse.json() as Array<{ id: string; title: string; hostUserId: string; guestVerificationEnabled: boolean; badgeLevel: string }>;
  const property = owned.find((item) => item.title === listingTitle);
  expect(property).toBeTruthy();
  expect(property!.hostUserId).toBe(host.userId);
  expect(property!.guestVerificationEnabled).toBe(false);
  expect(property!.badgeLevel).toBe("Free");
  await capture(page, testInfo, "host-owned-listing-disabled");

  // Follow the listing's real edit link and verify the persisted update is still
  // scoped to the authenticated host (the API ignores spoofed identity fields).
  await page.goto(`/host/properties/edit?id=${property!.id}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("host-06-page")).toBeVisible();
  const editedTitle = `${listingTitle} edited`;
  await page.locator('input[type="text"]').first().fill(editedTitle);
  await page.getByRole("button", { name: "Save Changes", exact: true }).click();
  await expect(page.getByText("Property sections saved and updated.", { exact: true })).toBeVisible({ timeout: 30_000 });
  const editedResponse = await api.get(`/api/properties/${property!.id}`);
  expect(editedResponse.ok(), await editedResponse.text()).toBeTruthy();
  const edited = await editedResponse.json() as { title: string; hostUserId: string; hostName: string; badgeLevel: string };
  expect(edited.title).toBe(editedTitle);
  expect(edited.hostUserId).toBe(host.userId);
  expect(edited.hostName).toBe("Contract Host");
  expect(edited.badgeLevel).toBe("Free");
  await capture(page, testInfo, "host-property-editor");

  await registerViaUi(page, "Guest", "Contract Guest");
  await page.goto(`/properties/${property!.id}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "Book this stay", exact: true }).click();
  await chooseUniqueDates(page, testInfo.project.name, 8000);
  await page.getByRole("button", { name: /Continue to quote/ }).click();
  await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/checkout$/);
  await expect(page.getByTestId("book-03-page").or(page.getByTestId("book-03-stripe-config-missing"))).toBeVisible({ timeout: 60_000 });
  await expect(page.getByText(/Hold dates & verify/i)).toHaveCount(0);
  await capture(page, testInfo, "guest-booking-no-ekyc-checkout");
  await api.dispose();
});

test("guest completes the enabled eKYC UI path through PENDING and held dates", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const propertiesResponse = await api.get("/api/properties");
  expect(propertiesResponse.ok(), await propertiesResponse.text()).toBeTruthy();
  const enabled = (await propertiesResponse.json() as Array<{ id: string; guestVerificationEnabled: boolean }>).find((item) => item.guestVerificationEnabled);
  expect(enabled).toBeTruthy();
  await api.dispose();

  const guest = await registerViaUi(page, "Guest", "Contract eKYC Guest");
  await page.goto(`/properties/${enabled!.id}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByText("eKYC REQUIRED", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Book this stay", exact: true }).click();
  await chooseUniqueDates(page, testInfo.project.name, 9000);
  await page.getByRole("button", { name: /Continue to quote/ }).click();
  await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/identity$/);
  await page.getByRole("button", { name: /Hold dates & verify/ }).click();
  await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/pending$/);
  await expect(page.getByText("DATES HELD FOR", { exact: true })).toBeVisible();
  await expect(page.getByText(/of 60:00 · holdExpiresAt/i)).toBeVisible();
  await expect(page.getByText(/Open eKYC transaction|Preparing verification session/i)).toBeVisible();

  const bookingId = page.url().match(/\/booking\/([0-9a-f-]+)\/pending$/)?.[1];
  expect(bookingId).toBeTruthy();
  const verificationApi = await playwrightRequest.newContext({ baseURL });
  const bookingResponse = await verificationApi.get(`/api/bookings/${bookingId}`, { headers: { Authorization: `Bearer ${guest.accessToken}` } });
  expect(bookingResponse.ok(), await bookingResponse.text()).toBeTruthy();
  const booking = await bookingResponse.json() as { status: string; verificationStatus: string; paymentStatus: string; datesHeld: boolean; holdExpiresAt?: string };
  expect(booking.status).toBe("PENDING");
  expect(booking.verificationStatus).toBe("PENDING");
  expect(booking.paymentStatus).toBe("PENDING");
  expect(booking.datesHeld).toBe(true);
  expect(new Date(booking.holdExpiresAt ?? 0).getTime()).toBeGreaterThan(Date.now());
  await capture(page, testInfo, "guest-booking-ekyc-pending-held");
  await verificationApi.dispose();
});

async function registerViaUi(page: Page, role: "Guest" | "Host", displayName: string) {
  const email = `contract-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  // Establish the app origin before reading localStorage (a fresh Playwright page
  // starts on about:blank, where browser storage access is prohibited).
  await page.goto("/", { waitUntil: "domcontentloaded" });
  const existingSession = await page.evaluate(() => window.localStorage.getItem("nestyStay.session"));
  if (existingSession) {
    await page.goto("/logout", { waitUntil: "domcontentloaded" });
    // The route performs the real API logout; clear the browser fixture as a
    // deterministic test isolation guard if the API is intentionally unavailable.
    await page.evaluate(() => window.localStorage.removeItem("nestyStay.session"));
  }
  await page.goto("/register", { waitUntil: "domcontentloaded" });
  // The registration form uses visible span labels rather than label-for/input ids.
  // Use stable semantic attributes/placeholders so this validates the actual UI.
  await page.getByPlaceholder("Keisha Brown").fill(displayName);
  await page.getByPlaceholder("+1 876 555 0123").fill("+15550102030");
  await page.locator('input[type="email"]').fill(email);
  await page.locator("select").selectOption(role);
  const passwords = page.locator('input[autocomplete="new-password"]');
  await passwords.nth(0).fill(password);
  await passwords.nth(1).fill(password);
  await page.getByRole("button", { name: "Create my account", exact: true }).click();
  await expect(page).toHaveURL(role === "Host" ? /\/host-dashboard$/ : /\/guest-dashboard$/);
  const session = await page.evaluate(() => JSON.parse(window.localStorage.getItem("nestyStay.session") ?? "null") as { userId: string; accessToken: string; email: string });
  expect(session?.accessToken).toBeTruthy();
  return session;
}

async function chooseUniqueDates(page: Page, projectName: string, baseOffset: number) {
  const viewportOffset = projectName.includes("tablet") ? 17 : projectName.includes("mobile") ? 23 : 11;
  const inputs = page.locator('input[type="date"]');
  const continueButton = page.getByRole("button", { name: /Continue to quote/ });
  const seedOffset = baseOffset + viewportOffset + Math.floor(Math.random() * 500);
  // Persistent PostgreSQL evidence can legitimately occupy a previously chosen
  // far-future range. Probe a few deterministic alternatives rather than making
  // the browser test flaky when the API correctly reports a held date.
  for (let attempt = 0; attempt < 8; attempt += 1) {
    const checkInDate = new Date(Date.now() + (seedOffset + attempt * 19) * 86_400_000);
    const checkOutDate = new Date(checkInDate.getTime() + 3 * 86_400_000);
    await inputs.nth(0).fill(checkInDate.toISOString().slice(0, 10));
    await inputs.nth(1).fill(checkOutDate.toISOString().slice(0, 10));
    await page.waitForTimeout(700);
    if (await continueButton.isEnabled()) return;
  }
  await expect(continueButton).toBeEnabled({ timeout: 30_000 });
}

async function capture(page: Page, testInfo: { project: { name: string } }, name: string) {
  const viewport = testInfo.project.name.replace("-chromium", "");
  mkdirSync(evidenceRoot, { recursive: true });
  await page.screenshot({ fullPage: true, path: path.join(evidenceRoot, `${name}-${viewport}.png`) });
}
