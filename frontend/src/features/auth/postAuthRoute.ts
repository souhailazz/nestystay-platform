import { getRouteAccess, getRouteDefinition, parseRoute, USER_ROLES } from "../../app/routeManifest";
import type { UserRole } from "../../lib/api";

export function postAuthRoute(roles: readonly string[] | undefined, fallbackRole: string) {
  const normalizedRoles = (roles?.length ? roles : [fallbackRole]).map((role) => role.toLowerCase());

  if (normalizedRoles.includes("admin")) return "/admin";
  if (normalizedRoles.includes("propertymanager")) return "/pm/dashboard";
  if (normalizedRoles.includes("owner")) return "/owner/dashboard";
  if (normalizedRoles.includes("host")) return "/host-dashboard";
  if (normalizedRoles.includes("officer")) return "/officer/wellness";
  if (normalizedRoles.includes("serviceprovider") || normalizedRoles.includes("localbusiness")) return "/directory/provider";
  return "/guest-dashboard";
}

export function isSafeInternalReturnTo(value: string | undefined): value is string {
  return Boolean(value && value.startsWith("/") && !value.startsWith("//") && !/[\r\n]/.test(value));
}

/**
 * A same-origin return target is only restored when its manifest role guard
 * accepts the authenticated role. Unknown legacy paths remain safe internal
 * targets for backwards compatibility; known role routes cannot be used to
 * jump into another workspace after sign-in.
 */
export function isRoleSafeReturnTo(value: string | undefined, roles: readonly string[] | undefined): value is string {
  if (!isSafeInternalReturnTo(value)) return false;
  const path = value.split(/[?#]/, 1)[0] || "/";
  const route = parseRoute(path, new URLSearchParams(value.includes("?") ? value.slice(value.indexOf("?") + 1).split("#", 1)[0] : ""));
  const definition = getRouteDefinition(route);
  if (!definition || route.name === "not-found") return true;
  const normalizedRoles = (roles ?? []).map((role) => USER_ROLES.find((known) => known.toLowerCase() === role.toLowerCase())).filter((role): role is UserRole => Boolean(role));
  return getRouteAccess(route, { roles: normalizedRoles }).kind === "allowed";
}
