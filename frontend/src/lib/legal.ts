export const LEGAL_DETAILS = {
  brand: "NestyStay",
  website: "https://nestystay.net",
  supportPhone: "754-248-2435",
  supportTel: "tel:+17542482435",
  whatsappUrl: "https://wa.me/17542482435",
  lastUpdated: "September 17, 2026",
} as const;

export const COOKIE_CONSENT_STORAGE_KEY = "nesty-cookie-consent-v1";

export type CookieConsentChoice = "accepted" | "rejected";

export function openCookieSettings() {
  window.dispatchEvent(new CustomEvent("nesty:open-cookie-settings"));
}
