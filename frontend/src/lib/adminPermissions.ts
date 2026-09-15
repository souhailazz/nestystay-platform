import type { AdminPermission } from "./api";
import type { AuthSession } from "./auth";

export const AdminPermissions = {
  superAdministration: "super_administration",
  bookingManagement: "booking_management",
  refundManagement: "refund_management",
  paymentManagement: "payment_management",
  userManagement: "user_management",
  propertyModeration: "property_moderation",
  officerManagement: "officer_management",
  financialReporting: "financial_reporting",
  auditLogAccess: "audit_log_access",
  systemConfiguration: "system_configuration",
} as const satisfies Record<string, AdminPermission>;

export function isAdminSession(session: AuthSession | null | undefined) {
  return Boolean(session?.roles.includes("Admin"));
}

export function hasAdminPermission(session: AuthSession | null | undefined, permission: AdminPermission) {
  const permissions = session?.permissions ?? [];
  return permissions.includes(AdminPermissions.superAdministration) || permissions.includes(permission);
}
