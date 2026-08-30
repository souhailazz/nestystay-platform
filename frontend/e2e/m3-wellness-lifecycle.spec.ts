import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

type Session = { userId: string; email: string; displayName: string; accessToken: string; expiresAt?: string; roles: string[]; permissions: string[] };
const password = "NestyStay1";
const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
if (!adminToken) throw new Error("NESTYSTAY_E2E_ADMIN_TOKEN is required for this suite.");
const evidenceDir = path.resolve(process.cwd(), "..", "testing-evidence", "milestones-1-4", "wellness");
const onePixelPng = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=", "base64");

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("M3 wellness lifecycle: host request, officer report, completion and payout", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const host = await createSession(api, "Host", "Lifecycle Host");
  const officerSession = await createSession(api, "Officer", "Lifecycle Officer");
  const headers = (token: string) => ({ Authorization: `Bearer ${token}` });

  for (const level of ["Verified", "Wellness"]) {
    const badge = await api.post("/api/badges-pricing/badges/purchase", {
      headers: headers(adminToken),
      data: { subjectType: "Host", subjectId: host.userId, level, hostVerificationPassed: true, completedApprovedBookings: 3, hasPropertyAddress: true, hasWellnessSubscription: true, paymentSucceeded: true },
    });
    expect(badge.ok(), await badge.text()).toBeTruthy();
  }

  const propertyResponse = await api.post("/api/properties", {
    headers: headers(host.accessToken),
    data: { hostUserId: host.userId, hostName: host.displayName, hostEmail: host.email, title: `Lifecycle Villa ${Date.now()}`, location: "Ocho Rios", country: "Jamaica", nightlyRate: 180, currency: "USD", badgeLevel: "Wellness", guestVerificationEnabled: false, insuraGuestEnabled: false, cancellationPolicy: "Flexible" },
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string };

  const badgeNumber = `LIFE-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
  const application = await api.post("/api/wellness/officers", {
    headers: headers(officerSession.accessToken),
    data: { userId: officerSession.userId, badgeNumber, parish: "St. Ann", coverageArea: "Ocho Rios", isActiveOffDuty: true, isRetired: false, verificationMetadata: "Live lifecycle applicant evidence" },
  });
  expect(application.ok(), await application.text()).toBeTruthy();
  const officer = await application.json() as { id: string; verificationStatus: string };
  expect(officer.verificationStatus).toBe("Pending");
  const approval = await api.post(`/api/wellness/officers/${officer.id}/approve`, { headers: headers(adminToken), data: { reason: "Live lifecycle acceptance" } });
  expect(approval.ok(), await approval.text()).toBeTruthy();

  const scheduledAt = new Date(Date.now() + 2_000).toISOString();
  const visitResponse = await api.post("/api/wellness/visits", {
    headers: headers(host.accessToken),
    data: { hostUserId: host.userId, propertyId: property.id, visitType: "StandardWellnessCheck", scheduledAt, parish: "St. Ann", area: "Ocho Rios" },
  });
  expect(visitResponse.ok(), await visitResponse.text()).toBeTruthy();
  const requested = await visitResponse.json() as { id: string; visitStatus: string; price: number; officerPayoutAmount: number };
  expect(requested.visitStatus).toBe("Requested");
  expect(requested.price).toBe(50);
  expect(requested.officerPayoutAmount).toBe(46);

  const assignment = await api.post(`/api/wellness/visits/${requested.id}/assign`, { headers: headers(adminToken), data: { officerId: officer.id } });
  expect(assignment.ok(), await assignment.text()).toBeTruthy();
  const scheduled = await assignment.json() as { visitStatus: string; officerBadgeNumber: string };
  expect(scheduled.visitStatus).toBe("Scheduled");
  expect(scheduled.officerBadgeNumber).toBe(badgeNumber.toUpperCase());

  await installSession(page, host);
  await page.goto("/host/wellness", { waitUntil: "networkidle" });
  await expect(page.getByText(/Wellness visits/i).first()).toBeVisible();
  await expect(page.getByText(/Scheduled/i).first()).toBeVisible();
  await capture(page, testInfo, "host-request");

  await installSession(page, officerSession);
  await page.goto("/officer/wellness", { waitUntil: "networkidle" });
  await expect(page.getByText(/Officer wellness/i).first()).toBeVisible();
  const inputs = page.locator('input:not([type="checkbox"]):not([type="file"])');
  await inputs.nth(3).fill(requested.id);
  await inputs.nth(4).fill(badgeNumber);
  await page.locator('input[type="file"]').setInputFiles({ name: "wellness-report.png", mimeType: "image/png", buffer: onePixelPng });
  await expect(page.getByText("Clean", { exact: true }).first()).toBeVisible({ timeout: 30_000 });
  await page.getByRole("button", { name: /Submit report/i }).click();
  await expect(page.getByText(/Report submitted/i)).toBeVisible({ timeout: 30_000 });
  await capture(page, testInfo, "officer-report");

  const reportResponse = await api.get(`/api/wellness/visits/${requested.id}/report`, { headers: headers(host.accessToken) });
  expect(reportResponse.ok(), await reportResponse.text()).toBeTruthy();
  const report = await reportResponse.json() as { reportStatus: string; photos: string[] };
  expect(report.reportStatus).toBe("Submitted");
  expect(report.photos).toHaveLength(1);

  const completedResponse = await api.get(`/api/wellness/visits/${requested.id}`, { headers: headers(host.accessToken) });
  const completed = await completedResponse.json() as { visitStatus: string; paymentStatus: string; timeline: string[] };
  expect(completed.visitStatus).toBe("Completed");
  expect(completed.paymentStatus).toBe("PayoutPending");
  expect(completed.timeline.join(" ")).toMatch(/Photo report submitted|Officer payout pending/);

  const payouts = await api.get(`/api/wellness/payouts?status=Pending`, { headers: headers(adminToken) });
  expect(payouts.ok(), await payouts.text()).toBeTruthy();
  const pendingPayouts = await payouts.json() as Array<{ visitId: string; officerAmount: number; status: string }>;
  const payout = pendingPayouts.find((item) => item.visitId === requested.id);
  expect(payout?.officerAmount).toBe(46);
  const paid = await api.post(`/api/wellness/visits/${requested.id}/payout`, { headers: headers(adminToken), data: { providerReference: `browser-payout-${Date.now()}` } });
  expect(paid.ok(), await paid.text()).toBeTruthy();
  expect((await paid.json() as { status: string }).status).toBe("Paid");

  await installSession(page, host);
  await page.goto("/host/wellness", { waitUntil: "networkidle" });
  await expect(page.getByText(/Completed/i).first()).toBeVisible();
  await expect(page.getByText(/Report submitted · 1 verified photo/i).first()).toBeVisible({ timeout: 30_000 });
  await capture(page, testInfo, "host-completed");
  await api.dispose();
});

async function createSession(api: APIRequestContext, role: "Guest" | "Host" | "Officer", displayName: string): Promise<Session> {
  const email = `lifecycle-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: "+15550102033", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json() as Session;
  return { ...body, email, displayName, accessToken: body.accessToken };
}

async function installSession(page: Page, session: Session) {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate((value) => localStorage.setItem("nestyStay.session", JSON.stringify(value)), session);
}

async function capture(page: Page, testInfo: { project: { name: string } }, name: string) {
  mkdirSync(evidenceDir, { recursive: true });
  await page.screenshot({ fullPage: true, path: path.join(evidenceDir, `${name}-${testInfo.project.name}.png`) });
}
