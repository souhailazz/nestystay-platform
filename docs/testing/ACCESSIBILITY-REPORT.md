# NestyStay accessibility report

Updated 2026-09-01. This is an implementation-level accessibility review, not a formal WCAG certification.

## Scope and method

- Reviewed the shared workspace shell, authentication, booking, listing, directory, wellness, property-manager, owner and gate screens.
- Checked semantic headings/regions, labels, focus-visible styling, keyboard paths, status/error announcements, dialog semantics, responsive touch targets and loading/empty states in source.
- Exercised the real app with Playwright at desktop (1440px), tablet (1024px) and mobile (390px) widths.
- The global usability suite passed **13 tests** with **2 intentional mobile-only skips**. The M4/M5 enhancement suite passed **18/18** across all three viewports.
- No axe/WCAG conformance runner is installed in this repository, so no automated conformance score is claimed.

## Findings

| Area | Result | Evidence / implementation |
|---|---|---|
| Keyboard navigation | PASS locally | Shared skip link, `/` workspace-search shortcut, Escape clear/blur, native buttons/links, and keyboard-tested host workspace flow. |
| Focus visibility | PASS locally | Shared controls use visible focus borders/rings; modal close button has a focus-visible ring. |
| Labels and names | PASS locally | Form fields use visible labels or `aria-label`; QR token, directory search, filters, file inputs and action buttons are named. |
| Landmark structure | PASS locally | Workspace navigation, breadcrumb navigation, main content, quick-actions region and mobile navigation have accessible names. |
| Dialog semantics | PASS locally | Booking/listing preview dialogs use `role="dialog"`, `aria-modal`, labelled headings and an explicit close action. |
| Live feedback | PASS locally | Loading skeletons use `role="status"`/`aria-busy`; success and failure feedback use `status`, `alert` or `aria-live`. |
| Responsive interaction | PASS locally | Mobile navigation has 48px touch targets; guard validation, booking, directories and manager workflows were exercised at 390px. |
| Colour-independent status | PASS locally | QR results render explicit text (`ACCESS APPROVED`, `ACCESS DENIED`, status and property) in addition to colour. |
| Error and empty states | PASS locally | Shared error notices, no-result copy, loading skeletons and retry/fallback paths are present in tested workflows. |
| Automated WCAG certification | OUT OF SCOPE | A formal audit with assistive technology and colour-contrast tooling still requires an accessibility specialist/client sign-off. |

## Remaining operational recommendation

Before public launch, run axe/Lighthouse plus keyboard-only and screen-reader passes on the deployed build, test high-contrast/forced-colour mode, and obtain client approval for any content-language or motion preferences. These are release assurance activities, not missing local application flows.
