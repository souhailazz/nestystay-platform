# Frontend SonarQube refresh — 2026-09-27

## Scope

- Repository: NestyStay frontend certification worktree
- Commit: `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0`
- Server: local SonarQube Community Build `26.9.0.129388` on port `9002`
- Project: `nestystay-frontend-refresh`
- Scanner result: upload and server-side processing succeeded
- Analysis task: `7e06d0bd-3027-4f56-a7d1-2c38f131bb84`
- Analysis ID: `cb921fa4-45b3-482c-802a-e589f2919315`

## Measures

| Measure | Result |
| --- | ---: |
| Quality Gate | OK |
| Bugs | 0 |
| Vulnerabilities | 0 |
| Security hotspots | 0 |
| Open code smells | 972 |
| Sonar-imported line coverage | 56.6% |
| Duplication | 1.4% |

The configured gate is the local new-code gate. The open code-smell count and imported overall coverage remain release-quality debt; `OK` must not be interpreted as full production certification.

## Independent frontend checks at the same commit

- Vitest: 146 passed across 34 files.
- V8 coverage: 61.16% lines, 57.12% statements, 48.76% branches, 53.49% functions.
- Typecheck: PASS.
- Production build: PASS.
- Lint: 0 errors / 107 warnings.
- npm audit at high severity: 0 known vulnerabilities.

The scanner emitted CSS highlight-offset warnings for short CSS lines while still completing and processing the analysis; these are scanner diagnostics, not failed application tests.
