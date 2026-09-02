import { defineConfig, devices } from "@playwright/test";

const evidenceRoot = "../testing-evidence/final-hardening";

export default defineConfig({
  testDir: "./e2e",
  testMatch: /final-hardening-.*\.spec\.ts/,
  timeout: 180_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  reporter: [
    ["line"],
    ["json", { outputFile: `${evidenceRoot}/07-browser/playwright-hardening-results.json` }],
    ["html", { open: "never", outputFolder: `${evidenceRoot}/07-browser/html-report` }],
  ],
  outputDir: `${evidenceRoot}/07-browser/test-results`,
  snapshotPathTemplate: `${evidenceRoot}/16-visual/baselines/{projectName}/{arg}{ext}`,
  use: {
    baseURL: "http://127.0.0.1:4173",
    reducedMotion: "reduce",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "desktop-chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1920, height: 1080 } } },
    { name: "laptop-chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1440, height: 900 } } },
    { name: "tablet-chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 768, height: 1024 } } },
    { name: "mobile-chromium", use: { ...devices["Pixel 5"], viewport: { width: 390, height: 844 } } },
    { name: "small-mobile-chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 360, height: 800 }, isMobile: true, hasTouch: true } },
    { name: "desktop-firefox", use: { ...devices["Desktop Firefox"], viewport: { width: 1440, height: 900 } } },
    { name: "desktop-webkit", use: { ...devices["Desktop Safari"], viewport: { width: 1440, height: 900 } } },
  ],
  webServer: [
    {
      command: "dotnet run --project ../backend/src/NestyStay.Api --launch-profile http --no-build",
      url: "http://localhost:5019/api/health",
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command: "npm run preview -- --host 127.0.0.1 --port 4173",
      url: "http://127.0.0.1:4173",
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
});
