using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// Persists badge payment intents and delegates provider state changes to the
/// configured payment gateway. A badge assignment is never created from a
/// browser-supplied success flag; it is created only after a zero-value path
/// or a trusted Stripe success/webhook.
/// </summary>
public sealed class EfBadgePaymentStore(
    NestyStayDbContext db,
    IPhaseTwoStore phaseTwoStore,
    IPhaseOneStore phaseOneStore,
    IPaymentGateway paymentGateway,
    TimeProvider timeProvider) : IBadgePaymentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BadgePaymentIntentDto> CreatePurchaseIntentAsync(
        PurchaseBadgeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateIdempotencyKey(idempotencyKey);
        var subjectType = NormalizeSubjectType(request.SubjectType);
        if (request.SubjectId == Guid.Empty) throw new InvalidOperationException("A badge subject is required.");

        await EnsureSeededAsync(cancellationToken);
        var existing = await db.MilestoneBadgePayments
            .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey && !item.IsDeleted, cancellationToken);
        if (existing is not null) return ToDto(existing);

        var normalizedRequest = await ResolveAuthoritativeRequestAsync(
            request with { SubjectType = subjectType, IdempotencyKey = idempotencyKey },
            cancellationToken);
        EnsurePurchaseCanStart(normalizedRequest);

        var existingPending = await db.MilestoneBadgePayments
            .AsNoTracking()
            .Where(item =>
                !item.IsDeleted &&
                item.SubjectType == subjectType &&
                item.SubjectId == normalizedRequest.SubjectId &&
                item.Level == normalizedRequest.Level &&
                (item.Status == PaymentStatus.Pending || item.Status == PaymentStatus.Authorized))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingPending is not null) return ToDto(existingPending);

        var quote = phaseTwoStore.GetBadgePurchaseQuote(normalizedRequest);
        if (!quote.Eligible)
        {
            throw new InvalidOperationException($"Badge eligibility failed: {string.Join(" ", quote.MissingRequirements)}");
        }

        var now = timeProvider.GetUtcNow();
        var payment = new MilestoneBadgePayment
        {
            Id = Guid.NewGuid(),
            SubjectId = normalizedRequest.SubjectId,
            SubjectType = subjectType,
            Level = normalizedRequest.Level,
            Provider = quote.IsFree ? "NestyStay" : paymentGateway.ProviderName,
            IdempotencyKey = idempotencyKey,
            RequestSnapshotJson = JsonSerializer.Serialize(normalizedRequest, JsonOptions),
            Amount = quote.Amount,
            Currency = quote.Currency,
            Status = quote.IsFree ? PaymentStatus.Captured : PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = quote.IsFree ? now : null
        };
        db.MilestoneBadgePayments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        if (quote.IsFree)
        {
            var assignment = phaseTwoStore.FinalizeBadgePurchase(normalizedRequest, $"free_badge_{normalizedRequest.Level.ToString().ToLowerInvariant()}_{normalizedRequest.SubjectId:N}");
            payment.BadgeAssignmentId = assignment.Id;
            payment.ProviderPaymentIntentId = payment.PaymentReferenceForFree();
            payment.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(payment, assignment.Id, null, null, true);
        }

        PaymentAuthorizationResult authorization;
        try
        {
            authorization = await paymentGateway.AuthorizeAsync(
                new PaymentAuthorizationRequest(
                    payment.Id,
                    quote.Amount,
                    quote.Currency,
                    $"NestyStay {normalizedRequest.Level} host badge",
                    idempotencyKey,
                    ManualCapture: false),
                cancellationToken);
        }
        catch (Exception exception)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = exception.Message;
            payment.CompletedAt = timeProvider.GetUtcNow();
            payment.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(payment);
        }

        payment.ProviderPaymentIntentId = authorization.AuthorizationReference;
        payment.Status = authorization.Status;
        payment.CompletedAt = IsTerminal(authorization.Status) ? timeProvider.GetUtcNow() : null;
        payment.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        Guid? assignmentId = null;
        if (authorization.Status == PaymentStatus.Captured)
        {
            var assignment = phaseTwoStore.FinalizeBadgePurchase(normalizedRequest, authorization.AuthorizationReference);
            payment.BadgeAssignmentId = assignment.Id;
            assignmentId = assignment.Id;
            payment.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(payment, assignmentId, authorization.ClientSecret, authorization.PublishableKey);
    }

    public async Task<BadgePaymentIntentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        var payment = await db.MilestoneBadgePayments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == paymentId && !item.IsDeleted, cancellationToken);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<BadgePaymentIntentDto> CreateRenewalIntentAsync(
        Guid assignmentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateIdempotencyKey(idempotencyKey);
        await EnsureSeededAsync(cancellationToken);
        var existing = await db.MilestoneBadgePayments
            .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey && !item.IsDeleted, cancellationToken);
        if (existing is not null) return ToDto(existing);

        var assignment = phaseTwoStore.GetBadgeAssignments().SingleOrDefault(item => item.Id == assignmentId)
            ?? throw new InvalidOperationException("Badge assignment not found.");
        if (!string.Equals(assignment.SubjectType, "Host", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only host badges can be renewed.");
        }
        if (!string.Equals(assignment.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only active badge assignments can be renewed.");
        }

        var renewal = phaseTwoStore.GetRenewals(assignmentId)
            .FirstOrDefault(item => item.PaymentStatus.Equals(nameof(PaymentStatus.Pending), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("No pending renewal exists for this badge assignment.");
        var now = timeProvider.GetUtcNow();
        var payment = new MilestoneBadgePayment
        {
            Id = Guid.NewGuid(),
            SubjectId = assignment.SubjectId,
            SubjectType = assignment.SubjectType,
            Level = assignment.Level,
            BadgeAssignmentId = assignmentId,
            RenewalId = renewal.Id,
            Provider = paymentGateway.ProviderName,
            IdempotencyKey = idempotencyKey,
            RequestSnapshotJson = "{}",
            Amount = renewal.AmountDue,
            Currency = renewal.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MilestoneBadgePayments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        PaymentAuthorizationResult authorization;
        try
        {
            authorization = await paymentGateway.AuthorizeAsync(
                new PaymentAuthorizationRequest(
                    payment.Id,
                    payment.Amount,
                    payment.Currency,
                    $"NestyStay {assignment.Level} host badge renewal",
                    idempotencyKey,
                    ManualCapture: false),
                cancellationToken);
        }
        catch (Exception exception)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = exception.Message;
            payment.CompletedAt = timeProvider.GetUtcNow();
            payment.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(payment);
        }

        payment.ProviderPaymentIntentId = authorization.AuthorizationReference;
        payment.Status = authorization.Status;
        payment.CompletedAt = IsTerminal(authorization.Status) ? timeProvider.GetUtcNow() : null;
        payment.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        Guid? renewedAssignmentId = assignmentId;
        if (authorization.Status == PaymentStatus.Captured)
        {
            phaseTwoStore.FinalizeBadgeRenewal(assignmentId, authorization.AuthorizationReference);
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(payment, renewedAssignmentId, authorization.ClientSecret, authorization.PublishableKey);
    }

    public async Task<BadgePaymentIntentDto?> ApplyWebhookAsync(
        PaymentWebhookUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        var payment = await db.MilestoneBadgePayments
            .SingleOrDefaultAsync(item =>
                !item.IsDeleted &&
                item.ProviderPaymentIntentId == request.PaymentIntentReference,
                cancellationToken);
        if (payment is null) return null;
        if (string.Equals(payment.LastProviderEventId, request.ProviderEventId, StringComparison.Ordinal))
        {
            return ToDto(payment);
        }

        if (ShouldIgnoreTransition(payment.Status, request.Status))
        {
            payment.LastProviderEventId = request.ProviderEventId;
            payment.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(payment);
        }

        payment.Status = request.Status;
        payment.LastProviderEventId = request.ProviderEventId;
        payment.FailureReason = request.Status == PaymentStatus.Failed ? request.Reason : null;
        payment.CompletedAt = IsTerminal(request.Status) ? timeProvider.GetUtcNow() : null;
        payment.UpdatedAt = timeProvider.GetUtcNow();

        if (request.Status == PaymentStatus.Captured && payment.BadgeAssignmentId is null)
        {
            if (payment.RenewalId is not null)
            {
                // Renewal rows carry the assignment relationship through the
                // payment row even before the assignment is refreshed.
                var renewal = phaseTwoStore.GetRenewals()
                    .SingleOrDefault(item => item.Id == payment.RenewalId.Value)
                    ?? throw new InvalidOperationException("Badge renewal record not found.");
                phaseTwoStore.FinalizeBadgeRenewal(renewal.BadgeAssignmentId, payment.ProviderPaymentIntentId);
                payment.BadgeAssignmentId = renewal.BadgeAssignmentId;
            }
            else
            {
                var purchase = JsonSerializer.Deserialize<PurchaseBadgeRequest>(payment.RequestSnapshotJson, JsonOptions)
                    ?? throw new InvalidOperationException("Badge payment request snapshot is invalid.");
                var authoritativePurchase = await ResolveAuthoritativeRequestAsync(purchase, cancellationToken);
                var assignment = phaseTwoStore.FinalizeBadgePurchase(authoritativePurchase, payment.ProviderPaymentIntentId);
                payment.BadgeAssignmentId = assignment.Id;
            }
        }
        else if (request.Status == PaymentStatus.Refunded && payment.BadgeAssignmentId is not null)
        {
            phaseTwoStore.RefundBadge(payment.BadgeAssignmentId.Value, payment.ProviderPaymentIntentId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(payment);
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        // The phase-two store owns deterministic seed creation. Calling the
        // read path here also keeps the payment table usable on a clean local DB.
        _ = phaseOneStore.GetProperties();
        _ = phaseTwoStore.GetBadgeDefinitions();
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<PurchaseBadgeRequest> ResolveAuthoritativeRequestAsync(
        PurchaseBadgeRequest request,
        CancellationToken cancellationToken)
    {
        var subjectType = NormalizeSubjectType(request.SubjectType);
        if (!subjectType.Equals("Host", StringComparison.OrdinalIgnoreCase))
        {
            return request with { SubjectType = subjectType };
        }

        var host = await db.MilestoneUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.SubjectId && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Host account was not found. Badge payments require a real host account.");

        var now = timeProvider.GetUtcNow();
        var approvedBookingCount = await db.MilestoneBookings
            .AsNoTracking()
            .CountAsync(item =>
                item.HostUserId == host.Id &&
                !item.IsDeleted &&
                (item.Status == BookingStatus.Approved ||
                 item.Status == BookingStatus.PaymentCaptured ||
                 item.Status == BookingStatus.Confirmed), cancellationToken);
        var hasPropertyAddress = await db.MilestoneProperties
            .AsNoTracking()
            .AnyAsync(item =>
                item.HostUserId == host.Id &&
                !item.IsDeleted &&
                !item.IsArchived &&
                !item.IsDraft &&
                !string.IsNullOrWhiteSpace(item.Location), cancellationToken);
        var hasWellnessSubscription = await db.MilestoneWellnessSubscriptions
            .AsNoTracking()
            .AnyAsync(item =>
                item.HostUserId == host.Id &&
                !item.IsDeleted &&
                item.Status == "Active" &&
                item.CurrentPeriodEnd > now, cancellationToken);

        // Eligibility facts are intentionally rebuilt from persisted records.
        // Values supplied by the browser are never allowed to grant a badge.
        return request with
        {
            SubjectType = subjectType,
            HostVerificationPassed = string.Equals(host.HostVerificationStatus, "Approved", StringComparison.OrdinalIgnoreCase),
            CompletedApprovedBookings = approvedBookingCount,
            HasPropertyAddress = hasPropertyAddress,
            HasWellnessSubscription = hasWellnessSubscription
        };
    }

    private static void ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 255)
        {
            throw new InvalidOperationException("A unique idempotency key is required for badge payments.");
        }
    }

    private static string NormalizeSubjectType(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Subject type is required.") : value.Trim();

    private void EnsurePurchaseCanStart(PurchaseBadgeRequest request)
    {
        if (request.Level == BadgeLevel.Free)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var current = phaseTwoStore
            .GetBadgeAssignments(request.SubjectType, request.SubjectId)
            .Where(assignment =>
                assignment.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                assignment.PaymentStatus.Equals(nameof(PaymentStatus.Captured), StringComparison.OrdinalIgnoreCase) &&
                assignment.ExpiresAt > now)
            .OrderByDescending(assignment => assignment.Level)
            .FirstOrDefault();

        if (current is not null && current.Level >= request.Level)
        {
            throw new InvalidOperationException(
                $"The {current.Level} badge is already active. Choose a higher badge tier or wait until renewal is due.");
        }
    }

    private static bool IsTerminal(PaymentStatus status) =>
        status is PaymentStatus.Captured or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Refunded;

    private static bool ShouldIgnoreTransition(PaymentStatus current, PaymentStatus next) =>
        current == PaymentStatus.Refunded ||
        (current == PaymentStatus.Captured && (next is PaymentStatus.Pending or PaymentStatus.Authorized));

    private static BadgePaymentIntentDto ToDto(
        MilestoneBadgePayment payment,
        Guid? assignmentId = null,
        string? clientSecret = null,
        string? publishableKey = null,
        bool isFree = false,
        bool alreadyActive = false) =>
        new(
            payment.Id,
            payment.SubjectId,
            payment.SubjectType,
            payment.Level,
            payment.Amount,
            payment.Currency,
            payment.Provider,
            payment.ProviderPaymentIntentId,
            clientSecret,
            publishableKey,
            payment.Status.ToString().ToUpperInvariant(),
            assignmentId ?? payment.BadgeAssignmentId,
            payment.RenewalId,
            payment.IdempotencyKey,
            payment.FailureReason,
            payment.CreatedAt,
            payment.CompletedAt,
            isFree || payment.Amount == 0m,
            alreadyActive);
}

internal static class BadgePaymentEntityExtensions
{
    public static string PaymentReferenceForFree(this MilestoneBadgePayment payment) =>
        $"free_badge_{payment.Level.ToString().ToLowerInvariant()}_{payment.SubjectId:N}";
}
