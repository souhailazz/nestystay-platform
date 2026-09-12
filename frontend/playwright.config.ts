import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:5173";
const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5019/api/health";
const outputDir = process.env.PLAYWRIGHT_OUTPUT_DIR ?? "../artifacts/playwright-results";
const htmlReportDir = process.env.PLAYWRIGHT_HTML_REPORT ?? "../artifacts/playwright-report";

// The repository-owned local PostgreSQL cluster runs on 55432. Keep the
// supervised backend aligned with the documented bootstrap instead of
// silently falling back to a password-protected machine-wide PostgreSQL on
// 5432.
if (!process.env.ConnectionStrings__Postgres && !process.env.PLAYWRIGHT_API_URL) {
  process.env.ConnectionStrings__Postgres = "Host=127.0.0.1;Port=55432;Database=nestystay_dev;Username=nestystay";
  process.env.BackgroundJobs__Enabled = "false";
}

export default defineConfig({
  testDir: "./e2e",
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
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: "npm run dev -- --host 127.0.0.1 --port 5173",
      url: baseURL,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
  ],
});
