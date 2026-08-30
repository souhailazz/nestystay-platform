import fs from 'node:fs/promises';
const base = process.env.NESTYSTAY_API_BASE ?? 'http://localhost:5019/api';
const admin = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
if (!admin) throw new Error('NESTYSTAY_E2E_ADMIN_TOKEN is required.');
const password = 'NestyStay1';
async function call(path, options = {}) {
  const response = await fetch(`${base}${path}`, { ...options, headers: { 'content-type': 'application/json', ...(options.token ? { authorization: `Bearer ${options.token}` } : {}), ...(options.headers ?? {}) }, body: options.body === undefined ? undefined : JSON.stringify(options.body) });
  const text = await response.text(); let body; try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: response.status, body };
}
const email = `concurrency-${Date.now()}@nestystay.local`;
await call('/auth/register', { method: 'POST', body: { email, password, confirmPassword: password, displayName: 'Concurrency Host', phone: '+15550102030', acceptedTerms: true, acceptedPrivacy: true, role: 'Host' } });
const login = await call('/auth/login', { method: 'POST', body: { email, password } });
const host = login.body;
await call('/badges-pricing/badges/purchase', { method: 'POST', token: admin, body: { subjectType: 'Host', subjectId: host.userId, level: 'Verified', hostVerificationPassed: true, completedApprovedBookings: 3, hasPropertyAddress: true, paymentSucceeded: true } });
await call('/badges-pricing/badges/purchase', { method: 'POST', token: admin, body: { subjectType: 'Host', subjectId: host.userId, level: 'Wellness', hostVerificationPassed: true, completedApprovedBookings: 3, hasPropertyAddress: true, hasWellnessSubscription: true, paymentSucceeded: true } });
const property = await call('/properties', { method: 'POST', token: host.accessToken, body: { hostUserId: host.userId, hostName: 'Concurrency Host', hostEmail: email, title: `Concurrency Wellness ${Date.now()}`, location: 'Ocho Rios', country: 'Jamaica', nightlyRate: 150, currency: 'USD', badgeLevel: 'Wellness', guestVerificationEnabled: false, insuraGuestEnabled: false, cancellationPolicy: 'Flexible' } });
const officerResponse = await call('/wellness/officers', { method: 'POST', body: { badgeNumber: `RACE-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`, parish: 'St. Ann', coverageArea: 'Ocho Rios', isActiveOffDuty: true, isRetired: false } });
const officer = officerResponse.body;
await call(`/wellness/officers/${officer.id}/approve`, { method: 'POST', token: admin, body: { reason: 'Concurrency smoke' } });
const scheduledAt = new Date(Date.now() + 3 * 3600_000).toISOString();
const create = () => call('/wellness/visits', { method: 'POST', token: host.accessToken, body: { hostUserId: host.userId, propertyId: property.body.id, visitType: 'StandardWellnessCheck', scheduledAt, parish: 'St. Ann', area: 'Ocho Rios' } });
const visits = await Promise.all([create(), create()]);
if (visits.some(item => item.status < 200 || item.status >= 300)) throw new Error(`Wellness visit setup failed: ${JSON.stringify(visits)}`);
const assignments = await Promise.all(visits.map(item => call(`/wellness/visits/${item.body.id}/assign`, { method: 'POST', token: admin, body: { officerId: officer.id } })));
const passed = assignments.filter(item => item.status >= 200 && item.status < 300).length;
const failed = assignments.filter(item => item.status >= 400).length;
const output = { startedAt: new Date().toISOString(), visits: visits.map(item => ({ status: item.status, id: item.body?.id })), assignments: assignments.map(item => ({ status: item.status, message: item.body?.title ?? item.body?.message })), passed, failed, invariant: passed === 1 && failed === 1 };
await fs.writeFile(new URL('./m3-m4-concurrency-smoke.json', import.meta.url), JSON.stringify(output, null, 2));
if (!output.invariant) throw new Error(`Officer assignment race invariant failed: ${JSON.stringify(output)}`);
console.log(JSON.stringify({ passed, failed, invariant: output.invariant, output: 'm3-m4-concurrency-smoke.json' }));
