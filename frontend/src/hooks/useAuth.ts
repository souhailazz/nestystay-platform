import { useCallback, useEffect, useMemo, useState } from "react";
import { api, type GoogleSignInRequest, type LoginResponse, type RegisterUserRequest } from "../lib/api";
import { clearSession, createGoogleSession, createLoginSession, createSession, loadSession, saveSession, type AuthSession } from "../lib/auth";

type PendingChallenge = {
  challengeId: string;
  email: string;
  displayName?: string;
  expiresAt: string;
};

export function useAuth() {
  const [session, setSession] = useState<AuthSession | null>(() => loadSession());
  const [pendingChallenge, setPendingChallenge] = useState<PendingChallenge | null>(null);
  const [isAuthBusy, setIsAuthBusy] = useState(false);

  useEffect(() => {
    const onStorage = () => setSession(loadSession());
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  // Rehydrate identity from the server-managed HttpOnly session cookie after
  // a refresh.  The profile request deliberately never receives a bearer
  // secret in JavaScript; accessToken stays an empty compatibility field.
  useEffect(() => {
    let active = true;
    void api.getProfile().then((profile) => {
      if (!active) return;
      const previous = loadSession();
      const next: AuthSession = {
        userId: profile.userId,
        email: profile.email,
        displayName: profile.displayName,
        accessToken: "",
        expiresAt: previous?.expiresAt ?? new Date(Date.now() + 12 * 60 * 60 * 1000).toISOString(),
        roles: profile.roles,
        permissions: previous?.permissions ?? [],
      };
      saveSession(next);
      setSession(next);
    }).catch((error: unknown) => {
      if (!active) return;
      const status = (error as { status?: number } | null)?.status;
      if (status === 401 && !loadSession()) setSession(null);
    });
    return () => { active = false; };
  }, []);

  const register = useCallback(async (body: RegisterUserRequest) => {
    setIsAuthBusy(true);
    try {
      const registered = await api.register(body);
      const login = await api.login({ email: body.email, password: body.password });
      if (login.requiresTwoFactor) {
        setPendingChallenge(toChallenge(login, registered.displayName));
      } else {
        const nextSession = createLoginSession(login, registered.displayName);
        saveSession(nextSession);
        setSession(nextSession);
        setPendingChallenge(null);
      }
      return { ...registered, requiresTwoFactor: login.requiresTwoFactor };
    } finally {
      setIsAuthBusy(false);
    }
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    setIsAuthBusy(true);
    try {
      const response = await api.login({ email, password });
      if (response.requiresTwoFactor) {
        const challenge = toChallenge(response);
        setPendingChallenge(challenge);
        return challenge;
      }

      const nextSession = createLoginSession(response);
      saveSession(nextSession);
      setSession(nextSession);
      setPendingChallenge(null);
      return nextSession;
    } finally {
      setIsAuthBusy(false);
    }
  }, []);

  const requestPasswordlessLogin = useCallback(async (email: string) => {
    setIsAuthBusy(true);
    try {
      return await api.requestPasswordlessLogin(email);
    } finally {
      setIsAuthBusy(false);
    }
  }, []);

  const completePasswordlessLogin = useCallback(async (flowId: string, token: string) => {
    setIsAuthBusy(true);
    try {
      const response = await api.completePasswordlessLogin({ flowId, token });
      const nextSession: AuthSession = {
        userId: response.userId,
        email: response.email,
        displayName: response.displayName,
        accessToken: "",
        expiresAt: response.expiresAt,
        roles: response.roles,
        permissions: response.permissions ?? [],
      };
      saveSession(nextSession);
      setSession(nextSession);
      setPendingChallenge(null);
      return nextSession;
    } finally {
      setIsAuthBusy(false);
    }
  }, []);

  const verify = useCallback(
    async (code: string) => {
      if (!pendingChallenge) {
        throw new Error("Start login before verifying a 2FA code.");
      }

      setIsAuthBusy(true);
      try {
        const verified = await api.verifyTwoFactor(pendingChallenge.challengeId, code);
        const nextSession = createSession(
          verified,
          pendingChallenge.email,
          pendingChallenge.displayName,
        );
        saveSession(nextSession);
        setSession(nextSession);
        setPendingChallenge(null);
        return nextSession;
      } finally {
        setIsAuthBusy(false);
      }
    },
    [pendingChallenge],
  );

  const signInWithGoogle = useCallback(async (profile: GoogleSignInRequest) => {
    setIsAuthBusy(true);
    try {
      const response = await api.googleSignIn(profile);
      const nextSession = createGoogleSession(response);
      saveSession(nextSession);
      setSession(nextSession);
      setPendingChallenge(null);
      return nextSession;
    } finally {
      setIsAuthBusy(false);
    }
  }, []);

  const logout = useCallback(async () => {
    const accessToken = session?.accessToken;
    try {
      // Cookie sessions deliberately keep accessToken empty.  Always call the
      // server logout endpoint so it clears the HttpOnly cookie and invalidates
      // the persisted session; bearer sessions continue to work unchanged.
      await api.logout(accessToken);
    } finally {
      clearSession();
      setSession(null);
      setPendingChallenge(null);
    }
  }, [session?.accessToken]);

  return useMemo(
    () => ({
      session,
      pendingChallenge,
      isAuthenticated: Boolean(session),
      isAuthBusy,
      register,
      login,
      requestPasswordlessLogin,
      completePasswordlessLogin,
      signInWithGoogle,
      verify,
      logout,
    }),
    [completePasswordlessLogin, isAuthBusy, login, logout, pendingChallenge, register, requestPasswordlessLogin, session, signInWithGoogle, verify],
  );
}

export type AuthController = ReturnType<typeof useAuth>;

function toChallenge(login: LoginResponse, displayName?: string): PendingChallenge {
  if (!login.challengeId || !login.challengeExpiresAt) {
    throw new Error("Password login did not include a 2FA challenge.");
  }

  return {
    challengeId: login.challengeId,
    email: login.email,
    displayName,
    expiresAt: login.challengeExpiresAt,
  };
}
