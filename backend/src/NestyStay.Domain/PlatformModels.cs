namespace NestyStay.Domain;

public sealed record PlatformModule(
    string Key,
    string Name,
    string Phase,
    string Summary,
    IReadOnlyList<UserRole> PrimaryRoles,
    IReadOnlyList<string> Capabilities);

public sealed record PortalDefinition(
    UserRole Role,
    string Name,
    string Purpose,
    IReadOnlyList<string> Navigation);

public sealed record PricebookItem(
    string Key,
    string Label,
    decimal Amount,
    string Currency,
    string Cadence,
    string AppliesTo,
    bool IsConfigurable);

public sealed record VendorAdapterDescriptor(
    ProviderKind Kind,
    string Primary,
    IReadOnlyList<string> Backups,
    string SwitchingTarget,
    string Notes);

public sealed record BookingWorkflowStep(
    int Order,
    BookingStatus Status,
    string Name,
    string Actor,
    string Description);
