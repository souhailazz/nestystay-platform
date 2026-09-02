# PostgreSQL relationship inventory — final hardening

Generated 2026-09-02 against the local PostgreSQL 18 `nestystay_dev` database (`localhost:5432`, migration `20260902125706_FixMaintenanceVendorForeignKey`).

## Before/after

| Check | Before | After | Result |
| --- | ---: | ---: | --- |
| Public tables | 145 | 145 | PASS |
| Foreign-key constraints | 0 | 45 | PASS |
| Reviewed M5 relationship candidates | 46 | 45 enforced + 1 contextual | PASS |
| Enforced M5 orphan rows | 0 | 0 | PASS |
| Contextual QR scan property ids | 1 | 1 | REVIEWED |

The one non-enforced relationship is `milestone_manager_qr_scan.property_id`. It is the guard-supplied comparison value recorded during validation (including a `WRONG_PROPERTY` attempt), rather than an ownership reference. Constraining it would prevent preserving invalid/mismatched gate attempts. All persisted ownership, parent/child, invoice, payment, governance, document, QR-access, and actor references are enforced with `ON DELETE RESTRICT`.

## Enforced M5 relationships

All `manager_user_id`, `owner_user_id`, and optional actor ids reference `milestone_user.id`; invoice lines/payments/ledger/utility rows reference `milestone_manager_invoice.id`; invoice/property/document/maintenance/QR rows reference their M5 parent; governance voters/votes/proxies reference their proposal/proxy parents; and QR scans reference `milestone_manager_qr_access.id`.

The migration is [20260902125555_FinalHardeningM5Relationships.cs](../../../backend/src/NestyStay.Infrastructure/Persistence/Migrations/20260902125555_FinalHardeningM5Relationships.cs) plus the corrective [20260902125706_FixMaintenanceVendorForeignKey.cs](../../../backend/src/NestyStay.Infrastructure/Persistence/Migrations/20260902125706_FixMaintenanceVendorForeignKey.cs). The latter corrects `maintenance.vendor_id` to reference `milestone_manager_vendor.id`.

## Validation SQL

```sql
select count(*) from pg_constraint where contype = 'f'; -- 45
select count(*) from milestone_manager_invoice_line c
left join milestone_manager_invoice p on p.id = c.invoice_id where p.id is null; -- 0
select count(*) from milestone_manager_qr_scan c
left join milestone_manager_qr_access p on p.id = c.qr_access_id where p.id is null; -- 0
```

The complete per-column orphan inventory and the machine-readable result are stored in `fk-validation.json`.
