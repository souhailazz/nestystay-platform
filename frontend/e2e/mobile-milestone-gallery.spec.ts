import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdirSync } from "node:fs";
import path from "node:path";
import { installCookieSession } from "./helpers/session";

type Session = { userId: string; email: string; displayName: string; accessToken: string; roles: string[]; permissions: string[] };

const password = "NestyStay1";
const evidenceDirectory = path.resolve(process.cwd(), "..", "testing-evidence", "mobile-milestone-gallery");

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("captures a real mobile screenshot gallery for all five milestones", async ({ baseURL, page }) => {
  mkdirSync(evidenceDirectory, { recursive: true });
  const api = await playwrightRequest.newContext({ baseURL });
  const seed = await api.post("/api/spec/seed");
  expect(seed.ok(), await seed.text()).toBeTruthy();

  const host = await createSession(api, "Host", "Mobile Gallery Host");
  const guest = await createSession(api, "Guest", "Mobile Gallery Guest");
  const manager = await createSession(api, "PropertyManager", "Mobile Gallery Manager");
  const owner = await createSession(api, "Owner", "Mobile Gallery Owner");

  const propertyResponse = await api.post("/api/properties", {
    headers: { Authorization: `Bearer ${host.accessToken}` },
    data: {
      hostUserId: host.userId,
      hostName: host.displayName,
      hostEmail: host.email,
      title: "Mobile Gallery Villa",
      location: "Montego Bay, Jamaica",
      country: "Jamaica",
      nightlyRate: 185,
      currency: "USD",
      badgeLevel: "Trusted",
      guestVerificationEnabled: false,
      insuraGuestEnabled: false,
      cancellationPolicy: "Flexible",
    },
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string };

  const insurance = await api.post(`/api/insurance/properties/${property.id}/policy`, {
    headers: { Authorization: `Bearer ${host.accessToken}` },
    data: { planCode: "non-us-50", idempotencyKey: `mobile-gallery-${Date.now()}` },
  });
  expect(insurance.ok(), await insurance.text()).toBeTruthy();

  const invitedOwner = await postJson(api, "/api/property-manager/owners", manager, { email: owner.email, displayName: owner.displayName });
  const ownerId = (invitedOwner as { ownerUserId: string }).ownerUserId;
  const managedProperty = await postJson(api, "/api/property-manager/properties", manager, { ownerUserId: ownerId, title: "Mobile Gallery Managed Home", unitNumber: "MG-01", address: "Kingston, Jamaica" }) as { id: string };
  await postJson(api, "/api/property-manager/invoices", manager, { ownerUserId: ownerId, propertyId: managedProperty.id, dueDate: "2099-01-01", tax: 5, lines: [{ description: "Monthly community fee", quantity: 1, unitAmount: 125 }] });

  await screenshotRoute(page, "/explore", "M1-01-explore-search.png");
  await screenshotRoute(page, "/explore/map", "M1-02-explore-map.png");
  await screenshotRoute(page, "/login", "M1-03-login-security.png");

  await installSession(page, host);
  await screenshotRoute(page, "/host-dashboard", "M2-01-host-dashboard-badges.png");
  await screenshotRoute(page, "/host/insurance", "M2-02-host-insurance-coverage.png");
  await screenshotRoute(page, "/host/properties", "M2-03-host-properties.png");

  await screenshotRoute(page, "/host/wellness", "M3-01-host-wellness.png");
  await screenshotRoute(page, "/host/wellness/book", "M3-02-wellness-booking.png");
  await screenshotRoute(page, "/host/wellness/directory", "M3-03-wellness-directory.png");

  await screenshotRoute(page, "/directory/custodians", "M4-01-custodian-directory.png");
  await screenshotRoute(page, "/directory/police", "M4-02-police-trust-directory.png");
  await screenshotRoute(page, "/gate/qr", "M4-03-qr-gate-validator.png");

  await installSession(page, manager);
  await screenshotRoute(page, "/pm/dashboard", "M5-01-manager-dashboard.png");
  await screenshotRoute(page, "/pm/invoices", "M5-02-manager-invoices.png");
  await installSession(page, owner);
  await screenshotRoute(page, "/owner/dashboard", "M5-03-owner-portal.png");

  await api.dispose();
  console.log(`Saved 15 mobile screenshots to ${evidenceDirectory}`);
  // Keep the setup sessions in the flow so the generated screens are real, but
  // make it explicit that no personal or production account was used.
  void guest;
});

async function screenshotRoute(page: Page, route: string, filename: string) {
  const response = await page.goto(route, { waitUntil: "networkidle" });
  expect(response?.status() ?? 0, `${route} should render successfully`).toBeLessThan(500);
  await page.screenshot({ path: path.join(evidenceDirectory, filename), fullPage: false });
}

async function createSession(api: APIRequestContext, role: string, displayName: string): Promise<Session> {
  const email = `mobile-gallery-${role.toLowerCase()}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registration = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName, phone: "+15550105055", acceptedTerms: true, acceptedPrivacy: true, role },
  });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { ...(await login.json()) as Session, email, displayName };
}

async function installSession(page: Page, session: Session) {
  await installCookieSession(page, session);
}

async function postJson(api: APIRequestContext, route: string, session: Session, data: unknown) {
  const response = await api.post(route, { headers: { Authorization: `Bearer ${session.accessToken}` }, data });
  expect(response.ok(), `${route}: ${await response.text()}`).toBeTruthy();
  return response.json();
}
