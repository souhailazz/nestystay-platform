namespace NestyStay.Domain;

public enum UserRole
{
    Guest,
    Host,
    Owner,
    PropertyManager,
    AssociationExecutive,
    Tenant,
    ServiceProvider,
    LocalBusiness,
    Officer,
    GateGuard,
    Admin
}

public enum BadgeLevel
{
    Free,
    Verified,
    Trusted,
    Wellness
}

public enum BookingStatus
{
    Draft,
    PendingVerification,
    Approved,
    PaymentCaptured,
    Confirmed,
    Rejected,
    Cancelled
}

public enum VerificationStatus
{
    NotStarted,
    Pending,
    Passed,
    Failed,
    Expired
}

public enum ProviderKind
{
    Ekyc,
    Payment,
    Storage,
    Notification,
    Insurance
}

public enum WellnessVisitType
{
    DriveByPatrol = 1,
    InPersonWithGuest = 2,
    InPersonWithoutGuest = 3,
    PropertyEscort = 4,
    PersonalEscort = 5,
    HalfDaySecurity = 6,
    FullDaySecurity = 7,
    OvernightSecurity = 8
}

public enum AccountStatus
{
    Pending,
    Active,
    Suspended,
    Closed
}

public enum ConsentType
{
    Privacy,
    Ekyc,
    Payments,
    Messaging,
    Insurance
}

public enum VerificationSubjectType
{
    User,
    Property,
    ServiceProvider,
    LocalBusiness,
    Officer,
    Guest
}

public enum PropertyStatus
{
    Draft,
    Listed,
    Unverified,
    Suspended,
    Archived
}

public enum FoundingTier
{
    Standard,
    Platinum,
    Gold,
    Silver
}

public enum CancellationPolicyType
{
    Flexible,
    Moderate,
    Strict,
    Custom
}

public enum PaymentScheduleType
{
    FullAtApproval,
    SplitFortyEightHoursBefore,
    SplitSevenDaysBefore
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Refunded,
    Failed,
    Cancelled
}

public enum EscrowStatus
{
    Held,
    ReleasePending,
    Released,
    Disputed,
    Refunded
}

public enum BadgeAssignmentStatus
{
    Active,
    Expired,
    Suspended,
    Revoked
}

public enum DirectoryProviderType
{
    Custodian,
    Trades
}

public enum BusinessStanding
{
    TopRated,
    GoodStanding,
    Warning,
    FinalWarning,
    Removed
}

public enum GovernanceMode
{
    LicensedManager,
    NoManager
}

public enum MeetingStatus
{
    Draft,
    NoticeSent,
    Open,
    Closed,
    Archived
}

public enum ProxyCutoffOption
{
    FortyEightHours,
    TwentyFourHours,
    TwelveHours,
    MeetingOpen,
    Custom
}

public enum StorageAccessScope
{
    Private,
    Owner,
    Community,
    Admin,
    Public
}

public enum NotificationStatus
{
    Queued,
    Sent,
    Failed,
    Cancelled
}
