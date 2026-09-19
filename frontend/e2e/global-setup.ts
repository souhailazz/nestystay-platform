import type { FullConfig } from "@playwright/test";

export default async function globalSetup(_config: FullConfig) {
  const healthUrl = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5019/api/health";
  try {
    const response = await fetch(healthUrl, { signal: AbortSignal.timeout(10_000) });
    const body = await response.text();
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${body.slice(0, 240)}`);
  } catch (error) {
    throw new Error(`Local API is not ready at ${healthUrl}. Start PostgreSQL and the backend, then retry Playwright. ${error instanceof Error ? error.message : "Health check failed."}`);
  }
}
