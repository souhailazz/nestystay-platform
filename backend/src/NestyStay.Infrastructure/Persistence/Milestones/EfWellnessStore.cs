using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.Wellness;
using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class EfWellnessStore(
    NestyStayDbContext db,
    IPhaseOneStore phaseOneStore,
    IPhaseTwoStore phaseTwoStore,
    IPaymentGateway paymentGateway,
    INotificationGateway notificationGateway,
    IStorageProvider storageProvider,
    IFileSafetyScanner fileSafetyScanner,
    TimeProvider timeProvider) : IWellnessStore
{
    private const string OfficerStatusPending = "Pending";
    private const string OfficerStatusVerified = "Verified";
    private const string OfficerStatusRejected = "Rejected";
    private const string OfficerStatusSuspended = "Suspended";
    private const string OfficerStatusInactive = "Inactive";

    private const string AvailabilityAvailable = "Available";
    private const string AvailabilityInactive = "Inactive";

    private const string VisitRequested = "Requested";
    private const string VisitScheduled = "Scheduled";
    private const string VisitCompleted = "Completed";
    private const string VisitCancelled = "Cancelled";
    private const string VisitRejected = "Rejected";

    private const string ReportMissing = "Missing";
    private const string ReportSubmitted = "Submitted";
    private const string UploadStatusPending = "PendingUpload";
    private const string UploadStatusUploaded = "Uploaded";
    private const string UploadStatusExpired = "Expired";
    private const string UploadStatusQuarantined = "Quarantined";
    private const string ScanStatusPending = "PendingScan";
    private const string ScanStatusClean = "Clean";

    private const string PaymentPending = "Pending";
    private const string PaymentAuthorized = "Authorized";
    private const string PaymentCaptured = "Captured";
    private const string PaymentCancelled = "Cancelled";
    private const string PaymentRefunded = "Refunded";
    private const string PaymentPayoutPending = "PayoutPending";
    private const string PaymentPaidOut = "PaidOut";
    private const long MaximumReportPhotoBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan ReportPhotoUploadLifetime = TimeSpan.FromMinutes(15);
    private static readonly IReadOnlyDictionary<string, string[]> AllowedReportPhotoExtensions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"]
    };
    private static readonly SemaphoreSlim SubscriptionGate = new(1, 1);
    private static readonly SemaphoreSlim AssignmentGate = new(1, 1);

    public async Task<WellnessOfficerDto> OnboardOfficerAsync(OnboardOfficerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BadgeNumber) ||
            string.IsNullOrWhiteSpace(request.Parish) ||
            string.IsNullOrWhiteSpace(request.CoverageArea))
        {
            throw new InvalidOperationException("Officer badge number, parish, and coverage area are required.");
        }
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            throw new InvalidOperationException("Coverage coordinates are invalid.");
        if (request.ServiceRadiusKm is <= 0 or > 500)
            throw new InvalidOperationException("Coverage radius must be between 0 and 500 km.");

        var badgeNumber = NormalizeBadge(request.BadgeNumber);
        if (await db.MilestoneWellnessOfficers.AnyAsync(officer => officer.BadgeNumber == badgeNumber, cancellationToken))
        {
            throw new InvalidOperationException("Officer badge/ID number is already onboarded.");
        }

        var isEligible = NestyStayBusinessRules.IsOfficerEligible(request.IsActiveOffDuty, request.IsRetired);
        var now = timeProvider.GetUtcNow();
        var officer = new MilestoneWellnessOfficer
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            BadgeNumber = badgeNumber,
            Parish = request.Parish.Trim(),
            CoverageArea = request.CoverageArea.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ServiceRadiusKm = request.ServiceRadiusKm,
            IsActiveOffDuty = request.IsActiveOffDuty,
            IsRetired = request.IsRetired,
            VerificationStatus = isEligible ? OfficerStatusPending : OfficerStatusRejected,
            OnboardingStatus = isEligible ? OfficerStatusPending : OfficerStatusRejected,
            AvailabilityStatus = isEligible ? AvailabilityInactive : OfficerStatusRejected,
            VerificationMetadataJson = MilestoneJson.Serialize(new
            {
                submitted = request.VerificationMetadata,
                rule = isEligible ? "Active off-duty JCF officer declared." : "Retired or inactive JCF status blocked."
            }),
            AdminReviewMetadataJson = MilestoneJson.Serialize(new
            {
                reviewedAt = isEligible ? null : (DateTimeOffset?)now,
                reason = isEligible ? null : "Retired or inactive officers cannot join wellness services."
            }),
            NotificationEventsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Officer onboarding submitted"]),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.MilestoneWellnessOfficers.Add(officer);
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Officer onboarding submitted", $"Officer {badgeNumber} entered the wellness queue.", cancellationToken);

        return ToOfficerDto(officer);
    }

    public async Task<IReadOnlyList<WellnessOfficerDto>> GetOfficersAsync(string? status, CancellationToken cancellationToken)
    {
        var officers = await db.MilestoneWellnessOfficers
            .AsNoTracking()
            .OrderByDescending(officer => officer.CreatedAt)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(status))
        {
            officers = officers
                .Where(officer =>
                    officer.VerificationStatus.Equals(status, StringComparison.OrdinalIgnoreCase) ||
                    officer.OnboardingStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return officers.Select(ToOfficerDto).ToList();
    }

    public async Task<WellnessOfficerDto?> GetOfficerAsync(Guid officerId, CancellationToken cancellationToken) =>
        await db.MilestoneWellnessOfficers.AsNoTracking().SingleOrDefaultAsync(officer => officer.Id == officerId, cancellationToken) is { } officer
            ? ToOfficerDto(officer)
            : null;

    public async Task<WellnessOfficerDto?> GetOfficerForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.MilestoneWellnessOfficers.AsNoTracking().SingleOrDefaultAsync(officer => officer.UserId == userId && !officer.IsDeleted, cancellationToken) is { } officer
            ? ToOfficerDto(officer)
            : null;

    public async Task<WellnessSubscriptionDto?> GetSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var subscription = await db.MilestoneWellnessSubscriptions
            .Where(item => item.HostUserId == hostUserId && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (subscription is null) return null;
        if (subscription.Status == "Active" && subscription.CurrentPeriodEnd <= now)
        {
            subscription.Status = "Expired";
            subscription.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        return ToSubscriptionDto(subscription, now);
    }

    public async Task<WellnessSubscriptionDto> StartSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        if (hostUserId == Guid.Empty) throw new InvalidOperationException("A host account is required.");
        await SubscriptionGate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            var current = await db.MilestoneWellnessSubscriptions.SingleOrDefaultAsync(item => item.HostUserId == hostUserId && item.Status == "Active" && item.CurrentPeriodEnd > now && !item.IsDeleted, cancellationToken);
            if (current is not null) return ToSubscriptionDto(current, now);
            var authorization = await paymentGateway.AuthorizeAsync(new PaymentAuthorizationRequest(Guid.NewGuid(), 19m, "USD", "NestyStay Wellness monthly subscription"), cancellationToken);
            if (authorization.Status is not (PaymentStatus.Authorized or PaymentStatus.Captured)) throw new InvalidOperationException("Wellness subscription payment authorization failed.");
            var subscription = new MilestoneWellnessSubscription
            {
                Id = Guid.NewGuid(), HostUserId = hostUserId, MonthlyAmount = 19m, Currency = "USD", Status = "Active",
                CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), IncludedVisits = 1, UsedVisits = 0,
                PaymentProvider = authorization.ProviderName, PaymentReference = authorization.AuthorizationReference,
                CreatedAt = now, UpdatedAt = now, CreatedByUserId = hostUserId
            };
            db.MilestoneWellnessSubscriptions.Add(subscription);
            await db.SaveChangesAsync(cancellationToken);
            await QueueEventAsync("Wellness subscription started", $"Host {hostUserId:N} activated the $19 monthly wellness plan.", cancellationToken);
            return ToSubscriptionDto(subscription, now);
        }
        finally { SubscriptionGate.Release(); }
    }

    public async Task<WellnessSubscriptionDto> RenewSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        if (hostUserId == Guid.Empty) throw new InvalidOperationException("A host account is required.");
        await SubscriptionGate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            var current = await db.MilestoneWellnessSubscriptions
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(item => item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken);
            if (current is not null && current.Status == "Active" && current.CurrentPeriodEnd > now)
            {
                return ToSubscriptionDto(current, now);
            }

            if (current is not null && current.Status == "Active")
            {
                current.Status = "Expired";
                current.UpdatedAt = now;
            }
            var authorization = await paymentGateway.AuthorizeAsync(new PaymentAuthorizationRequest(Guid.NewGuid(), 19m, "USD", "NestyStay Wellness monthly renewal"), cancellationToken);
            if (authorization.Status is not (PaymentStatus.Authorized or PaymentStatus.Captured)) throw new InvalidOperationException("Wellness subscription renewal payment authorization failed.");
            var renewed = new MilestoneWellnessSubscription
            {
                Id = Guid.NewGuid(), HostUserId = hostUserId, MonthlyAmount = 19m, Currency = "USD", Status = "Active",
                CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), IncludedVisits = 1, UsedVisits = 0,
                PaymentProvider = authorization.ProviderName, PaymentReference = authorization.AuthorizationReference,
                CreatedAt = now, UpdatedAt = now, CreatedByUserId = hostUserId
            };
            db.MilestoneWellnessSubscriptions.Add(renewed);
            await db.SaveChangesAsync(cancellationToken);
            await QueueEventAsync("Wellness subscription renewed", $"Host {hostUserId:N} renewed the $19 monthly wellness plan.", cancellationToken);
            return ToSubscriptionDto(renewed, now);
        }
        finally { SubscriptionGate.Release(); }
    }

    public async Task<WellnessSubscriptionDto?> CancelSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var subscription = await db.MilestoneWellnessSubscriptions.SingleOrDefaultAsync(item => item.HostUserId == hostUserId && item.Status == "Active" && !item.IsDeleted, cancellationToken);
        if (subscription is null) return null;
        var now = timeProvider.GetUtcNow();
        subscription.Status = "Cancelled";
        subscription.CancelledAt = now;
        subscription.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Wellness subscription cancelled", $"Host {hostUserId:N} cancelled the wellness subscription.", cancellationToken);
        return ToSubscriptionDto(subscription, now);
    }

    public async Task<IReadOnlyList<WellnessOfficerDto>> GetAvailableOfficersAsync(string parish, DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        var officers = await db.MilestoneWellnessOfficers
            .AsNoTracking()
            .Where(officer =>
                officer.VerificationStatus == OfficerStatusVerified &&
                officer.OnboardingStatus == OfficerStatusVerified &&
                officer.AvailabilityStatus == AvailabilityAvailable &&
                officer.IsActiveOffDuty &&
                !officer.IsRetired)
            .ToListAsync(cancellationToken);

        var activeVisits = await db.MilestoneWellnessVisits
            .AsNoTracking()
            .Where(visit => visit.OfficerId != null && (visit.VisitStatus == VisitScheduled || visit.VisitStatus == "Accepted" || visit.VisitStatus == "InProgress") && visit.ScheduledAt >= scheduledAt.AddDays(-30))
            .GroupBy(visit => visit.OfficerId!.Value)
            .Select(group => new { OfficerId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.OfficerId, item => item.Count, cancellationToken);

        return officers
            .Where(officer => ParishMatches(officer.Parish, parish) && !HasOfficerOverlap(officer.Id, scheduledAt, 60))
            .OrderBy(officer => activeVisits.GetValueOrDefault(officer.Id))
            .ThenBy(officer => officer.UpdatedAt)
            .Select(ToOfficerDto)
            .ToList();
    }

    public Task<WellnessOfficerDto?> ApproveOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) =>
        ReviewOfficerAsync(officerId, OfficerStatusVerified, request, cancellationToken);

    public Task<WellnessOfficerDto?> RejectOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) =>
        ReviewOfficerAsync(officerId, OfficerStatusRejected, request, cancellationToken);

    public Task<WellnessOfficerDto?> SuspendOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) =>
        ReviewOfficerAsync(officerId, OfficerStatusSuspended, request, cancellationToken);

    public Task<WellnessOfficerDto?> ReactivateOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) =>
        ReviewOfficerAsync(officerId, OfficerStatusVerified, request, cancellationToken);

    public async Task<WellnessQuoteDto> QuoteVisitAsync(WellnessQuoteRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var normalizedType = NormalizeVisitType(request.VisitType);
        var pricing = ResolveVisitPricing(normalizedType);
        var missing = ResolveEligibility(request.HostUserId, request.PropertyId, request.ScheduledAt);
        var platformFee = decimal.Round(pricing.Price * ResolveOfficerCommissionPercent() / 100m, 2);

        return new WellnessQuoteDto(
            request.HostUserId,
            request.PropertyId,
            normalizedType,
            request.ScheduledAt,
            pricing.DurationMinutes,
            pricing.Price,
            platformFee,
            decimal.Round(pricing.Price - platformFee, 2),
            "USD",
            missing.Count == 0,
            missing,
            NestyStayBusinessRules.JamaicaEmergencyNumber);
    }

    public async Task<WellnessVisitDto> CreateVisitAsync(CreateWellnessVisitRequest request, CancellationToken cancellationToken)
    {
        var quote = await QuoteVisitAsync(
            new WellnessQuoteRequest(request.HostUserId, request.PropertyId, request.VisitType, request.ScheduledAt, request.Parish, request.Area),
            cancellationToken);
        if (!quote.Eligible)
        {
            throw new InvalidOperationException($"Wellness visit is locked: {string.Join(" ", quote.MissingRequirements)}");
        }

        var visitId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();
        var subscription = await db.MilestoneWellnessSubscriptions.SingleOrDefaultAsync(item => item.HostUserId == request.HostUserId && item.Status == "Active" && item.CurrentPeriodEnd > now && item.UsedVisits < item.IncludedVisits && !item.IsDeleted, cancellationToken);
        var authorization = subscription is null
            ? await paymentGateway.AuthorizeAsync(new PaymentAuthorizationRequest(visitId, quote.Price, quote.Currency, $"NestyStay wellness visit {visitId:N}"), cancellationToken)
            : new PaymentAuthorizationResult("NestyStay Wellness Subscription", subscription.PaymentReference, null, PaymentStatus.Captured, subscription.CurrentPeriodEnd);
        if (authorization.Status is not (PaymentStatus.Authorized or PaymentStatus.Captured)) throw new InvalidOperationException("Wellness visit payment authorization failed.");
        if (subscription is not null) subscription.UsedVisits++;
        var visit = new MilestoneWellnessVisit
        {
            Id = visitId,
            HostUserId = request.HostUserId,
            PropertyId = request.PropertyId,
            Parish = request.Parish.Trim(),
            Area = string.IsNullOrWhiteSpace(request.Area) ? request.Parish.Trim() : request.Area.Trim(),
            VisitType = quote.VisitType,
            ScheduledAt = request.ScheduledAt,
            ScheduledTimeZone = "America/Jamaica",
            DurationMinutes = quote.DurationMinutes,
            Price = quote.Price,
            PlatformFee = quote.PlatformFee,
            OfficerPayoutAmount = quote.OfficerPayoutAmount,
            Currency = quote.Currency,
            PaymentStatus = ToApiPaymentStatus(authorization.Status),
            VisitStatus = VisitRequested,
            ReportStatus = ReportMissing,
            PaymentProvider = authorization.ProviderName,
            PaymentAuthorizationReference = authorization.AuthorizationReference,
            PaymentClientSecret = authorization.ClientSecret ?? string.Empty,
            TimelineJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wellness visit requested", "Host payment authorized"]),
            NotificationEventsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wellness visit requested"]),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.MilestoneWellnessVisits.Add(visit);
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Wellness visit requested", $"Wellness visit {visit.Id:N} is waiting for officer assignment.", cancellationToken);

        return ToVisitDto(visit);
    }

    public async Task<IReadOnlyList<WellnessVisitDto>> GetVisitsAsync(Guid? hostUserId, Guid? propertyId, Guid? officerId, CancellationToken cancellationToken)
    {
        var visits = await db.MilestoneWellnessVisits
            .AsNoTracking()
            .OrderByDescending(visit => visit.ScheduledAt)
            .ToListAsync(cancellationToken);

        return visits
            .Where(visit =>
                (hostUserId is null || visit.HostUserId == hostUserId) &&
                (propertyId is null || visit.PropertyId == propertyId) &&
                (officerId is null || visit.OfficerId == officerId))
            .Select(ToVisitDto)
            .ToList();
    }

    public async Task<WellnessVisitDto?> GetVisitAsync(Guid visitId, CancellationToken cancellationToken) =>
        await db.MilestoneWellnessVisits.AsNoTracking().SingleOrDefaultAsync(visit => visit.Id == visitId, cancellationToken) is { } visit
            ? ToVisitDto(visit)
            : null;

    public async Task<WellnessReportDto?> GetReportAsync(Guid visitId, CancellationToken cancellationToken)
    {
        var report = await db.MilestoneWellnessReports.AsNoTracking().SingleOrDefaultAsync(item => item.VisitId == visitId && !item.IsDeleted, cancellationToken);
        return report is null ? null : new WellnessReportDto(report.Id, report.VisitId, report.OfficerId, report.SubmittedAt, report.ReportStatus, report.Notes, MilestoneJson.DeserializeList<string>(report.PhotosJson));
    }

    public async Task<WellnessVisitDto?> AssignOfficerAsync(Guid visitId, AssignOfficerRequest request, CancellationToken cancellationToken)
    {
        await AssignmentGate.WaitAsync(cancellationToken);
        try
        {
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        if (visit.VisitStatus is VisitCancelled or VisitRejected or VisitCompleted)
        {
            throw new InvalidOperationException("Cancelled, rejected, or completed visits cannot be assigned.");
        }

        var officer = await db.MilestoneWellnessOfficers.SingleOrDefaultAsync(item => item.Id == request.OfficerId, cancellationToken)
            ?? throw new InvalidOperationException("Officer not found.");
        EnsureOfficerAssignable(officer);

        if (!ParishMatches(officer.Parish, visit.Parish))
        {
            throw new InvalidOperationException("Officer coverage does not match the requested parish.");
        }

        if (HasOfficerOverlap(officer.Id, visit.ScheduledAt, visit.DurationMinutes, visit.Id))
        {
            throw new InvalidOperationException("Officer is already booked for an overlapping wellness visit.");
        }

        visit.OfficerId = officer.Id;
        visit.OfficerBadgeNumber = officer.BadgeNumber;
        visit.VisitStatus = VisitScheduled;
        visit.UpdatedAt = timeProvider.GetUtcNow();
        AddVisitTimeline(visit, $"Officer {officer.BadgeNumber} assigned", "Visit scheduled");
        AddVisitEvent(visit, "Officer assigned");
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Officer assigned", $"Officer {officer.BadgeNumber} was assigned to visit {visit.Id:N}.", cancellationToken);

        return ToVisitDto(visit);
        }
        finally { AssignmentGate.Release(); }
    }

    public async Task<WellnessVisitDto?> CancelVisitAsync(Guid visitId, CancelWellnessVisitRequest request, CancellationToken cancellationToken)
    {
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        if (visit.VisitStatus == VisitCompleted)
        {
            throw new InvalidOperationException("Completed visits cannot be cancelled.");
        }

        if (visit.VisitStatus == VisitCancelled)
        {
            return ToVisitDto(visit);
        }

        visit.VisitStatus = VisitCancelled;
        visit.PaymentStatus = visit.PaymentStatus is PaymentCaptured or PaymentPayoutPending or PaymentPaidOut ? PaymentRefunded : PaymentCancelled;
        visit.UpdatedAt = timeProvider.GetUtcNow();
        AddVisitTimeline(visit, string.IsNullOrWhiteSpace(request.Reason) ? "Visit cancelled" : $"Visit cancelled: {request.Reason}");
        AddVisitEvent(visit, "Visit cancelled");
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Visit cancelled", $"Wellness visit {visit.Id:N} was cancelled.", cancellationToken);

        return ToVisitDto(visit);
    }

    public async Task<WellnessVisitDto?> RescheduleVisitAsync(Guid visitId, RescheduleWellnessVisitRequest request, CancellationToken cancellationToken)
    {
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null) return null;
        if (visit.VisitStatus is VisitCompleted or VisitCancelled)
            throw new InvalidOperationException("Completed or cancelled visits cannot be rescheduled.");
        if (request.ScheduledAt <= timeProvider.GetUtcNow().AddMinutes(15))
            throw new InvalidOperationException("Choose a future time at least 15 minutes from now.");
        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "America/Jamaica" : request.TimeZone.Trim();
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone); }
        catch (TimeZoneNotFoundException) { throw new InvalidOperationException("The selected timezone is not supported."); }
        catch (InvalidTimeZoneException) { throw new InvalidOperationException("The selected timezone is not supported."); }

        if (visit.OfficerId is { } officerId && HasOfficerOverlap(officerId, request.ScheduledAt, visit.DurationMinutes, visit.Id))
            throw new InvalidOperationException("The assigned officer is unavailable at that time. Choose another slot.");

        var previous = visit.ScheduledAt;
        visit.ScheduledAt = request.ScheduledAt.ToUniversalTime();
        visit.ScheduledTimeZone = timeZone;
        visit.UpdatedAt = timeProvider.GetUtcNow();
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Schedule changed" : request.Reason.Trim();
        AddVisitTimeline(visit, $"Visit rescheduled from {previous:u} to {visit.ScheduledAt:u} ({timeZone})", $"Reason: {reason}");
        AddVisitEvent(visit, "Wellness visit rescheduled");
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Wellness visit rescheduled", $"Wellness visit {visit.Id:N} was moved to {visit.ScheduledAt:u}.", cancellationToken);
        return ToVisitDto(visit);
    }

    public async Task<WellnessReportPhotoUploadDto> PrepareReportPhotoUploadAsync(
        Guid visitId,
        PrepareWellnessReportPhotoUploadRequest request,
        bool adminOverride,
        CancellationToken cancellationToken)
    {
        var (_, officer) = await RequireReportPhotoAccessAsync(visitId, request.OfficerBadgeNumber, adminOverride, cancellationToken);
        await EnsureReportNotSubmittedAsync(visitId, cancellationToken);

        var safeFileName = ValidateWellnessReportPhoto(request.FileName, request.ContentType, request.SizeBytes);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var photoId = Guid.NewGuid();
        var objectKey = $"wellness/{visitId:N}/reports/{photoId:N}{extension}";
        var uploadUrl = adminOverride
            ? $"/api/wellness/visits/{visitId}/complete/photos/{photoId}/content"
            : $"/api/wellness/visits/{visitId}/report/photos/{photoId}/content";
        var now = timeProvider.GetUtcNow();

        var photo = new MilestoneWellnessReportPhoto
        {
            Id = photoId,
            VisitId = visitId,
            OfficerId = officer.Id,
            OriginalFileName = Path.GetFileName(request.FileName.Trim()),
            SafeFileName = safeFileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            ObjectKey = objectKey,
            UploadUrl = uploadUrl,
            Status = UploadStatusPending,
            StorageProviderName = storageProvider.ProviderName,
            ScanStatus = ScanStatusPending,
            UploadExpiresAt = now.Add(ReportPhotoUploadLifetime),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.MilestoneWellnessReportPhotos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(photo);
    }

    public async Task<WellnessReportPhotoUploadDto> UploadReportPhotoContentAsync(
        Guid visitId,
        Guid photoId,
        string officerBadgeNumber,
        string contentType,
        long sizeBytes,
        Stream content,
        bool adminOverride,
        CancellationToken cancellationToken)
    {
        var (_, officer) = await RequireReportPhotoAccessAsync(visitId, officerBadgeNumber, adminOverride, cancellationToken);
        await EnsureReportNotSubmittedAsync(visitId, cancellationToken);
        var photo = await db.MilestoneWellnessReportPhotos
            .SingleOrDefaultAsync(item => item.Id == photoId && item.VisitId == visitId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Wellness report photo is not available for this visit.");

        if (photo.OfficerId != officer.Id)
        {
            throw new UnauthorizedAccessException("Wellness report photo is not available to this officer.");
        }

        var now = await RequirePendingReportPhotoUploadAsync(photo, cancellationToken);
        ValidateWellnessReportPhotoUploadMetadata(photo, contentType, sizeBytes);

        var upload = await storageProvider.SaveObjectAsync(
            new StorageObjectWriteRequest(photo.ObjectKey, photo.ContentType, MaximumReportPhotoBytes),
            content,
            cancellationToken);
        ValidateWellnessReportPhotoUploadMetadata(photo, upload.ContentType, upload.SizeBytes);

        return await FinalizeReportPhotoUploadAsync(
            photo,
            now,
            upload.ProviderName,
            upload.ContentType,
            upload.SizeBytes,
            upload.Sha256Hash,
            upload.HeaderBytes,
            cancellationToken);
    }

    public async Task<WellnessVisitDto?> SubmitReportAsync(
        Guid visitId,
        SubmitWellnessReportRequest request,
        bool adminOverride,
        CancellationToken cancellationToken)
    {
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        if (visit.VisitStatus is VisitCancelled or VisitRejected)
        {
            throw new InvalidOperationException("Reports cannot be submitted for cancelled or rejected visits.");
        }

        if (visit.OfficerId is null)
        {
            throw new InvalidOperationException("A wellness visit must have an assigned officer before report submission.");
        }

        var officer = await db.MilestoneWellnessOfficers.SingleAsync(item => item.Id == visit.OfficerId.Value, cancellationToken);
        if (!adminOverride && !officer.BadgeNumber.Equals(NormalizeBadge(request.OfficerBadgeNumber), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only the assigned officer can submit this report.");
        }

        if (!adminOverride && visit.ScheduledAt > timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Report cannot be submitted before the scheduled visit time.");
        }

        var existing = await db.MilestoneWellnessReports.SingleOrDefaultAsync(report => report.VisitId == visitId, cancellationToken);
        if (existing is not null)
        {
            return ToVisitDto(visit);
        }

        var reportPhotos = await ResolveCleanWellnessReportPhotosAsync(visit, officer.Id, request.Photos, cancellationToken);

        if (visit.PaymentStatus == PaymentAuthorized)
        {
            var capture = await paymentGateway.CaptureAsync(
                new PaymentCaptureRequest(visit.PaymentAuthorizationReference, visit.Price, visit.Currency),
                cancellationToken);
            if (capture.Status != PaymentStatus.Captured)
            {
                throw new InvalidOperationException("Wellness payment capture failed; payout cannot be unlocked.");
            }

            visit.PaymentCaptureReference = capture.CaptureReference;
            visit.PaymentStatus = PaymentCaptured;
            AddVisitTimeline(visit, "Host payment captured after visit report");
        }

        var now = timeProvider.GetUtcNow();
        var reportId = Guid.NewGuid();
        db.MilestoneWellnessReports.Add(new MilestoneWellnessReport
        {
            Id = reportId,
            VisitId = visit.Id,
            OfficerId = officer.Id,
            SubmittedAt = now,
            Notes = request.Notes.Trim(),
            PhotosJson = MilestoneJson.Serialize(reportPhotos.Select(photo => photo.Id.ToString("N")).ToList()),
            LocationMetadataJson = string.IsNullOrWhiteSpace(request.LocationMetadata) ? "{}" : request.LocationMetadata,
            ReportStatus = ReportSubmitted,
            CreatedAt = now,
            UpdatedAt = now
        });

        foreach (var photo in reportPhotos)
        {
            photo.ReportId = reportId;
            photo.UpdatedAt = now;
        }

        var payout = new MilestoneWellnessPayout
        {
            Id = Guid.NewGuid(),
            VisitId = visit.Id,
            OfficerId = officer.Id,
            GrossAmount = visit.Price,
            PlatformFee = visit.PlatformFee,
            OfficerAmount = visit.OfficerPayoutAmount,
            Currency = visit.Currency,
            Status = "Pending",
            EligibleAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MilestoneWellnessPayouts.Add(payout);

        visit.VisitStatus = VisitCompleted;
        visit.ReportStatus = ReportSubmitted;
        visit.PaymentStatus = PaymentPayoutPending;
        visit.UpdatedAt = now;
        AddVisitTimeline(visit, "Photo report submitted", "Visit completed", "Officer payout pending");
        AddVisitEvent(visit, "Report submitted", "Payout pending");

        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Report submitted", $"Wellness visit {visit.Id:N} report was submitted.", cancellationToken);
        await QueueEventAsync("Payout pending", $"Officer payout is pending for visit {visit.Id:N}.", cancellationToken);

        return ToVisitDto(visit);
    }

    public async Task<WellnessPayoutDto?> MarkPayoutPaidAsync(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken)
    {
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        var payout = await db.MilestoneWellnessPayouts.SingleOrDefaultAsync(item => item.VisitId == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Payout is not eligible until the visit is completed, reported, and payment is captured.");

        if (payout.Status == "Paid")
        {
            return ToPayoutDto(payout);
        }

        if (visit.VisitStatus != VisitCompleted || visit.ReportStatus != ReportSubmitted || visit.PaymentStatus != PaymentPayoutPending)
        {
            throw new InvalidOperationException("Payout can only be paid after completion, report submission, and captured payment.");
        }

        var now = timeProvider.GetUtcNow();
        payout.Status = "Paid";
        payout.PaidAt = now;
        payout.ProviderReference = string.IsNullOrWhiteSpace(request.ProviderReference)
            ? $"local_payout_{payout.Id:N}"
            : request.ProviderReference.Trim();
        payout.LedgerNotes = request.Notes ?? string.Empty;
        payout.UpdatedAt = now;
        visit.PaymentStatus = PaymentPaidOut;
        visit.UpdatedAt = now;
        AddVisitTimeline(visit, "Officer payout paid");
        AddVisitEvent(visit, "Payout paid");
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync("Payout paid", $"Officer payout was paid for visit {visit.Id:N}.", cancellationToken);

        return ToPayoutDto(payout);
    }

    public async Task<IReadOnlyList<WellnessPayoutDto>> GetPayoutsAsync(string? status, CancellationToken cancellationToken)
    {
        var payouts = await db.MilestoneWellnessPayouts
            .AsNoTracking()
            .OrderByDescending(payout => payout.CreatedAt)
            .ToListAsync(cancellationToken);

        return payouts
            .Where(payout => string.IsNullOrWhiteSpace(status) || payout.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .Select(ToPayoutDto)
            .ToList();
    }

    public async Task<WellnessAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken)
    {
        var officers = await db.MilestoneWellnessOfficers.AsNoTracking().ToListAsync(cancellationToken);
        var visits = await db.MilestoneWellnessVisits.AsNoTracking().OrderByDescending(visit => visit.CreatedAt).ToListAsync(cancellationToken);
        var payouts = await db.MilestoneWellnessPayouts.AsNoTracking().OrderByDescending(payout => payout.CreatedAt).ToListAsync(cancellationToken);

        return new WellnessAdminDashboardDto(
            officers.Count(officer => officer.OnboardingStatus == OfficerStatusPending),
            officers.Count(officer => officer.VerificationStatus == OfficerStatusVerified),
            visits.Count(visit => visit.VisitStatus == VisitRequested),
            visits.Count(visit => visit.VisitStatus == VisitScheduled),
            visits.Count(visit => visit.VisitStatus == VisitCompleted),
            payouts.Count(payout => payout.Status == "Pending"),
            payouts.Where(payout => payout.Status == "Pending").Sum(payout => payout.OfficerAmount),
            officers
                .Where(officer => officer.OnboardingStatus == OfficerStatusPending)
                .OrderBy(officer => officer.CreatedAt)
                .Select(ToOfficerDto)
                .ToList(),
            visits.Take(12).Select(ToVisitDto).ToList(),
            payouts.Take(12).Select(ToPayoutDto).ToList());
    }

    private async Task<WellnessOfficerDto?> ReviewOfficerAsync(
        Guid officerId,
        string status,
        AdminOfficerReviewRequest request,
        CancellationToken cancellationToken)
    {
        var officer = await db.MilestoneWellnessOfficers.SingleOrDefaultAsync(item => item.Id == officerId, cancellationToken);
        if (officer is null)
        {
            return null;
        }

        if (status == OfficerStatusVerified && !NestyStayBusinessRules.IsOfficerEligible(officer.IsActiveOffDuty, officer.IsRetired))
        {
            throw new InvalidOperationException("Retired or inactive officers cannot be approved.");
        }

        var now = timeProvider.GetUtcNow();
        officer.VerificationStatus = status;
        officer.OnboardingStatus = status;
        officer.AvailabilityStatus = status == OfficerStatusVerified ? AvailabilityAvailable : status;
        officer.FreeBadgesJson = status == OfficerStatusVerified
            ? MilestoneJson.Serialize<IReadOnlyList<string>>(["Verified", "Trusted"])
            : officer.FreeBadgesJson;
        officer.AdminReviewMetadataJson = MilestoneJson.Serialize(new
        {
            reviewedAt = now,
            reviewedBy = request.ReviewedBy,
            reason = request.Reason,
            status
        });
        officer.UpdatedAt = now;
        AddOfficerEvent(officer, $"Officer {status.ToLowerInvariant()}");
        await db.SaveChangesAsync(cancellationToken);
        await QueueEventAsync($"Officer {status.ToLowerInvariant()}", $"Officer {officer.BadgeNumber} review status is {status}.", cancellationToken);

        return ToOfficerDto(officer);
    }

    private IReadOnlyList<string> ResolveEligibility(Guid hostUserId, Guid propertyId, DateTimeOffset scheduledAt)
    {
        var missing = new List<string>();
        var property = phaseOneStore.GetProperty(propertyId);
        if (property is null)
        {
            missing.Add("Property must exist.");
            return missing;
        }

        if (property.HostUserId != hostUserId)
        {
            missing.Add("Only the property host can request wellness visits.");
        }

        if (scheduledAt <= timeProvider.GetUtcNow())
        {
            missing.Add("Wellness visits must be scheduled in the future.");
        }

        var hostAccess = phaseTwoStore.GetFeatureAccess("Host", hostUserId);
        var propertyAccess = phaseTwoStore.GetFeatureAccess("Property", propertyId);
        var hasWellnessFeature =
            property.BadgeLevel == BadgeLevel.Wellness ||
            hostAccess.UnlockedFeatures.Contains("Wellness visits", StringComparer.OrdinalIgnoreCase) ||
            propertyAccess.UnlockedFeatures.Contains("Wellness visits", StringComparer.OrdinalIgnoreCase);

        if (!hasWellnessFeature)
        {
            missing.Add("Wellness visits require a Wellness badge or unlocked Wellness visits feature.");
        }

        if (property.BadgeLevel == BadgeLevel.Free)
        {
            missing.Add("Free listings cannot book wellness visits.");
        }

        return missing;
    }

    private void EnsureOfficerAssignable(MilestoneWellnessOfficer officer)
    {
        if (officer.VerificationStatus != OfficerStatusVerified ||
            officer.OnboardingStatus != OfficerStatusVerified ||
            officer.AvailabilityStatus != AvailabilityAvailable ||
            !officer.IsActiveOffDuty ||
            officer.IsRetired)
        {
            throw new InvalidOperationException("Only verified active off-duty officers can be assigned.");
        }
    }

    private async Task<(MilestoneWellnessVisit Visit, MilestoneWellnessOfficer Officer)> RequireReportPhotoAccessAsync(
        Guid visitId,
        string officerBadgeNumber,
        bool adminOverride,
        CancellationToken cancellationToken)
    {
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Wellness visit was not found.");

        if (visit.VisitStatus is VisitCancelled or VisitRejected)
        {
            throw new InvalidOperationException("Reports cannot be submitted for cancelled or rejected visits.");
        }

        if (visit.OfficerId is null)
        {
            throw new InvalidOperationException("A wellness visit must have an assigned officer before report submission.");
        }

        var officer = await db.MilestoneWellnessOfficers.SingleAsync(item => item.Id == visit.OfficerId.Value, cancellationToken);
        if (!adminOverride && !officer.BadgeNumber.Equals(NormalizeBadge(officerBadgeNumber), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only the assigned officer can upload report photos.");
        }

        if (!adminOverride && visit.ScheduledAt > timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Report photos cannot be uploaded before the scheduled visit time.");
        }

        return (visit, officer);
    }

    private async Task EnsureReportNotSubmittedAsync(Guid visitId, CancellationToken cancellationToken)
    {
        if (await db.MilestoneWellnessReports.AnyAsync(report => report.VisitId == visitId, cancellationToken))
        {
            throw new InvalidOperationException("Wellness report has already been submitted.");
        }
    }

    private async Task<DateTimeOffset> RequirePendingReportPhotoUploadAsync(MilestoneWellnessReportPhoto photo, CancellationToken cancellationToken)
    {
        if (photo.Status != UploadStatusPending)
        {
            throw new InvalidOperationException("Wellness report photo upload is not pending.");
        }

        var now = timeProvider.GetUtcNow();
        if (photo.UploadExpiresAt <= now)
        {
            photo.Status = UploadStatusExpired;
            photo.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Wellness report photo upload URL has expired.");
        }

        return now;
    }

    private async Task<WellnessReportPhotoUploadDto> FinalizeReportPhotoUploadAsync(
        MilestoneWellnessReportPhoto photo,
        DateTimeOffset verifiedAt,
        string providerName,
        string contentType,
        long sizeBytes,
        string sha256Hash,
        byte[] headerBytes,
        CancellationToken cancellationToken)
    {
        var scan = await fileSafetyScanner.ScanAsync(
            new FileSafetyScanRequest(photo.ObjectKey, photo.SafeFileName, contentType, sizeBytes, sha256Hash, headerBytes),
            cancellationToken);

        photo.StorageProviderName = providerName;
        photo.VerifiedContentType = contentType;
        photo.UploadedSizeBytes = sizeBytes;
        photo.Sha256Hash = sha256Hash;
        photo.ScanStatus = scan.Status;
        photo.ScanProviderName = fileSafetyScanner.ProviderName;
        photo.ScanCheckedAt = verifiedAt;
        photo.UpdatedAt = verifiedAt;

        if (!scan.Status.Equals(ScanStatusClean, StringComparison.OrdinalIgnoreCase))
        {
            photo.Status = UploadStatusQuarantined;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(scan.Reason ?? "Wellness report photo failed safety scanning.");
        }

        photo.Status = UploadStatusUploaded;
        photo.UploadedAt = verifiedAt;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(photo);
    }

    private async Task<IReadOnlyList<MilestoneWellnessReportPhoto>> ResolveCleanWellnessReportPhotosAsync(
        MilestoneWellnessVisit visit,
        Guid officerId,
        IReadOnlyList<string>? photoReferences,
        CancellationToken cancellationToken)
    {
        if (photoReferences is null || photoReferences.Count == 0)
        {
            throw new InvalidOperationException("Wellness reports require at least one uploaded clean photo.");
        }

        var photoIds = new List<Guid>();
        foreach (var reference in photoReferences)
        {
            if (!Guid.TryParse(reference, out var photoId))
            {
                throw new InvalidOperationException("Wellness report photos must reference prepared upload IDs.");
            }

            photoIds.Add(photoId);
        }

        photoIds = photoIds.Distinct().ToList();
        if (photoIds.Count == 0)
        {
            throw new InvalidOperationException("Wellness reports require at least one uploaded clean photo.");
        }

        var photos = await db.MilestoneWellnessReportPhotos
            .Where(photo => photo.VisitId == visit.Id && photo.OfficerId == officerId && photoIds.Contains(photo.Id) && !photo.IsDeleted)
            .ToListAsync(cancellationToken);

        if (photos.Count != photoIds.Count)
        {
            throw new UnauthorizedAccessException("Wellness report photo is not available for this visit.");
        }

        foreach (var photo in photos)
        {
            if (photo.ReportId is not null)
            {
                throw new InvalidOperationException("Wellness report photo is already attached to a report.");
            }

            if (photo.Status != UploadStatusUploaded || !photo.ScanStatus.Equals(ScanStatusClean, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Wellness report photos must be uploaded and pass safety scanning before submission.");
            }
        }

        return photoIds
            .Select(photoId => photos.Single(photo => photo.Id == photoId))
            .ToList();
    }

    private bool HasOfficerOverlap(Guid officerId, DateTimeOffset scheduledAt, int durationMinutes, Guid? excludingVisitId = null)
    {
        var start = scheduledAt;
        var end = scheduledAt.AddMinutes(durationMinutes);
        return db.MilestoneWellnessVisits
            .AsNoTracking()
            .ToList()
            .Any(visit =>
                visit.Id != excludingVisitId &&
                visit.OfficerId == officerId &&
                visit.VisitStatus is VisitScheduled or "Accepted" or "InProgress" or VisitCompleted &&
                visit.ScheduledAt < end &&
                start < visit.ScheduledAt.AddMinutes(visit.DurationMinutes));
    }

    private decimal ResolveOfficerCommissionPercent()
    {
        var item = phaseTwoStore.GetPricebook().SingleOrDefault(entry => entry.Key == "officer-commission");
        return item?.Amount ?? 8m;
    }

    private static VisitPricing ResolveVisitPricing(string visitType) =>
        visitType switch
        {
            "StandardWellnessCheck" => new VisitPricing(50m, 60),
            "InPersonGuestIdCheck" => new VisitPricing(50m, 75),
            "DriveByPatrol" => new VisitPricing(25m, 45),
            _ => throw new InvalidOperationException("Unsupported wellness visit type.")
        };

    private static string NormalizeVisitType(string visitType)
    {
        var normalized = visitType.Trim().Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("-", string.Empty, StringComparison.Ordinal);
        return normalized.ToLowerInvariant() switch
        {
            "standardwellnesscheck" or "standard" => "StandardWellnessCheck",
            "inpersonguestidcheck" or "inpersonguestcheck" or "guestidcheck" => "InPersonGuestIdCheck",
            "drivebypatrol" or "driveby" => "DriveByPatrol",
            _ => throw new InvalidOperationException("Unsupported wellness visit type.")
        };
    }

    private static string ValidateWellnessReportPhoto(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumReportPhotoBytes)
        {
            throw new InvalidOperationException("Wellness report photos must be 10 MB or smaller.");
        }

        var normalizedContentType = NormalizeUploadContentType(contentType);
        if (!AllowedReportPhotoExtensions.TryGetValue(normalizedContentType, out var allowedExtensions))
        {
            throw new InvalidOperationException("Wellness report photo type is not allowed.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Wellness report photo filename is required.");
        }

        var originalFileName = Path.GetFileName(fileName.Trim());
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Wellness report photo extension does not match the content type.");
        }

        var stem = Path.GetFileNameWithoutExtension(originalFileName).Trim().ToLowerInvariant();
        var safeStem = new string(stem.Select(character => IsSafeUploadFileNameCharacter(character) ? character : '-').ToArray());
        safeStem = string.Join("-", safeStem.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeStem))
        {
            safeStem = "wellness-report-photo";
        }

        if (safeStem.Length > 80)
        {
            safeStem = safeStem[..80];
        }

        return $"{safeStem}{extension}";
    }

    private static void ValidateWellnessReportPhotoUploadMetadata(MilestoneWellnessReportPhoto photo, string? contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumReportPhotoBytes)
        {
            throw new InvalidOperationException("Wellness report photos must be 10 MB or smaller.");
        }

        if (!NormalizeUploadContentType(contentType).Equals(photo.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded wellness report photo content type does not match the prepared upload.");
        }

        if (sizeBytes != photo.SizeBytes)
        {
            throw new InvalidOperationException("Uploaded wellness report photo size does not match the prepared upload.");
        }
    }

    private static string NormalizeUploadContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new InvalidOperationException("Uploaded content type is required.");
        }

        return contentType.Trim().ToLowerInvariant();
    }

    private static bool IsSafeUploadFileNameCharacter(char character) =>
        (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-';

    private static string NormalizeBadge(string badgeNumber)
    {
        if (string.IsNullOrWhiteSpace(badgeNumber))
        {
            throw new InvalidOperationException("Officer badge number is required.");
        }

        return badgeNumber.Trim().ToUpperInvariant();
    }

    private static bool ParishMatches(string officerParish, string requestedParish) =>
        officerParish.Equals(requestedParish, StringComparison.OrdinalIgnoreCase) ||
        officerParish.Equals("All Jamaica", StringComparison.OrdinalIgnoreCase);

    private static string ToApiPaymentStatus(PaymentStatus status) =>
        status.ToString();

    private static WellnessOfficerDto ToOfficerDto(MilestoneWellnessOfficer officer) =>
        new(
            officer.Id,
            officer.UserId,
            officer.BadgeNumber,
            officer.Parish,
            officer.CoverageArea,
            officer.IsActiveOffDuty,
            officer.IsRetired,
            officer.VerificationStatus,
            officer.OnboardingStatus,
            officer.AvailabilityStatus,
            MilestoneJson.DeserializeList<string>(officer.FreeBadgesJson),
            officer.CreatedAt,
            officer.UpdatedAt,
            officer.AdminReviewMetadataJson == "{}" ? null : officer.AdminReviewMetadataJson,
            officer.Latitude,
            officer.Longitude,
            officer.ServiceRadiusKm);

    private static WellnessSubscriptionDto ToSubscriptionDto(MilestoneWellnessSubscription subscription, DateTimeOffset now) =>
        new(subscription.Id, subscription.HostUserId, subscription.PlanKey, subscription.MonthlyAmount, subscription.Currency,
            subscription.Status == "Active" && subscription.CurrentPeriodEnd <= now ? "Expired" : subscription.Status,
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.IncludedVisits, subscription.UsedVisits,
            Math.Max(0, subscription.IncludedVisits - subscription.UsedVisits), subscription.PaymentProvider, subscription.PaymentReference);

    private static WellnessVisitDto ToVisitDto(MilestoneWellnessVisit visit) =>
        new(
            visit.Id,
            visit.HostUserId,
            visit.PropertyId,
            visit.OfficerId,
            string.IsNullOrWhiteSpace(visit.OfficerBadgeNumber) ? null : visit.OfficerBadgeNumber,
            visit.Parish,
            visit.Area,
            visit.VisitType,
            visit.ScheduledAt,
            visit.DurationMinutes,
            visit.Price,
            visit.PlatformFee,
            visit.OfficerPayoutAmount,
            visit.Currency,
            visit.PaymentStatus,
            visit.VisitStatus,
            visit.ReportStatus,
            string.IsNullOrWhiteSpace(visit.PaymentAuthorizationReference) ? null : visit.PaymentAuthorizationReference,
            string.IsNullOrWhiteSpace(visit.PaymentCaptureReference) ? null : visit.PaymentCaptureReference,
            MilestoneJson.DeserializeList<string>(visit.TimelineJson),
            visit.CreatedAt,
            visit.UpdatedAt,
            string.IsNullOrWhiteSpace(visit.ScheduledTimeZone) ? "America/Jamaica" : visit.ScheduledTimeZone);

    private static WellnessReportPhotoUploadDto ToDto(MilestoneWellnessReportPhoto photo) =>
        new(
            photo.Id,
            photo.VisitId,
            photo.OfficerId,
            photo.SafeFileName,
            photo.ContentType,
            photo.SizeBytes,
            photo.ObjectKey,
            photo.UploadUrl,
            photo.Status,
            photo.ScanStatus,
            photo.UploadExpiresAt,
            photo.Sha256Hash);

    private static WellnessPayoutDto ToPayoutDto(MilestoneWellnessPayout payout) =>
        new(
            payout.Id,
            payout.VisitId,
            payout.OfficerId,
            payout.GrossAmount,
            payout.PlatformFee,
            payout.OfficerAmount,
            payout.Currency,
            payout.Status,
            payout.EligibleAt,
            payout.PaidAt,
            string.IsNullOrWhiteSpace(payout.ProviderReference) ? null : payout.ProviderReference);

    private static void AddVisitTimeline(MilestoneWellnessVisit visit, params string[] entries)
    {
        var timeline = MilestoneJson.DeserializeList<string>(visit.TimelineJson);
        timeline.AddRange(entries);
        visit.TimelineJson = MilestoneJson.Serialize(timeline);
    }

    private static void AddVisitEvent(MilestoneWellnessVisit visit, params string[] events)
    {
        var current = MilestoneJson.DeserializeList<string>(visit.NotificationEventsJson);
        current.AddRange(events);
        visit.NotificationEventsJson = MilestoneJson.Serialize(current);
    }

    private static void AddOfficerEvent(MilestoneWellnessOfficer officer, string entry)
    {
        var current = MilestoneJson.DeserializeList<string>(officer.NotificationEventsJson);
        current.Add(entry);
        officer.NotificationEventsJson = MilestoneJson.Serialize(current);
    }

    private Task QueueEventAsync(string subject, string body, CancellationToken cancellationToken) =>
        notificationGateway.QueueAsync(new NotificationMessage("wellness-ops@nestystay.local", subject, body), cancellationToken);

    private sealed record VisitPricing(decimal Price, int DurationMinutes);
}
