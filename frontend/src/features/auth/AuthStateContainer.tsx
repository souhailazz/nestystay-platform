import { useEffect, useRef, useState } from "react";
import { AppLink, navigate } from "../../components/AppLink";
import type { AuthController } from "../../hooks/useAuth";
import { AuthModalSuite } from "./AuthModalSuite";

interface AuthStateContainerProps {
  mode?: "login" | "register" | "forgot-password";
  auth: AuthController;
  returnTo?: string;
}

export function AuthStateContainer({ mode = "login", auth, returnTo }: AuthStateContainerProps) {
  return <AuthModalSuite initialMode={mode} auth={auth} returnTo={returnTo} />;
}

export function PasswordlessCompletionPage({ auth }: { auth: AuthController }) {
  const [error, setError] = useState<string | null>(null);
  const [complete, setComplete] = useState(false);
  const { completePasswordlessLogin } = auth;
  const request = useRef<Promise<Awaited<ReturnType<AuthController["completePasswordlessLogin"]>>> | null>(null);

  useEffect(() => {
    let active = true;
    const params = new URLSearchParams(window.location.search);
    const flowId = params.get("flowId");
    const token = params.get("token");
    if (!flowId || !token) {
      setError("This sign-in link is incomplete. Request a new link and try again.");
      return () => { active = false; };
    }
    // React's effect replay and auth-state updates must reuse the same request:
    // this credential can only be consumed once.
    request.current ??= completePasswordlessLogin(flowId, token);
    void request.current
      .then((session) => {
        if (!active) return;
        setComplete(true);
        const roles = session.roles.map((role) => role.toLowerCase());
        window.setTimeout(() => {
          if (!active) return;
          if (roles.includes("propertymanager")) navigate("/pm/dashboard");
          else if (roles.includes("owner")) navigate("/owner/dashboard");
          else if (roles.includes("host")) navigate("/host-dashboard");
          else if (roles.includes("officer")) navigate("/officer/wellness");
          else navigate("/guest-dashboard");
        }, 500);
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : "This sign-in link is no longer valid.");
      });
    return () => { active = false; };
  }, [completePasswordlessLogin]);

  return (
    <main className="mx-auto flex min-h-[60vh] max-w-xl items-center px-6 py-16">
      <section aria-live="polite" className="w-full rounded-card border border-sand-border bg-cream p-8 text-center">
        {error ? (
          <>
            <h1 className="m-0 font-display text-3xl">Sign-in link unavailable</h1>
            <p className="mt-3 text-sm text-gray-600">{error}</p>
            <AppLink className="mt-5 inline-flex min-h-11 items-center rounded-pill bg-deep px-5 font-semibold text-on-dark-heading" href="/login">Request a new link</AppLink>
          </>
        ) : (
          <>
            <h1 className="m-0 font-display text-3xl">{complete ? "You’re signed in" : "Signing you in securely…"}</h1>
            <p className="mt-3 text-sm text-gray-600">{complete ? "Opening your workspace now." : "Please keep this tab open while we verify your one-time link."}</p>
          </>
        )}
      </section>
    </main>
  );
}
