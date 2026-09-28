# Frontend behavioral coverage refresh

Date: 2026-09-28  
Repository: `NESTY-STAY_Frontend`  
Branch: `codex/final-release-certification`  
Commit: `f1f935b`  

## Scope

This refresh added tests for the landing SearchBar and the Property Manager module dispatcher/content. The tests cover destination filtering and empty states, date and guest controls, keyboard/Escape/outside-click behavior, submit URL construction, and invoice, payment, utility, maintenance, calendar, reporting, subscription, gate/QR, insurance, vendor, document, governance, and community actions.

## Results

| Check | Result |
|---|---:|
| Vitest files | 36 passed |
| Vitest tests | 153 passed, 0 failed |
| V8 lines | 62.84% (4,761/7,576) |
| V8 statements | 59.21% (5,551/9,374) |
| V8 branches | 50.80% (3,838/7,554) |
| V8 functions | 55.93% (2,130/3,808) |
| Typecheck | PASS |
| Production build | PASS |
| Lint | 0 errors / 107 existing warnings |

The local line result exceeds the 60% frontend threshold. This is not a Sonar upload and does not change the previously reported Sonar metrics for frontend commit `352f2f8`; a separate Sonar refresh is required before claiming new Sonar coverage or Quality Gate parity.

No credentials, tokens, secrets, migrations, staging, or production systems were changed.
