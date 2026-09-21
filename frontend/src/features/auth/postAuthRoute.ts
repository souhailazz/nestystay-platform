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
