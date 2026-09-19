import { mkdir, readFile, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const output = path.join(root, "public", "sitemap.xml");
const siteUrl = (process.env.SEO_SITE_URL ?? "https://nestystay.net").replace(/\/$/, "");
const apiUrl = (process.env.SITEMAP_API_URL ?? "http://127.0.0.1:5019/api").replace(/\/$/, "");
const staticPaths = ["/", "/explore", "/about", "/trust", "/help", "/contact", "/experiences", "/journal", "/privacy", "/terms", "/cookies", "/refund-policy"];

let propertyPaths = [];
try {
  const response = await fetch(`${apiUrl}/properties`, { signal: AbortSignal.timeout(5000) });
  if (response.ok) {
    const body = await response.json();
    const properties = Array.isArray(body) ? body : body.items ?? body.properties ?? [];
    propertyPaths = properties
      .filter((property) => property?.id && property?.isArchived !== true && property?.isDraft !== true)
      .map((property) => `/properties/${encodeURIComponent(property.id)}`);
  }
} catch {
  console.warn("Sitemap API unavailable; writing static public URLs only.");
}

const urls = [...new Set([...staticPaths, ...propertyPaths])]
  .map((pathname) => `  <url><loc>${siteUrl}${pathname.replaceAll("&", "&amp;")}</loc></url>`)
  .join("\n");
const xml = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`;
await mkdir(path.dirname(output), { recursive: true });
await writeFile(output, xml, "utf8");
console.log(`Wrote ${urls.split("\n").filter(Boolean).length} URLs to ${output}`);
