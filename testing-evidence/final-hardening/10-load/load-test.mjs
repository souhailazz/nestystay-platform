import { mkdir, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { performance } from "node:perf_hooks";

const base = process.env.LOAD_BASE_URL ?? "http://localhost:5019";
const rounds = [10, 25, 50, 100];
const requestsPerRound = Number(process.env.LOAD_REQUESTS_PER_ROUND ?? 240);
const scenarios = [
  ["health", "/api/health"],
  ["property-browse", "/api/properties"],
  ["directory-search", "/api/directories/providers?search=Jamaica"],
  ["public-pages", "/api/spec/public/pages/about"],
  ["experiences", "/api/spec/experiences"],
  ["platform-modules", "/api/platform/modules"],
  ["qr-validation", "/api/property-manager/qr/validate"],
];

const evidenceDir = join(process.cwd(), "testing-evidence", "final-hardening", "10-load");
await mkdir(evidenceDir, { recursive: true });

async function request(path) {
  const started = performance.now();
  try {
    const response = await fetch(`${base}${path}`, { method: path.includes("qr/validate") ? "POST" : "GET", headers: { "content-type": "application/json" }, body: path.includes("qr/validate") ? JSON.stringify({ token: "invalid-load-token" }) : undefined, signal: AbortSignal.timeout(10_000) });
    await response.arrayBuffer();
    return { status: response.status, latencyMs: performance.now() - started, timeout: false };
  } catch (error) {
    return { status: 0, latencyMs: performance.now() - started, timeout: error?.name === "TimeoutError", error: String(error) };
  }
}

function percentile(values, p) {
  if (!values.length) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1)];
}

const results = [];
for (const concurrency of rounds) {
  const jobs = Array.from({ length: requestsPerRound }, (_, index) => {
    const [, path] = scenarios[index % scenarios.length];
    return { scenario: scenarios[index % scenarios.length][0], promise: null };
  });
  const started = performance.now();
  const output = [];
  for (let offset = 0; offset < jobs.length; offset += concurrency) {
    const batch = jobs.slice(offset, offset + concurrency);
    const settled = await Promise.all(batch.map(async (job) => ({ scenario: job.scenario, result: await request(scenarios.find(([name]) => name === job.scenario)[1]) })));
    output.push(...settled);
  }
  const elapsedMs = performance.now() - started;
  const successful = output.filter(({ result }) => result.status >= 200 && result.status < 400);
  const rateLimited = output.filter(({ result }) => result.status === 429);
  // 429 is an intentional protection response, not an application failure.
  // Keep it visible separately so a high-concurrency run proves the limiter is
  // active without turning expected abuse protection into a false 5xx failure.
  const errors = output.filter(({ result }) => !(result.status >= 200 && result.status < 400) && result.status !== 429);
  const latencies = output.map(({ result }) => result.latencyMs);
  results.push({ concurrency, requests: output.length, successful: successful.length, rateLimited: rateLimited.length, applicationErrors: errors.length, errors: errors.length, timeouts: output.filter(({ result }) => result.timeout).length, requestsPerSecond: Number((output.length / (elapsedMs / 1000)).toFixed(2)), p50Ms: Number(percentile(latencies, 50).toFixed(2)), p90Ms: Number(percentile(latencies, 90).toFixed(2)), p95Ms: Number(percentile(latencies, 95).toFixed(2)), p99Ms: Number(percentile(latencies, 99).toFixed(2)), maxMs: Number(Math.max(...latencies).toFixed(2)), errorRate: Number(((errors.length / output.length) * 100).toFixed(2)), byScenario: Object.fromEntries(scenarios.map(([name]) => [name, output.filter((item) => item.scenario === name).reduce((summary, item) => ({ requests: summary.requests + 1, errors: summary.errors + (!(item.result.status >= 200 && item.result.status < 400) && item.result.status !== 429 ? 1 : 0), rateLimited: summary.rateLimited + (item.result.status === 429 ? 1 : 0) }), { requests: 0, errors: 0, rateLimited: 0 })])) });
}

const output = { generatedAt: new Date().toISOString(), base, requestsPerRound, scenarios: scenarios.map(([name, path]) => ({ name, path })), results };
await writeFile(join(evidenceDir, "load-results.json"), JSON.stringify(output, null, 2));
console.log(JSON.stringify(output, null, 2));
