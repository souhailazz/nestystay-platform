import { describe, expect, it } from "vitest";
import { isSafeInternalReturnTo, postAuthRoute } from "./postAuthRoute";

describe("postAuthRoute", () => {
  it("routes a freshly authenticated property manager to the PM workspace", () => {
    expect(postAuthRoute(["PropertyManager"], "Guest")).toBe("/pm/dashboard");
  });

  it("routes an authenticated admin to the admin workspace", () => {
    expect(postAuthRoute(["Admin"], "Guest")).toBe("/admin");
  });

  it("preserves the highest-priority workspace when a user has multiple roles", () => {
    expect(postAuthRoute(["Guest", "PropertyManager", "Host"], "Guest")).toBe("/pm/dashboard");
  });

  it("uses the selected registration role when no session roles exist", () => {
    expect(postAuthRoute(undefined, "Host")).toBe("/host-dashboard");
  });

  it("routes directory roles to the provider workspace", () => {
    expect(postAuthRoute(["ServiceProvider"], "Guest")).toBe("/directory/provider");
    expect(postAuthRoute(["LocalBusiness"], "Guest")).toBe("/directory/provider");
  });

  it("accepts only same-origin path return targets", () => {
    expect(isSafeInternalReturnTo("/pm/dashboard?tab=finance")).toBe(true);
    expect(isSafeInternalReturnTo("//example.com/login")).toBe(false);
    expect(isSafeInternalReturnTo("https://example.com/login")).toBe(false);
  });
});
