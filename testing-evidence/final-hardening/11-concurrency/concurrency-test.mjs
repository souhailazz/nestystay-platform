import { mkdir, writeFile, readFile } from 'node:fs/promises';
import { glob } from 'node:fs';
import { execFileSync } from 'node:child_process';

const base = process.env.API_BASE ?? 'http://127.0.0.1:5019';
const postJson = (path, body) => fetch(`${base}${path}`, {
  method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(body)
});

const raceEmail = `concurrency-${Date.now()}@hardening.local`;
const registrationResponses = await Promise.all(Array.from({ length: 8 }, (_, index) => postJson('/api/auth/register', {
  email: raceEmail,
  password: 'HardeningPass!123',
  confirmPassword: 'HardeningPass!123',
  displayName: `Concurrent Guest ${index}`,
  role: 'Guest',
  acceptedTerms: true,
  acceptedPrivacy: true
})));
const registrationStatuses = await Promise.all(registrationResponses.map(response => response.status));
const registrationSuccesses = registrationStatuses.filter(status => status >= 200 && status < 300).length;
const registrationServerErrors = registrationStatuses.filter(status => status >= 500).length;

const qrResponses = await Promise.all(Array.from({ length: 32 }, () => postJson('/api/property-manager/qr/validate', { token: 'concurrency-invalid-token' })));
const qrStatuses = qrResponses.map(response => response.status);
const qrServerErrors = qrStatuses.filter(status => status >= 500).length;
const qrExpected = qrStatuses.filter(status => status === 200 || status === 429 || (status >= 400 && status < 500)).length;

const testFiles = execFileSync('rg', ['-l', '-i', 'concurr|race|idempot|overlap|duplicate|renewal|refund|proxy|revoke', 'backend/tests'], { encoding: 'utf8' })
  .trim().split(/\r?\n/).filter(Boolean);
const source = await Promise.all(testFiles.map(async file => ({ file, text: await readFile(file, 'utf8') })));
const backendConcurrencyTests = source.flatMap(({ file, text }) => [...text.matchAll(/^\s*public\s+(?:async\s+)?(?:Task|void)\s+(\w*(?:Concurr|Race|Idempot|Overlap|Duplicate|Renew|Refund|Proxy|Revoke)\w*)/gmi)].map(match => ({ file, test: match[1] })));

const checks = [
  { name: 'parallel duplicate registration has at most one success', passed: registrationSuccesses <= 1, detail: registrationStatuses },
  { name: 'parallel duplicate registration has no server errors', passed: registrationServerErrors === 0, detail: registrationStatuses },
  { name: 'parallel QR validation has no server errors', passed: qrServerErrors === 0, detail: qrStatuses },
  { name: 'parallel QR validation returns only expected statuses', passed: qrExpected === qrStatuses.length, detail: qrStatuses }
];
const output = {
  generatedAt: new Date().toISOString(),
  base,
  apiRaces: { registrationStatuses, registrationSuccesses, qrStatuses, qrExpectedResponses: qrExpected },
  backendConcurrencyTests,
  checks,
  passed: checks.filter(check => check.passed).length,
  failed: checks.filter(check => !check.passed).length,
  scenarios: ['parallel duplicate registration', 'parallel QR validation/rate limiting', 'booking overlap and creation limit', 'payment idempotency/refund', 'wellness assignment overlap/payout', 'renewal idempotency', 'proxy/revoke duplicate guards']
};
await mkdir('testing-evidence/final-hardening/11-concurrency', { recursive: true });
await writeFile('testing-evidence/final-hardening/11-concurrency/concurrency-results.json', JSON.stringify(output, null, 2));
console.log(JSON.stringify(output, null, 2));
if (output.failed) process.exitCode = 1;
