import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.API_BASE ?? 'http://127.0.0.1:5019';
const checks = [];
const assert = (name, passed, detail) => checks.push({ name, passed, detail });

const quote = async (bookingValue, nights, tier = 'Standard') => {
  const response = await fetch(`${base}/api/badges-pricing/commission-quote`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ bookingValue, nights, tier })
  });
  return { response, body: await response.json() };
};

const standard = await quote(1000, 5);
assert('standard quote returns 200', standard.response.status === 200, `status=${standard.response.status}`);
assert('standard host commission is 3%', standard.body.hostCommissionPercent === 3, JSON.stringify(standard.body));
assert('standard host commission rounds to cents', standard.body.hostCommissionAmount === 30, JSON.stringify(standard.body));
assert('standard guest platform fee is 9%', standard.body.guestFeeAmount === 90, JSON.stringify(standard.body));
assert('standard revenue equals commission plus guest fee', standard.body.nestyStayRevenue === 120, JSON.stringify(standard.body));

for (const [tier, expected] of [['Platinum', 29], ['Gold', 36], ['Silver', 45]]) {
  const result = await quote(1000, 5, tier);
  assert(`${tier} founding fee is flat`, result.body.guestFeeAmount === expected, JSON.stringify(result.body));
  assert(`${tier} revenue includes host commission`, result.body.nestyStayRevenue === expected + 30, JSON.stringify(result.body));
}

const fractional = await quote(123.45, 2);
assert('fractional booking commission is cent-rounded', fractional.body.hostCommissionAmount === 3.7, JSON.stringify(fractional.body));
assert('fractional booking guest fee is cent-rounded', fractional.body.guestFeeAmount === 11.11, JSON.stringify(fractional.body));
assert('fractional booking revenue is cent-rounded', fractional.body.nestyStayRevenue === 14.81, JSON.stringify(fractional.body));

const badValue = await fetch(`${base}/api/badges-pricing/commission-quote`, {
  method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ bookingValue: -1, nights: 1 })
});
assert('negative booking value rejected', badValue.status >= 400 && badValue.status < 500, `status=${badValue.status}`);
const badNights = await fetch(`${base}/api/badges-pricing/commission-quote`, {
  method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ bookingValue: 100, nights: 0 })
});
assert('zero nights rejected', badNights.status >= 400 && badNights.status < 500, `status=${badNights.status}`);

const output = {
  generatedAt: new Date().toISOString(),
  base,
  checks,
  passed: checks.filter(item => item.passed).length,
  failed: checks.filter(item => !item.passed).length,
  rulesCovered: ['host commission', 'standard guest fee', 'founding flat fees', 'cent rounding', 'negative/zero validation']
};
await mkdir('testing-evidence/final-hardening/18-business-logic', { recursive: true });
await writeFile('testing-evidence/final-hardening/18-business-logic/financial-results.json', JSON.stringify(output, null, 2));
console.log(JSON.stringify(output, null, 2));
if (output.failed) process.exitCode = 1;
