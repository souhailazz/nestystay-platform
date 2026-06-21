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
}

public interface IStorageProvider
{
    string ProviderName { get; }
    Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken);
}

public interface INotificationGateway
{
    string ProviderName { get; }
    Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken);
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
    string Description);

public sealed record PaymentAuthorizationResult(
    string ProviderName,
    string AuthorizationReference,
    string? ClientSecret,
    PaymentStatus Status,
    DateTimeOffset? ExpiresAt);

public sealed record PaymentCaptureRequest(
    string AuthorizationReference,
    decimal Amount,
    string Currency);

public sealed record PaymentCaptureResult(
    string ProviderName,
    string CaptureReference,
    PaymentStatus Status,
    decimal CapturedAmount,
    string Currency);

public sealed record NotificationMessage(string Recipient, string Subject, string Body);
