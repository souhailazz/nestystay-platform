// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
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

  it("renders every operational tab with real-shaped records", async () => {
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue({
      owners: [{ ownerUserId: "owner", displayName: "Owner One", email: "owner@example.test" }],
      properties: [{ id: "property", ownerUserId: "owner", title: "Kingston Flat", unitNumber: "1A" }],
      vendors: [{ id: "vendor", name: "Island Repairs", category: "Maintenance", contact: "vendor@example.test", isActive: true, isSuspended: false }],
    } as never);
    vi.mocked(api.getProfessionalOperationalDashboard).mockResolvedValue({ reservations: 1, occupiedNights: 2, portfolioNights: 30, occupancyPercent: 6.67, openMaintenance: 1, openWorkOrders: 1, notReady: 1, upcomingInspections: 1, openIncidents: 1, anomalousUtilities: 0, pendingApprovals: 0, activeVendors: 1, overdueActions: 0 });
    vi.mocked(api.listProfessionalReservations).mockResolvedValue([{
      bookingId: "booking-1", propertyId: "property", guestUserId: "guest", hostUserId: "host", checkIn: "2026-10-10T00:00:00Z", checkOut: "2026-10-12T00:00:00Z", status: "APPROVED", paymentStatus: "AUTHORIZED", totalAmount: 300, currency: "JMD", notes: [{ id: "note-1", bookingId: "booking-1", authorUserId: "manager", body: "Arrival note", visibility: "INTERNAL", createdAt: "2026-09-10T00:00:00Z" }], guestName: "Guest One", guestEmail: "guest@example.test",
    }]);
    vi.mocked(api.listProfessionalCalendar).mockResolvedValue([{ type: "BOOKING", sourceId: "booking-1", propertyId: "property", startsAt: "2026-10-10T00:00:00Z", endsAt: "2026-10-12T00:00:00Z", title: "Guest One", status: "APPROVED", ownerUserId: "owner", conflictLevel: "WARNING", conflicts: [] }]);
    vi.mocked(api.listProfessionalOwnerBlocks).mockResolvedValue([{ id: "block-1", ownerUserId: "owner", propertyId: "property", startsAt: "2026-10-20T00:00:00Z", endsAt: "2026-10-22T00:00:00Z", timeZone: "America/Jamaica", category: "OWNER_STAY", reason: "Family visit", notes: "Quiet hours", status: "ACTIVE", rowVersion: 1 }]);
    vi.mocked(api.listProfessionalMaintenance).mockResolvedValue([{ id: "maintenance-1", ownerUserId: "owner", propertyId: "property", number: "MC-1", title: "Leaking tap", description: "Kitchen tap", status: "REQUESTED", priority: "HIGH", expenseAmount: 0, ownerCharge: 0, managerFee: 0, currency: "JMD", rowVersion: 1, financiallyPosted: false }]);
    vi.mocked(api.listProfessionalWorkOrders).mockResolvedValue([{ id: "work-1", propertyId: "property", ownerUserId: "owner", workOrderNumber: "WO-1", scope: "Repair failed item", status: "QUOTING", laborAmount: 50, partsAmount: 0, otherAmount: 0, taxAmount: 0, finalAmount: 50, ownerResponsibility: 50, managerResponsibility: 0, vendorResponsibility: 0, currency: "JMD", postingStatus: "UNPOSTED", correctionCount: 0, rowVersion: 1 }]);
    vi.mocked(api.listProfessionalCleaning).mockResolvedValue([{ id: "clean-1", propertyId: "property", bookingId: "booking-1", dueAt: "2026-10-12T12:00:00Z", status: "OPEN", checklistJson: "[]", photosJson: "[]", issues: "", rowVersion: 1, templateName: "Turnover", templateVersion: 1 }]);
    vi.mocked(api.listProfessionalInspections).mockResolvedValue([{ id: "inspection-1", propertyId: "property", inspectionType: "SAFETY", scheduledAt: "2026-10-09T12:00:00Z", checklistJson: JSON.stringify([{ id: "door", label: "Door lock", required: true, completed: false }]), evidenceJson: "[]", findingsJson: "[]", status: "SCHEDULED", rowVersion: 1 }]);
    vi.mocked(api.listProfessionalAssets).mockResolvedValue([{ id: "asset-1", propertyId: "property", assetTag: "A-1", name: "Air conditioner", description: "Bedroom unit", serialReference: "SN-1", condition: "GOOD", category: "APPLIANCE", status: "ACTIVE", quantity: 1, location: "Bedroom", metadataJson: "{}", photosJson: "[]", rowVersion: 1 }]);
    vi.mocked(api.listProfessionalIncidents).mockResolvedValue([{ id: "incident-1", propertyId: "property", incidentType: "DAMAGE", severity: "MEDIUM", occurredAt: "2026-10-11T10:00:00Z", description: "Broken lamp", involvedPartiesJson: "[]", evidenceJson: "[]", actionTaken: "Logged", followUp: "Inspect", financialImpact: 25, status: "OPEN", rowVersion: 1 }]);
    vi.mocked(api.getP0Approvals).mockResolvedValue([]);
    vi.mocked(api.listProfessionalChecklistTemplates).mockResolvedValue([
      { id: "template-1", name: "Safety", workflowType: "INSPECTION", version: 2, itemsJson: "[]", status: "ACTIVE", createdAt: "2026-09-10T00:00:00Z" },
      { id: "template-2", name: "Turnover", workflowType: "CLEANING", version: 1, itemsJson: "[]", status: "ACTIVE", createdAt: "2026-09-10T00:00:00Z" },
    ]);
    vi.mocked(api.listProfessionalCorrectiveActions).mockResolvedValue([{ id: "action-1", inspectionId: "inspection-1", propertyId: "property", checklistItemId: "door", description: "Repair unsafe door", severity: "HIGH", blocksReadiness: true, status: "IN_PROGRESS", workOrderId: "work-1", resolutionNotes: "", rowVersion: 1, createdAt: "2026-09-10T00:00:00Z" }]);

    render(<PatoisProvider><PropertyManagerProfessionalPage auth={auth} /></PatoisProvider>);
    await screen.findByText("Reservation workspace");
    for (const [tab, heading] of [
      ["Master calendar", "Master calendar"],
      ["Owner blocks", "Owner blocks and conflicts"],
      ["Maintenance", "Maintenance lifecycle"],
      ["Work orders", "Work-order financial lifecycle"],
      ["Readiness", "Cleaning and readiness"],
      ["Inspections", "Inspections"],
      ["Assets & inventory", "Assets and inventory"],
      ["Incidents", "Incident tracking"],
    ]) {
      fireEvent.click(screen.getByRole("button", { name: tab }));
      await waitFor(() => expect(screen.getAllByText(heading).length).toBeGreaterThan(0));
    }

    expect(screen.getByText(/Broken lamp/)).toBeTruthy();
  });
});
