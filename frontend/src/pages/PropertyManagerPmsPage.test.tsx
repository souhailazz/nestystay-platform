// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PropertyManagerPmsPage } from "./PropertyManagerPmsPage";
import { api } from "../lib/api";
import type { AuthController } from "../hooks/useAuth";
import type { PropertyManagerDashboard, PropertyManagerReport } from "../lib/api";
import { PatoisProvider } from "../lib/patois";

vi.mock("../lib/api", () => ({
  api: { getPropertyManagerDashboard: vi.fn(), getPropertyManagerReport: vi.fn(), getPropertyManagerPayments: vi.fn(), getPropertyManagerCalendar: vi.fn() },
  formatMoney: (amount: number) => `$${amount.toFixed(2)}`,
}));
const auth = { session: { userId: "manager", accessToken: "" } } as AuthController;
const dashboard = { manager: {}, totalProperties: 1, totalOwners: 1, outstandingBalance: 125, openMaintenance: 0, owners: [], properties: [], maintenance: [], invoices: [], utilities: [], documents: [], vendors: [], notices: [], proposals: [] } as unknown as PropertyManagerDashboard;
const report = { grossInvoiceRevenue: 125, paymentRevenue: 0, maintenanceSpend: 0, utilityRevenue: 0 } as PropertyManagerReport;

afterEach(() => { cleanup(); vi.resetAllMocks(); });
describe("PM workspace loading", () => {
  it("loads reports and payments on the invoices route with a cookie session", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue(dashboard);
    vi.mocked(api.getPropertyManagerReport).mockResolvedValue(report);
    vi.mocked(api.getPropertyManagerPayments).mockResolvedValue([]);
    render(<PatoisProvider><PropertyManagerPmsPage auth={auth} module="invoices" /></PatoisProvider>);
    await screen.findByText("Real portfolio totals");
    await waitFor(() => expect(screen.queryByText("Calculating report…")).toBeNull());
    expect(api.getPropertyManagerReport).toHaveBeenCalledWith("");
    expect(api.getPropertyManagerPayments).toHaveBeenCalledWith("");
  });

  it("shows an initial API error and supports a successful retry", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockRejectedValueOnce(new Error("Service temporarily unavailable")).mockResolvedValue(dashboard);
    vi.mocked(api.getPropertyManagerReport).mockResolvedValue(report);
    render(<PatoisProvider><PropertyManagerPmsPage auth={auth} module="reports" /></PatoisProvider>);
    await screen.findByRole("alert");
    fireEvent.click(screen.getByRole("button", { name: "Retry loading workspace" }));
    await screen.findByText("Real portfolio totals");
    expect(screen.queryByRole("alert")).toBeNull();
    expect(api.getPropertyManagerDashboard).toHaveBeenCalledTimes(2);
  });
});
