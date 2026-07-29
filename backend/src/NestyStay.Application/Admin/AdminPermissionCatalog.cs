namespace NestyStay.Application.Admin;

public static class AdminPermissionCatalog
{
    public const string SuperAdministration = "super_administration";
    public const string BookingManagement = "booking_management";
    public const string RefundManagement = "refund_management";
    public const string PaymentManagement = "payment_management";
    public const string UserManagement = "user_management";
    public const string PropertyModeration = "property_moderation";
    public const string OfficerManagement = "officer_management";
    public const string FinancialReporting = "financial_reporting";
    public const string AuditLogAccess = "audit_log_access";
    public const string SystemConfiguration = "system_configuration";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdministration,
        BookingManagement,
        RefundManagement,
        PaymentManagement,
        UserManagement,
        PropertyModeration,
        OfficerManagement,
        FinancialReporting,
        AuditLogAccess,
        SystemConfiguration
    ];

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? permissions, bool defaultToSuperAdministration = false)
    {
        var selected = (permissions ?? [])
            .Select(permission => permission.Trim().ToLowerInvariant())
            .Where(permission => permission.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (selected.Count == 0 && defaultToSuperAdministration)
        {
            selected.Add(SuperAdministration);
        }

        foreach (var permission in selected)
        {
            if (!All.Contains(permission, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Administrator permission '{permission}' is not recognized.");
            }
        }

        return selected
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
