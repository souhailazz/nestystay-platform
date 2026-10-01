// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CookieConsent } from "./CookieConsent";
import { COOKIE_CONSENT_STORAGE_KEY, createCookieConsent } from "../../lib/legal";

describe("CookieConsent", () => {
  beforeEach(() => {
    window.localStorage.clear();
    document.body.className = "";
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    document.body.innerHTML = "";
  });

  it("keeps optional categories disabled until the visitor chooses them", async () => {
    const user = userEvent.setup();
    render(<CookieConsent />);

    expect(screen.getByRole("region", { name: "Your cookie choices" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Manage preferences" })).toBeTruthy();
    await user.click(screen.getByRole("button", { name: "Reject optional" }));

    const record = JSON.parse(window.localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY) ?? "null") as ReturnType<typeof createCookieConsent>;
    expect(record.essential).toBe(true);
    expect(record.analytics).toBe(false);
    expect(record.marketing).toBe(false);
    expect(Date.parse(record.expiresAt)).toBeGreaterThan(Date.now());
    expect(screen.queryByRole("region", { name: "Cookie consent" })).toBeNull();
    expect(document.body.classList.contains("cookie-consent-open")).toBe(false);
  });

  it("opens the preference dialog, moves focus into it, and restores footer focus", async () => {
    const user = userEvent.setup();
    const trigger = document.createElement("button");
    trigger.dataset.cookieSettingsTrigger = "true";
    trigger.textContent = "Cookie settings";
    document.body.appendChild(trigger);
    trigger.focus();
    window.localStorage.setItem(COOKIE_CONSENT_STORAGE_KEY, JSON.stringify(createCookieConsent(false, false)));
    render(<CookieConsent />);

    window.dispatchEvent(new CustomEvent("nesty:open-cookie-settings"));
    const dialog = await screen.findByRole("dialog", { name: "Manage cookie preferences" });
    await waitFor(() => expect(document.activeElement).toBe(dialog.querySelector("[tabindex='-1']")));
    expect((screen.getByRole("checkbox", { name: /Essential storage/i }) as HTMLInputElement).disabled).toBe(true);
    expect((screen.getByRole("checkbox", { name: /Analytics/i }) as HTMLInputElement).checked).toBe(false);
    expect((screen.getByRole("checkbox", { name: /Marketing/i }) as HTMLInputElement).checked).toBe(false);

    await user.keyboard("{Escape}");
    await waitFor(() => expect(document.activeElement).toBe(trigger));
    expect(screen.queryByRole("dialog", { name: "Manage cookie preferences" })).toBeNull();
  });

  it("reopens consent when the stored version is expired", () => {
    const expired = createCookieConsent(false, false, Date.now() - (181 * 24 * 60 * 60 * 1000));
    window.localStorage.setItem(COOKIE_CONSENT_STORAGE_KEY, JSON.stringify(expired));
    render(<CookieConsent />);
    expect(screen.getByRole("region", { name: "Your cookie choices" })).toBeTruthy();
  });

  it("emits category choices for future optional integrations", async () => {
    const user = userEvent.setup();
    const handler = vi.fn();
    window.addEventListener("nesty:cookie-consent-changed", handler);
    render(<CookieConsent />);

    await user.click(screen.getByRole("button", { name: "Manage preferences" }));
    await user.click(screen.getByRole("checkbox", { name: /Analytics/i }));
    await user.click(screen.getByRole("button", { name: "Save preferences" }));

    expect(handler).toHaveBeenCalledTimes(1);
    expect(handler.mock.calls[0][0]).toMatchObject({
      type: "nesty:cookie-consent-changed",
      detail: { essential: true, analytics: true, marketing: false },
    });
    window.removeEventListener("nesty:cookie-consent-changed", handler);
  });
});
