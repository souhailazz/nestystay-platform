# Security audit

## Passed controls

- Authentication, role and ownership matrix: 37/37.
- CORS allow/deny behavior, credentialed wildcard rejection, malformed JSON handling, SQL-like input handling, stored-XSS persistence/encoding: 7/7.
- Rate-limit behavior: named authentication/public-write/sensitive-action/upload policies with Retry-After problem responses; focused API rate-limit test passed.
- Security headers: CSP, HSTS policy, frame/referrer/type protections, and camera Permissions-Policy verified by API tests; camera is limited to same-origin use.
- Stripe adapter failures no longer return provider response bodies.
- npm audit and .NET vulnerable-package checks report zero vulnerabilities.
- Secret scan found no private-key/AWS-key/live credential material; matches are sample tokens used by validators, tests, and evidence text.

## Open hardening items

- `frontend/src/lib/auth.ts` persists the bearer session in `localStorage`; replace with an HttpOnly session boundary before production.
- PostgreSQL has no foreign-key constraints. Current audit has zero logical orphans, but relational constraints should be introduced with reviewed aggregate policies.
- Rate limits and booking coordination include process-local mechanisms; distributed deployment requires shared coordination.
- Production validator correctly refuses placeholders/test Stripe keys and incomplete Alibaba/Stripe configuration, so real provider validation remains blocked locally.

