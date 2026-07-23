import { describe, expect, it, vi } from "vitest";
import { ApiError, api } from "./api";

function jsonResponse(body: unknown, status = 200, statusText = "OK") {
  return new Response(JSON.stringify(body), {
    status,
    statusText,
    headers: { "Content-Type": "application/json" },
  });
}

function stubFetch(response: Response) {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("api client", () => {
  it("sends bearer tokens and JSON bodies when creating bookings", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "booking-1", status: "PENDING" }));

    await api.createBooking(
      {
        propertyId: "property-1",
        guestUserId: "client-user-id",
        checkIn: "2026-08-01",
        checkOut: "2026-08-04",
      },
      "signed-session-token",
    );

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;

    expect(url).toBe("/api/bookings");
    expect(init.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer signed-session-token");
    expect(headers.get("Content-Type")).toBe("application/json");
    expect(JSON.parse(init.body as string)).toMatchObject({
      propertyId: "property-1",
      guestUserId: "client-user-id",
    });
  });

  it("omits authorization when no token is supplied", async () => {
    const fetchMock = stubFetch(jsonResponse([]));

    await api.getBookings();

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(headers.has("Authorization")).toBe(false);
  });

  it("surfaces API problem details with status codes", async () => {
    stubFetch(jsonResponse({ title: "Forbidden" }, 403, "Forbidden"));

    let caught: unknown;
    try {
      await api.getAdminOperations("guest-token");
    } catch (error) {
      caught = error;
    }

    expect(caught).toBeInstanceOf(ApiError);
    expect(caught).toMatchObject({
      name: "ApiError",
      message: "Forbidden",
      status: 403,
    });
  });

  it("downloads booking documents with bearer tokens and filenames", async () => {
    const fetchMock = stubFetch(new Response(new Blob(["%PDF-1.4"]), {
      status: 200,
      headers: {
        "Content-Type": "application/pdf",
        "Content-Disposition": 'attachment; filename="nestystay-invoice.pdf"',
      },
    }));

    const file = await api.downloadBookingInvoice("booking-1", "signed-session-token");

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/bookings/booking-1/invoice");
    expect(headers.get("Authorization")).toBe("Bearer signed-session-token");
    expect(file.fileName).toBe("nestystay-invoice.pdf");
    expect(file.contentType).toBe("application/pdf");
    expect(await file.blob.text()).toBe("%PDF-1.4");
  });

  it("sends bearer tokens when disabling two-factor authentication", async () => {
    const fetchMock = stubFetch(jsonResponse({ disabled: true }));

    const response = await api.disableTwoFactor("signed-session-token", { code: "ABCD1234-EFGH5678" });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/auth/2fa");
    expect(init.method).toBe("DELETE");
    expect(headers.get("Authorization")).toBe("Bearer signed-session-token");
    expect(headers.get("Content-Type")).toBe("application/json");
    expect(JSON.parse(init.body as string)).toEqual({ code: "ABCD1234-EFGH5678" });
    expect(response.disabled).toBe(true);
  });

  it("creates and saves payment methods through setup-intent references", async () => {
    const setupResponse = jsonResponse({
      setupIntentReference: "stripe_local_seti_user_reference",
      clientSecret: "stripe_local_seti_secret",
      status: "requires_payment_method",
      expiresAt: "2026-08-01T00:00:00Z",
      publishableKey: "pk_test_local",
    });
    const paymentResponse = jsonResponse({
      id: "payment-method-1",
      providerPaymentMethodReference: "stripe_local_pm_reference",
      isDefault: true,
    });
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(setupResponse)
      .mockResolvedValueOnce(paymentResponse);
    vi.stubGlobal("fetch", fetchMock);

    const setupIntent = await api.createPaymentMethodSetupIntent("traveler-1", "signed-session-token");
    const saved = await api.addPaymentMethod("traveler-1", "signed-session-token", {
      setupIntentReference: setupIntent.setupIntentReference,
      isDefault: true,
    });

    const [setupUrl, setupInit] = fetchMock.mock.calls[0] as [string, RequestInit];
    const [saveUrl, saveInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    const setupHeaders = setupInit.headers as Headers;
    const saveHeaders = saveInit.headers as Headers;

    expect(setupUrl).toBe("/api/spec/traveler/traveler-1/payment-methods/setup-intents");
    expect(setupInit.method).toBe("POST");
    expect(setupHeaders.get("Authorization")).toBe("Bearer signed-session-token");
    expect(saveUrl).toBe("/api/spec/traveler/traveler-1/payment-methods");
    expect(saveInit.method).toBe("POST");
    expect(saveHeaders.get("Authorization")).toBe("Bearer signed-session-token");
    expect(JSON.parse(saveInit.body as string)).toEqual({
      setupIntentReference: "stripe_local_seti_user_reference",
      isDefault: true,
    });
    expect(saved.providerPaymentMethodReference).toBe("stripe_local_pm_reference");
  });

  it("sends verified attachment completion evidence", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "attachment-1", status: "Uploaded", scanStatus: "Clean" }));

    await api.completeMessageAttachmentUpload("conversation-1", "attachment-1", "traveler-1", "signed-session-token", {
      contentType: "application/pdf",
      sizeBytes: 32,
      headerBytesBase64: "JVBERi0xLjcK",
      sha256Hash: "a".repeat(64),
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/spec/messages/conversations/conversation-1/attachments/attachment-1/complete?userId=traveler-1");
    expect(init.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer signed-session-token");
    expect(headers.get("Content-Type")).toBe("application/json");
    expect(JSON.parse(init.body as string)).toEqual({
      contentType: "application/pdf",
      sizeBytes: 32,
      headerBytesBase64: "JVBERi0xLjcK",
      sha256Hash: "a".repeat(64),
    });
  });

  it("prepares host property photo uploads with bearer tokens", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "photo-1", status: "PendingUpload", scanStatus: "PendingScan" }));

    await api.preparePropertyPhotoUpload("property-1", "signed-host-token", {
      fileName: "front-porch.png",
      contentType: "image/png",
      sizeBytes: 12,
      sortOrder: 2,
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/properties/property-1/photos/uploads");
    expect(init.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer signed-host-token");
    expect(JSON.parse(init.body as string)).toEqual({
      fileName: "front-porch.png",
      contentType: "image/png",
      sizeBytes: 12,
      sortOrder: 2,
    });
  });

  it("prepares officer wellness report photos without bearer tokens", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "wellness-photo-1", status: "PendingUpload", scanStatus: "PendingScan" }));

    await api.prepareWellnessReportPhotoUpload("visit-1", {
      officerBadgeNumber: "JCF-2026",
      fileName: "report.png",
      contentType: "image/png",
      sizeBytes: 12,
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/wellness/visits/visit-1/report/photos/uploads");
    expect(init.method).toBe("POST");
    expect(headers.has("Authorization")).toBe(false);
    expect(JSON.parse(init.body as string)).toEqual({
      officerBadgeNumber: "JCF-2026",
      fileName: "report.png",
      contentType: "image/png",
      sizeBytes: 12,
    });
  });

  it("prepares admin wellness report photos with bearer tokens", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "wellness-photo-1", status: "PendingUpload", scanStatus: "PendingScan" }));

    await api.prepareAdminWellnessReportPhotoUpload("visit-1", "admin-token", {
      officerBadgeNumber: "ADMIN",
      fileName: "report.png",
      contentType: "image/png",
      sizeBytes: 12,
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/wellness/visits/visit-1/complete/photos/uploads");
    expect(init.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer admin-token");
  });

  it("prepares traveler identity document uploads with bearer tokens", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "identity-upload-1", status: "PendingUpload", scanStatus: "PendingScan" }));

    await api.prepareIdentityDocumentUpload("traveler-1", "signed-session-token", {
      documentType: "Passport",
      fileName: "passport.pdf",
      contentType: "application/pdf",
      sizeBytes: 32,
      issuingCountry: "JM",
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/spec/traveler/traveler-1/identity-documents/uploads");
    expect(init.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer signed-session-token");
    expect(JSON.parse(init.body as string)).toEqual({
      documentType: "Passport",
      fileName: "passport.pdf",
      contentType: "application/pdf",
      sizeBytes: 32,
      issuingCountry: "JM",
    });
  });

  it("prepares admin case evidence uploads with bearer tokens", async () => {
    const fetchMock = stubFetch(jsonResponse({ id: "case-evidence-1", status: "PendingUpload", scanStatus: "PendingScan" }));

    await api.prepareAdminCaseEvidenceUpload("admin-token", "case-1", {
      fileName: "refund-evidence.pdf",
      contentType: "application/pdf",
      sizeBytes: 32,
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Headers;
    expect(url).toBe("/api/spec/admin/cases/case-1/evidence/uploads");
    expect(init.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer admin-token");
    expect(JSON.parse(init.body as string)).toEqual({
      fileName: "refund-evidence.pdf",
      contentType: "application/pdf",
      sizeBytes: 32,
    });
  });
});
