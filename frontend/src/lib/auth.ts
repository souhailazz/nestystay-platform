import type { AdminPermission, GoogleSignInResponse, LoginResponse, UserRole, VerifyTwoFactorResponse } from "./api";

const STORAGE_KEY = "nestyStay.session";

export type AuthSession = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: UserRole[];
  permissions: AdminPermission[];
};

export function createSession(
  verification: VerifyTwoFactorResponse,
  email: string,
  displayName?: string,
): AuthSession {
  return {
    userId: verification.userId,
    email,
    displayName: displayName?.trim() || email.split("@")[0] || "Nesty guest",
    accessToken: "",
    expiresAt: verification.expiresAt,
    roles: verification.roles,
    permissions: verification.permissions ?? [],
  };
}

export function createGoogleSession(verification: GoogleSignInResponse): AuthSession {
  return {
    userId: verification.userId,
    email: verification.email,
    displayName: verification.displayName.trim() || verification.email.split("@")[0] || "Nesty guest",
    accessToken: "",
    expiresAt: verification.expiresAt,
    roles: verification.roles,
    permissions: verification.permissions ?? [],
  };
}

export function createLoginSession(login: LoginResponse, displayName?: string): AuthSession {
  if (!login.expiresAt || !login.roles) {
    throw new Error("Password login did not include a session.");
  }

  return {
    userId: login.userId,
    email: login.email,
    displayName: displayName?.trim() || login.email.split("@")[0] || "Nesty guest",
    accessToken: "",
    expiresAt: login.expiresAt,
    roles: login.roles,
    permissions: login.permissions ?? [],
  };
}

export function loadSession(): AuthSession | null {
  const stored = window.localStorage.getItem(STORAGE_KEY);
  if (!stored) return null;

  try {
    const session = JSON.parse(stored) as AuthSession;
    // Sessions written by older builds contained a bearer secret.  Remove
    // them rather than ever returning that secret to application code.
    if (session.accessToken || !session.expiresAt || new Date(session.expiresAt).getTime() <= Date.now()) {
      clearSession();
      return null;
    }
    const email = typeof session.email === "string" ? session.email : "";
    const displayName = typeof session.displayName === "string" ? session.displayName.trim() : "";
    if (!session.userId || !email || !session.roles?.length) {
      clearSession();
      return null;
    }
    return {
      ...session,
      email,
      displayName: displayName || email.split("@")[0] || "Nesty guest",
      accessToken: "",
      permissions: session.permissions ?? [],
    };
  } catch {
    clearSession();
    return null;
  }
}

export function saveSession(session: AuthSession) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...session, accessToken: "" }));
}

export function clearSession() {
  window.localStorage.removeItem(STORAGE_KEY);
}
