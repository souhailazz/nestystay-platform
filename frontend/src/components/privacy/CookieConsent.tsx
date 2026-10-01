import { useEffect, useRef, useState } from "react";
import { AppLink } from "../AppLink";
import {
  COOKIE_CONSENT_STORAGE_KEY,
  createCookieConsent,
  isCurrentCookieConsent,
  type CookieConsentPreferences,
} from "../../lib/legal";

type OptionalPreferences = Pick<CookieConsentPreferences, "analytics" | "marketing">;

const defaultOptionalPreferences: OptionalPreferences = {
  analytics: false,
  marketing: false,
};

function readConsent(): CookieConsentPreferences | null {
  try {
    const raw = window.localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY);
    if (!raw) return null;
    const parsed: unknown = JSON.parse(raw);
    return isCurrentCookieConsent(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function announceConsent(consent: CookieConsentPreferences) {
  window.dispatchEvent(
    new CustomEvent("nesty:cookie-consent-changed", {
      detail: {
        essential: true,
        analytics: consent.analytics,
        marketing: consent.marketing,
        version: consent.version,
        expiresAt: consent.expiresAt,
      },
    }),
  );
}

function focusableElements(container: HTMLElement) {
  return Array.from(
    container.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), input:not([disabled]), [tabindex]:not([tabindex="-1"])',
    ),
  ).filter((element) => element.getAttribute("aria-hidden") !== "true");
}

export function CookieConsent() {
  const [consent, setConsent] = useState<CookieConsentPreferences | null>(() => readConsent());
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [draft, setDraft] = useState<OptionalPreferences>(() => {
    const stored = readConsent();
    return stored ? { analytics: stored.analytics, marketing: stored.marketing } : defaultOptionalPreferences;
  });
  const panelRef = useRef<HTMLDivElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);

  const restoreFocus = () => {
    const target = returnFocusRef.current?.isConnected
      ? returnFocusRef.current
      : document.querySelector<HTMLElement>("[data-cookie-settings-trigger]");
    returnFocusRef.current = null;
    window.setTimeout(() => target?.focus(), 0);
  };

  const openSettings = () => {
    const activeElement = document.activeElement;
    returnFocusRef.current = activeElement instanceof HTMLElement ? activeElement : null;
    setDraft(consent ? { analytics: consent.analytics, marketing: consent.marketing } : defaultOptionalPreferences);
    setSettingsOpen(true);
  };

  const closeSettings = () => {
    setSettingsOpen(false);
    restoreFocus();
  };

  useEffect(() => {
    const handleOpenSettings = () => openSettings();
    window.addEventListener("nesty:open-cookie-settings", handleOpenSettings);
    return () => window.removeEventListener("nesty:open-cookie-settings", handleOpenSettings);
  });

  useEffect(() => {
    const visible = !consent || settingsOpen;
    document.body.classList.toggle("cookie-consent-open", visible);
    return () => document.body.classList.remove("cookie-consent-open");
  }, [consent, settingsOpen]);

  useEffect(() => {
    if (!settingsOpen) return undefined;

    const frame = window.requestAnimationFrame(() => panelRef.current?.focus());
    const handleKeyboard = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        closeSettings();
        return;
      }
      if (event.key !== "Tab" || !panelRef.current) return;
      const elements = focusableElements(panelRef.current);
      if (elements.length === 0) return;
      const first = elements[0];
      const last = elements[elements.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", handleKeyboard);
    return () => {
      window.cancelAnimationFrame(frame);
      document.removeEventListener("keydown", handleKeyboard);
    };
  }, [settingsOpen]);

  if (consent && !settingsOpen) return null;

  const savePreferences = (analytics: boolean, marketing: boolean) => {
    const nextConsent = createCookieConsent(analytics, marketing);
    try {
      window.localStorage.setItem(COOKIE_CONSENT_STORAGE_KEY, JSON.stringify(nextConsent));
    } catch {
      // The site remains usable if storage is unavailable; do not block access.
    }
    setConsent(nextConsent);
    setDraft({ analytics, marketing });
    setSettingsOpen(false);
    announceConsent(nextConsent);
    restoreFocus();
  };

  const dialogMode = settingsOpen;

  return (
    <section
      aria-label={dialogMode ? "Cookie preferences" : "Cookie consent"}
      aria-labelledby="cookie-consent-title"
      aria-modal={dialogMode ? "true" : undefined}
      className="cookie-consent-shell pointer-events-none"
      role={dialogMode ? "dialog" : "region"}
    >
      <div
        ref={dialogMode ? panelRef : undefined}
        aria-describedby="cookie-consent-copy"
        className="cookie-consent-surface pointer-events-auto flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-4 shadow-[0_16px_50px_rgba(4,31,31,0.22)] sm:p-5"
        tabIndex={dialogMode ? -1 : undefined}
      >
        {dialogMode ? (
          <>
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="m-0 text-[10px] font-bold uppercase tracking-[0.14em] text-deep-hover">Privacy controls</p>
                <h2 className="m-0 mt-1 font-display text-xl font-medium text-ink" id="cookie-consent-title">Manage cookie preferences</h2>
              </div>
              <button
                aria-label={consent ? "Close cookie preferences" : "Return to cookie choices"}
                className="cookie-consent-control min-h-11 rounded-pill border border-sand-input px-3 text-sm font-semibold text-ink hover:bg-shell"
                onClick={closeSettings}
                type="button"
              >
                {consent ? "Close" : "Back"}
              </button>
            </div>
            <p className="m-0 text-[13px] leading-5 text-gray-600" id="cookie-consent-copy">
              Essential storage is always on for sign-in, security, and remembering your choice. Optional categories stay off until you enable them.
            </p>
            <fieldset className="grid gap-2 border-0 p-0">
              <legend className="mb-1 text-xs font-bold uppercase tracking-[0.1em] text-deep-hover">Cookie categories</legend>
              <label className="flex items-start gap-3 rounded-field border border-sand-border bg-white p-3">
                <input checked disabled type="checkbox" />
                <span><strong className="block text-sm text-ink">Essential storage</strong><span className="block text-xs text-gray-600">Required for sessions, security, preferences, and consent records.</span></span>
              </label>
              <label className="flex cursor-pointer items-start gap-3 rounded-field border border-sand-border bg-white p-3 hover:bg-shell">
                <input checked={draft.analytics} onChange={(event) => setDraft((current) => ({ ...current, analytics: event.target.checked }))} type="checkbox" />
                <span><strong className="block text-sm text-ink">Analytics</strong><span className="block text-xs text-gray-600">Optional measurement to understand page performance. No analytics SDK is installed in this build.</span></span>
              </label>
              <label className="flex cursor-pointer items-start gap-3 rounded-field border border-sand-border bg-white p-3 hover:bg-shell">
                <input checked={draft.marketing} onChange={(event) => setDraft((current) => ({ ...current, marketing: event.target.checked }))} type="checkbox" />
                <span><strong className="block text-sm text-ink">Marketing</strong><span className="block text-xs text-gray-600">Optional campaigns or advertising measurement. None is active in this build.</span></span>
              </label>
            </fieldset>
            <div className="flex flex-wrap items-center justify-end gap-2 border-t border-sand-border pt-3">
              <button className="cookie-consent-control min-h-11 rounded-pill border border-sand-input bg-transparent px-4 text-sm font-semibold text-ink hover:bg-shell" onClick={() => savePreferences(false, false)} type="button">Reject optional</button>
              <button className="cookie-consent-control min-h-11 rounded-pill bg-deep px-4 text-sm font-semibold text-on-dark-heading hover:bg-deep-hover" onClick={() => savePreferences(draft.analytics, draft.marketing)} type="button">Save preferences</button>
            </div>
            <p aria-live="polite" className="m-0 text-xs text-sand-600">Your choice is saved on this device for 180 days and can be changed from the footer.</p>
          </>
        ) : (
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between sm:gap-6">
            <div className="min-w-0">
              <p className="m-0 text-[10px] font-bold uppercase tracking-[0.14em] text-deep-hover">Privacy controls</p>
              <h2 className="m-0 mt-1 font-display text-xl font-medium text-ink" id="cookie-consent-title">Your cookie choices</h2>
              <p className="m-0 mt-1.5 text-[13px] leading-5 text-gray-600" id="cookie-consent-copy">
                Essential storage keeps NestyStay secure. Analytics and marketing are optional and disabled by default.
                <AppLink className="ml-1 font-semibold text-deep-hover underline" href="/cookies">Cookie Policy</AppLink>
                <span aria-hidden="true"> · </span>
                <AppLink className="font-semibold text-deep-hover underline" href="/privacy">Privacy Policy</AppLink>.
              </p>
            </div>
            <div className="flex shrink-0 flex-wrap gap-2 sm:justify-end">
              <button className="cookie-consent-control min-h-11 rounded-pill border border-sand-input bg-transparent px-4 text-sm font-semibold text-ink hover:bg-shell" onClick={openSettings} type="button">Manage preferences</button>
              <button className="cookie-consent-control min-h-11 rounded-pill border border-sand-input bg-transparent px-4 text-sm font-semibold text-ink hover:bg-shell" onClick={() => savePreferences(false, false)} type="button">Reject optional</button>
              <button className="cookie-consent-control min-h-11 rounded-pill bg-deep px-4 text-sm font-semibold text-on-dark-heading hover:bg-deep-hover" onClick={() => savePreferences(true, true)} type="button">Allow all optional</button>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
