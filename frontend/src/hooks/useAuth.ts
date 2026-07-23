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
      return registered;
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

  const logout = useCallback(() => {
    clearSession();
    setSession(null);
    setPendingChallenge(null);
  }, []);

  return useMemo(
    () => ({
      session,
      pendingChallenge,
      isAuthenticated: Boolean(session),
      isAuthBusy,
      register,
      login,
      signInWithGoogle,
      verify,
      logout,
    }),
    [isAuthBusy, login, logout, pendingChallenge, register, session, signInWithGoogle, verify],
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
