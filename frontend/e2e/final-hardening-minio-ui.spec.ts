import { expect, request as playwrightRequest, test } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";

test.describe.configure({ mode: "serial", timeout: 180_000 });

test("profile photo crosses the real browser, API, and MinIO storage path", async ({ page, baseURL }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");

  const email = `minio-ui-${Date.now()}@test.local`;
  const password = "NestyStay1";
  const api = await playwrightRequest.newContext({ baseURL });
  try {
    const registered = await api.post("/api/auth/register", {
      data: {
        email,
        password,
        confirmPassword: password,
        displayName: "MinIO Browser Guest",
        phone: "+18765550123",
        acceptedTerms: true,
        acceptedPrivacy: true,
        role: "Guest",
      },
    });
    expect(registered.ok(), await registered.text()).toBeTruthy();
  } finally {
    await api.dispose();
  }

  await page.goto("/login", { waitUntil: "networkidle" });
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.locator("form").getByRole("button", { name: /^Log in/ }).click();
  await expect(page).toHaveURL(/guest-dashboard/);

  await page.goto("/profile", { waitUntil: "networkidle" });
  const photoBytes = Buffer.from("\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x06\x00\x00\x00\x1f\x15\xc4\x89\x00\x00\x00\x0dIDAT\x08\xd7c\xf8\xcf\xc0\xf0\x1f\x00\x05\x00\x01\xff\x89\x99=\x1d\x00\x00\x00\x00IEND\xaeB\x60\x82", "binary");
  await page.locator(".profile-photo-picker input[type='file']").setInputFiles({
    name: "minio-ui.png",
    mimeType: "image/png",
    buffer: photoBytes,
  });

  await expect(page.getByText("minio-ui.png · Clean", { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByRole("status")).toContainText("uploaded and verified");
  await page.reload({ waitUntil: "networkidle" });
  await expect(page.getByText("minio-ui.png · Clean", { exact: true })).toBeVisible({ timeout: 30_000 });

  const image = page.locator("#TRAV-12 img[alt='']");
  await expect(image).toHaveCount(1);
  const downloadUrl = await image.getAttribute("src");
  expect(downloadUrl).toMatch(/127\.0\.0\.1:19000/);
  const downloaded = await page.request.get(downloadUrl!);
  expect(downloaded.ok()).toBeTruthy();
  expect(await downloaded.body()).toEqual(photoBytes);

  const evidencePath = path.resolve(process.cwd(), "..", "testing-evidence", "final-hardening", "20-deployment", "minio-ui-browser.json");
  mkdirSync(path.dirname(evidencePath), { recursive: true });
  writeFileSync(evidencePath, JSON.stringify({
    generatedAt: new Date().toISOString(),
    provider: "MinIO",
    flow: "browser profile photo upload -> API authorization -> MinIO object -> signed download",
    assertions: { upload: true, reloadFromPostgresMetadata: true, signedDownload: true, byteEquality: true },
    result: "PASS",
  }, null, 2));
});
