/** @vitest-environment jsdom */

import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "./api";

const apiResponse = () => new Response("{}", {
  status: 200,
  headers: { "Content-Type": "application/json" },
});

class SuccessfulUploadRequest {
  status = 200;
  statusText = "OK";
  response: unknown = {};
  responseType = "";
  withCredentials = false;
  upload = { onprogress: null as ((event: { lengthComputable: boolean; loaded: number; total: number }) => void) | null };
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  onabort: (() => void) | null = null;

  open() {}
  setRequestHeader() {}
  getResponseHeader() { return null; }
  abort() { this.onabort?.(); }
  send() {
    this.upload.onprogress?.({ lengthComputable: true, loaded: 1, total: 1 });
    this.onload?.();
  }
}

describe("frontend API surface", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    document.cookie = "";
  });

  it("exercises every exported endpoint wrapper with a successful contract response", async () => {
    const fetchMock = vi.fn(async () => apiResponse());
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("XMLHttpRequest", SuccessfulUploadRequest);

    const representativeArgument = {
      id: "demo-id",
      token: "demo-token",
      status: "ACTIVE",
      reason: "contract-test",
      query: "Kingston",
      page: 1,
      body: "demo",
    };

    const endpointEntries = Object.entries(api).filter(([, value]) => typeof value === "function");
    expect(endpointEntries.length).toBeGreaterThan(250);

    for (const [name, endpoint] of endpointEntries) {
      const fn = endpoint as (...args: unknown[]) => Promise<unknown>;
      const args = Array.from({ length: fn.length }, () => representativeArgument);
      await expect(Promise.resolve(fn(...args)), name).resolves.toBeDefined();
    }

    expect(fetchMock.mock.calls.length).toBeGreaterThan(150);
  });

  it("sends JSON, credentials, bearer auth, and CSRF headers for unsafe requests", async () => {
    document.cookie = "nestyStay.csrf=csrf-contract-token";
    const fetchMock = vi.fn(async () => apiResponse());
    vi.stubGlobal("fetch", fetchMock);

    await api.createProperty({ title: "Contract property" } as never, "contract-token");

    const [, request] = fetchMock.mock.calls[0] as unknown as [RequestInfo | URL, RequestInit];
    expect(request).toMatchObject({
      method: "POST",
      credentials: "include",
      body: JSON.stringify({ title: "Contract property" }),
    });
    expect(request?.headers).toBeInstanceOf(Headers);
    expect((request?.headers as Headers).get("Authorization")).toBe("Bearer contract-token");
    expect((request?.headers as Headers).get("X-CSRF-Token")).toBe("csrf-contract-token");
    expect((request?.headers as Headers).get("Content-Type")).toBe("application/json");
  });

  it("normalizes structured API errors and retry-after metadata", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({
      title: "Rate limited",
      code: "RATE_LIMITED",
      extensions: { code: "EXTENSION_CODE" },
    }), {
      status: 429,
      statusText: "Too Many Requests",
      headers: { "Content-Type": "application/problem+json", "Retry-After": "2.2" },
    })));

    await expect(api.health()).rejects.toMatchObject({
      message: "Rate limited",
      status: 429,
      code: "RATE_LIMITED",
      retryAfterSeconds: 3,
    });
  });

  it("reports upload progress and supports a successful upload response", async () => {
    vi.stubGlobal("XMLHttpRequest", SuccessfulUploadRequest);
    const progress: number[] = [];
    const file = new File(["payload"], "demo.txt", { type: "text/plain" });

    await expect(api.uploadPropertyPhotoContent("property-id", "photo-id", "token", file, {
      onProgress: (value) => progress.push(value),
    })).resolves.toEqual({});

    expect(progress).toEqual([100, 100]);
  });
});
