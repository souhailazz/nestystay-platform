# Frontend quality audit

The production build is lazy-loaded at route boundaries: initial JavaScript is 93,950 bytes raw / 22,770 bytes gzip and the largest JavaScript chunk is 248,941 bytes raw. No Vite chunk exceeds 500 KB. Clean install, lint, typecheck, unit tests, and build all pass.

Playwright exercised 127 defined route cases, 12 axe pages, 60 responsive screens across five Chromium viewports, keyboard focus/Tab/Shift+Tab/Enter behavior, dialog focus trapping and Escape handling, visual baselines, and Chromium/Firefox/WebKit smoke. The hardening matrix reported 0 unexpected failures and 0 flaky tests.

Full-source unit coverage is intentionally low (statements 4.97%, branches 4.36%, functions 3.81%, lines 5.66%) because most UI behavior is integration/browser tested. The coverage matrix at `testing-evidence/final-hardening/06-frontend/FRONTEND-COVERAGE-MATRIX.md` keeps that distinction explicit.

