# NestyStay Platform Architecture Inventory

**Audit date:** 2026-09-22  
**Scope:** local repository and local runtime only; no staging or production mutation was performed.

## Tested source references

| Repository | Branch | Tested SHA |
|---|---|---|
| Root/orchestration | `codex/frontend-hardening-release-candidate` | `37f0b750ab7e7c81a948b4fe3d660acf9faed091` |
| Backend | `codex/fix-minio-test-skip` | `a082b84645b93681a68389252854a8775e4976bd` |
| Frontend | `codex/fix-pm-owner-invitation-feedback` | `b96a7aedf3ce194188d8f4e11d55e76ffebb00f3` |

The root and frontend worktrees contain pre-existing user changes. They were not reset, cleaned, or discarded.

## Runtime topology

- Frontend: React/Vite SPA with public, guest, host, wellness, directory, and property-manager workspaces.
- Backend: ASP.NET Core API with Domain, Application, Infrastructure, and API projects.
- Database: PostgreSQL with migrations and deterministic local seed data.
- Production object storage architecture: MinIO/S3-compatible storage through `MinioStorageProvider`; local-disk storage is a development fallback only.
- Identity: Stripe Identity is the configured application provider. No active Alibaba provider was found in backend/frontend runtime source.
- Payments: Stripe payment and Connect abstractions, webhook signature/idempotency handling, and server-authoritative badge PaymentIntent flow.
- Email: provider abstraction with Brevo adapter, outbox/worker path, and local development transport.
- Deployment reported by the deployment owner: nginx/systemd on staging and production. Deployment SHA parity was not externally exposed and therefore remains UNKNOWN.
- Maps/geocoding: coordinates/map-related UI exists in partial form, but no certified external geocoder/tile integration was found.

## Architecture conclusion

The core architecture is present and locally buildable. Certification is incomplete because physical MinIO I/O, real Brevo delivery, SonarQube, complete browser execution, deployment SHA parity, and full role/IDOR testing were not proven. Existing historical Alibaba references, including immutable migration snapshots and legacy deployment/config documentation, are inventoried separately; migrations were not modified.
