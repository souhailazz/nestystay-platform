// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { AuthController } from "../hooks/useAuth";
import { PatoisProvider } from "../lib/patois";
import { api } from "../lib/api";
import { PropertyManagerProfessionalPage } from "./PropertyManagerProfessionalPage";

vi.mock("../lib/api", () => ({ api: {
  getPropertyManagerDashboard: vi.fn(), getProfessionalOperationalDashboard: vi.fn(), listProfessionalReservations: vi.fn(), listProfessionalCalendar: vi.fn(), listProfessionalOwnerBlocks: vi.fn(), listProfessionalMaintenance: vi.fn(), listProfessionalWorkOrders: vi.fn(), listProfessionalCleaning: vi.fn(), listProfessionalInspections: vi.fn(), listProfessionalAssets: vi.fn(), listProfessionalIncidents: vi.fn(), getP0Approvals: vi.fn(), listProfessionalChecklistTemplates: vi.fn(), listProfessionalCorrectiveActions: vi.fn(),
}, formatMoney: (n: number) => `$${n.toFixed(2)}` }));
const auth = { isAuthenticated: true, session: { userId: "manager", accessToken: "" } } as AuthController;
afterEach(() => { cleanup(); vi.resetAllMocks(); });

describe("professional PM operations", () => {
  it("loads the real operations collections and exposes owner-block workflow", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue({ owners: [{ ownerUserId: "owner" }], properties: [{ id: "property", ownerUserId: "owner" }] } as never);
    vi.mocked(api.getProfessionalOperationalDashboard).mockResolvedValue({ reservations: 2, occupiedNights: 4, portfolioNights: 30, occupancyPercent: 13.33, openMaintenance: 1, openWorkOrders: 0, notReady: 0, upcomingInspections: 0, openIncidents: 0, anomalousUtilities: 0, pendingApprovals: 0, activeVendors: 1, overdueActions: 0 });
    vi.mocked(api.listProfessionalReservations).mockResolvedValue([]); vi.mocked(api.listProfessionalCalendar).mockResolvedValue([]); vi.mocked(api.listProfessionalOwnerBlocks).mockResolvedValue([]); vi.mocked(api.listProfessionalMaintenance).mockResolvedValue([]); vi.mocked(api.listProfessionalWorkOrders).mockResolvedValue([{ id: "work-1", propertyId: "property", ownerUserId: "owner", workOrderNumber: "WO-1", scope: "Repair failed item", status: "IN_PROGRESS", laborAmount: 50, partsAmount: 0, otherAmount: 0, taxAmount: 0, finalAmount: 50, ownerResponsibility: 50, managerResponsibility: 0, vendorResponsibility: 0, currency: "JMD", postingStatus: "UNPOSTED", correctionCount: 0, rowVersion: 2 }]); vi.mocked(api.listProfessionalCleaning).mockResolvedValue([]); vi.mocked(api.listProfessionalInspections).mockResolvedValue([]); vi.mocked(api.listProfessionalAssets).mockResolvedValue([]); vi.mocked(api.listProfessionalIncidents).mockResolvedValue([]); vi.mocked(api.getP0Approvals).mockResolvedValue([]); vi.mocked(api.listProfessionalChecklistTemplates).mockResolvedValue([{ id: "template-1", name: "Safety", workflowType: "INSPECTION", version: 2, itemsJson: "[]", status: "ACTIVE", createdAt: "2026-09-10T00:00:00Z" }]); vi.mocked(api.listProfessionalCorrectiveActions).mockResolvedValue([{ id: "action-1", inspectionId: "inspection-1", propertyId: "property", checklistItemId: "door", description: "Repair unsafe door", severity: "HIGH", blocksReadiness: true, status: "IN_PROGRESS", workOrderId: "work-1", resolutionNotes: "", rowVersion: 1, createdAt: "2026-09-10T00:00:00Z" }]);
    render(<PatoisProvider><PropertyManagerProfessionalPage auth={auth} /></PatoisProvider>);
    await screen.findByText("Reservation workspace"); await waitFor(() => expect(api.listProfessionalReservations).toHaveBeenCalledWith(""));
    expect(screen.getByText("Operations at a glance")).toBeTruthy();
    screen.getByRole("button", { name: "Owner blocks" }).click(); await screen.findByText("Owner blocks and conflicts"); expect(screen.getByText("Owner blocks and conflicts")).toBeTruthy();
    screen.getByRole("button", { name: "Work orders" }).click(); expect(await screen.findByText("Work-order financial lifecycle")).toBeTruthy(); expect(screen.getByText(/WO-1/)).toBeTruthy();
    screen.getByRole("button", { name: "Inspections" }).click(); expect(await screen.findByText("Templates and corrective actions")).toBeTruthy(); expect(screen.getByText("HIGH · Repair unsafe door")).toBeTruthy();
  });
});
