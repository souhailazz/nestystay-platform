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

## Still open before production (needs backend auth design, tracked separately)
- Ownership/auth/rate-limit rules on mutating business endpoints (badge purchase, campaign
  enrollment, wellness visit/report actions): these validate signed user tokens manually today;
  wiring them into a proper authorization policy is a scoped backend task, not a config flip.
