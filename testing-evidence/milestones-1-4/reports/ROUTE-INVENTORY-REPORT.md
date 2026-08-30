# Real Browser Route Inventory

The current Playwright route inventory exercised 18 contractual routes at desktop, tablet and mobile viewports (54 navigations). The run completed with **15 targeted M1–M4 tests passed and 0 failed**; the inventory itself recorded HTTP 200 page responses, no console errors, no failed requests and no server-error body markers for every route.

Machine-readable result: [`route-inventory.json`](../browser/route-inventory.json).

The inventory includes anonymous exploration/directories, authenticated guest/host routes, officer wellness, host wellness/directory, provider dashboard, admin and booking/property surfaces. Police is intentionally rendered as a privacy/access-controlled surface; its UI route loads without exposing provider data while the direct API remains unauthorized/forbidden as required.
