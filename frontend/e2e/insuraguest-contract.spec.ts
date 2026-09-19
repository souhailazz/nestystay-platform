import { expect, request as playwrightRequest, test, type APIRequestContext } from "@playwright/test";
import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { installCookieSession } from "./helpers/session";

const password = "NestyStay1";

test("host can review and activate a real InsuraGuest contract policy", async ({ baseURL, page }, testInfo) => {
  const api = await playwrightRequest.newContext({ baseURL });
  const seed = await api.post("/api/spec/seed");
  expect(seed.ok(), await seed.text()).toBeTruthy();
  const host = await createHost(api);
  const propertyResponse = await api.post("/api/properties", {
    headers: { Authorization: `Bearer ${host.accessToken}` },
    data: {
      hostUserId: host.userId,
      hostName: host.displayName,
      hostEmail: host.email,
      title: `InsuraGuest Demo Villa ${Date.now()}`,
      location: "Kingston, Jamaica",
      country: "Jamaica",
      nightlyRate: 150,
      currency: "USD",
      badgeLevel: "Free",
      guestVerificationEnabled: false,
      insuraGuestEnabled: false,
      cancellationPolicy: "Flexible",
    },
  });
  expect(propertyResponse.ok(), await propertyResponse.text()).toBeTruthy();

  if (testInfo.project.name === "desktop-chromium") await page.setViewportSize({ width: 1440, height: 1000 });
  await installCookieSession(page, host);
  await page.goto("/host/insurance", { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("host-insurance-page")).toBeVisible();
  await expect(page.getByText("Contract plans", { exact: true })).toBeVisible();
  await expect(page.getByText("$50 / month", { exact: false })).toBeVisible();

  const activate = page.getByRole("button", { name: "Activate coverage", exact: true });
  await expect(activate).toHaveCount(1);
  await activate.click();
  await expect(page.getByText("Coverage is active.", { exact: false })).toBeVisible();
  await expect(page.getByText("Policy lifecycle", { exact: true })).toBeVisible();

  const evidenceDirectory = resolve("..", "testing-evidence", "insuraguest-contract");
  mkdirSync(evidenceDirectory, { recursive: true });
  await page.screenshot({ path: resolve(evidenceDirectory, `${testInfo.project.name}.png`), fullPage: true });
  await page.screenshot({ path: resolve(evidenceDirectory, `${testInfo.project.name}-viewport.png`), fullPage: false });
  await api.dispose();
});

async function createHost(api: APIRequestContext) {
  const email = `insuraguest-browser-${Date.now()}@nestystay.local`;
  const registration = await api.post("/api/auth/register", {
    data: { email, password, confirmPassword: password, displayName: "InsuraGuest Demo Host", phone: "+15550102030", acceptedTerms: true, acceptedPrivacy: true, role: "Host" },
  });
  expect(registration.ok(), await registration.text()).toBeTruthy();
  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { ...(await login.json()) as { userId: string; accessToken: string }, email, displayName: "InsuraGuest Demo Host" };
}
