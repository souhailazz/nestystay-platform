using NestyStay.Domain;

namespace NestyStay.Application.Abstractions;

public interface IEkycProvider
{
    string ProviderName { get; }
    Task<EkycStartResult> StartCheckAsync(EkycStartRequest request, CancellationToken cancellationToken);
}

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken);
    Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken);
    Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken);
}

public interface IStorageProvider
{
    string ProviderName { get; }
    Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken);
    Task<string> CreateDownloadUrlAsync(string objectKey, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}

public interface INotificationGateway
{
    string ProviderName { get; }
    Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken);
}

public interface IEmailSender
{
    string ProviderName { get; }
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public interface ISmsSender
{
    string ProviderName { get; }
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken);
}

public interface IGoogleIdentityValidator
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken);
}

public interface IDevelopmentAuthSecretStore
{
    void Store(DevelopmentAuthSecret secret);
    DevelopmentAuthSecret? Get(Guid correlationId);
    void Remove(Guid correlationId);
}

public interface IInsuranceProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<string>> GetAvailablePlansAsync(CancellationToken cancellationToken);
}

public sealed record EkycStartRequest(
    string SubjectId,
    UserRole SubjectRole,
    string MerchantBizId,
    string? MetaInfo,
    string DocumentType,
    string? CallbackUrl);

public sealed record EkycStartResult(
    string ProviderName,
    VerificationStatus Status,
    string TransactionId,
    string? TransactionUrl,
    string? ClientPayload);

public sealed record PaymentAuthorizationRequest(
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Description,
    string IdempotencyKey = "");

public sealed record PaymentAuthorizationResult(
    string ProviderName,
    string AuthorizationReference,
    string? ClientSecret,
    PaymentStatus Status,
    DateTimeOffset? ExpiresAt);

public sealed record PaymentCaptureRequest(
    string AuthorizationReference,
    decimal Amount,
    string Currency,
    string IdempotencyKey = "");

public sealed record PaymentCaptureResult(
    string ProviderName,
    string CaptureReference,
    PaymentStatus Status,
    decimal CapturedAmount,
    string Currency);

public sealed record PaymentRefundRequest(
    string PaymentReference,
    decimal Amount,
    string Currency,
    string Reason,
    string IdempotencyKey = "");

public sealed record PaymentRefundResult(
    string ProviderName,
    string RefundReference,
    PaymentStatus Status,
    decimal RefundedAmount,
    string Currency,
    DateTimeOffset RefundedAt);

public sealed record NotificationMessage(string Recipient, string Subject, string Body);

public sealed record EmailMessage(string To, string Subject, string Body, Guid? CorrelationId = null);

public sealed record SmsMessage(string To, string Body, Guid? CorrelationId = null);

public sealed record GoogleIdentity(
    string Subject,
    string Email,
    string DisplayName,
    bool EmailVerified,
    DateTimeOffset ExpiresAt,
    string Issuer,
    string Audience,
    string? PictureUrl);

public sealed record DevelopmentAuthSecret(
    Guid CorrelationId,
    string Destination,
    string Channel,
    string Code,
    string Token,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
