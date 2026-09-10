// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { AuthController } from "../hooks/useAuth";
import { PatoisProvider } from "../lib/patois";
import { api } from "../lib/api";
import { PropertyManagerProfessionalPage } from "./PropertyManagerProfessionalPage";

vi.mock("../lib/api", () => ({ api: {
  getPropertyManagerDashboard: vi.fn(), getProfessionalOperationalDashboard: vi.fn(), listProfessionalReservations: vi.fn(), listProfessionalCalendar: vi.fn(), listProfessionalOwnerBlocks: vi.fn(), listProfessionalMaintenance: vi.fn(), listProfessionalCleaning: vi.fn(), listProfessionalInspections: vi.fn(), listProfessionalAssets: vi.fn(), listProfessionalIncidents: vi.fn(),
}, formatMoney: (n: number) => `$${n.toFixed(2)}` }));
const auth = { isAuthenticated: true, session: { userId: "manager", accessToken: "" } } as AuthController;
afterEach(() => { cleanup(); vi.resetAllMocks(); });

describe("professional PM operations", () => {
  it("loads the real operations collections and exposes owner-block workflow", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue({ owners: [{ ownerUserId: "owner" }], properties: [{ id: "property", ownerUserId: "owner" }] } as never);
    vi.mocked(api.getProfessionalOperationalDashboard).mockResolvedValue({ reservations: 2, occupiedNights: 4, portfolioNights: 30, occupancyPercent: 13.33, openMaintenance: 1, openWorkOrders: 0, notReady: 0, upcomingInspections: 0, openIncidents: 0, anomalousUtilities: 0, pendingApprovals: 0, activeVendors: 1, overdueActions: 0 });
    vi.mocked(api.listProfessionalReservations).mockResolvedValue([]); vi.mocked(api.listProfessionalCalendar).mockResolvedValue([]); vi.mocked(api.listProfessionalOwnerBlocks).mockResolvedValue([]); vi.mocked(api.listProfessionalMaintenance).mockResolvedValue([]); vi.mocked(api.listProfessionalCleaning).mockResolvedValue([]); vi.mocked(api.listProfessionalInspections).mockResolvedValue([]); vi.mocked(api.listProfessionalAssets).mockResolvedValue([]); vi.mocked(api.listProfessionalIncidents).mockResolvedValue([]);
    render(<PatoisProvider><PropertyManagerProfessionalPage auth={auth} /></PatoisProvider>);
    await screen.findByText("Reservation workspace"); await waitFor(() => expect(api.listProfessionalReservations).toHaveBeenCalledWith(""));
    expect(screen.getByText("Operations at a glance")).toBeTruthy();
    screen.getByRole("button", { name: "Owner blocks" }).click(); await screen.findByText("Owner blocks and conflicts"); expect(screen.getByText("Owner blocks and conflicts")).toBeTruthy();
  });
});
