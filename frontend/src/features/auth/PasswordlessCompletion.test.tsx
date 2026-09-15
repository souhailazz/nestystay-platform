// @vitest-environment jsdom
import { StrictMode } from "react";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { PasswordlessCompletionPage } from "./AuthStateContainer";
import type { AuthController } from "../../hooks/useAuth";
import type { AuthSession } from "../../lib/auth";

vi.mock("../../components/AppLink", () => ({ AppLink: ({ children }: { children: React.ReactNode }) => <span>{children}</span>, navigate: vi.fn() }));
afterEach(cleanup);

it("consumes a magic link once across StrictMode replay and auth-state updates", async () => {
  window.history.replaceState(null, "", "/auth/passwordless?flowId=test&token=one-time");
  let resolve!: (session: AuthSession) => void;
  const promise = new Promise<AuthSession>(done => { resolve = done; });
  const completePasswordlessLogin = vi.fn(() => promise);
  const auth = { completePasswordlessLogin } as unknown as AuthController;
  const view = render(<StrictMode><PasswordlessCompletionPage auth={auth} /></StrictMode>);
  view.rerender(<StrictMode><PasswordlessCompletionPage auth={{ ...auth, isAuthBusy: true }} /></StrictMode>);
  expect(completePasswordlessLogin).toHaveBeenCalledTimes(1);
  resolve({ userId: "owner", roles: ["Owner"] } as AuthSession);
  await waitFor(() => expect(screen.getByRole("heading").textContent).toBe("You’re signed in"));
  expect(completePasswordlessLogin).toHaveBeenCalledTimes(1);
});
