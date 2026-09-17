import { expect, test } from "@playwright/test";

test.describe("crawl and index surfaces", () => {
  test("robots and XML sitemap are publicly reachable", async ({ request }) => {
    const robots = await request.get("/robots.txt");
    expect(robots.ok()).toBe(true);
    expect(await robots.text()).toContain("Sitemap: https://nestystay.net/sitemap.xml");
    const sitemap = await request.get("/sitemap.xml");
    expect(sitemap.ok()).toBe(true);
    const body = await sitemap.text();
    expect(body).toContain("<urlset");
    expect(body).toContain("https://nestystay.net/explore");
    expect(body).not.toContain("/guest-dashboard");
  });

  test("public pages expose canonical metadata and home organization schema", async ({ page }) => {
    await page.goto("/", { waitUntil: "domcontentloaded" });
    await expect(page.locator('meta[name="description"]')).toHaveAttribute("content", /Jamaican stays/);
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", "https://nestystay.net/");
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", /index,follow/);
    const jsonLd = await page.locator("script#nesty-seo-jsonld").textContent();
    expect(jsonLd).toContain("Organization");
    expect(jsonLd).toContain("WebSite");
  });

  test("private and duplicate query URLs are not indexable", async ({ page }) => {
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", /noindex/);
    await page.goto("/explore?search=Kingston", { waitUntil: "domcontentloaded" });
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", /noindex/);
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", "https://nestystay.net/explore");
  });
});
