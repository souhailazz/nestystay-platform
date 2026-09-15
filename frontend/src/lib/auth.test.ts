/** @vitest-environment jsdom */
import { beforeEach, describe, expect, it } from "vitest";
import { clearSession, createLoginSession, loadSession, saveSession } from "./auth";

describe("cookie-backed auth session storage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("stores identity metadata without a bearer secret", () => {
    const session = createLoginSession({
      userId: "user-1",
      email: "guest@example.test",
      requiresTwoFactor: false,
      accessToken: "server-token-that-must-not-be-stored",
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      roles: ["Guest"],
      permissions: [],
    }, "Guest User");

    saveSession(session);
    const raw = window.localStorage.getItem("nestyStay.session");
    expect(raw).not.toContain("server-token");
    expect(loadSession()).toMatchObject({ userId: "user-1", accessToken: "", roles: ["Guest"] });
  });

  it("removes legacy bearer sessions and expired sessions", () => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify({
      userId: "legacy", email: "legacy@example.test", displayName: "Legacy", accessToken: "legacy-secret",
      expiresAt: new Date(Date.now() + 60_000).toISOString(), roles: ["Guest"], permissions: [],
    }));
    expect(loadSession()).toBeNull();
    expect(window.localStorage.getItem("nestyStay.session")).toBeNull();

    window.localStorage.setItem("nestyStay.session", JSON.stringify({
      userId: "expired", email: "expired@example.test", displayName: "Expired", accessToken: "",
      expiresAt: new Date(Date.now() - 60_000).toISOString(), roles: ["Guest"], permissions: [],
    }));
    expect(loadSession()).toBeNull();
  });

  it("clears the client identity on logout while the server cookie is cleared by API", () => {
    window.localStorage.setItem("nestyStay.session", JSON.stringify({ userId: "user-2", email: "u@example.test", displayName: "U", accessToken: "", expiresAt: new Date(Date.now() + 60_000).toISOString(), roles: ["Owner"], permissions: [] }));
    expect(loadSession()).not.toBeNull();
    clearSession();
    expect(loadSession()).toBeNull();
  });
});
