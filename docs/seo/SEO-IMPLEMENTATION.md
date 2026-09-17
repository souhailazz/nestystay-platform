# NestyStay SEO implementation

## What is shipped

- `frontend/public/robots.txt` allows public discovery routes and excludes authenticated, operational, API, map, and internal screens.
- `frontend/public/sitemap.xml` contains the canonical public pages that can be indexed without an account.
- `frontend/scripts/generate-sitemap.mjs` can append published property URLs from a read-only public properties endpoint.
- The app writes route-aware canonical, robots, Open Graph, Twitter, and JSON-LD metadata for public routes. Query-string result pages are canonicalized to their clean route and marked `noindex`.
- Property detail pages emit `VacationRental` structured data only from real listing fields. Missing rating or location data is not invented.
- The HTML shell contains a small no-JavaScript fallback so crawlers can discover the public marketplace entry points even before the app hydrates.

## Deployment checklist

Run this from the frontend directory after the public API is available:

```text
npm run seo:sitemap
```

The generator reads `SITEMAP_API_URL` when set, otherwise it uses `http://127.0.0.1:5019/api`. Set `SEO_SITE_URL` for a non-production hostname. Only published, non-archived properties are added. If the API is unavailable, the command safely keeps a static sitemap containing only the public routes.

The production deployment should run the command as part of the release/build step, then serve `/robots.txt` and `/sitemap.xml` from the same canonical host. Submit `https://nestystay.net/sitemap.xml` in Google Search Console and inspect representative public routes after deployment.

## Performance and indexing validation

Validate the deployed site with PageSpeed Insights or Chrome Lighthouse on mobile and desktop. Confirm HTTPS, one canonical URL per public page, no accidental `noindex`, no blocked public assets, and no mixed-content errors. Use Search Console URL Inspection and the Rich Results Test for representative property pages.

Google does not guarantee rankings or rich-result placement. VacationRental structured data may require additional Google partner/Hotel Center eligibility, so its presence is not a promise of a Google listing feature.
