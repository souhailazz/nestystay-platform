# Frontend coverage evidence — booking invoice and receipt workflows

Date: 2026-09-28  
Repository: `https://github.com/NestyStayJamaica/NESTY-STAY_Frontend.git`  
Branch: `codex/final-release-certification`  
Commit: `e74c701`

## Scope

Added behavioral tests to the existing booking test suite for invoice and receipt workflows. The tests cover authenticated download requests, print behavior, successful receipt download, and user-facing receipt-download failure handling.

## Results

- Vitest: **157 passed, 0 failed** across **37 files**.
- V8 line coverage: **63.41%** (4,804/7,576).
- V8 statement coverage: **59.68%** (5,595/9,374).
- V8 branch coverage: **51.20%** (3,868/7,554).
- V8 function coverage: **56.27%** (2,143/3,808).
- Typecheck: PASS.
- Production build: PASS.
- Lint: **0 errors / 107 warnings**.

This is local unit-test evidence. It does not claim a real external payment/receipt transaction, fresh Sonar analysis, staging role/IDOR verification, provider delivery, MinIO durability, or deployed-SHA parity.
