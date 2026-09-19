# Security audit

## Passed controls

- Authentication, role and ownership matrix: 54/54 (100%); cookie login, cookie 2FA, logout revocation, and multi-tab invalidation are covered.
- CORS allow/deny behavior, credentialed wildcard rejection, malformed JSON handling, SQL-like input handling, stored-XSS persistence/encoding: 7/7.
- Rate-limit behavior: named authentication/public-write/sensitive-action/upload policies with Retry-After problem responses; focused API rate-limit test passed.
- Security headers: CSP, HSTS policy, frame/referrer/type protections, and camera Permissions-Policy verified by API tests; camera is limited to same-origin use.
- Stripe adapter failures no longer return provider response bodies.
- npm audit and .NET vulnerable-package checks report zero vulnerabilities.
- Secret scan found no private-key/AWS-key/live credential material; matches are sample tokens used by validators, tests, and evidence text.

## Closed in resumed hardening pass

 - Browser sessions use HttpOnly `nestyStay.session` plus a CSRF double-submit token; localStorage no longer stores a bearer secret.
 - PostgreSQL now has 45 reviewed foreign-key constraints and 0 enforced M5 orphan rows. The one contextual QR mismatch value is documented in the relationship inventory.

## Open production hardening items

- Rate limits and booking coordination include process-local mechanisms; distributed deployment requires shared coordination.
- Production validator correctly refuses placeholders/test Stripe keys and incomplete Alibaba/Stripe configuration, so real provider validation remains blocked locally.
