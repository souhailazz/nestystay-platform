# Local-to-production certification route

The local certification result is not silently promoted to staging or production. After protected-main merge, the operator should run the following against the deployed SHA:

1. Check `/api/health/live` and `/api/health/ready`.
2. Verify migrations, seed/fixture policy and PostgreSQL backups.
3. Run non-destructive public Explore/property/map checks.
4. Run dedicated guest booking, Stripe Identity, host approval/rejection and payment/refund journeys.
5. Verify Stripe webhook signatures, replay/idempotency and required Identity events.
6. Verify Brevo email delivery and retry/dead-letter behavior.
7. Verify MinIO/S3/R2 private upload/download/retention and restore.
8. Run M2 badge lifecycle, M3 wellness, M4 directory/QR and M5 manager/owner journeys by role.
9. Complete device/camera and human accessibility review.
10. Record deployed frontend/backend/root SHAs and the exact environment/provider modes.

The current local result is a prerequisite for this route, not a substitute for it.
