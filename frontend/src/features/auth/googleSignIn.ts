import type { GoogleSignInRequest } from "../../lib/api";

/** Google Identity Services sign-in (native overlay — not stylable, per AUTH-01 spec). */
export async function signInWithGoogle(
  signIn: (profile: GoogleSignInRequest) => Promise<unknown>,
  role: "Guest" | "Host",
) {
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  if (!googleClientId) {
    throw new Error("Google sign-in is unavailable until OAuth is configured.");
  }

  const credential = await requestGoogleCredential(googleClientId);
  return signIn({ credential: credential.raw, role });
}

function requestGoogleCredential(clientId: string) {
  return new Promise<{ email: string; name: string; sub: string; picture?: string; raw: string }>(
    (resolve, reject) => {
      const scriptId = "google-identity-services";
      const existing = document.getElementById(scriptId);
      const loadScript = existing
        ? Promise.resolve()
        : new Promise<void>((scriptResolve, scriptReject) => {
            const script = document.createElement("script");
            script.id = scriptId;
            script.src = "https://accounts.google.com/gsi/client";
            script.async = true;
            script.defer = true;
            script.onload = () => scriptResolve();
            script.onerror = () => scriptReject(new Error("Google sign-in could not load."));
            document.head.appendChild(script);
          });

      loadScript
        .then(() => {
          const google = window.google;
          if (!google?.accounts?.id) {
            reject(new Error("Google sign-in is unavailable in this browser."));
            return;
          }

          google.accounts.id.initialize({
            client_id: clientId,
            callback: (response: { credential?: string }) => {
              if (!response.credential) {
                reject(new Error("Google did not return a credential."));
                return;
              }
              resolve(decodeGoogleCredential(response.credential));
            },
          });
          google.accounts.id.prompt(
            (notification: { isNotDisplayed?: () => boolean; isSkippedMoment?: () => boolean }) => {
              if (notification.isNotDisplayed?.() || notification.isSkippedMoment?.()) {
                reject(new Error("Google sign-in prompt was dismissed."));
              }
            },
          );
        })
        .catch(reject);
    },
  );
}

function decodeGoogleCredential(raw: string) {
  const payload = raw.split(".")[1];
  if (!payload) throw new Error("Google credential is malformed.");
  const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
  const decoded = JSON.parse(window.atob(padded)) as {
    email?: string;
    name?: string;
    sub?: string;
    picture?: string;
  };
  if (!decoded.email || !decoded.sub) throw new Error("Google credential is missing account details.");
  return {
    email: decoded.email,
    name: decoded.name ?? decoded.email.split("@")[0],
    sub: decoded.sub,
    picture: decoded.picture,
    raw,
  };
}
