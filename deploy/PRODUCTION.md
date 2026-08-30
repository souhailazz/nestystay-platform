# NestyStay — production hardening checklist

## Server environment (systemd unit for NestyStay.Api)
- Set `ASPNETCORE_ENVIRONMENT=Staging` NOW (demo server). This stops `appsettings.Development.json`
  from loading; combined with the code gates, all `/api/auth/development/*`, `/api/spec/seed`,
  `/api/backend-schema/*` and `/api/backend-jobs` conveniences require Development and are off.
- Switch to `ASPNETCORE_ENVIRONMENT=Production` for go-live. Note: `ProductionIntegrationValidator`
  then REQUIRES real values for Stripe, eKYC, R2 and InsuraGuest settings (see `.env.example`)
  or the API refuses to start — by design.
- `Security:EnableHttpsRedirection=true` only once TLS actually exists (Caddy below).

## HTTPS
- Attach a domain (A record -> server IP), fill it into `deploy/Caddyfile`, reload Caddy.
- Caddy then handles certificates + HTTP->HTTPS redirect; the app already honors
  `X-Forwarded-Proto` (ForwardedHeaders in Program.cs), so OpenAPI advertises the true scheme.

## Static frontend
- Serve `frontend/dist` through the Caddyfile here: it adds CSP, X-Frame-Options,
  X-Content-Type-Options, Referrer-Policy, Permissions-Policy and immutable caching for /assets/*.

## Still open before production
- Real Stripe, Alibaba Cloud eKYC, Cloudflare R2, and InsuraGuest credentials plus provider
  webhook/signature tests must be supplied and verified in staging.
- The signed milestone agreement is now stored at `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`;
  complete the separate compliance/retention review before release.
- The local implementation now enforces ownership and authorization for badge assignment,
  eligibility, renewal, campaign enrollment, and founding-benefit reads. Keep the API integration
  and security tests in the release gate when promoting to staging.

## Go-live gates (not M1–M4 contractual blockers)

- [ ] Production hosting, domain, Cloudflare/CDN and TLS certificate validated.
- [ ] Production PostgreSQL provisioned with least-privilege credentials; migrations applied from a clean baseline.
- [ ] Cloudflare R2/object storage configured with private buckets, signed URLs and lifecycle rules.
- [ ] Automated backups configured and a restore test recorded.
- [ ] Monitoring, alerting, centralized logs, error tracking and rate limiting enabled.
- [ ] Secret manager configured; no credentials in source, appsettings, evidence or build output.
- [ ] Stripe live account, PaymentIntents, webhook signing secret and Connect/payout configuration validated in staging.
- [ ] Alibaba production eKYC credentials, callback signatures and replay protection validated in staging.
- [ ] Email/SMS/push notification providers configured and delivery/failure tests recorded.
- [ ] Wellness payout provider configured and payout reconciliation tested.
- [ ] InsuraGuest configured if required for launch and its webhook path validated.
- [ ] Privacy, data-retention and compliance review approved; production security/DAST test completed.
- [ ] Staging smoke passes; production smoke passes after deployment with synthetic/test data.
