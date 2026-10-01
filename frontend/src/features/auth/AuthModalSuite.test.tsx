// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { AuthController } from "../../hooks/useAuth";
import { AuthModalSuite } from "./AuthModalSuite";
import type { PublicWorkspaceRole } from "./workspaceOptions";

const mocks = vi.hoisted(() => ({
  beginTwoFactorEnrollment: vi.fn(),
  confirmTwoFactorEnrollment: vi.fn(),
  requestPasswordReset: vi.fn(),
  getDevelopmentTwoFactorCode: vi.fn(),
  navigate: vi.fn(),
  loadSession: vi.fn(() => null),
  signInWithGoogle: vi.fn(),
}));

vi.mock("../../lib/api", () => ({
  api: {
    beginTwoFactorEnrollment: mocks.beginTwoFactorEnrollment,
    confirmTwoFactorEnrollment: mocks.confirmTwoFactorEnrollment,
    requestPasswordReset: mocks.requestPasswordReset,
    getDevelopmentTwoFactorCode: mocks.getDevelopmentTwoFactorCode,
  },
}));

vi.mock("../../lib/auth", () => ({ loadSession: mocks.loadSession }));
vi.mock("../../components/AppLink", () => ({
  AppLink: ({ children, ...props }: { children: React.ReactNode; [key: string]: unknown }) => <a {...props}>{children}</a>,
  navigate: mocks.navigate,
}));
vi.mock("../../components/layout/PublicShell", () => ({
  EmblemRoundel: () => <span aria-hidden="true">N</span>,
  deepPatternBackground: {},
}));
vi.mock("./googleSignIn", () => ({ signInWithGoogle: mocks.signInWithGoogle }));

const signedInSession = {
  userId: "user-1",
  email: "guest@example.test",
  displayName: "Guest User",
  accessToken: "",
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  roles: ["Guest"],
  permissions: [],
};

function makeAuth(overrides: Partial<AuthController> = {}) {
  return {
    session: null,
    pendingChallenge: null,
    isAuthenticated: false,
    isAuthBusy: false,
    register: vi.fn(),
    login: vi.fn(),
    requestPasswordlessLogin: vi.fn(),
    completePasswordlessLogin: vi.fn(),
    requestSmsFallback: vi.fn(),
    verifySmsFallback: vi.fn(),
    registerPasskey: vi.fn(),
    signInWithPasskey: vi.fn(),
    signInWithGoogle: vi.fn(),
    verify: vi.fn(),
    logout: vi.fn(),
    ...overrides,
  } as unknown as AuthController;
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("AuthModalSuite", () => {
  it.each([
    ["Guest", "For guests", "Find a stay"],
    ["Host", "For hosts & owners", "Share the place"],
    ["PropertyManager", "For property managers", "Keep the whole portfolio"],
    ["ServiceProvider", "For custodians & service providers", "Bring your craft"],
    ["LocalBusiness", "For local businesses", "Put local"],
    ["Officer", "For wellness officers", "Support the stay"],
  ] as const)("shows the right role context for %s", (role, eyebrow, title) => {
    render(<AuthModalSuite auth={makeAuth()} workspaceRole={role as PublicWorkspaceRole} />);

    expect(screen.getByText(eyebrow)).toBeTruthy();
    expect(screen.getByRole("heading", { name: new RegExp(title) })).toBeTruthy();
  });

  it("lets users reveal and hide the login password", () => {
    render(<AuthModalSuite auth={makeAuth()} />);
    const password = screen.getByLabelText("Password") as HTMLInputElement;

    expect(password.type).toBe("password");
    fireEvent.change(password, { target: { value: "Password1!" } });
    fireEvent.click(screen.getByRole("button", { name: "Show password" }));
    expect(password.type).toBe("text");
    expect(password.value).toBe("Password1!");

    fireEvent.click(screen.getByRole("button", { name: "Hide password" }));
    expect(password.type).toBe("password");
  });

  it("lets users reveal and hide both signup password fields", () => {
    render(<AuthModalSuite auth={makeAuth()} initialMode="register" />);
    const password = document.getElementById("register-password") as HTMLInputElement;
    const confirmation = document.getElementById("register-confirm-password") as HTMLInputElement;

    expect(password.type).toBe("password");
    expect(confirmation.type).toBe("password");
    const showButtons = screen.getAllByRole("button", { name: "Show password" });
    fireEvent.click(showButtons[0]);
    fireEvent.click(showButtons[1]);
    expect(password.type).toBe("text");
    expect(confirmation.type).toBe("text");
    expect(screen.getAllByRole("button", { name: "Hide password" })).toHaveLength(2);
  });

  it("shows inline validation for mismatched signup passwords and invalid login email", () => {
    render(<AuthModalSuite auth={makeAuth()} initialMode="register" />);
    fireEvent.change(document.getElementById("register-password")!, { target: { value: "StrongPass1" } });
    fireEvent.change(document.getElementById("register-confirm-password")!, { target: { value: "DifferentPass1" } });
    expect(screen.getByText("Passwords do not match yet.")).toBeTruthy();

    fireEvent.click(screen.getAllByRole("button", { name: /^Log in$/ })[0]);
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "not-an-email" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "Password1!" } });
    fireEvent.click(screen.getAllByRole("button", { name: /^Log in$/ })[1]);
    expect(screen.getByText("Enter a valid email address.")).toBeTruthy();
  });

  it("logs in, handles a server-side 2FA challenge, and completes verification", async () => {
    const login = vi.fn().mockResolvedValue({
      challengeId: "challenge-1",
      email: "guest@example.test",
      challengeExpiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
    const verify = vi.fn().mockResolvedValue(signedInSession);
    const auth = makeAuth({ login, verify });

    render(<AuthModalSuite auth={auth} returnTo="/trips" />);
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "guest@example.test" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "Password1!" } });
    fireEvent.click(screen.getAllByRole("button", { name: /Log in/ })[1]);

    await screen.findByText("Two-factor check");
    for (let index = 1; index <= 6; index += 1) {
      fireEvent.change(screen.getByLabelText(`Digit ${index}`), { target: { value: String(index) } });
    }
    fireEvent.click(screen.getByRole("button", { name: /Verify code/ }));

    await waitFor(() => expect(verify).toHaveBeenCalledWith("123456"));
    expect(mocks.navigate).toHaveBeenCalledWith("/trips");
  });

  it("validates registration, then submits a compliant host registration", async () => {
    const register = vi.fn().mockResolvedValue({ requiresTwoFactor: true, displayName: "Host User" });
    const auth = makeAuth({ register });

    render(<AuthModalSuite auth={auth} initialMode="register" />);
    fireEvent.click(screen.getByRole("button", { name: "Create my account" }));
    expect(screen.getByText("Enter your display name.")).toBeTruthy();
    expect(screen.getByText("Accept the Terms of Service to create an account.")).toBeTruthy();

    fireEvent.change(screen.getByPlaceholderText("Keisha Brown"), { target: { value: "Host User" } });
    fireEvent.change(screen.getByPlaceholderText("you@example.com"), { target: { value: "host@example.test" } });
    fireEvent.change(screen.getByPlaceholderText("+1 876 555 0123"), { target: { value: "+18765550100" } });
    fireEvent.change(document.getElementById("register-password")!, { target: { value: "StrongPass1" } });
    fireEvent.change(document.getElementById("register-confirm-password")!, { target: { value: "StrongPass1" } });
    fireEvent.change(screen.getByRole("combobox"), { target: { value: "Host" } });
    fireEvent.click(screen.getByLabelText(/Terms of Service/));
    fireEvent.click(screen.getByLabelText(/Privacy Policy/));
    fireEvent.click(screen.getByRole("button", { name: "Create my account" }));

    await waitFor(() => expect(register).toHaveBeenCalledWith(expect.objectContaining({
      email: "host@example.test",
      displayName: "Host User",
      role: "Host",
      acceptedTerms: true,
      acceptedPrivacy: true,
    })));
    expect(await screen.findByText("Two-factor check")).toBeTruthy();
  });

  it("supports passwordless request, password reset, and authenticator enrollment", async () => {
    const auth = makeAuth({ requestPasswordlessLogin: vi.fn().mockResolvedValue({}) });
    mocks.beginTwoFactorEnrollment.mockResolvedValue({ enrollmentId: "enrollment-1", otpAuthUri: "data:image/png;base64,qr" });
    mocks.confirmTwoFactorEnrollment.mockResolvedValue({ enabled: true });
    mocks.requestPasswordReset.mockResolvedValue({ message: "Reset email sent." });

    render(<AuthModalSuite auth={auth} />);
    fireEvent.click(screen.getByRole("button", { name: /passwordless/i }));
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "guest@example.test" } });
    fireEvent.click(screen.getByRole("button", { name: "Send secure link" }));
    await screen.findByText(/secure sign-in link has been sent/i);

    fireEvent.click(screen.getByRole("button", { name: /Back to password login/i }));
    fireEvent.click(screen.getByRole("button", { name: /Enable 2FA Authenticator/i }));
    await screen.findByText("Enable authenticator");
    fireEvent.change(screen.getByPlaceholderText("000 000"), { target: { value: "123456" } });
    fireEvent.click(screen.getByRole("button", { name: /Verify & enable 2FA/i }));
    await waitFor(() => expect(mocks.confirmTwoFactorEnrollment).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Forgot password?" }));
    fireEvent.change(screen.getByPlaceholderText("Your account email"), { target: { value: "guest@example.test" } });
    fireEvent.click(screen.getByRole("button", { name: "Send reset link" }));
    expect(await screen.findByText("Reset email sent.")).toBeTruthy();
  });
});
