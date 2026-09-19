import {
  FRONTEND_URL,
  HOST_PASSWORD,
  apiContext,
  closeRecordedBrowser,
  demoPause,
  finalizeVideo,
  humanClick,
  humanSelect,
  humanType,
  loginViaUi,
  openRecordedBrowser,
  prepareDemoData,
  smoothMove,
  smoothScroll,
  waitForApp,
  ADMIN_EMAIL,
  ADMIN_PASSWORD,
} from "./demo-video-helpers.mjs";

function failOnBrowserErrors(recording) {
  if (recording.pageErrors.length || recording.consoleErrors.length) {
    throw new Error(`Browser errors during milestone 2: ${JSON.stringify({ pageErrors: recording.pageErrors, consoleErrors: recording.consoleErrors })}`);
  }
}

async function showPublicProfiles(page, hosts) {
  await page.goto(`${FRONTEND_URL}/hosts`);
  await waitForApp(page, page.getByRole("heading", { name: "Meet the people behind the stay.", exact: true }), { pause: 1300 });
  await demoPause(page, 1000);

  for (const fixture of [hosts.free, hosts.verified, hosts.trusted, hosts.wellness]) {
    await page.goto(`${FRONTEND_URL}/hosts`);
    await waitForApp(page, page.getByRole("heading", { name: "Meet the people behind the stay.", exact: true }), { pause: 750 });
    const card = page.locator(".spec-card").filter({ hasText: fixture.displayName }).first();
    await card.waitFor({ state: "visible" });
    await humanClick(page, card.getByRole("link", { name: "Open profile", exact: true }));
    await waitForApp(page, page.getByRole("heading", { name: fixture.displayName, exact: true }), { pause: 1200 });
    await smoothMove(page, page.getByText(fixture.badge, { exact: true }).first(), { steps: 28, pause: 20 });
    await demoPause(page, 1550);
  }
}

async function showPublicListings(page, hosts) {
  await page.goto(`${FRONTEND_URL}/explore`);
  await waitForApp(page, page.getByRole("heading", { name: "Explore stays", exact: true }), { pause: 1100 });
  const filters = [
    ["Free", hosts.free],
    ["✓ Verified", hosts.verified],
    ["★ Trusted", hosts.trusted],
    ["✦ Wellness", hosts.wellness],
  ];
  for (const [label, fixture] of filters) {
    await humanClick(page, page.getByRole("button", { name: label, exact: true }), { hoverPause: 420, afterPause: 850 });
    const card = page.locator("article").filter({ hasText: fixture.title }).first();
    await card.waitFor({ state: "visible", timeout: 15000 });
    const badge = card.getByText(label, { exact: true }).first();
    await smoothMove(page, badge, { steps: 27, pause: 20 });
    await demoPause(page, 1400);
  }
}

async function showAdminBadgeManagement(page, context) {
  await page.goto(`${FRONTEND_URL}/logout`);
  await demoPause(page, 700);
  await loginViaUi(page, context, ADMIN_EMAIL, ADMIN_PASSWORD);
  await waitForApp(page, page.getByRole("link", { name: "Configuration", exact: true }).first(), { pause: 900 });
  await humanClick(page, page.getByRole("link", { name: "Configuration", exact: true }).first(), { hoverPause: 550, afterPause: 1150 });
  await waitForApp(page, page.getByRole("heading", { name: /Badge management/i }).first(), { pause: 1500 });
  await demoPause(page, 1700);

  const search = page.getByLabel("Search badge assignments", { exact: true });
  await humanType(page, search, "Wellness", { delay: 82, afterPause: 500 });
  await humanSelect(page, page.getByLabel("Filter assignment status", { exact: true }), "active");
  await waitForApp(page, page.getByText("Wellness", { exact: true }).first(), { pause: 1300 });
  await demoPause(page, 900);
  await humanType(page, search, "", { afterPause: 500 });
  await demoPause(page, 1100);
  await smoothScroll(page, 590, { afterPause: 1400 });
}

async function showFinalComparison(page, hosts) {
  await page.goto(`${FRONTEND_URL}/hosts`);
  await waitForApp(page, page.getByRole("heading", { name: "Meet the people behind the stay.", exact: true }), { pause: 1200 });
  for (const fixture of [hosts.free, hosts.verified, hosts.trusted, hosts.wellness]) {
    const card = page.locator(".spec-card").filter({ hasText: fixture.displayName }).first();
    await card.waitFor({ state: "visible" });
    await humanClick(page, card.getByRole("link", { name: "Open profile", exact: true }), { hoverPause: 380, afterPause: 900 });
    await waitForApp(page, page.getByRole("heading", { name: fixture.displayName, exact: true }), { pause: 950 });
    await smoothMove(page, page.getByText(fixture.badge, { exact: true }).first(), { steps: 25, pause: 18 });
    await demoPause(page, 5200);
    if (fixture !== hosts.wellness) await page.goto(`${FRONTEND_URL}/hosts`);
  }
}

async function main() {
  const context = await apiContext();
  const prepared = await prepareDemoData(context);
  const recording = await openRecordedBrowser();
  let result = null;
  try {
    const { page } = recording;
    await showPublicProfiles(page, prepared.hosts);
    await showPublicListings(page, prepared.hosts);
    await showAdminBadgeManagement(page, context);
    await showFinalComparison(page, prepared.hosts);
    result = await closeRecordedBrowser(recording);
  } catch (error) {
    await recording.context.close().catch(() => undefined);
    await recording.browser.close().catch(() => undefined);
    throw error;
  } finally {
    await context.dispose();
  }
  failOnBrowserErrors(result);
  const files = await finalizeVideo(result.videoPath, "02_Milestone_2_Badge_System");
  console.log(JSON.stringify({ ...files, videoPath: result.videoPath }, null, 2));
}

main().catch((error) => { console.error(error.stack ?? error); process.exitCode = 1; });
