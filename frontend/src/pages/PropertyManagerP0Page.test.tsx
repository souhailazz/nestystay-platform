// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { AuthController } from "../hooks/useAuth";
import { PatoisProvider } from "../lib/patois";
import { api } from "../lib/api";
import { PropertyManagerP0Page } from "./PropertyManagerP0Page";

vi.mock("../lib/api", () => ({
  api: {
    getPropertyManagerDashboard: vi.fn(),
    getP0Portfolio: vi.fn(),
    getP0Agreements: vi.fn(),
    getP0FeeRules: vi.fn(),
    getP0Accounts: vi.fn(),
    getP0Journals: vi.fn(),
    getP0Approvals: vi.fn(),
    getP0Payouts: vi.fn(),
    getP0Staff: vi.fn(),
    getP0Profitability: vi.fn(),
    getP0OwnerProfile: vi.fn(),
    getP0OwnerLifecycle: vi.fn(),
    getP0AssignmentHistory: vi.fn(),
    saveP0OwnerProfile: vi.fn(),
  },
  formatMoney: (amount: number, currency = "JMD") => `${currency} ${amount.toFixed(2)}`,
}));

const auth = { session: { userId: "manager", accessToken: "token" } } as AuthController;
const dashboard = {
  totalOwners: 1,
  owners: [{ ownerUserId: "owner", displayName: "Owner One", email: "owner@example.test" }],
  properties: [{ id: "property", ownerUserId: "owner", title: "Kingston Flat", unitNumber: "1A" }],
} as never;
const portfolio = {
  total: 1,
  items: [{ propertyId: "property", propertyTitle: "Kingston Flat", ownerUserId: "owner", ownerName: "Owner One", ownerStatus: "ACTIVE", address: "Kingston" }],
} as never;
const profile = {
  legalName: "Owner One Ltd",
  contactEmail: "owner@example.test",
  contactPhone: "+18765550100",
  billingAddress: "Kingston",
  preferredCurrency: "JMD",
  timeZone: "America/Jamaica",
  notes: "",
} as never;

afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

describe("Property Manager P0 workspace", () => {
  it("loads persisted portfolio data and saves the owner profile through the API", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue(dashboard);
    vi.mocked(api.getP0Portfolio).mockResolvedValue(portfolio);
    vi.mocked(api.getP0Agreements).mockResolvedValue([]);
    vi.mocked(api.getP0FeeRules).mockResolvedValue([]);
    vi.mocked(api.getP0Accounts).mockResolvedValue([]);
    vi.mocked(api.getP0Journals).mockResolvedValue([]);
    vi.mocked(api.getP0Approvals).mockResolvedValue([]);
    vi.mocked(api.getP0Payouts).mockResolvedValue([]);
    vi.mocked(api.getP0Staff).mockResolvedValue([]);
    vi.mocked(api.getP0Profitability).mockResolvedValue({ income: 0, expenses: 0, managementFees: 0, payouts: 0, ownerNet: 0, pmMargin: 0, cashCollected: 0, unreconciledCash: 0, rows: [] } as never);
    vi.mocked(api.getP0OwnerProfile).mockResolvedValue(profile);
    vi.mocked(api.getP0OwnerLifecycle).mockResolvedValue([]);
    vi.mocked(api.getP0AssignmentHistory).mockResolvedValue([]);
    vi.mocked(api.saveP0OwnerProfile).mockResolvedValue(profile);

    render(<PatoisProvider><PropertyManagerP0Page auth={auth} /></PatoisProvider>);

    await screen.findByText("Owner profile & lifecycle");
    await waitFor(() => expect(api.getP0OwnerProfile).toHaveBeenCalledWith("token", "owner"));
    fireEvent.click(screen.getByRole("button", { name: "Save owner profile" }));
    await waitFor(() => expect(api.saveP0OwnerProfile).toHaveBeenCalledWith("token", "owner", expect.objectContaining({ legalName: "Owner One Ltd", preferredCurrency: "JMD" })));
    expect(screen.getByText("Portfolio ownership")).toBeTruthy();
  });
});
