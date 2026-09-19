import { createHash } from "node:crypto";
import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:5173";
const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5019/api/health";
const outputDir = process.env.PLAYWRIGHT_OUTPUT_DIR ?? "../artifacts/playwright-results";
const htmlReportDir = process.env.PLAYWRIGHT_HTML_REPORT ?? "../artifacts/playwright-report";
const reuseExistingServer = process.env.PLAYWRIGHT_REUSE_SERVERS === "true";

// The repository-owned local PostgreSQL cluster runs on 55432. Keep the
// supervised backend aligned with the documented bootstrap instead of
// silently falling back to a password-protected machine-wide PostgreSQL on
// 5432.
if (!process.env.ConnectionStrings__Postgres && !process.env.PLAYWRIGHT_API_URL) {
  process.env.ConnectionStrings__Postgres = "Host=127.0.0.1;Port=55432;Database=nestystay_dev;Username=nestystay";
}

// The acceptance matrix is a local-only supervised environment. Keep its
// providers deterministic and run the real outbox worker so email-link tests
// observe the same generated messages a developer would inspect locally.
process.env.BackgroundJobs__Enabled = "true";
process.env.Email__Provider = "file";
process.env.EMAIL_PROVIDER = "file";
process.env.NESTYSTAY_EMAIL_PROVIDER = "file";
process.env.Email__Brevo__Enabled = "false";
process.env.BREVO_ENABLED = "false";
process.env.NESTYSTAY_EMAIL_OUTBOX_ROOT ??= `${process.env.TEMP ?? process.env.TMP ?? "."}/nestystay-email-outbox`;

// The backend accepts this legacy token only in Development. The hash is
// derived here rather than storing a reusable secret in the repository.
process.env.NESTYSTAY_E2E_ADMIN_TOKEN ??= "test-admin-token";
process.env.NESTYSTAY_ADMIN_TOKEN_SHA256 ??= createHash("sha256").update(process.env.NESTYSTAY_E2E_ADMIN_TOKEN).digest("hex");

export default defineConfig({
  testDir: "./e2e",
  // Client-demo recordings are standalone Node scripts, not regression tests.
  // Keep them runnable directly while preventing Playwright from executing
  // them as part of the platform test matrix.
  testIgnore: ["**/client-demo/**"],
  globalSetup: "./e2e/global-setup.ts",
  timeout: 120_000,
  expect: {
    timeout: 10_000,
  },
  // The local milestone suite shares one repository-owned database. Serialize
  // tests so fixtures and browser journeys cannot observe another test's
  // transient records (for example a malicious-property security fixture).
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [
    ["line"],
    ["html", { open: "never", outputFolder: htmlReportDir }],
  ],
  outputDir,
  use: {
    baseURL,
    // Operational acceptance tests start with a documented local consent
    // decision so the real banner does not cover controls. The privacy suite
    // removes this value explicitly to certify first-visit consent behavior.
    storageState: "./e2e/storage-state.local.json",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "desktop-chromium",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1440, height: 900 },
      },
    },
    // Keep the full Chromium matrix as the primary suite. Firefox and WebKit
    // intentionally run the small critical smoke test only, so provider- or
    // Chromium-specific evidence does not make cross-browser certification
    // prohibitively slow while still exercising the core login/directory path.
    {
      name: "desktop-firefox",
      grep: /critical UI smoke works/,
      use: {
        ...devices["Desktop Firefox"],
        viewport: { width: 1440, height: 900 },
      },
    },
    {
      name: "desktop-webkit",
      grep: /critical UI smoke works/,
      use: {
        ...devices["Desktop Safari"],
        viewport: { width: 1440, height: 900 },
      },
    },
    {
      name: "tablet-chromium",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1024, height: 768 },
      },
    },
    {
      name: "mobile-chromium",
      use: {
        ...devices["Pixel 5"],
        viewport: { width: 390, height: 844 },
      },
    },
  ],
  webServer: [
    {
      command: "dotnet run --project ../backend/src/NestyStay.Api --configuration Release --launch-profile http --no-build",
      url: apiURL,
      // Reuse is explicit and is only used after scripts/dev-up.ps1 has
      // started the repository-owned healthy services.
      reuseExistingServer,
      timeout: 120_000,
    },
    {
      command: "npm run dev -- --host 127.0.0.1 --port 5173",
      url: baseURL,
      // Reuse is explicit and is only used after scripts/dev-up.ps1 has
      // started the repository-owned healthy services.
      reuseExistingServer,
      timeout: 120_000,
    },
  ],
});
