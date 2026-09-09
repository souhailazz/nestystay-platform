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
    const verifiedPurchase = await api.post("/api/badges-pricing/badges/purchase", {
      headers: { Authorization: `Bearer ${adminToken}` },
      data: {
        subjectType: "Host",
        subjectId: host.userId,
        level: "Verified",
        hostVerificationPassed: true,
        paymentSucceeded: true,
      },
    });
    expect(verifiedPurchase.ok(), await verifiedPurchase.text()).toBeTruthy();
    const verifiedAssignment = await verifiedPurchase.json() as { id: string; subjectId: string; status: string; paymentStatus: string };
    expect(verifiedAssignment.subjectId).toBe(host.userId);
    expect(verifiedAssignment.status).toBe("Active");
    expect(verifiedAssignment.paymentStatus).toBe("CAPTURED");

    const purchase = await api.post("/api/badges-pricing/badges/purchase", {
      headers: { Authorization: `Bearer ${adminToken}` },
      data: {
        subjectType: "Host",
        subjectId: host.userId,
        level: "Trusted",
        hostVerificationPassed: true,
        completedApprovedBookings: 3,
        paymentSucceeded: true,
      },
    });
    expect(purchase.ok(), await purchase.text()).toBeTruthy();
    const assignment = await purchase.json() as { id: string; level: string };
    expect(assignment.level).toBe("Trusted");

    const renewals = await api.get(`/api/badges-pricing/renewals?assignmentId=${assignment.id}`, {
      headers: { Authorization: `Bearer ${adminToken}` },
    });
    expect(renewals.ok(), await renewals.text()).toBeTruthy();
    const renewalRows = await renewals.json() as { paymentStatus: string }[];
    expect(renewalRows.some((item) => item.paymentStatus === "PENDING")).toBeTruthy();

    const paid = await api.post(`/api/badges-pricing/renewals/${assignment.id}/pay`, {
      headers: { Authorization: `Bearer ${host.accessToken}` },
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
