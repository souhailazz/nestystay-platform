import { randomBytes } from "node:crypto";
import { mkdirSync } from "node:fs";
import path from "node:path";
import { expect, request, test, type APIRequestContext, type Page } from "@playwright/test";
import { installCookieSession } from "./helpers/session";

type DemoSession = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: string[];
  permissions: string[];
};

const phase = process.env.LOGO_BRANDING_PHASE === "after" ? "after" : "before";
const evidenceDirectory = path.resolve(process.cwd(), "..", "testing-evidence", "logo-branding", phase);
const responsiveViewports = [
  { width: 320, height: 568 },
  { width: 360, height: 800 },
  { width: 390, height: 844 },
  { width: 414, height: 896 },
  { width: 430, height: 932 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 1000 },
  { width: 1920, height: 1080 },
];

test("NestyStay branding stays visible across public and workspace layouts", async ({ page, baseURL }) => {
  test.setTimeout(180_000);
  mkdirSync(evidenceDirectory, { recursive: true });
  const pageErrors: string[] = [];
  const consoleErrors: string[] = [];
  const failedResponses: string[] = [];
  page.on("pageerror", () => pageErrors.push("uncaught page error"));
  page.on("console", (message) => {
    if (message.type() === "error") consoleErrors.push(message.text());
  });
  page.on("response", (response) => {
    if (response.status() >= 400) {
      const url = new URL(response.url());
      failedResponses.push(`${response.request().method()} ${url.pathname} ${response.status()}`);
    }
  });

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/", { waitUntil: "networkidle" });
  await rejectOptionalCookies(page);

  const homeBrand = page.locator('header[aria-label="Main navigation"] a[href="/"]');
  await expect(homeBrand).toBeVisible();
  await expect(homeBrand.locator("img")).toBeVisible();
  await expect(homeBrand.getByText("NESTY STAY", { exact: true })).toBeVisible();
  if (phase === "after") {
    await expect(homeBrand).toHaveAttribute("aria-label", "NestyStay home");
    await expect(homeBrand.locator("img")).toHaveAttribute("alt", "NestyStay");
  }
  await page.screenshot({ path: path.join(evidenceDirectory, "home-desktop.png") });

  for (const viewport of responsiveViewports) {
    await page.setViewportSize(viewport);
    await expect(homeBrand).toBeVisible();
    await expect(homeBrand.getByText("NESTY STAY", { exact: true })).toBeVisible();
    const metrics = await page.evaluate(() => ({
      viewportWidth: window.innerWidth,
      documentWidth: document.documentElement.scrollWidth,
      logo: (() => {
        const image = document.querySelector('header[aria-label="Main navigation"] a[href="/"] img');
        const rect = image?.getBoundingClientRect();
        const header = image?.closest("header");
        const brandRect = image?.closest("a")?.getBoundingClientRect();
        const overlaps = (left?: DOMRect, right?: DOMRect) => Boolean(left && right && left.left < right.right && left.right > right.left && left.top < right.bottom && left.bottom > right.top);
        const controls = Array.from(header?.querySelectorAll("form[aria-label='Global search'], nav a, a.group, button[aria-label='Open menu']") ?? []);
        return {
          width: rect?.width ?? 0,
          naturalWidth: image instanceof HTMLImageElement ? image.naturalWidth : 0,
          objectFit: image ? getComputedStyle(image).objectFit : "",
          overlapsControls: controls.some((control) => overlaps(brandRect, control.getBoundingClientRect())),
        };
      })(),
    }));
    expect(metrics.documentWidth, `no horizontal page overflow at ${viewport.width}px`).toBeLessThanOrEqual(metrics.viewportWidth + 1);
    expect(metrics.logo.width, `logo image at ${viewport.width}px`).toBeGreaterThanOrEqual(phase === "after" ? 48 : 28);
    expect(metrics.logo.naturalWidth, `logo asset loads at ${viewport.width}px`).toBeGreaterThan(0);
    expect(metrics.logo.objectFit).toBe("contain");
    expect(metrics.logo.overlapsControls, `logo does not collide with controls at ${viewport.width}px`).toBe(false);
    if (viewport.width === 1920) {
      await page.screenshot({ path: path.join(evidenceDirectory, "home-desktop-1920.png") });
    }
  }

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/", { waitUntil: "networkidle" });
  await rejectOptionalCookies(page);
  await page.screenshot({ path: path.join(evidenceDirectory, "home-mobile.png") });

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/privacy", { waitUntil: "networkidle" });
  await rejectOptionalCookies(page);
  for (const width of [320, 390]) {
    await page.setViewportSize({ width, height: width === 320 ? 568 : 844 });
    const innerHeader = await page.evaluate(() => {
      const header = document.querySelector('header[aria-label="Main navigation"]');
      const brand = header?.querySelector('a[href="/"]')?.getBoundingClientRect();
      const controls = Array.from(header?.querySelectorAll("a.group, button[aria-expanded]") ?? []);
      const intersects = (left?: DOMRect, right?: DOMRect) => Boolean(left && right && left.left < right.right && left.right > right.left && left.top < right.bottom && left.bottom > right.top);
      return {
        viewportWidth: window.innerWidth,
        documentWidth: document.documentElement.scrollWidth,
        brandCollision: controls.some((control) => intersects(brand, control.getBoundingClientRect())),
      };
    });
    expect(innerHeader.documentWidth, `inner-page navbar has no overflow at ${width}px`).toBeLessThanOrEqual(width + 1);
    expect(innerHeader.brandCollision, `inner-page brand does not collide with controls at ${width}px`).toBe(false);
  }
  const publicFooterBrand = page.locator('footer a[href="/"]');
  expect(await publicFooterBrand.count()).toBeGreaterThan(0);
  await publicFooterBrand.scrollIntoViewIfNeeded();
  await expect(publicFooterBrand.locator("img")).toBeVisible();
  await expect(publicFooterBrand.getByText("NESTY STAY", { exact: true })).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDirectory, "public-footer-desktop.png") });
  await page.setViewportSize({ width: 390, height: 844 });
  await publicFooterBrand.scrollIntoViewIfNeeded();
  await expect(publicFooterBrand).toBeInViewport();
  await expect(publicFooterBrand.locator("img")).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDirectory, "public-footer-mobile.png") });

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/login", { waitUntil: "networkidle" });
  await rejectOptionalCookies(page);
  await expect(page.locator('#AUTH-01 aside a[href="/"]')).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDirectory, "login-desktop.png") });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/login", { waitUntil: "networkidle" });
  await rejectOptionalCookies(page);
  await expect(page.locator('#AUTH-01 aside a[href="/"]')).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDirectory, "login-mobile.png") });

  if (phase === "after") {
    expect(consoleErrors, "no console errors on public, footer, or auth routes").toEqual([]);
  }

  const api = await request.newContext({ baseURL });
  try {
    for (const [role, route, fileName] of [
      ["Host", "/host-dashboard", "host-dashboard"],
      ["Owner", "/owner/dashboard", "owner-dashboard"],
      ["PropertyManager", "/pm/dashboard", "pm-dashboard"],
    ] as const) {
      const session = await createSession(api, role);
      for (const [device, viewport] of [
        ["desktop", { width: 1440, height: 1000 }],
        ["mobile", { width: 390, height: 844 }],
      ] as const) {
        await page.setViewportSize(viewport);
        await installCookieSession(page, session);
        await page.goto(route, { waitUntil: "networkidle" });
        const workspaceBrand = page.locator('.workspace-sidebar a[href="/"]');
        await expect(workspaceBrand).toBeVisible();
        await expect(workspaceBrand.locator("img")).toBeVisible();
        if (phase === "after") {
          await expect(workspaceBrand).toHaveAttribute("aria-label", "NestyStay home");
          await expect(workspaceBrand.locator("img")).toHaveAttribute("alt", "NestyStay");
          await expect(workspaceBrand.getByText("NESTY STAY", { exact: true })).toBeVisible();
        }
        await page.screenshot({ path: path.join(evidenceDirectory, `${fileName}-${device}.png`) });
      }
    }
  } finally {
    await api.dispose();
  }

  if (phase === "after") expect(pageErrors, "no uncaught browser page errors").toEqual([]);
  if (consoleErrors.length || failedResponses.length) {
    test.info().annotations.push({
      type: "workspace-api-diagnostics",
      description: [...new Set(failedResponses)].join(", "),
    });
  }
});

async function rejectOptionalCookies(page: Page) {
  const rejectButton = page.getByRole("button", { name: "Reject optional", exact: true });
  if (await rejectButton.count()) await rejectButton.click();
}

async function createSession(api: APIRequestContext, role: "Host" | "Owner" | "PropertyManager"): Promise<DemoSession> {
  const suffix = randomBytes(8).toString("hex");
  const email = `branding-${role.toLowerCase()}-${suffix}@nestystay.local`;
  const displayName = role === "Host" ? "NestyStay Host" : role === "Owner" ? "NestyStay Owner" : "NestyStay Manager";
  const password = `NestyBrand${randomBytes(12).toString("base64url")}!9`;
  const registration = await api.post("/api/auth/register", {
    data: {
      email,
      password,
      confirmPassword: password,
      displayName,
      phone: `+1555${String(Math.floor(Math.random() * 1_000_000_000)).padStart(9, "0")}`,
      acceptedTerms: true,
      acceptedPrivacy: true,
      role,
    },
  });
  expect(registration.ok(), `Register local ${role} QA account: ${await registration.text()}`).toBeTruthy();

  const login = await api.post("/api/auth/login", { data: { email, password } });
  expect(login.ok(), `Log in local ${role} QA account: ${await login.text()}`).toBeTruthy();
  const session = await login.json() as DemoSession & { requiresTwoFactor?: boolean; challengeId?: string };
  if (session.requiresTwoFactor) {
    const challenge = await api.get(`/api/auth/development/challenges/${session.challengeId}`);
    expect(challenge.ok(), `Read local ${role} test challenge`).toBeTruthy();
    const { code } = await challenge.json() as { code: string };
    const verification = await api.post("/api/auth/2fa/verify", { data: { challengeId: session.challengeId, code } });
    expect(verification.ok(), `Verify local ${role} test challenge`).toBeTruthy();
    return { ...session, ...await verification.json() as DemoSession };
  }
  return session;
}
