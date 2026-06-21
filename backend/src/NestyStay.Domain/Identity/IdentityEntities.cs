using NestyStay.Domain.Common;

namespace NestyStay.Domain.Identity;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ExternalAuthSubject { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Pending;
}

public sealed class Role : BaseEntity
{
    public UserRole Key { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UserRoleAssignment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public sealed class UserConsent : BaseEntity
{
    public Guid UserId { get; set; }
    public ConsentType Type { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
}
