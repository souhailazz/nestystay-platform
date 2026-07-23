using NestyStay.Domain;

namespace NestyStay.Application.PhaseOne;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string? Phone,
    string? ConfirmPassword = null,
    bool AcceptedTerms = false,
    bool AcceptedPrivacy = false,
    UserRole Role = UserRole.Guest);

public sealed record RegisterUserResponse(Guid UserId, string Email, string DisplayName, bool RequiresTwoFactor);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(Guid UserId, string Email, bool RequiresTwoFactor, string ChallengeId, DateTimeOffset ChallengeExpiresAt);

public sealed record DevelopmentAuthCodeResponse(string ChallengeId, string Code, DateTimeOffset ExpiresAt);

public sealed record VerifyTwoFactorRequest(string ChallengeId, string Code);

public sealed record VerifyTwoFactorResponse(Guid UserId, string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyList<UserRole> Roles);

public sealed record GoogleSignInRequest(string Credential, UserRole? Role = null);

public sealed record GoogleSignInResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<UserRole> Roles,
    string Provider);

public sealed record PropertyListingDto(
    Guid Id,
    Guid HostUserId,
    string HostName,
    string Title,
    string Location,
    string Country,
    decimal NightlyRate,
    string Currency,
    BadgeLevel BadgeLevel,
    bool GuestVerificationEnabled,
    bool InsuraGuestEnabled,
    string CancellationPolicy,
    IReadOnlyList<string> Highlights);

public sealed record CreatePropertyRequest(
    Guid HostUserId,
    string HostName,
    string HostEmail,
    string Title,
    string Location,
    string Country,
    decimal NightlyRate,
    string Currency,
    BadgeLevel BadgeLevel = BadgeLevel.Free,
    bool GuestVerificationEnabled = false,
    bool InsuraGuestEnabled = false,
    string CancellationPolicy = "Flexible",
    IReadOnlyList<string>? Highlights = null);

public sealed record BookingPropertySummaryDto(
    Guid Id,
    string Title,
    string Location,
    string Country,
    string HostName,
    BadgeLevel BadgeLevel,
    bool GuestVerificationEnabled,
    bool InsuraGuestEnabled,
    string CancellationPolicy);

public sealed record BookingPriceLineDto(
    string Code,
    string Description,
    decimal Amount,
    string Currency,
    bool IsRefundable);

public sealed record BookingQuoteRequest(Guid PropertyId, DateOnly CheckIn, DateOnly CheckOut);

public sealed record BookingQuoteDto(
    BookingPropertySummaryDto Property,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal NightlyRate,
    decimal StaySubtotal,
    decimal GuestPlatformFee,
    decimal TotalAmount,
    string Currency,
    bool RequiresGuestVerification,
    bool DatesAvailable,
    DateTimeOffset? HoldExpiresAt,
    IReadOnlyList<BookingPriceLineDto> PriceBreakdown);

public sealed record CreateBookingRequest(
    Guid PropertyId,
    Guid GuestUserId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string? EkycMetaInfo = null,
    string? DocumentType = null,
    string? EkycCallbackUrl = null);

public sealed record BookingNotificationDto(
    string RecipientType,
    string Recipient,
    string Subject,
    DateTimeOffset QueuedAt);

public sealed record BookingDto(
    Guid Id,
    Guid PropertyId,
    Guid HostUserId,
    Guid GuestUserId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string Status,
    string VerificationStatus,
    string PaymentStatus,
    bool RequiresGuestVerification,
    bool DatesHeld,
    DateTimeOffset? HoldExpiresAt,
    int Nights,
    decimal NightlyRate,
    decimal StaySubtotal,
    decimal GuestPlatformFee,
    decimal TotalAmount,
    string Currency,
    string? PropertyTitle,
    string? HostName,
    string? EkycProvider,
    string? EkycTransactionId,
    string? EkycTransactionUrl,
    string? PaymentProvider,
    string? PaymentAuthorizationReference,
    string? PaymentClientSecret,
    string? PaymentCaptureReference,
    IReadOnlyList<BookingPriceLineDto> PriceBreakdown,
    IReadOnlyList<BookingNotificationDto> Notifications,
    IReadOnlyList<string> Timeline);

public sealed record ResolveVerificationRequest(bool Passed, string? ProviderReference = null);
