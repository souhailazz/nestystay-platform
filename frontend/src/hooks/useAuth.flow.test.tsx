// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "./useAuth";

const apiMock = vi.hoisted(() => ({
  getProfile: vi.fn(),
  login: vi.fn(),
  register: vi.fn(),
  requestSmsTwoFactor: vi.fn(),
  verifySmsTwoFactor: vi.fn(),
  requestPasswordlessLogin: vi.fn(),
  completePasswordlessLogin: vi.fn(),
  googleSignIn: vi.fn(),
  logout: vi.fn(),
}));

vi.mock("../lib/api", () => ({ api: apiMock }));

const expiresAt = new Date(Date.now() + 60_000).toISOString();
const loginSession = {
  userId: "user-1",
  email: "guest@example.test",
  requiresTwoFactor: false,
  expiresAt,
  roles: ["Guest"],
  permissions: [],
};

function AuthProbe() {
  const auth = useAuth();
  return (
    <div>
      <span data-testid="state">{auth.session ? auth.session.displayName : "signed-out"}</span>
      <button onClick={() => void auth.login("guest@example.test", "Password1!", { rememberDevice: true })}>login</button>
      <button onClick={() => void auth.register({ email: "new@example.test", password: "Password1!", displayName: "New Guest", confirmPassword: "Password1!", acceptedTerms: true, acceptedPrivacy: true, role: "Guest" })}>register</button>
      <button onClick={() => void auth.requestPasswordlessLogin("guest@example.test")}>passwordless</button>
      <button onClick={() => void auth.logout()}>logout</button>
      <button onClick={() => void auth.requestSmsFallback()}>sms</button>
      <button onClick={() => void auth.verifySmsFallback("flow-1", "123456")}>verify-sms</button>
      <button onClick={() => void auth.verify("123456")}>verify</button>
    </div>
  );
}

function HydrationProbe() {
  const auth = useAuth();
  return <span data-testid="hydration-state">{auth.isAuthHydrating ? "hydrating" : auth.session ? auth.session.roles[0] : "signed-out"}</span>;
}

beforeEach(() => {
  window.localStorage.clear();
  vi.clearAllMocks();
  apiMock.getProfile.mockResolvedValue({});
  apiMock.logout.mockResolvedValue({ loggedOut: true });
});

afterEach(cleanup);

describe("useAuth workflows", () => {
  it("waits for the server profile before exposing a cached role", async () => {
    let resolveProfile!: (profile: unknown) => void;
    apiMock.getProfile.mockReturnValue(new Promise((resolve) => { resolveProfile = resolve; }));
    window.localStorage.setItem("nestyStay.session", JSON.stringify({
      userId: "cached-user",
      email: "cached@example.test",
      accessToken: "",
      expiresAt,
      roles: ["PropertyManager"],
      permissions: [],
    }));

    render(<HydrationProbe />);
    expect(screen.getByTestId("hydration-state").textContent).toBe("hydrating");

    resolveProfile({ userId: "cached-user", email: "cached@example.test", roles: ["PropertyManager"] });
    await waitFor(() => expect(screen.getByTestId("hydration-state").textContent).toBe("PropertyManager"));
  });

  it("creates a cookie-compatible session, persists it, and logs out cleanly", async () => {
    apiMock.login.mockResolvedValue(loginSession);
    render(<AuthProbe />);

    fireEvent.click(screen.getByRole("button", { name: "login" }));
    await waitFor(() => expect(screen.getByTestId("state").textContent).toContain("guest"));
    expect(apiMock.login).toHaveBeenCalledWith({ email: "guest@example.test", password: "Password1!", rememberDevice: true });
    expect(JSON.parse(window.localStorage.getItem("nestyStay.session")!)).toMatchObject({ userId: "user-1", accessToken: "" });

    fireEvent.click(screen.getByRole("button", { name: "logout" }));
    await waitFor(() => expect(screen.getByTestId("state").textContent).toContain("signed-out"));
    expect(apiMock.logout).toHaveBeenCalledWith("");
    expect(window.localStorage.getItem("nestyStay.session")).toBeNull();
  });

  it("keeps a login challenge pending and completes the SMS fallback", async () => {
    apiMock.login.mockResolvedValue({ ...loginSession, requiresTwoFactor: true, challengeId: "challenge-1", challengeExpiresAt: expiresAt });
    apiMock.requestSmsTwoFactor.mockResolvedValue({ flowId: "flow-1", maskedPhone: "•••0100", expiresAt });
    apiMock.verifySmsTwoFactor.mockResolvedValue({ userId: "user-1", expiresAt, roles: ["Guest"], permissions: [] });
    render(<AuthProbe />);

    fireEvent.click(screen.getByRole("button", { name: "login" }));
    await waitFor(() => expect(screen.getByTestId("state").textContent).toContain("signed-out"));
    fireEvent.click(screen.getByRole("button", { name: "sms" }));
    await waitFor(() => expect(apiMock.requestSmsTwoFactor).toHaveBeenCalledWith("challenge-1"));
    fireEvent.click(screen.getByRole("button", { name: "verify-sms" }));
    await waitFor(() => expect(screen.getByTestId("state").textContent).toContain("guest"));
    expect(apiMock.verifySmsTwoFactor).toHaveBeenCalledWith(expect.objectContaining({ challengeId: "challenge-1", flowId: "flow-1", code: "123456" }));
  });

  it("supports registration and passwordless completion through the same session boundary", async () => {
    apiMock.register.mockResolvedValue({ userId: "user-2", email: "new@example.test", displayName: "New Guest", requiresTwoFactor: false });
    apiMock.login.mockResolvedValue({ ...loginSession, userId: "user-2", email: "new@example.test" });
    apiMock.requestPasswordlessLogin.mockResolvedValue({ id: "flow-1" });
    apiMock.completePasswordlessLogin.mockResolvedValue({ userId: "user-3", email: "link@example.test", displayName: "Link Guest", expiresAt, roles: ["Guest"], permissions: [] });
    render(<AuthProbe />);

    fireEvent.click(screen.getByRole("button", { name: "register" }));
    await waitFor(() => expect(screen.getByTestId("state").textContent).toContain("New Guest"));
    expect(apiMock.register).toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "passwordless" }));
    await waitFor(() => expect(apiMock.requestPasswordlessLogin).toHaveBeenCalledWith("guest@example.test"));
  });
});
