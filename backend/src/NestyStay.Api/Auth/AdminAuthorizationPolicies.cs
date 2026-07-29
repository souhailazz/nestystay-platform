using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NestyStay.Application.Admin;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public static class AdminAuthorizationPolicies
{
    public const string PermissionClaimType = "nesty_admin_permission";
    public const string BookingManagement = "Admin.BookingManagement";
    public const string RefundManagement = "Admin.RefundManagement";
    public const string PaymentManagement = "Admin.PaymentManagement";
    public const string UserManagement = "Admin.UserManagement";
    public const string PropertyModeration = "Admin.PropertyModeration";
    public const string OfficerManagement = "Admin.OfficerManagement";
    public const string FinancialReporting = "Admin.FinancialReporting";
    public const string AuditLogAccess = "Admin.AuditLogAccess";
    public const string SystemConfiguration = "Admin.SystemConfiguration";
    public const string SuperAdministration = "Admin.SuperAdministration";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminTokenAuthenticationHandler.AdminPolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(UserRole.Admin.ToString());
        });

        AddPermissionPolicy(options, BookingManagement, AdminPermissionCatalog.BookingManagement);
        AddPermissionPolicy(options, RefundManagement, AdminPermissionCatalog.RefundManagement);
        AddPermissionPolicy(options, PaymentManagement, AdminPermissionCatalog.PaymentManagement);
        AddPermissionPolicy(options, UserManagement, AdminPermissionCatalog.UserManagement);
        AddPermissionPolicy(options, PropertyModeration, AdminPermissionCatalog.PropertyModeration);
        AddPermissionPolicy(options, OfficerManagement, AdminPermissionCatalog.OfficerManagement);
        AddPermissionPolicy(options, FinancialReporting, AdminPermissionCatalog.FinancialReporting);
        AddPermissionPolicy(options, AuditLogAccess, AdminPermissionCatalog.AuditLogAccess);
        AddPermissionPolicy(options, SystemConfiguration, AdminPermissionCatalog.SystemConfiguration);
        AddPermissionPolicy(options, SuperAdministration, AdminPermissionCatalog.SuperAdministration);
    }

    public static bool HasPermission(ClaimsPrincipal user, string permission) =>
        user.HasClaim(PermissionClaimType, AdminPermissionCatalog.SuperAdministration) ||
        user.HasClaim(PermissionClaimType, permission);

    private static void AddPermissionPolicy(AuthorizationOptions options, string policyName, string permission)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(UserRole.Admin.ToString());
            policy.RequireAssertion(context => HasPermission(context.User, permission));
        });
    }
}
