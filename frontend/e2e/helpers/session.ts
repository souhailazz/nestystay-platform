import { randomUUID } from "node:crypto";
import type { Page } from "@playwright/test";

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
