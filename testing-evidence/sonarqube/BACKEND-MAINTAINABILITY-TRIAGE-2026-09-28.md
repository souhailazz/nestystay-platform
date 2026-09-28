# Backend maintainability triage — 2026-09-28

## Source and limitation

This triage uses the committed Sonar export `backend-issues.json`. That export contains the first **500** records of a paged response reporting **1,356** total historical issues, while the later fresh current-head server-side scan reported **996** open code smells. It is therefore a prioritization inventory, not a claim that every current issue has been individually classified.

No historical migration was changed during this pass.

## Exported-record breakdown

| Severity | Count in exported page |
|---|---:|
| Critical | 60 |
| Major | 215 |
| Minor | 97 |
| Info | 128 |

| Rule | Count in exported page | Initial classification |
|---|---:|---|
| `csharpsquid:S2681` | 149 | Review by location; likely complex/unsafe condition expressions, prioritize business flows |
| `csharpsquid:S1192` | 82 | Safe constants only where a shared domain/status concept exists |
| `external_roslyn:CA1862` | 57 | Performance-oriented string-comparison review |
| `external_roslyn:CA1861` | 57 | Allocation cleanup; defer in migrations/generated code |
| `csharpsquid:S3972` | 47 | Refactor-only review; prioritize non-migration runtime code |
| `csharpsquid:S3358` | 32 | Nested ternary clarity review |
| `csharpsquid:S3776` | 13 | Critical cognitive-complexity candidates; prioritize authorization, money, and lifecycle methods |

## Highest-risk runtime concentration

The largest critical/major concentration in the exported page is `EfPropertyManagerP0Store.cs` (**184** records), followed by the professional and general Property Manager stores. These are multi-owner, finance, staff-scope, Gate Guard, and approval workflows, so they outrank cosmetic cleanup and immutable migration findings.

## AUTO-0030 remediation

Backend commit `b726d6d5e58d4604b6fe7d85ab793ffd9f40cbcf` centralizes Property Manager staff roles, lifecycle statuses, role normalization, status normalization, and the shared Gate Guard/finance validation rule. It removes duplicated high-risk validation from invitation and update paths while preserving the original messages and validation order.

Validation:

- Focused Property Manager P0, authorization, and passkey regression: **21 passed, 0 failed, 0 skipped**.
- Full non-container solution: **225 passed, 0 failed, 2 explicit environment skips** (local MinIO and two-instance PostgreSQL).
- The last container-enabled run at the immediately preceding test-only commit `4ece56f` passed **225/225**. A post-refactor container run is pending local Docker recovery and is not claimed as complete.

Next priority after Docker recovery: rerun the complete configured suite and fresh Sonar analysis at `b726d6d`, then address the next critical non-migration complexity finding with focused lifecycle/authorization tests.
