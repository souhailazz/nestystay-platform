// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { createElement } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AdminPermissions, hasAdminPermission, isAdminSession } from "./adminPermissions";
import { requestConfirmation } from "./confirmation";
import { announceFeedback } from "./feedback";
import { classifyError, userSafeErrorMessage } from "./errorMessages";
import { COOKIE_CONSENT_STORAGE_KEY, COOKIE_CONSENT_VERSION, LEGAL_DETAILS, createCookieConsent, isCurrentCookieConsent, openCookieSettings } from "./legal";
import { cx } from "./ui";
import { getStayImage, stayImages } from "./stayImages";
import { StatusChip, statusToneOf } from "../components/ui/StatusChip";

describe("platform foundation helpers", () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it.each([
    [{ status: 401 }, "auth"],
    [{ status: 404 }, "not-found"],
    [{ status: 409 }, "conflict"],
    [{ status: 422 }, "validation"],
    [{ status: 503 }, "server"],
    [new TypeError("Failed to fetch"), "network"],
    [new Error("unexpected"), "unknown"],
  ] as const)("classifies %j as %s", (error, expected) => {
    expect(classifyError(error)).toBe(expected);
  });

  it("keeps user-safe text while suppressing exception-shaped messages", () => {
    expect(userSafeErrorMessage("The booking date is unavailable.")).toBe("The booking date is unavailable.");
    expect(userSafeErrorMessage("System.Exception: stack trace at Foo()", "Try again.")).toBe("Try again.");
    expect(userSafeErrorMessage({ status: 401 })).toContain("session has expired");
  });

  it("matches classes without introducing empty or false values", () => {
    expect(cx("card", false, null, undefined, "selected")).toBe("card selected");
  });

  it("cycles stay imagery deterministically and keeps descriptive metadata", () => {
    expect(stayImages).toHaveLength(4);
    expect(getStayImage(0)).toEqual(stayImages[0]);
    expect(getStayImage(4)).toEqual(stayImages[0]);
    expect(getStayImage(-1)).toEqual(stayImages[1]);
    stayImages.forEach((image) => {
      expect(image.src).toMatch(/^\/assets\/stays\//);
      expect(image.alt.length).toBeGreaterThan(10);
      expect(image.srcSet).toContain("768w");
    });
  });

  it("maps badge and operational states to the documented visual tones", () => {
    expect(statusToneOf("WELLNESS")).toBe("mint");
    expect(statusToneOf("REJECTED")).toBe("coral");
    expect(statusToneOf("PENDING_VERIFICATION")).toBe("amber");
    expect(statusToneOf("SCHEDULED")).toBe("blue");
    expect(statusToneOf("CONFIRMED")).toBe("green");
    expect(statusToneOf("unknown-state")).toBe("slate");
    render(createElement(StatusChip, { label: "Booking", value: "CONFIRMED" }));
    expect(screen.getByText("Booking: CONFIRMED")).toBeTruthy();
  });

  it("recognizes admin access and the super-administration override", () => {
    const admin = { roles: ["Admin"], permissions: [AdminPermissions.refundManagement] } as never;
    const manager = { roles: ["PropertyManager"], permissions: [] } as never;
    expect(isAdminSession(admin)).toBe(true);
    expect(isAdminSession(manager)).toBe(false);
    expect(hasAdminPermission(admin, AdminPermissions.refundManagement)).toBe(true);
    expect(hasAdminPermission(admin, AdminPermissions.userManagement)).toBe(false);
    expect(hasAdminPermission({ roles: ["Admin"], permissions: [AdminPermissions.superAdministration] } as never, AdminPermissions.userManagement)).toBe(true);
  });

  it("emits confirmation and feedback events without using browser dialogs", async () => {
    const confirmation = vi.fn();
    const feedback = vi.fn();
    const cookieSettings = vi.fn();
    window.addEventListener("nesty:confirm", confirmation);
    window.addEventListener("nesty:feedback", feedback);
    window.addEventListener("nesty:open-cookie-settings", cookieSettings);

    const pending = requestConfirmation({ title: "Archive?", message: "Archive the stay?" });
    announceFeedback("Saved", "success");
    openCookieSettings();

    expect(confirmation).toHaveBeenCalledTimes(1);
    expect(confirmation.mock.calls[0][0]).toMatchObject({ type: "nesty:confirm" });
    expect(feedback.mock.calls[0][0]).toMatchObject({ type: "nesty:feedback" });
    expect(cookieSettings).toHaveBeenCalledTimes(1);
    const event = confirmation.mock.calls[0][0] as CustomEvent<{ resolve: (value: boolean) => void }>;
    event.detail.resolve(true);
    await expect(pending).resolves.toBe(true);
    window.removeEventListener("nesty:confirm", confirmation);
    window.removeEventListener("nesty:feedback", feedback);
    window.removeEventListener("nesty:open-cookie-settings", cookieSettings);
  });

  it("keeps legal and consent constants stable for footer and settings links", () => {
    expect(LEGAL_DETAILS.website).toBe("https://nestystay.net");
    expect(LEGAL_DETAILS.supportTel).toMatch(/^tel:\+/);
    expect(COOKIE_CONSENT_STORAGE_KEY).toBe("nesty-cookie-consent-v2");
    expect(COOKIE_CONSENT_VERSION).toBe("2026-09");
    const consent = createCookieConsent(true, false, Date.UTC(2026, 8, 1));
    expect(consent.essential).toBe(true);
    expect(consent.analytics).toBe(true);
    expect(consent.marketing).toBe(false);
    expect(isCurrentCookieConsent(consent, Date.UTC(2026, 8, 2))).toBe(true);
    expect(isCurrentCookieConsent({ ...consent, version: "old" }, Date.UTC(2026, 8, 2))).toBe(false);
  });
});
