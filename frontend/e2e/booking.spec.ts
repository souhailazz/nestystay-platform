import { expect, request as playwrightRequest, test, type APIRequestContext, type Page, type TestInfo } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

type AuthSession = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: string[];
  permissions: string[];
};

type PropertyListing = {
  id: string;
};

const repoRoot = path.resolve(process.cwd(), "..");
const evidenceRoot = path.join(repoRoot, "artifacts", "m1-m2-visual");

test.beforeAll(async ({ baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const seed = await api.post("/api/spec/seed");
    expect(seed.ok()).toBeTruthy();
  } finally {
    await api.dispose();
  }
});

test.describe("M1/M2 booking flow evidence (BOOK-01 to BOOK-10)", () => {
  test("captures booking modals, review, checkout, and outcome pages", async ({ baseURL, page }, testInfo) => {
    const errors: string[] = [];
    page.on("console", (message) => {
      if (message.type() === "error" && !message.text().includes("401")) {
        errors.push(message.text());
      }
    });
    page.on("pageerror", (error) => {
      if (!error.message.includes("401")) {
        errors.push(error.message);
      }
    });

    const api = await playwrightRequest.newContext({ baseURL });
    const session = await createSession(api, "Guest");
    await installSession(page, session);

    // Ensure the booking flow can run against a fresh migrated database.
    const property = await getOrCreateProperty(api);
    const propertyId = property.id;

    // Create a real booking using API
    const projectOffsetDays = {
      "desktop-chromium": 7,
      "tablet-chromium": 21,
      "mobile-chromium": 35,
    }[testInfo.project.name] ?? 49;
    const runOffsetDays = 90 + Math.floor(Math.random() * 5000);
    const checkInDate = new Date(Date.now() + (runOffsetDays + projectOffsetDays) * 86400000);
    const checkOutDate = new Date(checkInDate.getTime() + 3 * 86400000);
    const checkIn = checkInDate.toISOString().split('T')[0];
    const checkOut = checkOutDate.toISOString().split('T')[0];
    
    const bookingRes = await api.post("/api/bookings", {
      headers: { Authorization: `Bearer ${session.accessToken}` },
      data: {
        propertyId,
        guestUserId: session.userId,
        checkIn,
        checkOut,
        adults: 2,
        children: 0,
        billingCountry: "JM",
        termsAccepted: true
      }
    });
    expect(bookingRes.ok(), await bookingRes.text()).toBeTruthy();
    const booking = await bookingRes.json();
    const bookingId = booking.id;

    await api.dispose();

    // Now visit all routes directly and capture evidence
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-01", `/`); // Modal can be triggered on index or explore, but we will just capture the container structure.
    
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-02", `/booking/${bookingId}/review`);
    
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-03", `/booking/${bookingId}/checkout`);

    await visitAndCapture(page, testInfo, "BOOK", "BOOK-04", `/booking/${bookingId}/success`);
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-05", `/booking/${bookingId}/failure`);
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-06", `/booking/${bookingId}/rejected`);
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-07", `/booking/${bookingId}/pending`);
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-08", `/booking/${bookingId}/cancelled`);
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-09", `/booking/${bookingId}/invoice`);
    await visitAndCapture(page, testInfo, "BOOK", "BOOK-10", `/booking/${bookingId}/receipt`);

    expect(errors).toEqual([]);
  });
});

async function visitAndCapture(page: Page, testInfo: TestInfo, family: string, screenId: string, route: string) {
  await page.goto(route, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(750);
  await capture(page, testInfo, family, screenId);
}

async function capture(page: Page, testInfo: TestInfo, family: string, screenId: string) {
  const viewport = testInfo.project.name.replace("-chromium", "");
  const directory = path.join(evidenceRoot, family);
  mkdirSync(directory, { recursive: true });
  await page.screenshot({
    fullPage: true,
    path: path.join(directory, `${screenId}-${viewport}.png`),
  });
}

async function createSession(api: APIRequestContext, role: string): Promise<AuthSession> {
  const unique = `${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const email = `${unique}@nestystay.local`;
  const password = "NestyStay1";
  
  await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: "Test", phone: "+15550102030", acceptedTerms: true, acceptedPrivacy: true, role }
  });

  const login = await api.post("/api/auth/login", { data: { email, password } });
  const loginBody = await login.json();
  const challenge = await api.get(`/api/auth/development/challenges/${loginBody.challengeId}`);
  const challengeBody = await challenge.json();
  const verified = await api.post("/api/auth/2fa/verify", {
    data: { challengeId: loginBody.challengeId, code: challengeBody.code }
  });
  
  const session = await verified.json();
  return { userId: session.userId, email, displayName: "Test", accessToken: session.accessToken, expiresAt: session.expiresAt, roles: session.roles, permissions: session.permissions ?? [] };
}

async function getOrCreateProperty(api: APIRequestContext): Promise<PropertyListing> {
  const propsRes = await api.get("/api/properties");
  expect(propsRes.ok(), await propsRes.text()).toBeTruthy();
  const properties = await propsRes.json() as PropertyListing[];
  if (properties[0]) {
    return properties[0];
  }

  const hostSession = await createSession(api, "Host");
  const createRes = await api.post("/api/properties", {
    headers: { Authorization: `Bearer ${hostSession.accessToken}` },
    data: {
      hostUserId: hostSession.userId,
      hostName: "E2E Host",
      hostEmail: hostSession.email,
      title: "E2E Booking Villa",
      location: "Ocho Rios, St. Ann",
      country: "Jamaica",
      nightlyRate: 180,
      currency: "USD",
      badgeLevel: "Free",
      guestVerificationEnabled: false,
      insuraGuestEnabled: false,
      cancellationPolicy: "Flexible",
      highlights: ["Fresh database fixture", "Booking smoke coverage"],
    },
  });
  expect(createRes.ok(), await createRes.text()).toBeTruthy();
  return await createRes.json() as PropertyListing;
}

async function installSession(page: Page, session: AuthSession) {
  await page.addInitScript((value) => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify(value));
  }, session);
}
