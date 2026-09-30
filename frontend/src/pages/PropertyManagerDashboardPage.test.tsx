// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { AuthController } from "../hooks/useAuth";
import { api } from "../lib/api";
import type { PropertyManagerDashboard } from "../lib/api";
import { PatoisProvider } from "../lib/patois";
import { PropertyManagerDashboardPage } from "./PropertyManagerPages";

vi.mock("../lib/api", () => ({
  api: {
    getPropertyManagerDashboard: vi.fn(),
    listPropertyManagerQrs: vi.fn(),
    invitePropertyManagerOwner: vi.fn(),
  },
  formatMoney: (amount: number, currency = "JMD") => `${currency} ${amount.toFixed(2)}`,
}));

vi.mock("./PropertyManagerEnhancementHub", () => ({
  PropertyManagerEnhancementHub: () => null,
}));

const auth = { session: { userId: "manager", accessToken: "manager-token" } } as AuthController;
const dashboard = {
  manager: {
    managerUserId: "manager",
    businessName: "Demo Management",
    subscriptionTier: "STANDARD",
    monthlyAmount: 0,
    subscriptionStatus: "ACTIVE",
    nextBillingAt: "2026-10-01T00:00:00.000Z",
    autoRenew: false,
    billingProviderStatus: "LOCAL",
    unitsUsed: 0,
  },
  totalOwners: 1,
  totalProperties: 0,
  outstandingBalance: 0,
  invoicesDue: 0,
  openMaintenance: 0,
  pendingVerification: 0,
  gateActivity: 0,
  owners: [{ id: "owner-record", ownerUserId: "owner", displayName: "Demo Owner", email: "owner@example.test", verificationStatus: "PENDING", invitationStatus: "INVITED" }],
  properties: [],
  invoices: [],
  maintenance: [],
  utilities: [],
  vendors: [],
  notices: [],
  proposals: [],
  documents: [],
  gateMessages: [],
} as unknown as PropertyManagerDashboard;

afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

describe("Property Manager owner invitations", () => {
  it("does not offer a resend action or claim a duplicate invitation was sent", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue(dashboard);
    vi.mocked(api.listPropertyManagerQrs).mockResolvedValue([]);

    render(<PatoisProvider><PropertyManagerDashboardPage auth={auth} /></PatoisProvider>);

    await screen.findByRole("heading", { name: "Invite owner" });
    expect(screen.queryByRole("button", { name: "Resend invitation" })).toBeNull();

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "owner@example.test" } });
    expect(await screen.findByText(/A duplicate invitation was not sent\./)).toBeTruthy();
    expect(screen.getByRole("button", { name: "Invite owner" }).hasAttribute("disabled")).toBe(true);

    await waitFor(() => expect(api.invitePropertyManagerOwner).not.toHaveBeenCalled());
  });
});
