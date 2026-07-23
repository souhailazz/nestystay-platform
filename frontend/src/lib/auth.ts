import type { GoogleSignInResponse, LoginResponse, UserRole, VerifyTwoFactorResponse } from "./api";

const STORAGE_KEY = "nestyStay.session";

export type AuthSession = {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  expiresAt: string;
  roles: UserRole[];
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
    accessToken: verification.accessToken,
    expiresAt: verification.expiresAt,
    roles: verification.roles,
  };
}

export function createGoogleSession(verification: GoogleSignInResponse): AuthSession {
  return {
    userId: verification.userId,
    email: verification.email,
    displayName: verification.displayName.trim() || verification.email.split("@")[0] || "Nesty guest",
    accessToken: verification.accessToken,
    expiresAt: verification.expiresAt,
    roles: verification.roles,
  };
}

export function createLoginSession(login: LoginResponse, displayName?: string): AuthSession {
  if (!login.accessToken || !login.expiresAt || !login.roles) {
    throw new Error("Password login did not include a session.");
  }

  return {
    userId: login.userId,
    email: login.email,
    displayName: displayName?.trim() || login.email.split("@")[0] || "Nesty guest",
    accessToken: login.accessToken,
    expiresAt: login.expiresAt,
    roles: login.roles,
  };
}

export function loadSession(): AuthSession | null {
  const stored = window.localStorage.getItem(STORAGE_KEY);
  if (!stored) return null;

  try {
    const session = JSON.parse(stored) as AuthSession;
    if (!session.accessToken || new Date(session.expiresAt).getTime() <= Date.now()) {
      clearSession();
      return null;
    }
    return session;
  } catch {
    clearSession();
    return null;
  }
}

export function saveSession(session: AuthSession) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearSession() {
  window.localStorage.removeItem(STORAGE_KEY);
}
