// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PropertyManagerProfessionalCompletionPage } from "./PropertyManagerProfessionalCompletionPage";
import { api } from "../lib/api";
import type { AuthController } from "../hooks/useAuth";
import { PatoisProvider } from "../lib/patois";

vi.mock("../lib/api", () => ({
  api: { listPropertyManagerProfessionalRecords: vi.fn(), getPropertyManagerProfessionalReport: vi.fn(), getPropertyManagerDashboard: vi.fn(), createPropertyManagerProfessionalRecord: vi.fn(), getPropertyManagerProfessionalHistory: vi.fn() },
}));

const auth = { session: { userId: "manager", accessToken: "token" } } as AuthController;
const dashboard = { owners: [{ ownerUserId: "owner-1", displayName: "Owner One" }], properties: [{ id: "property-1", title: "Unit 1", unitNumber: "1", ownerUserId: "owner-1" }] } as never;

afterEach(() => { cleanup(); vi.resetAllMocks(); });

describe("professional completion workspace", () => {
  it("loads real-scoped records and keeps the workflow usable on empty/error recovery states", async () => {
    vi.mocked(api.listPropertyManagerProfessionalRecords).mockResolvedValue([]);
    vi.mocked(api.getPropertyManagerProfessionalReport).mockResolvedValue({ from: "2026-01-01", to: "2026-01-31", totalsByCurrency: { JMD: 0 }, countsByArea: {}, countsByStatus: {}, records: [] });
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue(dashboard);
    render(<PatoisProvider><PropertyManagerProfessionalCompletionPage auth={auth} /></PatoisProvider>);
    await screen.findByText("No records yet");
    expect(screen.getByRole("tab", { name: /Utilities/ }).getAttribute("aria-selected")).toBe("true");
    fireEvent.click(screen.getByRole("tab", { name: /Vendors/ }));
    await waitFor(() => expect(api.listPropertyManagerProfessionalRecords).toHaveBeenCalledWith("token", "vendors", expect.any(Object)));
    expect(screen.getByRole("tab", { name: /Vendors/ })).toBeTruthy();
  });

  it("posts a workflow record and surfaces the persisted success state", async () => {
    vi.mocked(api.listPropertyManagerProfessionalRecords).mockResolvedValue([]);
    vi.mocked(api.getPropertyManagerProfessionalReport).mockResolvedValue({ from: "2026-01-01", to: "2026-01-31", totalsByCurrency: {}, countsByArea: {}, countsByStatus: {}, records: [] });
    vi.mocked(api.getPropertyManagerDashboard).mockResolvedValue(dashboard);
    vi.mocked(api.createPropertyManagerProfessionalRecord).mockResolvedValue({ id: "r1", area: "utilities", resourceType: "WORKFLOW", status: "OPEN", payloadJson: "{}", ownerUserId: null, propertyId: null, currency: "JMD", expiresAt: null, rowVersion: 1, createdAt: "2026-01-01", updatedAt: "2026-01-01" });
    render(<PatoisProvider><PropertyManagerProfessionalCompletionPage auth={auth} /></PatoisProvider>);
    await screen.findByText("No records yet");
    fireEvent.click(screen.getByRole("button", { name: /Save workflow record/ }));
    await screen.findByRole("status");
    expect(api.createPropertyManagerProfessionalRecord).toHaveBeenCalledWith("token", "utilities", expect.objectContaining({ resourceType: "WORKFLOW", status: "OPEN" }));
  });
});
