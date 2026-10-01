export const PUBLIC_WORKSPACE_OPTIONS = [
  { role: "Guest", label: "Guest", description: "Discover and book stays" },
  { role: "Host", label: "Host / Property Owner", description: "Manage listings and reservations" },
  { role: "PropertyManager", label: "Property Manager", description: "Manage properties, owners, finances, and operations" },
  { role: "ServiceProvider", label: "Service Provider", description: "Offer trusted services" },
  { role: "LocalBusiness", label: "Local Business", description: "Promote local products and services" },
  { role: "Officer", label: "Wellness Officer", description: "Provide approved wellness and safety support" },
] as const;

export type PublicWorkspaceRole = typeof PUBLIC_WORKSPACE_OPTIONS[number]["role"];

export const PUBLIC_WORKSPACE_LOGIN_PATHS: Record<PublicWorkspaceRole, string> = {
  Guest: "/login/guest",
  Host: "/login/host",
  PropertyManager: "/login/property-manager",
  ServiceProvider: "/login/service-provider",
  LocalBusiness: "/login/local-business",
  Officer: "/login/wellness-officer",
};

export function isPublicWorkspaceRole(value: string | null | undefined): value is PublicWorkspaceRole {
  return PUBLIC_WORKSPACE_OPTIONS.some((option) => option.role === value);
}
