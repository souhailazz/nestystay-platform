# M4 final acceptance

Evidence basis: local PostgreSQL directory/provider data, moderation/QR lifecycle tests, responsive browser journeys and [M1-M5-FINAL-GAP-AUDIT.md](M1-M5-FINAL-GAP-AUDIT.md).

| Check | Result | Evidence or remaining action |
|---|---|---|
| Code complete | YES for current local/test scope | Four directories, provider moderation, privacy rules, QR lifecycle, gate workflow and manual fallback are connected. |
| Local E2E | YES for covered directory/QR workflows | Directory, moderation, QR, wrong-property, expiry/revoke and guard journeys passed locally. |
| Staging E2E | NO | Run provider onboarding → moderation → public visibility and QR/gate journeys on staging. |
| Production map/geocoder | NO | Select/configure provider; verify tiles, markers, geocoding, invalid address, rate limit, network failure, mobile and fallback. |
| Physical camera testing | NO | Test Android/iPhone permission, unavailable camera, low light, valid/invalid/expired/revoked/wrong-property and repeated scans. |
| Gate communications | NO | Configure real email/SMS delivery and verify success, failure, retry and history. |
| Accessibility | NO | Manually certify filters, profiles, moderation, QR fallback, keyboard and screen-reader announcements. |

## Final status

**NOT COMPLETE**
