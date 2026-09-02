import { expect, request as playwrightRequest, test } from "@playwright/test";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";

test.describe.configure({ mode: "serial", timeout: 180_000 });

type AuthFlow = { id: string; status: string };
type FlowSecret = { token: string; code: string };

function latestCapturedEmail(): string {
  const root = join(tmpdir(), "nestystay-email-outbox");
  if (!existsSync(root)) return "";
  const files = readdirSync(root)
    .filter((name) => name.endsWith(".eml"))
    .map((name) => ({ name, modified: statSync(join(root, name)).mtimeMs }))
    .sort((a, b) => b.modified - a.modified);
  return files.length ? readFileSync(join(root, files[0].name), "utf8") : "";
}

test("transactional email links are captured and complete through the real browser routes", async ({ page, baseURL }, testInfo) => {
  test.skip(testInfo.project.name !== "laptop-chromium");
  const api = await playwrightRequest.newContext({ baseURL });
  const destination = `clickable-${Date.now()}@test.local`;

  const emailStart = await api.post("/api/spec/auth/flows", {
    data: { flowType: "EmailVerification", destination },
  });
  expect(emailStart.ok(), await emailStart.text()).toBeTruthy();
  const emailFlow = await emailStart.json() as AuthFlow;
  const emailSecretResponse = await api.get(`/api/spec/auth/development/flows/${emailFlow.id}`);
  expect(emailSecretResponse.ok(), await emailSecretResponse.text()).toBeTruthy();
  const emailSecret = await emailSecretResponse.json() as FlowSecret;

  await expect.poll(() => latestCapturedEmail(), { timeout: 20_000, intervals: [250, 500, 1000] })
    .toContain(`/auth/email-verification?flowId=${emailFlow.id.replaceAll("-", "")}`);
  await page.goto(`/auth/email-verification?flowId=${emailFlow.id}&token=${encodeURIComponent(emailSecret.token)}`);
  await expect(page.getByText("Email verified. You can continue to NestyStay.")).toBeVisible();

  const invitationStart = await api.post("/api/spec/auth/flows", {
    data: { flowType: "OwnerInvitation", destination: `owner-${Date.now()}@test.local` },
  });
  expect(invitationStart.ok(), await invitationStart.text()).toBeTruthy();
  const invitation = await invitationStart.json() as AuthFlow;
  const invitationSecretResponse = await api.get(`/api/spec/auth/development/flows/${invitation.id}`);
  expect(invitationSecretResponse.ok(), await invitationSecretResponse.text()).toBeTruthy();
  const invitationSecret = await invitationSecretResponse.json() as FlowSecret;
  await page.goto(`/owner/invitation?flowId=${invitation.id}&token=${encodeURIComponent(invitationSecret.token)}`);
  await expect(page.getByText("Invitation accepted. Sign in to open your owner portal.")).toBeVisible();

  const password = "NestyStay1";
  const resetEmail = `reset-link-${Date.now()}@test.local`;
  const register = await api.post("/api/auth/register", {
    data: {
      email: resetEmail,
      password,
      confirmPassword: password,
      displayName: "Clickable Reset User",
      role: "Guest",
      acceptedTerms: true,
      acceptedPrivacy: true,
    },
  });
  expect(register.ok(), await register.text()).toBeTruthy();
  const resetStart = await api.post("/api/auth/password-reset/request", { data: { email: resetEmail } });
  expect(resetStart.ok(), await resetStart.text()).toBeTruthy();
  const reset = await resetStart.json() as { requestId: string };
  const resetSecretResponse = await api.get(`/api/auth/development/password-resets/${reset.requestId}`);
  expect(resetSecretResponse.ok(), await resetSecretResponse.text()).toBeTruthy();
  const resetSecret = await resetSecretResponse.json() as { token: string };
  await page.goto(`/auth/reset-password?requestId=${encodeURIComponent(reset.requestId)}&token=${encodeURIComponent(resetSecret.token)}`);
  await page.getByLabel("New password").fill("NestyStay2");
  await page.getByLabel("Confirm password").fill("NestyStay2");
  await page.getByRole("button", { name: "Reset password" }).click();
  await expect(page.getByText("Password reset completed.")).toBeVisible();

  await api.dispose();
});
