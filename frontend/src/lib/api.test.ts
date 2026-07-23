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
});
