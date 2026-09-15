import { expect, request as playwrightRequest, test } from "@playwright/test";
import { installCookieSession } from "./helpers/session";

const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;

test.describe.configure({ mode: "serial", timeout: 120_000 });

test.beforeAll(async ({ baseURL }) => {
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const seed = await api.post("/api/spec/seed");
    expect(seed.ok(), await seed.text()).toBeTruthy();
  } finally {
    await api.dispose();
  }
});

test("admin can search badge assignments and review expiry and audit surfaces", async ({ page }) => {
  test.skip(!adminToken, "NESTYSTAY_E2E_ADMIN_TOKEN is required for the privileged badge-management journey.");
  await installCookieSession(page, {
    userId: "00000000-0000-0000-0000-000000000001",
    email: "client-admin@nestystay.local",
    displayName: "NestyStay Administrator",
    accessToken: adminToken!,
    expiresAt: new Date(Date.now() + 86_400_000).toISOString(),
    roles: ["Admin"],
    permissions: ["super_administration", "system_configuration", "user_management", "audit_log_access"],
  });

  await page.goto("/admin/ops/badges", { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("admin-badges-page")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Badge management" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Search badge assignments" })).toBeVisible();
  await expect(page.getByText("Badge catalog", { exact: true })).toBeVisible();
  await expect(page.getByText("Price & assignment history", { exact: true })).toBeVisible();
  await page.getByRole("textbox", { name: "Search badge assignments" }).fill("does-not-exist");
  await expect(page.getByText("No badge assignments match this search.", { exact: true })).toBeVisible();
});

test("host renewal payment is ownership-scoped and persists through the API", async ({ baseURL }) => {
  test.skip(!adminToken, "NESTYSTAY_E2E_ADMIN_TOKEN is required for the privileged badge-renewal journey.");
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const host = await createHost(api, "renewal-host");
    await prepareAuthoritativeBadgeFacts(api, host);
    const verifiedPurchase = await api.post("/api/badges-pricing/badges/purchase-intent", {
      headers: { Authorization: `Bearer ${adminToken}`, "Idempotency-Key": `m2-verified-${host.userId}` },
      data: {
        subjectType: "Host",
        subjectId: host.userId,
        level: "Verified",
        hostVerificationPassed: true,
      },
    });
    expect(verifiedPurchase.ok(), await verifiedPurchase.text()).toBeTruthy();
    const verifiedPayment = await verifiedPurchase.json() as { status: string; assignmentId?: string };
    expect(verifiedPayment.status).toBe("CAPTURED");
    expect(verifiedPayment.assignmentId).toBeTruthy();

    const purchase = await api.post("/api/badges-pricing/badges/purchase-intent", {
      headers: { Authorization: `Bearer ${adminToken}`, "Idempotency-Key": `m2-trusted-${host.userId}` },
      data: {
        subjectType: "Host",
        subjectId: host.userId,
        level: "Trusted",
        hostVerificationPassed: true,
        completedApprovedBookings: 3,
      },
    });
    expect(purchase.ok(), await purchase.text()).toBeTruthy();
    const trustedPayment = await purchase.json() as { status: string; assignmentId?: string };
    expect(trustedPayment.status).toBe("CAPTURED");
    expect(trustedPayment.assignmentId).toBeTruthy();
    const assignment = { id: trustedPayment.assignmentId! };

    const renewals = await api.get(`/api/badges-pricing/renewals?assignmentId=${assignment.id}`, {
      headers: { Authorization: `Bearer ${adminToken}` },
    });
    expect(renewals.ok(), await renewals.text()).toBeTruthy();
    const renewalRows = await renewals.json() as { paymentStatus: string }[];
    expect(renewalRows.some((item) => item.paymentStatus === "PENDING")).toBeTruthy();

    const paid = await api.post(`/api/badges-pricing/renewals/${assignment.id}/pay`, {
      headers: { Authorization: `Bearer ${host.accessToken}`, "Idempotency-Key": `m2-renewal-${host.userId}` },
    });
    expect(paid.ok(), await paid.text()).toBeTruthy();

    const otherHost = await createHost(api, "other-renewal-host");
    const forbidden = await api.post(`/api/badges-pricing/renewals/${assignment.id}/pay`, {
      headers: { Authorization: `Bearer ${otherHost.accessToken}` },
    });
    expect(forbidden.status()).toBe(403);
  } finally {
    await api.dispose();
  }
});

async function createHost(api: Awaited<ReturnType<typeof playwrightRequest.newContext>>, prefix: string) {
  const email = `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@nestystay.local`;
  const password = "NestyStay1";
  const registered = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName: prefix,
      phone: "+15550102030",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role: "Host",
    },
  });
  expect(registered.ok(), await registered.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as { userId: string; accessToken: string };
  expect(body.accessToken).toBeTruthy();
  return body;
}

async function prepareAuthoritativeBadgeFacts(
  api: Awaited<ReturnType<typeof playwrightRequest.newContext>>,
  host: { userId: string; accessToken: string; displayName?: string; email?: string },
  includeWellnessSubscription = false,
) {
  const hostHeaders = { Authorization: "Bearer " + host.accessToken };
  const submitted = await api.post("/api/host-verification", {
    headers: hostHeaders,
    data: { documentType: "Passport", notes: "Automated badge eligibility fixture." },
  });
  expect(submitted.ok(), await submitted.text()).toBeTruthy();

  const review = await api.post("/api/host-verification/" + host.userId + "/decision", {
    headers: { Authorization: "Bearer " + adminToken },
    data: { status: "Approved", reason: "Automated badge eligibility fixture approved." },
  });
  expect(review.ok(), await review.text()).toBeTruthy();

  const propertyResponse = await api.post("/api/properties", {
    headers: hostHeaders,
    data: {
      hostUserId: host.userId,
      hostName: host.displayName ?? "Badge fixture host",
      hostEmail: host.email ?? "badge-fixture@nestystay.local",
      title: "Badge payment fixture " + Date.now(),
      location: "Kingston, Jamaica",
      parish: "St. Andrew",
      country: "Jamaica",
      nightlyRate: 120,
      currency: "USD",
      badgeLevel: "Free",
      cancellationPolicy: "Flexible",
      maxGuests: 2,
      guestVerificationEnabled: false,
      insuraGuestEnabled: false,
    },
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string };

  for (let index = 0; index < 3; index += 1) {
    const guest = await createGuest(api, "badge-booking-guest-" + index);
    const checkIn = new Date(Date.now() + (45 + index * 4) * 86_400_000).toISOString().slice(0, 10);
    const checkOut = new Date(Date.now() + (46 + index * 4) * 86_400_000).toISOString().slice(0, 10);
    const booking = await api.post("/api/bookings", {
      headers: { Authorization: "Bearer " + guest.accessToken },
      data: { propertyId: property.id, guestUserId: guest.userId, checkIn, checkOut, adults: 1, children: 0, termsAccepted: true },
    });
    expect(booking.ok(), await booking.text()).toBeTruthy();
    const bookingBody = await booking.json() as { status: string };
    expect(bookingBody.status).toBe("APPROVED");
  }

  if (includeWellnessSubscription) {
    const subscription = await api.post("/api/wellness/subscriptions", { headers: hostHeaders });
    expect(subscription.ok(), await subscription.text()).toBeTruthy();
  }
}

async function createGuest(api: Awaited<ReturnType<typeof playwrightRequest.newContext>>, prefix: string) {
  const email = prefix + "-" + Date.now() + "-" + Math.random().toString(36).slice(2, 8) + "@nestystay.local";
  const password = "NestyStay1";
  const registered = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName: prefix,
      phone: "+15550102031",
      acceptedTerms: true,
      acceptedPrivacy: true,
      role: "Guest",
    },
  });
  expect(registered.ok(), await registered.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as { userId: string; accessToken: string };
  expect(body.accessToken).toBeTruthy();
  return body;
}

test("host badge workspace exposes all four real levels on desktop and mobile", async ({ baseURL, page }, testInfo) => {
  test.skip(!adminToken, "NESTYSTAY_E2E_ADMIN_TOKEN is required for the seeded badge-level journey.");
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const host = await createHost(api, "badge-levels-host");
    await prepareAuthoritativeBadgeFacts(api, host, true);
    const headers = (key: string) => ({ Authorization: `Bearer ${adminToken}`, "Idempotency-Key": `m2-level-${key}-${host.userId}` });
    for (const [level, facts] of [
      ["Verified", { hostVerificationPassed: true }],
      ["Trusted", { hostVerificationPassed: true, completedApprovedBookings: 3 }],
      ["Wellness", { hostVerificationPassed: true, completedApprovedBookings: 3, hasPropertyAddress: true, hasWellnessSubscription: true }],
    ] as const) {
      const response = await api.post("/api/badges-pricing/badges/purchase-intent", {
        headers: headers(level),
        data: { subjectType: "Host", subjectId: host.userId, level, ...facts },
      });
      expect(response.ok(), await response.text()).toBeTruthy();
    }
    await installCookieSession(page, host);
    await page.goto("/host/badges", { waitUntil: "domcontentloaded" });
    await expect(page.getByTestId("host-13-page")).toBeVisible();
    for (const level of ["FREE", "VERIFIED", "TRUSTED", "WELLNESS"]) {
      await expect(page.getByText(level, { exact: true }).first()).toBeVisible();
    }
    await expect(page.getByText("Unlocked vs locked", { exact: true })).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath("m2-badge-levels.png"), fullPage: true });
  } finally {
    await api.dispose();
  }
});
