import { useEffect, useState } from "react";
import { AppLink } from "../AppLink";
import { COOKIE_CONSENT_STORAGE_KEY, type CookieConsentChoice } from "../../lib/legal";

function readChoice(): CookieConsentChoice | null {
  try {
    const value = window.localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY);
    return value === "accepted" || value === "rejected" ? value : null;
  } catch {
    return null;
  }
}

function announceConsent(choice: CookieConsentChoice) {
  window.dispatchEvent(new CustomEvent("nesty:cookie-consent-changed", { detail: { analytics: choice === "accepted" } }));
}

export function CookieConsent() {
  const [choice, setChoice] = useState<CookieConsentChoice | null>(() => readChoice());
  const [settingsOpen, setSettingsOpen] = useState(false);

  useEffect(() => {
    const openSettings = () => setSettingsOpen(true);
    window.addEventListener("nesty:open-cookie-settings", openSettings);
    return () => window.removeEventListener("nesty:open-cookie-settings", openSettings);
  }, []);

  if (choice && !settingsOpen) return null;

  const saveChoice = (nextChoice: CookieConsentChoice) => {
    try {
      window.localStorage.setItem(COOKIE_CONSENT_STORAGE_KEY, nextChoice);
    } catch {
      // The site remains usable if storage is unavailable; do not block access.
    }
    setChoice(nextChoice);
    setSettingsOpen(false);
    announceConsent(nextChoice);
  };

  return (
    <div
      aria-labelledby="cookie-consent-title"
      className="pointer-events-none fixed inset-x-3 bottom-3 z-[100] mx-auto max-w-[760px] sm:inset-x-6"
      role="region"
    >
      <div
        aria-describedby="cookie-consent-copy"
        className="pointer-events-auto flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-4 shadow-[0_16px_50px_rgba(4,31,31,0.22)] sm:flex-row sm:items-start sm:justify-between sm:gap-6 sm:p-5"
      >
        <div className="min-w-0">
          <h2 className="m-0 font-display text-xl font-medium text-ink" id="cookie-consent-title">Your cookie choices</h2>
          <p className="m-0 mt-1.5 text-[13px] leading-5 text-gray-600" id="cookie-consent-copy">
            NestyStay uses essential storage for sign-in, security, and preferences. Optional analytics is currently off and will not run unless you choose it.
            <AppLink className="ml-1 font-semibold text-deep-hover underline" href="/cookies">Read the Cookie Policy</AppLink>.
          </p>
        </div>
        <div className="flex shrink-0 flex-wrap gap-2 sm:justify-end">
          <button className="min-h-11 rounded-pill border border-sand-input bg-transparent px-4 text-sm font-semibold text-ink hover:bg-shell" onClick={() => saveChoice("rejected")} type="button">
            Reject optional
          </button>
          <button className="min-h-11 rounded-pill bg-deep px-4 text-sm font-semibold text-on-dark-heading hover:bg-deep-hover" onClick={() => saveChoice("accepted")} type="button">
            Allow optional
          </button>
        </div>
      </div>
      {settingsOpen && <p className="m-0 mt-3 text-xs text-sand-600" role="status">Your preference is saved on this device. No optional analytics is installed in this build.</p>}
    </div>
  );
}
