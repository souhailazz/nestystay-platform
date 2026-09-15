# NestyStay accessibility certification checklist

This checklist contains the parts of accessibility review that require a human with assistive technology or operating-system settings. Automated axe and browser checks are recorded separately in `testing-evidence/final-hardening/08-accessibility/`.

## Test setup

- [ ] Test the current deployed build at the supported desktop and mobile breakpoints.
- [ ] Test Chromium with keyboard-only input and with browser zoom at 200%.
- [ ] Test at least one Windows screen reader (Narrator or NVDA) and one mobile screen reader (VoiceOver or TalkBack).

## Keyboard and focus

- [ ] Every reachable control can be reached in a logical order without a mouse.
- [ ] Every modal, sheet, calendar, menu, and popover traps focus while open, closes with Escape, and returns focus to its trigger.
- [ ] SPA navigation moves focus to the new page heading and announces important loading, success, and error changes once.
- [ ] Date pickers, guest controls, filters, tables, checkout, admin decisions, and property-manager workflows work without pointer input.

## Screen readers

- [ ] Headings, landmarks, buttons, links, form labels, required fields, validation messages, tables, and status badges have understandable names and roles.
- [ ] Loading, empty, error, payment, verification, booking, and moderation status changes are announced without duplicate or stale announcements.
- [ ] Icon-only controls, favorite buttons, close buttons, pagination, map controls, and sortable columns expose their purpose and state.

## Visual and motion settings

- [ ] Content remains usable in forced-colors/high-contrast mode; no essential information relies on color alone.
- [ ] With `prefers-reduced-motion: reduce`, transitions and scroll effects are removed or minimized without hiding content or breaking navigation.
- [ ] Text, focus indicators, error states, badges, and disabled states remain readable at 200% zoom and in dark/light system contrast settings.

## Mobile and touch

- [ ] Explore filters, date/guest controls, booking checkout, tables, admin operations, and property-manager screens fit and scroll correctly on a real phone.
- [ ] Bottom sheets and full-height workflows do not trap the user behind the browser chrome or safe-area insets.
- [ ] Touch targets are comfortable, content does not require horizontal scrolling unless the table is genuinely data-dense, and keyboard focus is not obscured.

## Certification record

Status: **Pending human certification**

Reviewer: ____________________  Date: ____________________  Build/commit: ____________________

Notes and defects: ________________________________________________________________
