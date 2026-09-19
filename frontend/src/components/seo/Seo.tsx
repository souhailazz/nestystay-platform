import { useEffect } from "react";

const SITE_URL = "https://nestystay.net";
const DEFAULT_IMAGE = `${SITE_URL}/assets/reference/landing-hero-editorial-realistic.png`;

function upsertMeta(attribute: "name" | "property", key: string, content: string) {
  let element = document.head.querySelector<HTMLMetaElement>(`meta[${attribute}="${key}"]`);
  if (!element) {
    element = document.createElement("meta");
    element.setAttribute(attribute, key);
    document.head.appendChild(element);
  }
  element.content = content;
}

function upsertLink(rel: string, href: string) {
  let element = document.head.querySelector<HTMLLinkElement>(`link[rel="${rel}"]`);
  if (!element) {
    element = document.createElement("link");
    element.rel = rel;
    document.head.appendChild(element);
  }
  element.href = href;
}

function upsertJsonLd(value: unknown) {
  const id = "nesty-seo-jsonld";
  let element = document.head.querySelector<HTMLScriptElement>(`script#${id}`);
  if (!element) {
    element = document.createElement("script");
    element.id = id;
    element.type = "application/ld+json";
    document.head.appendChild(element);
  }
  element.textContent = JSON.stringify(value);
}

export function Seo({
  title,
  description,
  canonicalPath,
  noindex = false,
  image = DEFAULT_IMAGE,
  jsonLd,
}: {
  title: string;
  description: string;
  canonicalPath: string;
  noindex?: boolean;
  image?: string;
  jsonLd?: unknown;
}) {
  useEffect(() => {
    const canonicalUrl = canonicalPath.startsWith("http") ? canonicalPath : `${SITE_URL}${canonicalPath === "/" ? "/" : canonicalPath.replace(/\/$/, "")}`;
    document.title = `${title} · NestyStay`;
    upsertMeta("name", "description", description);
    upsertMeta("name", "robots", noindex ? "noindex,follow" : "index,follow,max-image-preview:large");
    upsertMeta("property", "og:type", "website");
    upsertMeta("property", "og:site_name", "NestyStay");
    upsertMeta("property", "og:title", title);
    upsertMeta("property", "og:description", description);
    upsertMeta("property", "og:url", canonicalUrl);
    upsertMeta("property", "og:image", image.startsWith("http") ? image : `${SITE_URL}${image}`);
    upsertMeta("name", "twitter:card", "summary_large_image");
    upsertMeta("name", "twitter:title", title);
    upsertMeta("name", "twitter:description", description);
    upsertMeta("name", "twitter:image", image.startsWith("http") ? image : `${SITE_URL}${image}`);
    upsertLink("canonical", canonicalUrl);
    if (jsonLd) upsertJsonLd(jsonLd);
    else document.head.querySelector("script#nesty-seo-jsonld")?.remove();
  }, [canonicalPath, description, image, jsonLd, noindex, title]);

  return null;
}

export function siteUrl() { return SITE_URL; }
