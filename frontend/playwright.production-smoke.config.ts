import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.PRODUCTION_BASE_URL;

if (!baseURL) {
  throw new Error("PRODUCTION_BASE_URL must point to the deployed NestyStay URL.");
}

export default defineConfig({
  testDir: "./e2e",
  testMatch: /production-smoke\.spec\.ts/,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  forbidOnly: true,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [["line"], ["html", { open: "never", outputFolder: "../artifacts/production-smoke-report" }]],
  outputDir: "../artifacts/production-smoke-results",
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "desktop-chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1440, height: 900 } } },
    { name: "mobile-chromium", use: { ...devices["Pixel 5"], viewport: { width: 390, height: 844 } } },
  ],
});
