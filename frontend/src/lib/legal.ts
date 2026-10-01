export const LEGAL_DETAILS = {
  brand: "NestyStay",
  website: "https://nestystay.net",
  supportPhone: "754-248-2435",
  supportTel: "tel:+17542482435",
  whatsappUrl: "https://wa.me/17542482435",
  lastUpdated: "September 17, 2026",
} as const;

export const COOKIE_CONSENT_STORAGE_KEY = "nesty-cookie-consent-v2";
export const COOKIE_CONSENT_VERSION = "2026-09";
export const COOKIE_CONSENT_MAX_AGE_DAYS = 180;
export const COOKIE_CONSENT_MAX_AGE_MS = COOKIE_CONSENT_MAX_AGE_DAYS * 24 * 60 * 60 * 1000;

export type CookieConsentChoice = "accepted" | "rejected";
export type CookieConsentPreferences = {
  version: string;
  essential: true;
  analytics: boolean;
  marketing: boolean;
  updatedAt: string;
  expiresAt: string;
};

export function createCookieConsent(
  analytics: boolean,
  marketing: boolean,
  now = Date.now(),
): CookieConsentPreferences {
  const updatedAt = new Date(now).toISOString();
  return {
    version: COOKIE_CONSENT_VERSION,
    essential: true,
    analytics,
    marketing,
    updatedAt,
    expiresAt: new Date(now + COOKIE_CONSENT_MAX_AGE_MS).toISOString(),
  };
}

export function isCurrentCookieConsent(value: unknown, now = Date.now()): value is CookieConsentPreferences {
  if (!value || typeof value !== "object") return false;
  const record = value as Partial<CookieConsentPreferences>;
  return (
    record.version === COOKIE_CONSENT_VERSION &&
    record.essential === true &&
    typeof record.analytics === "boolean" &&
    typeof record.marketing === "boolean" &&
    typeof record.updatedAt === "string" &&
    typeof record.expiresAt === "string" &&
    Number.isFinite(Date.parse(record.expiresAt)) &&
    Date.parse(record.expiresAt) > now
  );
}

export function openCookieSettings() {
  window.dispatchEvent(new CustomEvent("nesty:open-cookie-settings"));
}
