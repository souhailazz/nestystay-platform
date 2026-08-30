import { expect, request as playwrightRequest, test, type APIRequestContext, type Page, type TestInfo } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.cwd(), "..");
const evidenceRoot = process.env.NESTYSTAY_EVIDENCE_ROOT ?? path.join(repoRoot, "testing-evidence", "milestones-1-2", "screenshots", "stripe-test-mode");
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

test("Stripe test-mode checkout receives a real PaymentIntent client secret", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const session = await createGuestSession(api);
  const properties = await api.get("/api/properties");
  expect(properties.ok(), await properties.text()).toBeTruthy();
  const property = (await properties.json() as Array<{ id: string; guestVerificationEnabled: boolean }>).find((item) => !item.guestVerificationEnabled);
  expect(property).toBeTruthy();

  const offset = 5000 + Math.floor(Math.random() * 1000);
  const checkInDate = new Date(Date.now() + offset * 86400000);
  const checkOutDate = new Date(checkInDate.getTime() + 2 * 86400000);
  const booking = await api.post("/api/bookings", {
    headers: { Authorization: `Bearer ${session.accessToken}` },
    data: {
      propertyId: property!.id,
      guestUserId: session.userId,
      checkIn: checkInDate.toISOString().slice(0, 10),
      checkOut: checkOutDate.toISOString().slice(0, 10),
    },
  });
  expect(booking.ok(), await booking.text()).toBeTruthy();
  const bookingBody = await booking.json() as { id: string; status: string; paymentProvider?: string; paymentClientSecret?: string };
  expect(bookingBody.status).toBe("APPROVED");
  expect(bookingBody.paymentProvider).toBe("Stripe");
  expect(bookingBody.paymentClientSecret).toMatch(/^pi_[^_]+_secret_/);
  await api.dispose();

  await page.addInitScript((value) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(value));
  }, session);
  await page.goto(`/booking/${bookingBody.id}/checkout`, { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("book-03-page")).toBeVisible({ timeout: 60_000 });
  await expect(page.getByText(/Processed directly via Stripe/)).toBeVisible();
  await page.waitForTimeout(3_000);
  expect(await page.locator("iframe").count()).toBeGreaterThan(0);
  await capture(page, testInfo, "stripe-checkout");
});

async function createGuestSession(api: APIRequestContext) {
  const email = `qa-stripe-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registered = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: "QA Stripe Guest", phone: "+15550102030", acceptedTerms: true, acceptedPrivacy: true, role: "Guest" },
  });
  expect(registered.ok(), await registered.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json();
  expect(body.accessToken).toBeTruthy();
  return {
    userId: body.userId,
    email,
    displayName: "QA Stripe Guest",
    accessToken: body.accessToken,
    expiresAt: body.expiresAt,
    roles: body.roles,
    permissions: body.permissions ?? [],
  };
}

async function capture(page: Page, testInfo: TestInfo, name: string) {
  const viewport = testInfo.project.name.replace("-chromium", "");
  const directory = path.join(evidenceRoot, "stripe");
  mkdirSync(directory, { recursive: true });
  await page.screenshot({ fullPage: true, path: path.join(directory, `${name}-${viewport}.png`) });
}
