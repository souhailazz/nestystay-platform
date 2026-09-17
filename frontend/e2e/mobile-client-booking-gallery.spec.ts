import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";
import { installCookieSession } from "./helpers/session";

type Session = { userId: string; email: string; displayName: string; accessToken: string; roles: string[]; permissions: string[] };
type Property = { id: string; title: string; guestVerificationEnabled?: boolean };

const password = "NestyStay1";
const evidenceDirectory = path.resolve(process.cwd(), "..", "testing-evidence", "mobile-client-booking-gallery");

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("captures the real guest/client booking experience on mobile", async ({ baseURL, page }) => {
  mkdirSync(evidenceDirectory, { recursive: true });
  const api = await playwrightRequest.newContext({ baseURL });
  const seed = await api.post("/api/spec/seed");
  expect(seed.ok(), await seed.text()).toBeTruthy();

  const guest = await createGuest(api);
  const propertiesResponse = await api.get("/api/properties");
  expect(propertiesResponse.ok(), await propertiesResponse.text()).toBeTruthy();
  const properties = await propertiesResponse.json() as Property[];
  const property = properties.find(item => item.guestVerificationEnabled === false) ?? properties[0];
  expect(property, "a seeded public property is required").toBeTruthy();

  await screenshotRoute(page, "/", "CLIENT-01-landing.png");
  await screenshotRoute(page, "/explore", "CLIENT-02-explore-search.png");
  await screenshotRoute(page, "/explore/map", "CLIENT-03-explore-map.png");

  await installCookieSession(page, guest);
  await page.goto(`/properties/${property!.id}`, { waitUntil: "networkidle" });
  await expect(page.getByRole("button", { name: "Book this stay", exact: true })).toHaveCount(1);
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-04-property-detail.png"), fullPage: false });

  await page.getByRole("button", { name: "Book this stay", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-05-booking-modal.png"), fullPage: false });

  const dateInputs = dialog.locator('input[type="date"]');
  const dateInputCount = await dateInputs.count();
  expect(dateInputCount).toBe(2);
  const quoteButton = dialog.getByRole("button", { name: "Get quote", exact: true });
  const createButton = dialog.getByRole("button", { name: "Create booking", exact: true });
  const quoteButtonCount = await quoteButton.count();
  const createButtonCount = await createButton.count();
  expect(quoteButtonCount).toBe(1);
  expect(createButtonCount).toBe(1);

  let selectedCheckIn = "";
  let selectedCheckOut = "";
  for (let attempt = 0; attempt < 8; attempt += 1) {
    const checkInDate = new Date(Date.now() + (120 + attempt * 11) * 86_400_000);
    const checkOutDate = new Date(checkInDate.getTime() + 3 * 86_400_000);
    selectedCheckIn = checkInDate.toISOString().slice(0, 10);
    selectedCheckOut = checkOutDate.toISOString().slice(0, 10);
    await dateInputs.nth(0).fill(selectedCheckIn);
    await dateInputs.nth(1).fill(selectedCheckOut);
    await quoteButton.click();
    await page.waitForTimeout(500);
    if (await createButton.isEnabled()) break;
  }
  expect(selectedCheckIn).not.toBe("");
  await expect(createButton).toBeEnabled({ timeout: 30_000 });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-06-booking-quote.png"), fullPage: false });

  await createButton.click();
  await page.waitForURL(/\/booking\/[0-9a-f-]+\/(checkout|identity|review)$/, { timeout: 30_000 });
  const bookingId = page.url().match(/\/booking\/([0-9a-f-]+)\//)?.[1];
  expect(bookingId).toBeTruthy();
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-07-booking-checkout.png"), fullPage: false });

  await page.goto(`/booking/${bookingId}/pending`, { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-08-booking-pending.png"), fullPage: false });

  await page.goto("/guest-dashboard", { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-09-guest-trips.png"), fullPage: false });
  await page.goto("/traveler/reservations", { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-10-reservations.png"), fullPage: false });
  await page.goto("/traveler/favorites", { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-11-saved-stays.png"), fullPage: false });
  await page.goto("/traveler/identity", { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-12-identity-verification.png"), fullPage: false });
  await page.goto("/traveler/notifications", { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(evidenceDirectory, "CLIENT-13-notifications.png"), fullPage: false });

  await api.dispose();
  console.log(`Saved 13 client-side mobile screenshots to ${evidenceDirectory}`);
});

async function screenshotRoute(page: Page, route: string, filename: string) {
  const response = await page.goto(route, { waitUntil: "networkidle" });
  expect(response?.status() ?? 0, `${route} should render successfully`).toBeLessThan(500);
  await page.screenshot({ path: path.join(evidenceDirectory, filename), fullPage: false });
}

async function createGuest(api: APIRequestContext): Promise<Session> {
  const email = `mobile-client-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: "Mobile Client Guest", phone: "+15550105066", acceptedTerms: true, acceptedPrivacy: true, role: "Guest" },
  });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { ...(await login.json()) as Session, email, displayName: "Mobile Client Guest" };
}
