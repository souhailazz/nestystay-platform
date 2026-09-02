# Codex implementation capabilities

This deployment pass was performed from hardened base `3cfb33818975fc688676e04004e20cf28672a171`.

| Capability | Use in this pass | Evidence |
| --- | --- | --- |
| Repository inspection and patching | Audited existing provider seams, configuration, migrations and compose/deploy files; changed only the current worktree. | Git diff and final commit |
| .NET/EF validation | Added the email outbox model and migration, built the complete solution, and applied the migration to the local PostgreSQL database. | `dotnet build`, `dotnet ef database update` |
| Frontend/browser verification | Existing Playwright hardening and M1–M5 browser evidence remains the regression baseline; no user journey was rewritten. | `testing-evidence/final-hardening/07-browser/` |
| Docker configuration | Added production images, private service networks, health checks, volumes and compose profiles. | `docker-compose.production.yml` |
| Operational documentation | Added first-server, backup/restore, DNS, provider, email, deployment and ownership-handover runbooks. | `docs/deployment/`, `docs/operations/`, `docs/handover/`, `scripts/deploy-production.sh` |

No provider credentials were requested, transmitted or committed. Real-provider delivery and deployment remain explicit client/operator actions.
