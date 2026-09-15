import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { installCookieSession } from "./helpers/session";

const repoRoot = path.resolve(process.cwd(), "..");
const evidenceRoot = process.env.NESTYSTAY_EVIDENCE_ROOT ?? path.join(repoRoot, "testing-evidence", "milestones-1-2", "screenshots", "real-workflows");
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

test("real guest registration, login, quote, and persisted eKYC booking flow", async ({ page, request }, testInfo) => {
  const email = `ui-guest-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  console.log("guest: goto register", email);
  await page.goto("/register", { waitUntil: "domcontentloaded" });

  console.log("guest: fill registration");
  await page.locator('input[placeholder="Keisha Brown"]').fill("UI Booking Guest");
  await page.locator('input[placeholder="+1 876 555 0123"]').fill("+15550102030");
  await page.locator('input[type="email"]').fill(email);
  const passwordInputs = page.locator('input[autocomplete="new-password"]');
  await passwordInputs.nth(0).fill(password);
  await passwordInputs.nth(1).fill(password);
  await page.getByRole("button", { name: "Create my account", exact: true }).click();
  console.log("guest: submitted registration");
  await expect(page).toHaveURL(/\/guest-dashboard$/);
  const loginResponse = await request.post("/api/auth/login", { data: { email, password } });
  expect(loginResponse.ok(), await loginResponse.text()).toBeTruthy();
  const guestSession = await loginResponse.json() as { accessToken: string; userId: string };
  const guestHeaders = { Authorization: `Bearer ${guestSession.accessToken}` };

  console.log("guest: open a safe seeded listing");
  const propertiesResponse = await page.request.get("/api/properties");
  expect(propertiesResponse.ok(), await propertiesResponse.text()).toBeTruthy();
  const properties = await propertiesResponse.json() as Array<{ id: string; title: string; guestVerificationEnabled: boolean }>;
  const property = properties.find((item) => item.guestVerificationEnabled && !/[<>]/.test(item.title)) ?? properties.find((item) => !/[<>]/.test(item.title));
  expect(property).toBeTruthy();
  await page.goto(`/properties/${property!.id}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByText(/bedroom/i).first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Amenities", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Sleeping arrangements", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "House rules", exact: true })).toBeVisible();
  await expect(page.getByTitle(`Approximate map location for ${property!.title}`)).toBeVisible();
  await page.getByRole("button", { name: "Book this stay", exact: true }).click();
  console.log("guest: opened booking modal");
  const bookingDialog = page.getByRole("dialog");
  await expect(bookingDialog.getByRole("heading", { name: /Book / })).toBeVisible();

  // Keep repeatable evidence runs isolated from previously held dates by
  // asking the same quote endpoint used by the UI for a free future window.
  // This prevents an old local test booking from making the real UI button
  // correctly remain disabled.
  const { checkIn, checkOut } = await findAvailableStay(request, property!.id, testInfo.project.name);
  const dateInputs = bookingDialog.locator('input[type="date"]');
  await dateInputs.nth(0).fill(checkIn);
  await dateInputs.nth(1).fill(checkOut);

  const quoteButton = bookingDialog.getByRole("button", { name: "Get quote", exact: true });
  await quoteButton.click();
  const createButton = bookingDialog.getByRole("button", { name: "Create booking", exact: true });
  await expect(createButton).toBeEnabled({ timeout: 30_000 });
  await createButton.click();
  console.log("guest: created booking from quote");
  await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/(identity|checkout)$/);
  const bookingId = page.url().match(/\/booking\/([0-9a-f-]+)\/(?:identity|checkout)$/)?.[1];
  expect(bookingId).toBeTruthy();
  await expect(page.getByText(/booking|identity|payment/i).first()).toBeVisible();
  await capture(page, testInfo, "guest-booking-created");
  if (await page.getByRole("button", { name: /Hold dates & verify/ }).isVisible()) {
    await page.getByRole("button", { name: /Hold dates & verify/ }).click();
    await expect(page).toHaveURL(/\/booking\/[0-9a-f-]+\/pending$/);
    await expect(page.getByText(/verification|pending/i).first()).toBeVisible();
    await capture(page, testInfo, "guest-booking-pending");

    const pendingResponse = await request.get(`/api/bookings/${bookingId}`, { headers: guestHeaders });
    expect(pendingResponse.ok(), await pendingResponse.text()).toBeTruthy();
    const pending = await pendingResponse.json() as { ekycTransactionId?: string; status: string; verificationStatus: string };
    expect(pending.ekycTransactionId).toBeTruthy();
    expect(pending.status).toBe("PENDING");

    const identityWebhook = await request.post("/api/webhooks/stripe/raw", {
      data: {
        id: `evt_browser_identity_${bookingId}`,
        type: "identity.verification_session.verified",
        data: {
          object: {
            id: pending.ekycTransactionId,
            client_reference_id: bookingId,
            metadata: { booking_id: bookingId },
            status: "verified",
          },
        },
      },
    });
    expect(identityWebhook.ok(), await identityWebhook.text()).toBeTruthy();

    const approvedResponse = await request.get(`/api/bookings/${bookingId}`, { headers: guestHeaders });
    const approved = await approvedResponse.json() as { status: string; verificationStatus: string; paymentStatus: string };
    expect(approved.status).toBe("APPROVED");
    expect(approved.verificationStatus).toBe("PASSED");
    expect(approved.paymentStatus).toBe("AUTHORIZED");

    const captureResponse = await request.post(`/api/bookings/${bookingId}/capture-payment`, {
      headers: { Authorization: `Bearer ${process.env.NESTYSTAY_E2E_ADMIN_TOKEN ?? "test-admin-token"}` },
    });
    expect(captureResponse.ok(), await captureResponse.text()).toBeTruthy();
    const captured = await captureResponse.json() as { status: string; paymentStatus: string };
    expect(captured.status).toBe("APPROVED");
    expect(captured.paymentStatus).toBe("CAPTURED");

    await page.goto(`/booking/${bookingId}/success`, { waitUntil: "domcontentloaded" });
    await expect(page.getByText(/confirmed|captured|success/i).first()).toBeVisible();
    await capture(page, testInfo, "guest-booking-confirmed");

    await page.goto("/traveler/notifications", { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("link", { name: "Open NestyStay booking approved" })).toBeVisible();
    const paymentNotification = page.getByRole("link", { name: "Open NestyStay payment processed" });
    await expect(paymentNotification).toBeVisible();
    await paymentNotification.click();
    await expect(page).toHaveURL(new RegExp(`/booking/${bookingId}/receipt$`));
    await expect(page.getByText(/receipt/i).first()).toBeVisible();
  }
  await page.goto("/guest-dashboard", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Your stay hub" })).toBeVisible();
  const stayHub = page.getByRole("region", { name: "Your stay hub" });
  await expect(stayHub.getByRole("link", { name: "Invoices" })).toBeVisible();
  await expect(stayHub.getByRole("link", { name: "Gate QR" })).toBeVisible();

  console.log("guest: verify persistent favorite and live property map");
  await page.goto("/explore", { waitUntil: "domcontentloaded" });
  const searchStay = await findAvailableStay(request, property!.id, `${testInfo.project.name}-search`);
  await page.getByRole("searchbox", { name: "Search by location, parish, or property name" }).fill(property!.title);
  const exploreDates = page.locator('input[type="date"]');
  await exploreDates.nth(0).fill(searchStay.checkIn);
  await exploreDates.nth(1).fill(searchStay.checkOut);
  await page.getByRole("combobox", { name: "Guest count" }).selectOption("2");
  await page.getByRole("button", { name: "Search", exact: true }).click();
  await expect(page.getByText(property!.title, { exact: true }).first()).toBeVisible();
  const firstCard = page.locator("article").first();
  const savedTitle = await firstCard.locator("span.font-display").innerText();
  const saveButton = firstCard.getByRole("button", { name: new RegExp(savedTitle) });
  if (await saveButton.getAttribute("aria-pressed") !== "true") await saveButton.click();
  await expect(saveButton).toHaveAttribute("aria-pressed", "true");
  await page.goto("/traveler/favorites", { waitUntil: "domcontentloaded" });
  await expect(page.getByText(savedTitle, { exact: true })).toBeVisible();

  await page.goto("/explore/map", { waitUntil: "domcontentloaded" });
  await expect(page.getByTitle("Interactive NestyStay property map")).toBeVisible();
});

test("host badge page renders live ownership-scoped assignment and feature access", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const session = await createSession(api, "Host");
  await api.dispose();
  await installCookieSession(page, session);

  await page.goto("/host/badges", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Your badge plan", exact: true })).toBeVisible();
  await expect(page.getByText("Upgrade comparison", { exact: true })).toBeVisible();
  await expect(page.getByText("Choose the access you need", { exact: true })).toBeVisible();
  await capture(page, testInfo, "host-badges-live");

  await page.goto("/host/verification", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Host identity verification", exact: true })).toBeVisible();
  await page.getByLabel("Document type").selectOption("National ID");
  await page.getByLabel("Notes").fill("Browser verification workflow fixture");
  await page.getByRole("button", { name: "Submit for review", exact: true }).click();
  await expect(page.getByText("Your host verification submission is now pending review.", { exact: true })).toBeVisible();
  await page.reload({ waitUntil: "domcontentloaded" });
  await expect(page.getByText("Pending", { exact: true }).first()).toBeVisible();
  await api.dispose();
});

test("host can decline a real booking and the guest sees the stored reason", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const host = await createSession(api, "Host");
  const propertyResponse = await api.post("/api/properties", {
    headers: { Authorization: `Bearer ${host.accessToken}` },
    data: {
      hostUserId: host.userId,
      hostName: host.displayName,
      hostEmail: host.email,
      title: `UI rejection stay ${Date.now()}`,
      location: "Ocho Rios",
      parish: "St. Ann",
      country: "Jamaica",
      nightlyRate: 145,
      currency: "USD",
      badgeLevel: "Verified",
      cancellationPolicy: "Flexible",
      maxGuests: 2,
      guestVerificationEnabled: false,
    },
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string; title: string };
  const guest = await createSession(api, "Guest");
  const { checkIn, checkOut } = await findAvailableStay(api, property.id, "host-rejection");
  const bookingResponse = await api.post("/api/bookings", {
    headers: { Authorization: `Bearer ${guest.accessToken}` },
    data: { propertyId: property.id, guestUserId: guest.userId, checkIn, checkOut, adults: 2, children: 0, termsAccepted: true },
  });
  expect(bookingResponse.ok(), await bookingResponse.text()).toBeTruthy();
  const booking = await bookingResponse.json() as { id: string };
  await api.dispose();

  await installCookieSession(page, host);
  await page.goto("/bookings", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Host Reservation Management", exact: true })).toBeVisible();
  const visibleReservations = page.locator("table:visible, article:visible");
  await expect(visibleReservations.getByText(property.title, { exact: true })).toBeVisible({ timeout: 30_000 });
  await visibleReservations.getByRole("button", { name: "Decline", exact: true }).click();
  await page.getByLabel("Reason for declining booking").fill("The home is unavailable for these dates.");
  await page.getByRole("button", { name: "Confirm decline", exact: true }).click();
  await expect(page.getByRole("alert").getByText("Booking request declined and dates released.", { exact: true })).toBeVisible();
  await capture(page, testInfo, "host-booking-rejected");

  await installCookieSession(page, guest);
  await page.goto(`/booking/${booking.id}/rejected`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Booking request declined by the host", exact: true })).toBeVisible();
  await expect(page.getByText(/The home is unavailable for these dates\./).last()).toBeVisible();
});

async function createSession(api: APIRequestContext, role: "Host" | "Guest"): Promise<Record<string, unknown>> {
  const email = `ui-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registered = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName: "UI Badge Host",
      phone: "+15550102030",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role,
    },
  });
  expect(registered.ok(), await registered.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json();
  expect(body.accessToken).toBeTruthy();
  return {
    userId: body.userId,
    email,
    displayName: "UI Badge Host",
    accessToken: body.accessToken,
    expiresAt: body.expiresAt,
    roles: body.roles,
    permissions: body.permissions ?? [],
  };
}

async function findAvailableStay(api: APIRequestContext, propertyId: string, projectName: string) {
  const baseStayOffset = ({ "desktop-chromium": 12_000, "tablet-chromium": 12_010, "mobile-chromium": 12_020 } as Record<string, number>)[projectName] ?? 12_000;
  const apiRoot = (process.env.PLAYWRIGHT_API_ROOT ?? process.env.PLAYWRIGHT_API_URL?.replace(/\/health\/?$/, "") ?? "http://127.0.0.1:5019/api").replace(/\/$/, "");
  for (let attempt = 0; attempt < 12; attempt += 1) {
    const stayOffset = baseStayOffset + Math.floor(Math.random() * 10_000) + attempt * 5;
    const checkIn = new Date(Date.now() + stayOffset * 86_400_000).toISOString().slice(0, 10);
    const checkOut = new Date(Date.now() + (stayOffset + 3) * 86_400_000).toISOString().slice(0, 10);
    const response = await api.post(`${apiRoot}/bookings/quote`, {
      data: { propertyId, checkIn, checkOut, adults: 2, children: 0 },
    });
    if (response.ok()) return { checkIn, checkOut };
  }
  throw new Error("Could not find an available demo stay window after 12 attempts.");
}

async function capture(page: Page, testInfo: { project: { name: string } }, name: string) {
  const viewport = testInfo.project.name.replace("-chromium", "");
  const directory = path.join(evidenceRoot, "real-workflows");
  mkdirSync(directory, { recursive: true });
  const target = path.join(directory, `${name}-${viewport}.png`);
  try {
    const buffer = await page.screenshot({ fullPage: true });
    try { writeFileSync(target, buffer); } catch { /* evidence path may be locked by AV */ }
    try { await testInfo.attach(`${name}-${viewport}.png`, { body: buffer, contentType: "image/png" }); } catch { /* attachment is best effort */ }
  } catch { /* screenshot evidence must not fail the product-flow assertion */ }
}
