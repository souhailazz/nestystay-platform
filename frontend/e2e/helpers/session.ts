import { randomUUID } from "node:crypto";
import type { Page } from "@playwright/test";

export type BrowserSession = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: string[];
  permissions: string[];
};

// Legacy journey fixtures obtain a real signed bearer session from the API.
// Install it in the server's HttpOnly cookie, never in JavaScript storage.
// Interactive login is exercised separately in property-manager-pms.spec.ts.
export async function installCookieSession(page: Page, session: Record<string, unknown>) {
  if (typeof session.accessToken !== "string" || !session.accessToken)
    throw new Error("The fixture must provide an API-issued session token.");
  await page.goto("/", { waitUntil: "domcontentloaded" });
  const origin = new URL(page.url()).origin;
  await page.context().clearCookies();
  await page.context().addCookies([
    { name: "nestyStay.session", value: session.accessToken, url: origin, httpOnly: true, sameSite: "Lax" },
    { name: "nestyStay.csrf", value: randomUUID(), url: origin, httpOnly: false, sameSite: "Lax" },
  ]);
  await page.evaluate(value => localStorage.setItem("nestyStay.session", JSON.stringify(value)), { ...session, accessToken: "" });
}

/** Provision a disposable non-admin account through the real API. */
export async function registerAndLoginSession(page: Page, role: string, label: string): Promise<BrowserSession> {
  // Registration is anonymous. Remove a prior browser session first so the
  // API does not interpret this setup request as a cookie-authenticated write.
  await page.context().clearCookies();
  const suffix = `${Date.now()}-${randomUUID().slice(0, 8)}`;
  const email = `e2e-${label.toLowerCase().replace(/[^a-z0-9]+/g, "-")}-${suffix}@nestystay.local`;
  const password = "NestyStay1";
  const phone = `+1555${String(Math.abs(hashSeed(suffix)) % 100000000).padStart(8, "0")}`;
  const registration = await page.request.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: `${label} E2E`, phone, acceptedTerms: true, acceptedPrivacy: true, role },
  });
  if (!registration.ok()) throw new Error(`E2E registration failed (${role}): ${await registration.text()}`);

  const login = await page.request.post("/api/auth/login", { data: { email, password } });
  if (!login.ok()) throw new Error(`E2E login failed (${role}): ${await login.text()}`);
  let result = await login.json() as { userId: string; accessToken?: string; expiresAt?: string; requiresTwoFactor?: boolean; challengeId?: string; roles?: string[]; permissions?: string[] };
  if (result.requiresTwoFactor) {
    if (!result.challengeId) throw new Error(`E2E ${role} login did not return a 2FA challenge.`);
    const challenge = await page.request.get(`/api/auth/development/challenges/${result.challengeId}`);
    if (!challenge.ok()) throw new Error(`E2E ${role} development 2FA code unavailable: ${await challenge.text()}`);
    const code = (await challenge.json() as { code: string }).code;
    const verified = await page.request.post("/api/auth/2fa/verify", { data: { challengeId: result.challengeId, code } });
    if (!verified.ok()) throw new Error(`E2E ${role} 2FA verification failed: ${await verified.text()}`);
    result = { ...result, ...await verified.json() as typeof result };
  }
  if (!result.accessToken || !result.expiresAt) throw new Error(`E2E ${role} login did not return a signed session.`);
  const session: BrowserSession = {
    userId: result.userId,
    email,
    displayName: `${label} E2E`,
    accessToken: result.accessToken,
    expiresAt: result.expiresAt,
    roles: result.roles ?? [role],
    permissions: result.permissions ?? [],
  };
  await installCookieSession(page, session);
  return session;
}

function hashSeed(value: string): number {
  return [...value].reduce((hash, character) => ((hash * 31) + character.charCodeAt(0)) | 0, 17);
}
