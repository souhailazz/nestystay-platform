import {
  API_URL,
  FRONTEND_URL,
  GUEST_EMAIL,
  GUEST_PASSWORD,
  apiContext,
  apiJson,
  closeRecordedBrowser,
  demoPause,
  deterministicDateRange,
  finalizeVideo,
  humanClick,
  humanSelect,
  humanType,
  openRecordedBrowser,
  prepareDemoData,
  smoothScroll,
  waitForApp,
} from "./demo-video-helpers.mjs";

function failOnBrowserErrors(recording) {
  if (recording.pageErrors.length || recording.consoleErrors.length) {
    throw new Error(`Browser errors during milestone 1: ${JSON.stringify({ pageErrors: recording.pageErrors, consoleErrors: recording.consoleErrors })}`);
  }
}

async function loginAsGuest(page, context, range) {
  await page.goto(`${FRONTEND_URL}/`);
  await waitForApp(page, page.getByRole("heading", { name: /More than.*a place to stay.*It’s home/i }).first(), { pause: 1200 });
  await demoPause(page, 5000);

  // The public home CTA leads into the public catalog. The sign-in control is
  // then visible in the same public shell, matching the real guest journey.
  await humanClick(page, page.getByRole("link", { name: /Explore stays/i }).first());
  await waitForApp(page, page.getByRole("heading", { name: "Explore stays", exact: true }), { pause: 850 });
  await demoPause(page, 2500);
  await humanClick(page, page.getByRole("link", { name: "Sign in", exact: true }).first());
  await waitForApp(page, page.getByRole("heading", { name: "Log in", exact: true }), { pause: 800 });
  await demoPause(page, 3000);

  const email = page.locator('input[type="email"]').first();
  const password = page.locator('input[type="password"]').first();
  await humanType(page, email, GUEST_EMAIL, { delay: 68 });
  await humanType(page, password, GUEST_PASSWORD, { delay: 74 });

  const responsePromise = page.waitForResponse((response) => response.url().includes("/api/auth/login") && response.request().method() === "POST");
  await humanClick(page, page.locator('button[type="submit"]').filter({ hasText: "Log in" }).first(), { hoverPause: 480, afterPause: 950 });
  const loginResponse = await responsePromise;
  const loginResult = await loginResponse.json();
  if (!loginResult.requiresTwoFactor || !loginResult.challengeId) throw new Error("The dedicated demo guest did not present the expected 2FA challenge.");

  await waitForApp(page, page.getByRole("heading", { name: "Two-factor check", exact: true }), { pause: 700 });
  await demoPause(page, 4500);
  const developmentChallenge = await apiJson(context, "GET", `/api/auth/development/challenges/${loginResult.challengeId}`);
  const code = developmentChallenge.code;
  await humanType(page, page.getByLabel("Digit 1", { exact: true }), code, { delay: 72, afterPause: 360 });
  await humanClick(page, page.getByRole("button", { name: "Verify code", exact: true }), { hoverPause: 500, afterPause: 1400 });
  await page.waitForFunction(() => Boolean(window.localStorage.getItem("nestyStay.session")), undefined, { timeout: 15000 });
  await waitForApp(page, page.getByText(/Guest workspace|Quick actions|Find a stay/i).first(), { pause: 1100 });
  await demoPause(page, 3000);
}

async function main() {
  const context = await apiContext();
  const prepared = await prepareDemoData(context);
  if (!prepared.verifiedPropertyId) throw new Error("The seeded verification-enabled villa was not available.");
  const existingBookings = await apiJson(context, "GET", "/api/bookings", undefined, prepared.admin.accessToken);
  const propertyBookings = existingBookings.filter((booking) => booking.propertyId === prepared.verifiedPropertyId);
  const approvedRange = deterministicDateRange(14 + propertyBookings.length * 12, 4);
  const rejectedRange = deterministicDateRange(20 + propertyBookings.length * 12, 3);

  const recording = await openRecordedBrowser();
  let result = null;
  try {
    const { page } = recording;
    await loginAsGuest(page, context, approvedRange);

    await humanClick(page, page.getByRole("link", { name: "Find a stay", exact: true }).first());
    await waitForApp(page, page.getByRole("heading", { name: "Explore stays", exact: true }), { pause: 1000 });
    await humanClick(page, page.getByRole("button", { name: "✓ Verified", exact: true }));
    await demoPause(page, 4000);
    const villaCard = page.locator("article").filter({ hasText: "Ocho Rios Verified Villa" }).first();
    await villaCard.waitFor({ state: "visible" });
    await humanClick(page, villaCard.getByRole("link", { name: "Details", exact: true }));
    await waitForApp(page, page.getByRole("heading", { name: "Ocho Rios Verified Villa", exact: true }), { pause: 1200 });
    await demoPause(page, 7000);
    await smoothScroll(page, 380, { afterPause: 1000 });
    await smoothScroll(page, -220, { afterPause: 700 });
    await humanClick(page, page.getByRole("button", { name: "Book this stay", exact: true }).first(), { hoverPause: 550, afterPause: 1100 });
    await waitForApp(page, page.getByRole("heading", { name: "Book Ocho Rios Verified Villa", exact: true }), { pause: 900 });
    await demoPause(page, 6000);

    const dateInputs = page.locator('input[type="date"]');
    await dateInputs.nth(0).fill(approvedRange.checkIn);
    await demoPause(page, 260);
    await dateInputs.nth(1).fill(approvedRange.checkOut);
    await demoPause(page, 300);
    await humanSelect(page, page.locator("select").first(), "02000000");
    await humanClick(page, page.getByRole("button", { name: "Get quote", exact: true }), { hoverPause: 500, afterPause: 1200 });
    await waitForApp(page, page.getByText("Total", { exact: true }).first(), { pause: 1300 });
    await demoPause(page, 7000);
    await humanClick(page, page.getByRole("button", { name: "Create booking", exact: true }), { hoverPause: 520, afterPause: 1300 });
    await page.waitForURL(/\/booking\/[^/]+\/identity$/, { timeout: 20000 });
    await waitForApp(page, page.getByRole("heading", { name: /Verify your identity/i }).first(), { pause: 1100 });
    await demoPause(page, 5000);
    await humanClick(page, page.locator("label").filter({ hasText: "National ID" }).first(), { hoverPause: 450, afterPause: 700 });
    await demoPause(page, 600);

    // The NestyStay identity page opens the configured provider in a new tab.
    // Close only that external test-provider tab so the recording stays on the
    // real NestyStay verification UX.
    page.on("popup", async (popup) => { await popup.close().catch(() => undefined); });
    await humanClick(page, page.getByRole("button", { name: /Hold dates & verify/i }).first(), { hoverPause: 550, afterPause: 1300 });
    await page.waitForURL(/\/booking\/[^/]+\/pending$/, { timeout: 20000 });
    await waitForApp(page, page.getByText(/identity is being verified/i).first(), { pause: 1800 });
    await demoPause(page, 7000);
    const approvedBookingId = new URL(page.url()).pathname.split("/")[2];

    const pending = await apiJson(context, "GET", `/api/bookings/${approvedBookingId}`, undefined, prepared.admin.accessToken);
    await apiJson(context, "POST", `/api/bookings/${approvedBookingId}/verification-result`, {
      passed: true,
      providerReference: pending.ekycTransactionId,
    }, prepared.admin.accessToken);
    await page.reload();
    await waitForApp(page, page.getByText("APPROVED", { exact: true }).first(), { pause: 1900 });
    await demoPause(page, 7500);

    // Approval authorizes the deterministic local payment adapter. The
    // checkout remains the real app route; the admin capture call mirrors the
    // real capture step used by the manual-capture payment lifecycle.
    await page.goto(`${FRONTEND_URL}/booking/${approvedBookingId}/checkout`);
    await waitForApp(page, page.getByText("Local payment test mode", { exact: true }), { pause: 1300 });
    await demoPause(page, 6500);
    await humanClick(page, page.getByRole("button", { name: /Continue with authorization/i }).first(), { hoverPause: 550, afterPause: 1300 });
    await page.waitForURL(new RegExp(`/booking/${approvedBookingId}/success$`), { timeout: 20000 });
    await apiJson(context, "POST", `/api/bookings/${approvedBookingId}/capture-payment`, undefined, prepared.admin.accessToken);
    await page.reload();
    await waitForApp(page, page.getByText("CAPTURED · CONFIRMED", { exact: true }), { pause: 2100 });
    await demoPause(page, 8500);

    const rejected = await page.evaluate(async ({ propertyId, range }) => {
      const response = await fetch("/api/bookings", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-Token": document.cookie.match(/(?:^|;\s*)nestyStay\.csrf=([^;]+)/)?.[1] ?? "",
        },
        body: JSON.stringify({
          propertyId,
          guestUserId: "00000000-0000-0000-0000-000000000000",
          checkIn: range.checkIn,
          checkOut: range.checkOut,
          documentType: "GLB03002",
          ekycMetaInfo: "Client demo rejection path",
        }),
      });
      const payload = await response.json();
      if (!response.ok) throw new Error(`Rejected demo booking creation failed (${response.status}): ${JSON.stringify(payload)}`);
      return payload;
    }, { propertyId: prepared.verifiedPropertyId, range: rejectedRange });
    await apiJson(context, "POST", `/api/bookings/${rejected.id}/verification-result`, {
      passed: false,
      providerReference: rejected.ekycTransactionId,
    }, prepared.admin.accessToken);
    await page.goto(`${FRONTEND_URL}/booking/${rejected.id}/rejected`);
    await waitForApp(page, page.getByRole("heading", { name: "Booking Request Declined", exact: true }), { pause: 1900 });
    await waitForApp(page, page.getByText("REJECTED", { exact: true }).first(), { pause: 1500 });
    await demoPause(page, 6500);
    await page.goto(`${FRONTEND_URL}/traveler/reservations/cancelled`);
    await waitForApp(page, page.getByRole("heading", { name: /Reservations Cancelled/i }).first(), { pause: 1300 });
    await waitForApp(page, page.getByText("REJECTED", { exact: true }).first(), { pause: 1500 });
    await demoPause(page, 5500);

    await page.goto(`${FRONTEND_URL}/booking/${approvedBookingId}/success`);
    await waitForApp(page, page.getByText("CAPTURED · CONFIRMED", { exact: true }), { pause: 1900 });
    await demoPause(page, 17000);
    result = await closeRecordedBrowser(recording);
  } catch (error) {
    await recording.context.close().catch(() => undefined);
    await recording.browser.close().catch(() => undefined);
    throw error;
  } finally {
    await context.dispose();
  }

  failOnBrowserErrors(result);
  const files = await finalizeVideo(result.videoPath, "01_Milestone_1_Core_Booking_System");
  console.log(JSON.stringify({ ...files, videoPath: result.videoPath, api: API_URL }, null, 2));
}

main().catch((error) => { console.error(error.stack ?? error); process.exitCode = 1; });
