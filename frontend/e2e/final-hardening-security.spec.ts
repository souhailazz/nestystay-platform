import { expect, request as playwrightRequest, test, type APIRequestContext, type APIResponse, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";

type Session = { userId: string; email: string; accessToken: string; roles: string[] };
type AuthorizationCase = { name: string; method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE"; url: string; session?: Session; data?: unknown; expected: number[] };

const evidenceRoot = path.resolve(process.cwd(), "..", "testing-evidence", "final-hardening");
const password = "NestyStay1";

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("authorization matrix rejects anonymous, wrong-role, and cross-owner access", async ({ baseURL }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  const api = await playwrightRequest.newContext({ baseURL });
  const guestA = await createSession(api, "Guest");
  const guestB = await createSession(api, "Guest");
  const hostA = await createSession(api, "Host");
  const hostB = await createSession(api, "Host");
  const managerA = await createSession(api, "PropertyManager");
  const managerB = await createSession(api, "PropertyManager");
  const ownerA = await createSession(api, "Owner");
  const ownerB = await createSession(api, "Owner");
  const officer = await createSession(api, "Officer");
  const provider = await createSession(api, "ServiceProvider");

  const propertyResponse = await api.post("/api/properties", {
    headers: auth(hostA),
    data: propertyPayload(hostA, `Authorization property ${Date.now()}`),
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string };

  const checkIn = new Date(Date.UTC(2075, 0, 1 + Math.floor(Math.random() * 300)));
  const checkOut = new Date(checkIn.getTime() + 3 * 86_400_000);
  const bookingResponse = await api.post("/api/bookings", {
    headers: auth(guestA),
    data: { propertyId: property.id, guestUserId: guestB.userId, checkIn: checkIn.toISOString().slice(0, 10), checkOut: checkOut.toISOString().slice(0, 10) },
  });
  expect(bookingResponse.ok(), await bookingResponse.text()).toBeTruthy();
  const booking = await bookingResponse.json() as { id: string; guestUserId: string };
  expect(booking.guestUserId).toBe(guestA.userId);

  const inviteResponse = await api.post("/api/property-manager/owners", {
    headers: auth(managerA),
    data: { email: ownerA.email, displayName: "Authorization Owner A" },
  });
  expect(inviteResponse.ok(), await inviteResponse.text()).toBeTruthy();
  const invited = await inviteResponse.json() as { ownerUserId: string };
  const managedPropertyResponse = await api.post("/api/property-manager/properties", {
    headers: auth(managerA),
    data: { ownerUserId: invited.ownerUserId, title: "Authorization managed property", unitNumber: "AUTH-1", address: "Kingston" },
  });
  expect(managedPropertyResponse.ok(), await managedPropertyResponse.text()).toBeTruthy();
  const managedProperty = await managedPropertyResponse.json() as { id: string };
  const invoiceResponse = await api.post("/api/property-manager/invoices", {
    headers: auth(managerA),
    data: { ownerUserId: invited.ownerUserId, propertyId: managedProperty.id, dueDate: "2075-12-31", tax: 0, lines: [{ description: "Authorization rent", quantity: 1, unitAmount: 100 }] },
  });
  expect(invoiceResponse.ok(), await invoiceResponse.text()).toBeTruthy();
  const invoice = await invoiceResponse.json() as { id: string };

  const maintenanceResponse = await api.post("/api/property-manager/maintenance", {
    headers: auth(managerA),
    data: { ownerUserId: ownerA.userId, propertyId: managedProperty.id, title: "Authorization maintenance", description: "Isolation fixture", category: "General", urgency: "NORMAL" },
  });
  expect(maintenanceResponse.ok(), await maintenanceResponse.text()).toBeTruthy();
  const maintenance = await maintenanceResponse.json() as { id: string };
  const documentResponse = await api.post("/api/property-manager/documents", {
    headers: auth(managerA),
    data: { ownerUserId: ownerA.userId, propertyId: managedProperty.id, title: "Authorization document", category: "Finance", fileName: "authorization.pdf", contentType: "application/pdf", sizeBytes: 12, contentBase64: Buffer.from("%PDF-1.7\nfoo").toString("base64") },
  });
  expect(documentResponse.ok(), await documentResponse.text()).toBeTruthy();
  const document = await documentResponse.json() as { id: string };
  const proposalResponse = await api.post("/api/property-manager/governance/proposals", {
    headers: auth(managerA),
    data: { title: "Authorization proposal", description: "Isolation fixture", opensAt: new Date(Date.now() - 60_000).toISOString(), closesAt: new Date(Date.now() + 86_400_000).toISOString(), isAnonymous: true, quorum: 1 },
  });
  expect(proposalResponse.ok(), await proposalResponse.text()).toBeTruthy();
  const proposal = await proposalResponse.json() as { id: string };
  const qrResponse = await api.post("/api/property-manager/qr", {
    headers: auth(managerA),
    data: { ownerUserId: ownerA.userId, propertyId: managedProperty.id, subjectType: "OWNER", validFrom: new Date(Date.now() - 60_000).toISOString(), validUntil: new Date(Date.now() + 86_400_000).toISOString() },
  });
  expect(qrResponse.ok(), await qrResponse.text()).toBeTruthy();
  const qr = await qrResponse.json() as { id: string };

  const anonymous: AuthorizationCase[] = [
    { name: "profile", method: "GET", url: "/api/auth/profile", expected: [401] },
    { name: "booking list", method: "GET", url: "/api/bookings", expected: [401] },
    { name: "owned properties", method: "GET", url: "/api/properties/owned", expected: [401] },
    { name: "property manager dashboard", method: "GET", url: "/api/property-manager/dashboard", expected: [401] },
    { name: "owner portal", method: "GET", url: "/api/property-manager/owner/portal", expected: [401] },
    { name: "wellness subscriptions", method: "GET", url: "/api/wellness/subscriptions", expected: [401] },
    { name: "wellness visits", method: "GET", url: "/api/wellness/visits", expected: [401] },
    { name: "messages inbox", method: "GET", url: "/api/spec/messages/inbox", expected: [401] },
    { name: "admin operations", method: "GET", url: "/api/spec/admin/operations", expected: [401] },
    { name: "badge assignments", method: "GET", url: "/api/badges-pricing/badges/assignments", expected: [401] },
  ];

  const wrongRole: AuthorizationCase[] = [
    { name: "guest cannot create property", method: "POST", url: "/api/properties", session: guestA, data: propertyPayload(guestA, "Spoofed"), expected: [403] },
    { name: "guest cannot list host properties", method: "GET", url: "/api/properties/owned", session: guestA, expected: [403] },
    { name: "guest cannot use PM dashboard", method: "GET", url: "/api/property-manager/dashboard", session: guestA, expected: [403] },
    { name: "guest cannot use owner portal", method: "GET", url: "/api/property-manager/owner/portal", session: guestA, expected: [403] },
    { name: "guest cannot use wellness admin", method: "GET", url: "/api/wellness/admin/dashboard", session: guestA, expected: [403] },
    { name: "guest cannot use spec admin", method: "GET", url: "/api/spec/admin/operations", session: guestA, expected: [403] },
    { name: "guest cannot read admin audit", method: "GET", url: "/api/spec/admin/audit-log", session: guestA, expected: [403] },
    { name: "host cannot use PM dashboard", method: "GET", url: "/api/property-manager/dashboard", session: hostA, expected: [403] },
    { name: "host cannot use owner portal", method: "GET", url: "/api/property-manager/owner/portal", session: hostA, expected: [403] },
    { name: "manager cannot use owner portal", method: "GET", url: "/api/property-manager/owner/portal", session: managerA, expected: [403] },
    { name: "owner cannot use PM dashboard", method: "GET", url: "/api/property-manager/dashboard", session: ownerA, expected: [403] },
    { name: "officer cannot use PM dashboard", method: "GET", url: "/api/property-manager/dashboard", session: officer, expected: [403] },
    { name: "provider cannot list host properties", method: "GET", url: "/api/properties/owned", session: provider, expected: [403] },
  ];

  const ownership: AuthorizationCase[] = [
    { name: "guest B cannot read guest A traveler data", method: "GET", url: `/api/spec/traveler/${guestA.userId}`, session: guestB, expected: [401, 403, 404] },
    { name: "guest B cannot create guest A collection", method: "POST", url: `/api/spec/traveler/${guestA.userId}/wishlist/collections`, session: guestB, data: { name: "stolen" }, expected: [401, 403, 404] },
    { name: "guest B cannot prepare guest A identity upload", method: "POST", url: `/api/spec/traveler/${guestA.userId}/identity-documents/uploads`, session: guestB, data: { documentType: "Passport", fileName: "id.png", contentType: "image/png", sizeBytes: 100 }, expected: [401, 403, 404] },
    { name: "host B cannot archive host A property", method: "POST", url: `/api/properties/${property.id}/archive`, session: hostB, expected: [401, 403, 404] },
    { name: "host B cannot delete host A property", method: "DELETE", url: `/api/properties/${property.id}`, session: hostB, expected: [401, 403, 404] },
    { name: "host B cannot prepare host A photo", method: "POST", url: `/api/properties/${property.id}/photos/uploads`, session: hostB, data: { fileName: "photo.png", contentType: "image/png", sizeBytes: 100 }, expected: [401, 403, 404] },
    { name: "guest B cannot read guest A booking", method: "GET", url: `/api/bookings/${booking.id}`, session: guestB, expected: [404] },
    { name: "host B cannot read host A booking", method: "GET", url: `/api/bookings/${booking.id}`, session: hostB, expected: [404] },
    { name: "guest B cannot capture guest A booking", method: "POST", url: `/api/bookings/${booking.id}/capture-payment`, session: guestB, expected: [403, 404] },
    { name: "guest B cannot download guest A invoice", method: "GET", url: `/api/bookings/${booking.id}/invoice`, session: guestB, expected: [404] },
    { name: "manager B cannot read manager A invoice", method: "GET", url: `/api/property-manager/invoices/${invoice.id}`, session: managerB, expected: [403, 404] },
    { name: "manager B cannot read manager A owner statement", method: "GET", url: `/api/property-manager/owners/${ownerA.userId}/statement`, session: managerB, expected: [400, 403, 404] },
    { name: "manager B cannot verify manager A owner", method: "POST", url: `/api/property-manager/owners/${ownerA.userId}/verification`, session: managerB, data: { status: "Verified" }, expected: [400, 403, 404] },
    { name: "owner B cannot read owner A invoice", method: "GET", url: `/api/property-manager/invoices/${invoice.id}`, session: ownerB, expected: [403, 404] },
    { name: "manager B cannot read manager A statement", method: "GET", url: `/api/property-manager/owners/${ownerA.userId}/statement`, session: managerB, expected: [400, 403, 404] },
    { name: "manager B cannot assign manager A owner property", method: "POST", url: "/api/property-manager/properties", session: managerB, data: { ownerUserId: ownerA.userId, title: "IDOR property", unitNumber: "IDOR-1", address: "Kingston" }, expected: [400, 403, 404] },
    { name: "manager B cannot create utility for manager A property", method: "POST", url: "/api/property-manager/utilities", session: managerB, data: { ownerUserId: ownerA.userId, propertyId: managedProperty.id, utilityType: "Water", billingPeriod: "2075-01", usage: 1, rate: 1 }, expected: [400, 403, 404] },
    { name: "manager B cannot update manager A maintenance", method: "PATCH", url: `/api/property-manager/maintenance/${maintenance.id}`, session: managerB, data: { status: "COMPLETED", cost: 1, notes: "IDOR" }, expected: [400, 403, 404] },
    { name: "manager B cannot store manager A document", method: "POST", url: "/api/property-manager/documents", session: managerB, data: { ownerUserId: ownerA.userId, propertyId: managedProperty.id, title: "IDOR", category: "Finance", fileName: "idor.pdf", contentType: "application/pdf", sizeBytes: 1 }, expected: [400, 403, 404] },
    { name: "manager B cannot download manager A document", method: "GET", url: `/api/property-manager/documents/${document.id}/download`, session: managerB, expected: [403, 404] },
    { name: "manager B cannot vote on manager A governance", method: "POST", url: `/api/property-manager/governance/proposals/${proposal.id}/votes`, session: managerB, data: { choice: "YES" }, expected: [400, 403, 404] },
    { name: "manager B cannot revoke manager A QR", method: "POST", url: `/api/property-manager/qr/${qr.id}/revoke`, session: managerB, expected: [400, 403, 404] },
    { name: "manager B cannot pay manager A invoice", method: "POST", url: `/api/property-manager/invoices/${invoice.id}/payments`, session: managerB, data: { amount: 1, idempotencyKey: `idor-${Date.now()}` }, expected: [400, 403, 404] },
    { name: "owner B cannot read owner A statement", method: "GET", url: `/api/property-manager/owners/${ownerA.userId}/statement`, session: ownerB, expected: [400, 403, 404] },
    { name: "owner B cannot download owner A document", method: "GET", url: `/api/property-manager/documents/${document.id}/download`, session: ownerB, expected: [403, 404] },
    { name: "owner B cannot create maintenance on owner A property", method: "POST", url: "/api/property-manager/maintenance", session: ownerB, data: { ownerUserId: ownerA.userId, propertyId: managedProperty.id, title: "IDOR", description: "IDOR", category: "General", urgency: "NORMAL" }, expected: [400, 403, 404] },
    { name: "owner B cannot vote on owner A governance", method: "POST", url: `/api/property-manager/governance/proposals/${proposal.id}/votes`, session: ownerB, data: { choice: "YES" }, expected: [400, 403, 404] },
    { name: "owner B cannot open owner A portal", method: "GET", url: "/api/property-manager/owner/portal", session: ownerB, expected: [400, 403, 404] },
    { name: "provider cannot read manager invoice", method: "GET", url: `/api/property-manager/invoices/${invoice.id}`, session: provider, expected: [403, 404] },
    { name: "gate guard cannot read manager invoice", method: "GET", url: `/api/property-manager/invoices/${invoice.id}`, session: guestA, expected: [403, 404] },
    { name: "anonymous cannot read manager invoice", method: "GET", url: `/api/property-manager/invoices/${invoice.id}`, expected: [401] },
  ];

  const cases = [...anonymous, ...wrongRole, ...ownership];
  const results: Array<Record<string, unknown>> = [];
  for (const item of cases) {
    const response = await execute(api, item);
    const body = await response.text();
    const passed = item.expected.includes(response.status()) && !/stack trace| at NestyStay\./i.test(body);
    results.push({ name: item.name, method: item.method, url: item.url, expected: item.expected, actual: response.status(), passed, bodyExposesStackTrace: /stack trace| at NestyStay\./i.test(body) });
  }

  const failed = results.filter((item) => !item.passed);
  writeEvidence("04-authorization/authorization-matrix.json", { generatedAt: new Date().toISOString(), total: cases.length, passed: cases.length - failed.length, failed: failed.length, rate: Number((((cases.length - failed.length) / cases.length) * 100).toFixed(2)), results });
  expect(failed).toEqual([]);
  await api.dispose();
});

test("CORS, injection, malformed JSON, and output encoding resist common attacks", async ({ baseURL, page }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  const api = await playwrightRequest.newContext({ baseURL });
  const checks: Array<{ name: string; passed: boolean; evidence: unknown }> = [];

  const allowed = await api.fetch("/api/health", { method: "OPTIONS", headers: { Origin: "http://localhost:5173", "Access-Control-Request-Method": "GET" } });
  checks.push({ name: "allowed CORS origin", passed: allowed.headers()["access-control-allow-origin"] === "http://localhost:5173", evidence: allowed.headers()["access-control-allow-origin"] ?? null });
  const denied = await api.fetch("/api/health", { method: "OPTIONS", headers: { Origin: "https://evil.example", "Access-Control-Request-Method": "GET" } });
  checks.push({ name: "untrusted CORS origin denied", passed: !denied.headers()["access-control-allow-origin"], evidence: denied.headers()["access-control-allow-origin"] ?? null });
  checks.push({ name: "credentialed wildcard absent", passed: denied.headers()["access-control-allow-credentials"] !== "true" && denied.headers()["access-control-allow-origin"] !== "*", evidence: denied.headers() });

  const malformed = await api.fetch("/api/auth/login", { method: "POST", headers: { "Content-Type": "application/json" }, data: "{not-json" });
  checks.push({ name: "malformed JSON rejected", passed: malformed.status() === 400, evidence: malformed.status() });
  const sqlLike = await api.get("/api/directories/providers?search=%27%20OR%201%3D1--");
  checks.push({ name: "SQL-like query remains data", passed: sqlLike.status() === 200, evidence: sqlLike.status() });

  const host = await createSession(api, "Host");
  const payload = `<img src=x onerror="window.__nestyXss=true">${Date.now()}`;
  const propertyResponse = await api.post("/api/properties", { headers: auth(host), data: propertyPayload(host, payload) });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();
  const property = await propertyResponse.json() as { id: string; title: string };
  checks.push({ name: "XSS payload persisted as text", passed: property.title === payload, evidence: property.title });
  await page.goto(`/properties/${property.id}`, { waitUntil: "networkidle" });
  const xss = await page.evaluate(() => ({ executed: Boolean((window as unknown as { __nestyXss?: boolean }).__nestyXss), injectedImageCount: document.querySelectorAll('img[src="x"]').length, visibleText: document.body.innerText.includes("<img src=x") }));
  checks.push({ name: "React output encoding prevents stored XSS", passed: !xss.executed && xss.injectedImageCount === 0 && xss.visibleText, evidence: xss });

  const failed = checks.filter((check) => !check.passed);
  writeEvidence("02-security/dynamic-security-checks.json", { generatedAt: new Date().toISOString(), total: checks.length, passed: checks.length - failed.length, failed: failed.length, checks });
  expect(failed).toEqual([]);
  await api.dispose();
});

async function createSession(api: APIRequestContext, role: string): Promise<Session> {
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const email = `security-${role.toLowerCase()}-${suffix}@nestystay.local`;
  const registration = await api.post("/api/auth/register", { data: { email, password, confirmPassword: password, displayName: `Security ${role}`, phone: "+15550108888", acceptedTerms: true, acceptedPrivacy: true, role } });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { ...await login.json() as Session, email };
}

function propertyPayload(session: Session, title: string) {
  return { hostUserId: session.userId, hostName: "Spoofed name", hostEmail: "spoofed@example.com", title, location: "Kingston", country: "Jamaica", nightlyRate: 120, currency: "USD", badgeLevel: "Trusted", guestVerificationEnabled: false, insuraGuestEnabled: false, cancellationPolicy: "Flexible" };
}

function auth(session: Session) {
  return { Authorization: `Bearer ${session.accessToken}` };
}

async function execute(api: APIRequestContext, item: AuthorizationCase): Promise<APIResponse> {
  return api.fetch(item.url, { method: item.method, headers: item.session ? auth(item.session) : undefined, data: item.data });
}

function writeEvidence(relativePath: string, value: unknown) {
  const target = path.join(evidenceRoot, relativePath);
  mkdirSync(path.dirname(target), { recursive: true });
  writeFileSync(target, JSON.stringify(value, null, 2));
}
