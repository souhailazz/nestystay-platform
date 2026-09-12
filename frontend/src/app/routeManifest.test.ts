// @vitest-environment jsdom
import { describe, expect, it } from "vitest";
import { FIGMA_SCREEN_MANIFEST } from "./figmaScreenManifest";
import {
  getRouteAccess,
  getRouteDefinition,
  getScreenDefinition,
  manifestAliases,
  manifestTestPaths,
  navigationForRole,
  ROLE_NAVIGATION,
  SCREEN_MANIFEST,
  routeForScreenId,
  USER_ROLES,
  parseRoute,
} from "./routeManifest";

const TEST_VALUE_BY_PARAM: Record<string, string> = {
  bookingId: "booking-00000000-0000-4000-8000-000000000000",
  propertyId: "22222222-2222-4222-8222-222222222222",
  reservationId: "reservation-00000000-0000-4000-8000-000000000000",
  screenId: "PUB-01",
  slug: "sample",
  state: "review",
};

function materialize(pattern: string) {
  return pattern.replace(/:([A-Za-z]+)/g, (_, key: string) => {
    if (pattern === "/booking/:bookingId/:state") return key === "state" ? "other-state" : TEST_VALUE_BY_PARAM[key] ?? "sample";
    return TEST_VALUE_BY_PARAM[key] ?? "sample";
  });
}

function patternParts(pattern: string) {
  return pattern.replace(/\/+$/, "").split("/").filter(Boolean);
}

function patternsCanOverlap(left: string, right: string) {
  const leftParts = patternParts(left);
  const rightParts = patternParts(right);
  if (leftParts.at(-1) === "*" || rightParts.at(-1) === "*") return true;
  if (leftParts.length !== rightParts.length) return false;
  return leftParts.every((part, index) => {
    const other = rightParts[index];
    return part.startsWith(":") || other.startsWith(":") || part === other;
  });
}

function staticSegmentCount(pattern: string) {
  return patternParts(pattern).filter((part) => !part.startsWith(":") && part !== "*").length;
}

describe("canonical route manifest", () => {
  it("has unique screen IDs and canonical paths", () => {
    const ids = SCREEN_MANIFEST.map((screen) => screen.id);
    const paths = SCREEN_MANIFEST.map((screen) => screen.canonicalPath);
    expect(new Set(ids).size).toBe(ids.length);
    expect(new Set(paths).size).toBe(paths.length);
    expect(SCREEN_MANIFEST.filter((screen) => screen.auth !== "public").every((screen) => screen.shell === "workspace")).toBe(true);
  });

  it("resolves every declared pattern to its owning screen with metadata", () => {
    expect(manifestTestPaths()).toHaveLength(SCREEN_MANIFEST.reduce((count, screen) => count + screen.patterns.length, 0));
    SCREEN_MANIFEST.forEach((screen) => {
      expect(screen.id).toBeTruthy();
      expect(screen.title).toBeTruthy();
      expect(screen.componentKey).toBeTruthy();
      screen.patterns.forEach((pattern) => {
        const path = materialize(pattern);
        const route = parseRoute(path, new URLSearchParams());
        expect(getRouteDefinition(route)?.id, `${screen.id} did not resolve ${pattern} as ${path}`).toBe(screen.id);
        expect(route.canonicalPath, `${screen.id} lost its canonical path for ${path}`).toBe(screen.canonicalPath);
        if (screen.id !== "ERR-404") expect(route.name, `${screen.id} fell through for ${pattern} as ${path}`).not.toBe("not-found");
      });
    });
  });

  it("keeps all aliases deterministic and free of loops", () => {
    const aliases = SCREEN_MANIFEST.flatMap((screen) => screen.patterns.slice(1).map((alias) => ({ screen, alias })));
    expect(aliases).toHaveLength(51);
    expect(manifestAliases()).toEqual(aliases.map(({ alias }) => alias));
    aliases.forEach(({ screen, alias }) => {
      expect(alias).not.toBe(screen.patterns[0]);
      const route = parseRoute(materialize(alias), new URLSearchParams("ref=alias"));
      expect(getRouteDefinition(route)?.id, `${alias} resolved to the wrong owner`).toBe(screen.id);
      expect(route.canonicalPath).toBe(screen.canonicalPath);
      expect(routeForScreenId(screen.id), `${screen.id} has no canonical route target`).toBeDefined();
    });
    expect(new Set(manifestAliases()).size).toBe(manifestAliases().length);
    expect(manifestAliases().some((alias) => SCREEN_MANIFEST.some((screen) => screen.patterns[0] === alias))).toBe(false);
  });

  it("rejects ambiguous dynamic patterns before they become unreachable", () => {
    const patterns = SCREEN_MANIFEST.flatMap((screen) => screen.patterns.map((pattern) => ({ screen, pattern })));
    for (let leftIndex = 0; leftIndex < patterns.length; leftIndex += 1) {
      for (let rightIndex = leftIndex + 1; rightIndex < patterns.length; rightIndex += 1) {
        const left = patterns[leftIndex];
        const right = patterns[rightIndex];
        if (left.screen.id === right.screen.id || !patternsCanOverlap(left.pattern, right.pattern)) continue;
        expect(
          staticSegmentCount(left.pattern),
          `Ambiguous route patterns ${left.pattern} (${left.screen.id}) and ${right.pattern} (${right.screen.id})`,
        ).not.toBe(staticSegmentCount(right.pattern));
      }
    }
  });

  it("keeps route components, auth metadata, roles, navigation, and Figma handoff aligned", () => {
    const roleSet = new Set(USER_ROLES);
    SCREEN_MANIFEST.forEach((screen) => {
      expect(screen.roleAccess.length, `${screen.id} has no role metadata`).toBeGreaterThan(0);
      screen.roleAccess.forEach((role) => expect(roleSet.has(role), `${screen.id} references unknown role ${role}`).toBe(true));
      const canonicalRoute = routeForScreenId(screen.id);
      expect(canonicalRoute, `${screen.id} is unreachable from routeForScreenId`).toBeDefined();
      expect(canonicalRoute?.name).toBe(screen.componentKey);
      const parsedCanonicalRoute = parseRoute(materialize(screen.canonicalPath), new URLSearchParams());
      expect(getRouteDefinition(parsedCanonicalRoute)?.id).toBe(screen.id);
      if (screen.auth === "public") {
        expect(screen.shell).not.toBe("workspace");
        expect(screen.roleAccess).toEqual(USER_ROLES);
      } else {
        expect(screen.shell).toBe("workspace");
      }
      if (screen.auth === "authenticated") expect(screen.roleAccess).toEqual(USER_ROLES);
    });

    expect(new Set(FIGMA_SCREEN_MANIFEST.map((screen) => screen.screenId)).size).toBe(FIGMA_SCREEN_MANIFEST.length);
    expect(FIGMA_SCREEN_MANIFEST).toHaveLength(SCREEN_MANIFEST.length);
    FIGMA_SCREEN_MANIFEST.forEach((entry) => {
      const screen = getScreenDefinition(entry.screenId);
      expect(screen, `${entry.screenId} is missing from the canonical route manifest`).toBeDefined();
      expect(entry.canonicalPath).toBe(screen?.canonicalPath);
      expect(entry.pathPatterns).toEqual(screen?.patterns);
      expect(entry.roles).toEqual(screen?.roleAccess);
    });

    USER_ROLES.forEach((role) => {
      const navigation = ROLE_NAVIGATION[role];
      expect(navigationForRole(role)).toEqual(navigation.desktop);
      expect(navigationForRole(role, true)).toEqual(navigation.mobile);
      expect(navigation.mobile).toHaveLength(5);
      [...navigation.desktop, ...navigation.mobile].forEach((item) => {
        const route = parseRoute(item.href, new URLSearchParams());
        expect(getRouteDefinition(route), `${role} navigation points to an unknown route: ${item.href}`).toBeDefined();
        expect(getScreenDefinition(item.screenId), `${role} navigation has an unknown screen ID: ${item.screenId}`).toBeDefined();
        expect(item.roles).toContain(role);
      });
    });
  });

  it("keeps aliases and navigation references attached to real screens", () => {
    SCREEN_MANIFEST.forEach((screen) => {
      (screen.aliases ?? screen.patterns.slice(1)).forEach((alias) => expect(alias).toMatch(/^\//));
    });
    Object.entries(ROLE_NAVIGATION).forEach(([role, navigation]) => [...navigation.desktop, ...navigation.mobile].forEach((item) => {
      expect(getScreenDefinition(item.screenId)).toBeDefined();
      expect(item.href).toMatch(/^\//);
      expect(item.roles).toContain(role);
    }));
    Object.values(ROLE_NAVIGATION).forEach(({ mobile }) => expect(mobile).toHaveLength(5));
  });

  it("enforces public, signed-out, wrong-role, and correct-role states", () => {
    const publicRoute = parseRoute("/explore", new URLSearchParams());
    const protectedRoute = parseRoute("/pm/dashboard", new URLSearchParams());
    expect(getRouteAccess(publicRoute, null).kind).toBe("allowed");
    expect(getRouteAccess(protectedRoute, null).kind).toBe("auth-required");
    expect(getRouteAccess(protectedRoute, { roles: ["Guest"] }).kind).toBe("forbidden");
    expect(getRouteAccess(protectedRoute, { roles: ["PropertyManager"] }).kind).toBe("allowed");
  });

  it("keeps the public QR validator on its dedicated route", () => {
    const route = parseRoute("/gate/qr", new URLSearchParams("token=test-token&propertyId=sample"));
    expect(route.name).toBe("qr-gate");
    expect(route.canonicalPath).toBe("/gate");
    expect(getRouteDefinition(route)?.id).toBe("PM-GUARD");
  });
});
