import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:5173";
const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5019/api/health";
const outputDir = process.env.PLAYWRIGHT_OUTPUT_DIR ?? "../artifacts/playwright-results";
const htmlReportDir = process.env.PLAYWRIGHT_HTML_REPORT ?? "../artifacts/playwright-report";

export default defineConfig({
  testDir: "./e2e",
  timeout: 120_000,
  expect: {
    timeout: 10_000,
  },
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 2,
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
