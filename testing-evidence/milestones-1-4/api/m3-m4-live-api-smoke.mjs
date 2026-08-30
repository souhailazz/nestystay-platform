import fs from 'node:fs/promises';

const base = process.env.NESTYSTAY_API_BASE ?? 'http://localhost:5019/api';
const admin = process.env.NESTYSTAY_E2E_ADMIN_TOKEN;
if (!admin) throw new Error('NESTYSTAY_E2E_ADMIN_TOKEN is required for the local admin smoke.');
const password = 'NestyStay1';
const result = { startedAt: new Date().toISOString(), checks: [] };
const redact = (detail) => String(detail)
  .replace(/("(?:accessToken|paymentClientSecret|token|email)"\s*:\s*")[^"]*(")/gi, '$1[redacted]$2');
const check = (name, ok, detail = '') => { const passed = Boolean(ok); const safeDetail = redact(detail); result.checks.push({ name, ok: passed, detail: safeDetail }); if (!passed) throw new Error(`${name}: ${safeDetail}`); };
async function call(path, options = {}) {
  const response = await fetch(`${base}${path}`, { ...options, headers: { 'content-type': 'application/json', ...(options.token ? { authorization: `Bearer ${options.token}` } : {}), ...(options.headers ?? {}) }, body: options.body === undefined ? undefined : JSON.stringify(options.body) });
  const text = await response.text();
  let body; try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { response, body };
}
async function session(role, label) {
  const email = `live-m3-m4-${label}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@nestystay.local`;
  const registered = await call('/auth/register', { method: 'POST', body: { email, password, confirmPassword: password, displayName: `Live ${label}`, phone: '+15550102030', acceptedTerms: true, acceptedPrivacy: true, role } });
  check(`${role} registration`, registered.response.ok, JSON.stringify(registered.body));
  const login = await call('/auth/login', { method: 'POST', body: { email, password } });
  check(`${role} login`, login.response.ok && login.body?.accessToken, JSON.stringify(login.body));
  return { ...login.body, email };
}

const host = await session('Host', 'host');
const guest = await session('Guest', 'guest');
const property = await call('/properties', { method: 'POST', token: host.accessToken, body: { hostUserId: host.userId, hostName: 'Live Host', hostEmail: host.email, title: `Live QR Villa ${Date.now()}`, location: 'Ocho Rios', country: 'Jamaica', nightlyRate: 120, currency: 'USD', badgeLevel: 'Free', guestVerificationEnabled: false, insuraGuestEnabled: false, cancellationPolicy: 'Flexible' } });
check('property persisted', property.response.ok, JSON.stringify(property.body));
const propertyId = property.body.id;
const today = new Date().toISOString().slice(0, 10);
const checkout = new Date(Date.now() + 86400000).toISOString().slice(0, 10);
const booking = await call('/bookings', { method: 'POST', token: guest.accessToken, body: { propertyId, guestUserId: guest.userId, checkIn: today, checkOut: checkout } });
check('no-eKYC booking approved', booking.response.ok && booking.body.status === 'APPROVED', JSON.stringify(booking.body));
const issued = await call(`/access/qr/bookings/${booking.body.id}`, { method: 'POST', token: guest.accessToken });
check('QR issued', issued.response.ok && issued.body.token?.length >= 40, JSON.stringify({ status: issued.body.status, tokenLength: issued.body.token?.length }));
const wrong = await call('/access/qr/validate', { method: 'POST', body: { token: issued.body.token, propertyId: '00000000-0000-0000-0000-000000000001' } });
check('wrong-property QR rejected', wrong.response.ok && wrong.body.result === 'WrongProperty', JSON.stringify(wrong.body));
const valid = await call('/access/qr/validate', { method: 'POST', body: { token: issued.body.token, propertyId } });
check('valid QR accepted', valid.response.ok && valid.body.valid === true && valid.body.bookingId === booking.body.id, JSON.stringify(valid.body));
const subscription = await call('/wellness/subscriptions', { method: 'POST', token: host.accessToken });
check('wellness subscription application state', subscription.response.ok && subscription.body.monthlyAmount === 19 && subscription.body.remainingVisits === 1, JSON.stringify(subscription.body));
const renewal = await call('/wellness/subscriptions/renew', { method: 'POST', token: host.accessToken });
check('wellness subscription renewal is idempotent while active', renewal.response.ok && renewal.body.id === subscription.body.id && renewal.body.status === 'Active', JSON.stringify(renewal.body));
const provider = await call('/directories/providers', { method: 'POST', token: host.accessToken, body: { kind: 'LocalBusiness', category: 'Tours', name: `Live Provider ${Date.now()}`, parish: 'St. Ann', badgeLevel: 'Free', description: 'Live persisted provider smoke', availabilitySummary: 'Daily', contactMode: 'direct', isBrickAndMortar: true, isActive: true } });
check('provider enters moderation queue', provider.response.ok && provider.body.status === 'PendingReview' && provider.body.isActive === false, JSON.stringify(provider.body));
const moderated = await call(`/directories/providers/${provider.body.slug}/moderate`, { method: 'POST', token: admin, body: { status: 'approve', reason: 'Live API smoke' } });
check('provider moderation publishes', moderated.response.ok && moderated.body.status === 'Published', JSON.stringify(moderated.body));
const publicProvider = await call(`/directories/providers/${provider.body.slug}`);
check('published provider is public', publicProvider.response.ok && publicProvider.body.verificationStatus === 'Verified', JSON.stringify(publicProvider.body));
const police = await call('/directories/providers?kind=Police');
check('police directory is restricted', police.response.status === 401, JSON.stringify(police.body));
result.finishedAt = new Date().toISOString();
result.passed = result.checks.filter(item => item.ok).length;
result.failed = result.checks.filter(item => !item.ok).length;
await fs.writeFile(new URL('./m3-m4-live-api-smoke.json', import.meta.url), JSON.stringify(result, null, 2));
console.log(JSON.stringify({ passed: result.passed, failed: result.failed, output: 'm3-m4-live-api-smoke.json' }));
