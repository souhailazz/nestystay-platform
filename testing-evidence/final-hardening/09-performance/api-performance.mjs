import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.API_BASE ?? 'http://127.0.0.1:5019';
const samples = Number(process.env.API_PERF_SAMPLES ?? 30);
const endpoints = [
  { name: 'health', path: '/api/health' },
  { name: 'property-browse', path: '/api/properties' },
  { name: 'directory-search', path: '/api/directories/providers?search=Jamaica' },
  { name: 'public-page', path: '/api/spec/public/pages/about' },
  { name: 'experiences', path: '/api/spec/experiences' },
  { name: 'platform-modules', path: '/api/platform/modules' },
  { name: 'qr-validation', path: '/api/property-manager/qr/validate', method: 'POST', body: JSON.stringify({ token: 'invalid-hardening-token' }) }
];

const percentile = (values, p) => {
  const sorted = [...values].sort((a, b) => a - b);
  if (!sorted.length) return null;
  return Number(sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * p) - 1)].toFixed(2));
};

const results = [];
for (const endpoint of endpoints) {
  const timings = [];
  const statuses = [];
  let errors = 0;
  for (let i = 0; i < samples; i += 1) {
    const started = performance.now();
    try {
      const response = await fetch(`${base}${endpoint.path}`, {
        method: endpoint.method ?? 'GET',
        body: endpoint.body,
        headers: endpoint.body ? { 'content-type': 'application/json' } : undefined,
        signal: AbortSignal.timeout(15_000)
      });
      statuses.push(response.status);
      await response.arrayBuffer();
      const acceptable = endpoint.acceptableStatuses?.includes(response.status) ?? (response.status >= 200 && response.status < 400);
      if (!acceptable) errors += 1;
    } catch {
      errors += 1;
      statuses.push('timeout');
    }
    timings.push(performance.now() - started);
  }
  results.push({
    ...endpoint,
    samples,
    statuses,
    errors,
    errorRate: Number((errors / samples).toFixed(4)),
    p50Ms: percentile(timings, 0.5),
    p90Ms: percentile(timings, 0.9),
    p95Ms: percentile(timings, 0.95),
    p99Ms: percentile(timings, 0.99),
    maxMs: Number(Math.max(...timings).toFixed(2))
  });
}

const allTimings = results.flatMap(result => [result.p50Ms, result.p90Ms, result.p95Ms, result.p99Ms, result.maxMs]).filter(Number.isFinite);
const output = {
  generatedAt: new Date().toISOString(),
  base,
  samplesPerEndpoint: samples,
  execution: 'sequential representative endpoint samples on local PostgreSQL-backed API',
  endpoints: results,
  endpointErrors: results.reduce((sum, result) => sum + result.errors, 0),
  maxObservedMs: Math.max(...allTimings)
};
await mkdir('testing-evidence/final-hardening/09-performance', { recursive: true });
await writeFile('testing-evidence/final-hardening/09-performance/api-performance.json', JSON.stringify(output, null, 2));
console.log(JSON.stringify(output, null, 2));
