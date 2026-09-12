import { useCallback, useEffect, useMemo, useState } from "react";
import { api, type GoogleSignInRequest, type LoginResponse, type RegisterUserRequest } from "../lib/api";
import { clearSession, createGoogleSession, createLoginSession, createSession, loadSession, saveSession, type AuthSession } from "../lib/auth";

type PendingChallenge = {
  challengeId: string;
  email: string;
  displayName?: string;
  expiresAt: string;
  deviceName?: string;
  rememberDevice?: boolean;
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

  const login = useCallback(async (email: string, password: string, options?: { deviceName?: string; rememberDevice?: boolean }) => {
    setIsAuthBusy(true);
    try {
      const response = await api.login({ email, password, ...options });
      if (response.requiresTwoFactor) {
        const challenge = toChallenge(response);
        challenge.deviceName = options?.deviceName;
        challenge.rememberDevice = options?.rememberDevice;
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

  const requestSmsFallback = useCallback(async () => {
    if (!pendingChallenge) throw new Error("Start login before requesting SMS verification.");
    return api.requestSmsTwoFactor(pendingChallenge.challengeId);
  }, [pendingChallenge]);

  const verifySmsFallback = useCallback(async (flowId: string, code: string) => {
    if (!pendingChallenge) throw new Error("Start login before verifying SMS.");
    setIsAuthBusy(true);
    try {
      const verified = await api.verifySmsTwoFactor({
        challengeId: pendingChallenge.challengeId,
        flowId,
        code,
        deviceName: pendingChallenge.deviceName,
        rememberDevice: pendingChallenge.rememberDevice,
        userAgent: typeof navigator !== "undefined" ? navigator.userAgent : undefined,
      });
      const nextSession = createSession(verified, pendingChallenge.email, pendingChallenge.displayName);
      saveSession(nextSession);
      setSession(nextSession);
      setPendingChallenge(null);
      return nextSession;
    } finally {
      setIsAuthBusy(false);
    }
  }, [pendingChallenge]);

  const registerPasskey = useCallback(async (label?: string) => {
    const options = await api.beginPasskeyRegistration(session?.accessToken);
    const publicKey = decodeCreationOptions(options.options as unknown as Record<string, unknown>);
    const credential = await navigator.credentials.create({ publicKey });
    if (!(credential instanceof PublicKeyCredential)) throw new Error("Passkey registration was cancelled.");
    const response = credential.response as AuthenticatorAttestationResponse;
    return api.completePasskeyRegistration({ challengeId: options.challengeId, label, response: serializeCredential(credential, response) }, session?.accessToken);
  }, [session?.accessToken]);

  const signInWithPasskey = useCallback(async (email?: string) => {
    const options = await api.beginPasskeyAssertion(email);
    const publicKey = decodeRequestOptions(options.options as unknown as Record<string, unknown>);
    const credential = await navigator.credentials.get({ publicKey });
    if (!(credential instanceof PublicKeyCredential)) throw new Error("Passkey sign-in was cancelled.");
    const response = credential.response as AuthenticatorAssertionResponse;
    const verified = await api.completePasskeyAssertion({ challengeId: options.challengeId, response: serializeCredential(credential, response) });
    const nextSession: AuthSession = { userId: verified.userId, email: verified.email, displayName: verified.displayName, accessToken: "", expiresAt: verified.expiresAt, roles: verified.roles, permissions: verified.permissions ?? [] };
    saveSession(nextSession);
    setSession(nextSession);
    return nextSession;
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
        const verified = await api.verifyTwoFactor(pendingChallenge.challengeId, code, {
          deviceName: pendingChallenge.deviceName,
          rememberDevice: pendingChallenge.rememberDevice,
          userAgent: typeof navigator !== "undefined" ? navigator.userAgent : undefined,
        });
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
    } catch {
      // Logout is intentionally idempotent: an expired or already-cleared
      // server session must not produce an unhandled browser rejection.
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
      requestSmsFallback,
      verifySmsFallback,
      registerPasskey,
      signInWithPasskey,
      signInWithGoogle,
      verify,
      logout,
    }),
    [completePasswordlessLogin, isAuthBusy, login, logout, pendingChallenge, register, registerPasskey, requestPasswordlessLogin, requestSmsFallback, session, signInWithGoogle, signInWithPasskey, verify, verifySmsFallback],
  );
}

export type AuthController = ReturnType<typeof useAuth>;

function decodeBase64Url(value: string): Uint8Array {
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  const binary = window.atob(normalized);
  return Uint8Array.from(binary, (char) => char.charCodeAt(0));
}

function decodeCreationOptions(options: Record<string, unknown>): PublicKeyCredentialCreationOptions {
  const publicKey = { ...options } as Record<string, unknown>;
  if (typeof publicKey.challenge === "string") publicKey.challenge = decodeBase64Url(publicKey.challenge);
  const user = publicKey.user as Record<string, unknown> | undefined;
  if (user && typeof user.id === "string") user.id = decodeBase64Url(user.id);
  const excludes = publicKey.excludeCredentials as Array<Record<string, unknown>> | undefined;
  excludes?.forEach((item) => { if (typeof item.id === "string") item.id = decodeBase64Url(item.id); });
  return publicKey as unknown as PublicKeyCredentialCreationOptions;
}

function decodeRequestOptions(options: Record<string, unknown>): PublicKeyCredentialRequestOptions {
  const publicKey = { ...options } as Record<string, unknown>;
  if (typeof publicKey.challenge === "string") publicKey.challenge = decodeBase64Url(publicKey.challenge);
  const allow = publicKey.allowCredentials as Array<Record<string, unknown>> | undefined;
  allow?.forEach((item) => { if (typeof item.id === "string") item.id = decodeBase64Url(item.id); });
  return publicKey as unknown as PublicKeyCredentialRequestOptions;
}

function encodeBase64Url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  bytes.forEach((byte) => { binary += String.fromCharCode(byte); });
  return window.btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function serializeCredential(credential: PublicKeyCredential, response: AuthenticatorResponse) {
  const base = { id: credential.id, rawId: encodeBase64Url(credential.rawId), type: credential.type };
  if (response instanceof AuthenticatorAttestationResponse) {
    return { ...base, response: { clientDataJSON: encodeBase64Url(response.clientDataJSON), attestationObject: encodeBase64Url(response.attestationObject), transports: response.getTransports?.() ?? [] } };
  }
  const assertion = response as AuthenticatorAssertionResponse;
  return { ...base, response: { clientDataJSON: encodeBase64Url(assertion.clientDataJSON), authenticatorData: encodeBase64Url(assertion.authenticatorData), signature: encodeBase64Url(assertion.signature), userHandle: assertion.userHandle ? encodeBase64Url(assertion.userHandle) : null } };
}

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
