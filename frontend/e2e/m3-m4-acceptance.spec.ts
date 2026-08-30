import { expect, request as playwrightRequest, test, type APIRequestContext } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";

const evidenceRoot = path.resolve(process.cwd(), "..", "testing-evidence", "milestones-1-4", "screenshots");
const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 120_000 });

test("M3/M4 live browser acceptance: officer, wellness, directories, privacy, QR", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const adminToken = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
  if (!adminToken) throw new Error("NESTYSTAY_E2E_ADMIN_TOKEN is required for this suite.");
  const host = await createSession(api, "Host", "M3 M4 Host");
  const officerSession = await createSession(api, "Officer", "M3 M4 Officer");

  const officerResponse = await api.post("/api/wellness/officers", { headers: { Authorization: `Bearer ${officerSession.accessToken}` }, data: {
    userId: officerSession.userId, badgeNumber: `BROWSER-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`, parish: "St. Ann", coverageArea: "Ocho Rios", isActiveOffDuty: true, isRetired: false,
  }});
  expect(officerResponse.ok(), await officerResponse.text()).toBeTruthy();
  const officer = await officerResponse.json() as { id: string; badgeNumber: string; verificationStatus: string };
  expect(officer.verificationStatus).toBe("Pending");
  const approve = await api.post(`/api/wellness/officers/${officer.id}/approve`, { headers: { Authorization: `Bearer ${adminToken}` }, data: { reason: "Browser acceptance" } });
  expect(approve.ok(), await approve.text()).toBeTruthy();

  const provider = await api.post("/api/directories/providers", { headers: { Authorization: `Bearer ${host.accessToken}` }, data: {
    kind: "LocalBusiness", category: "Tours", name: `Browser Provider ${Date.now()} ${Math.random().toString(36).slice(2, 8)}`, parish: "St. Ann", badgeLevel: "Free",
    description: "Persisted browser acceptance provider", availabilitySummary: "Daily", contactMode: "direct", isBrickAndMortar: true, isActive: true,
  }});
  expect(provider.ok(), await provider.text()).toBeTruthy();
  const providerBody = await provider.json() as { slug: string; status: string; isActive: boolean };
  expect(providerBody.status).toBe("PendingReview");
  expect(providerBody.isActive).toBe(false);
  const published = await api.post(`/api/directories/providers/${providerBody.slug}/moderate`, { headers: { Authorization: `Bearer ${adminToken}` }, data: { status: "approve" } });
  expect(published.ok(), await published.text()).toBeTruthy();
  const publicProvider = await api.get(`/api/directories/providers/${providerBody.slug}`);
  expect(publicProvider.ok(), await publicProvider.text()).toBeTruthy();

  const forgedQr = await api.post("/api/access/qr/validate", { data: { token: "forged-token", propertyId: "00000000-0000-0000-0000-000000000001" } });
  expect(forgedQr.ok()).toBeTruthy();
  expect((await forgedQr.json()).valid).toBe(false);

  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.evaluate((session) => localStorage.setItem("nestyStay.session", JSON.stringify(session)), host);
  await page.goto("/directory/businesses", { waitUntil: "networkidle" });
  await expect(page.getByText(/Local businesses/i).first()).toBeVisible();
  mkdirSync(evidenceRoot, { recursive: true });
  await page.screenshot({ path: path.join(evidenceRoot, `m3-m4-directory-${testInfo.project.name}.png`), fullPage: true });

  await page.goto("/host/wellness", { waitUntil: "networkidle" });
  await expect(page.getByText(/Wellness visits/i).first()).toBeVisible();
  await expect(page.getByText("Jamaica Emergency: 119", { exact: true })).toBeVisible();
  await page.screenshot({ path: path.join(evidenceRoot, `m3-m4-wellness-${testInfo.project.name}.png`), fullPage: true });

  await page.goto("/directory/provider", { waitUntil: "networkidle" });
  await expect(page.getByText(/Provider onboarding|Your provider profile/i).first()).toBeVisible();
  await page.screenshot({ path: path.join(evidenceRoot, `m3-m4-provider-${testInfo.project.name}.png`), fullPage: true });

  await page.evaluate((session) => localStorage.setItem("nestyStay.session", JSON.stringify(session)), officerSession);
  await page.goto("/officer/wellness", { waitUntil: "networkidle" });
  await expect(page.getByText(/Officer wellness|Officer onboarding/i).first()).toBeVisible();
  await page.screenshot({ path: path.join(evidenceRoot, `m3-m4-officer-${testInfo.project.name}.png`), fullPage: true });
  await api.dispose();
});

async function createSession(api: APIRequestContext, role: "Host" | "Officer", displayName: string) {
  const email = `m3-m4-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName, phone: "+15550102030", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const body = await login.json();
  return { userId: body.userId as string, email, displayName, accessToken: body.accessToken as string, expiresAt: body.expiresAt, roles: body.roles, permissions: body.permissions ?? [] };
}
