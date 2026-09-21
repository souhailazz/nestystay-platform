// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "./useAuth";
import { saveSession } from "../lib/auth";
import { api } from "../lib/api";

vi.mock("../lib/api", () => ({
  api: {
    getProfile: vi.fn(),
  },
}));

function Probe() {
  const auth = useAuth();
  return <span>{auth.session ? "signed-in" : "signed-out"}</span>;
}

describe("useAuth session hydration", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.mocked(api.getProfile).mockReset();
  });

  afterEach(cleanup);

  it("clears a locally cached session when the server rejects it", async () => {
    saveSession({
      userId: "revoked-user",
      email: "revoked@example.test",
      displayName: "Revoked User",
      accessToken: "",
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      roles: ["PropertyManager"],
      permissions: [],
    });
    vi.mocked(api.getProfile).mockRejectedValue({ status: 401 });

    render(<Probe />);

    await waitFor(() => expect(screen.getByText("signed-out")).toBeTruthy());
    expect(window.localStorage.getItem("nestyStay.session")).toBeNull();
  });
});
