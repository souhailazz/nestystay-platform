import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";

const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("M4 QR gate UI issues, validates, rejects wrong property, and shows revocation", async ({ baseURL, page }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const host = await createSession(api, "Host", "QR Gate Host");
  const guest = await createSession(api, "Guest", "QR Gate Guest");

  const propertyResponse = await api.post("/api/properties", {
    headers: bearer(host.accessToken),
    data: {
      hostUserId: host.userId,
      hostName: host.displayName,
      hostEmail: host.email,
      title: `QR Gate Villa ${Date.now()}`,
      location: "Ocho Rios",
      country: "Jamaica",
      nightlyRate: 120,
      currency: "USD",
      badgeLevel: "Free",
      guestVerificationEnabled: false,
      insuraGuestEnabled: false,
      cancellationPolicy: "Flexible",
    },
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string };

  // Start today so the issued access code is immediately usable at the gate.
  const checkIn = new Date();
  const checkOut = new Date(checkIn.getTime() + 2 * 86_400_000);
  const bookingResponse = await api.post("/api/bookings", {
    headers: bearer(guest.accessToken),
    data: {
      propertyId: property.id,
      guestUserId: guest.userId,
      checkIn: checkIn.toISOString().slice(0, 10),
      checkOut: checkOut.toISOString().slice(0, 10),
    },
  });
  expect(bookingResponse.ok(), await bookingResponse.text()).toBeTruthy();
  const booking = await bookingResponse.json() as { id: string; status: string; paymentStatus: string };
  expect(booking.status).toBe("APPROVED");

  const qrResponse = await api.post(`/api/access/qr/bookings/${booking.id}`, { headers: bearer(guest.accessToken) });
  expect(qrResponse.ok(), await qrResponse.text()).toBeTruthy();
  const qr = await qrResponse.json() as { id: string; token: string; propertyId: string };
  expect(qr.propertyId).toBe(property.id);

  const validationRequests: string[] = [];
  page.on("request", (request) => {
    if (request.method() === "POST" && request.url().includes("/api/access/qr/validate")) {
      validationRequests.push(request.url());
    }
  });

  // A scanned QR opens the frontend gate route and auto-validates its query values.
  const gateUrl = `/gate/qr?token=${encodeURIComponent(qr.token)}&propertyId=${encodeURIComponent(property.id)}`;
  await page.goto(gateUrl, { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("qr-gate-page")).toBeVisible();
  await expect(page.getByText("Access approved", { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText("Valid", { exact: true })).toBeVisible();
  await expect(page.getByText(/QR access approved/i)).toBeVisible();
  expect(validationRequests.length).toBeGreaterThan(0);

  const wrongPropertyUrl = `/gate/qr?token=${encodeURIComponent(qr.token)}&propertyId=00000000-0000-0000-0000-000000000001`;
  await page.goto(wrongPropertyUrl, { waitUntil: "domcontentloaded" });
  await expect(page.getByText("Access denied", { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText("WrongProperty", { exact: true })).toBeVisible();
  await expect(page.getByText(/not valid for this property/i)).toBeVisible();

  // The guest workspace uses the same persisted booking and exposes generation,
  // an encoded validator link, and revocation controls.
  await installSession(page, guest);
  await page.goto("/traveler/qr", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("button", { name: "Generate gate QR", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("button", { name: "Generate gate QR", exact: true }).click();
  await expect(page.getByText("Gate QR active", { exact: true })).toBeVisible({ timeout: 30_000 });
  const validatorLink = page.getByRole("link", { name: "Open gate validator", exact: true });
  await expect(validatorLink).toHaveAttribute("href", /\/gate\/qr\?/);
  const activeGateUrl = await validatorLink.getAttribute("href");
  expect(activeGateUrl).toBeTruthy();

  await page.getByRole("button", { name: "Revoke", exact: true }).click();
  await expect(page.getByText("Gate QR active", { exact: true })).toHaveCount(0);

  await page.goto(activeGateUrl!, { waitUntil: "domcontentloaded" });
  await expect(page.getByText("Access denied", { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText("Revoked", { exact: true })).toBeVisible();

  await page.goto(`/gate/qr?token=forged-token&propertyId=${encodeURIComponent(property.id)}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByText("Invalid", { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText("Access denied", { exact: true })).toBeVisible();
  await api.dispose();
});

async function createSession(api: APIRequestContext, role: "Host" | "Guest", displayName: string) {
  const email = `m4-qr-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName, phone: "+15550102030", acceptedTerms: true, acceptedPrivacy: true, role },
  });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as { userId: string; accessToken: string; expiresAt: string; roles: string[]; permissions?: string[] };
  return { userId: body.userId, email, displayName, accessToken: body.accessToken, expiresAt: body.expiresAt, roles: body.roles, permissions: body.permissions ?? [] };
}

function bearer(token: string) {
  return { Authorization: `Bearer ${token}` };
}

async function installSession(page: Page, session: Record<string, unknown>) {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate((value) => localStorage.setItem("nestyStay.session", JSON.stringify(value)), session);
}
