using NestyStay.Application.Services;
using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Application.PhaseTwo;

public interface IPhaseTwoStore
{
    IReadOnlyList<PhaseTwoPricebookItemDto> GetPricebook();
    PhaseTwoPricebookItemDto? GetPricebookItem(string key);
    PhaseTwoPricebookItemDto UpdatePricebookItem(string key, UpdatePricebookItemRequest request);
    IReadOnlyList<BadgeDefinitionDto> GetBadgeDefinitions();
    BadgeEligibilityDto GetBadgeEligibility(PurchaseBadgeRequest request);
    BadgeFeatureAccessDto GetFeatureAccess(string subjectType, Guid subjectId);
    IReadOnlyList<BadgeAssignmentDto> GetBadgeAssignments(string? subjectType = null, Guid? subjectId = null);
    IReadOnlyList<BadgeRenewalDto> GetRenewals(Guid? assignmentId = null);
    BadgeAssignmentDto PurchaseBadge(PurchaseBadgeRequest request);
    BadgeAssignmentDto PayRenewal(Guid assignmentId);
    BadgeAssignmentDto ExpireBadge(Guid assignmentId);
    BadgeAssignmentDto SuspendBadge(Guid assignmentId);
    IReadOnlyList<CampaignDto> GetCampaigns();
    CampaignDto CreateCampaign(CreateCampaignRequest request);
    CampaignEnrollmentDto EnrollCampaign(string campaignKey, EnrollCampaignRequest request);
    FoundingBenefitDto UpsertFoundingBenefit(FoundingBenefitRequest request);
    FoundingBenefitDto? GetFoundingBenefit(Guid propertyId);
    FoundingTransferEvaluationDto EvaluateFoundingTransfer(FoundingTransferEvaluationRequest request);
    CommissionQuoteDto QuoteCommission(CommissionQuoteRequest request);
}

public sealed class PhaseTwoStore : IPhaseTwoStore
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly List<PricebookEntryState> _pricebook;
    private readonly List<BadgeDefinitionState> _badgeDefinitions;
    private readonly List<BadgeAssignmentState> _assignments = [];
    private readonly List<BadgeRenewalState> _renewals = [];
    private readonly List<CampaignState> _campaigns = [];
    private readonly List<CampaignEnrollmentState> _campaignEnrollments = [];
    private readonly List<FoundingBenefitState> _foundingBenefits = [];

    public PhaseTwoStore(IPricebookService pricebookService)
        : this(pricebookService, TimeProvider.System)
    {
    }

    public PhaseTwoStore(IPricebookService pricebookService, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _pricebook = pricebookService.GetDefaultPricebook()
            .Select(item => new PricebookEntryState(
                item.Key,
                item.Label,
                item.Amount,
                item.Currency,
                item.Cadence,
                item.AppliesTo,
                item.IsConfigurable,
                true,
                _timeProvider.GetUtcNow(),
                null))
            .ToList();

        _badgeDefinitions =
        [
            new(
                Guid.Parse("10000000-0000-4000-8000-000000000000"),
                "host-free",
                BadgeLevel.Free,
                "Hosts",
                "host-listing",
                ["Listings", "Calendar", "Messaging", "QR", "Stripe", "InsuraGuest", "97% payout"]),
            new(
                Guid.Parse("10000000-0000-4000-8000-000000000001"),
                "host-verified",
                BadgeLevel.Verified,
                "Hosts",
                "verified-host-standard-annual",
                ["Verified badge", "Custodian directory", "Local business directory", "Guest verification upsell"]),
            new(
                Guid.Parse("10000000-0000-4000-8000-000000000002"),
                "host-trusted",
                BadgeLevel.Trusted,
                "Hosts",
                "trusted-host-standard-annual",
                ["Trades directory", "Search boost", "Referral program"]),
            new(
                Guid.Parse("10000000-0000-4000-8000-000000000003"),
                "host-wellness",
                BadgeLevel.Wellness,
                "Hosts",
                "verified-host-standard-annual",
                ["Police directory", "Wellness visits", "Wellness badge", "Security verified filter"])
        ];

        _campaigns.Add(new CampaignState(
            Guid.Parse("20000000-0000-4000-8000-000000000001"),
            "trusted-host-pdf-campaign",
            "Trusted host PDF campaign",
            "BadgePriceOverride",
            49m,
            "Hosts",
            _timeProvider.GetUtcNow().AddDays(-1),
            _timeProvider.GetUtcNow().AddYears(1),
            true));
    }

    public IReadOnlyList<PhaseTwoPricebookItemDto> GetPricebook()
    {
        lock (_gate)
        {
            return _pricebook.Select(ToDto).ToList();
        }
    }

    public PhaseTwoPricebookItemDto? GetPricebookItem(string key)
    {
        lock (_gate)
        {
            return _pricebook.SingleOrDefault(item => KeyEquals(item.Key, key)) is { } item ? ToDto(item) : null;
        }
    }

    public PhaseTwoPricebookItemDto UpdatePricebookItem(string key, UpdatePricebookItemRequest request)
    {
        if (request.Amount < 0)
        {
            throw new InvalidOperationException("Pricebook amount cannot be negative.");
        }

        lock (_gate)
        {
            var item = _pricebook.SingleOrDefault(entry => KeyEquals(entry.Key, key))
                ?? throw new InvalidOperationException("Pricebook item not found.");

            if (!item.IsConfigurable)
            {
                throw new InvalidOperationException("This pricebook item is not configurable.");
            }

            item.Amount = decimal.Round(request.Amount, 2);
            item.Currency = string.IsNullOrWhiteSpace(request.Currency) ? item.Currency : request.Currency.Trim().ToUpperInvariant();
            item.Cadence = string.IsNullOrWhiteSpace(request.Cadence) ? item.Cadence : request.Cadence.Trim();
            item.ActiveFrom = request.ActiveFrom ?? item.ActiveFrom;
            item.ActiveTo = request.ActiveTo;
            item.IsActive = request.IsActive ?? item.IsActive;

            return ToDto(item);
        }
    }

    public IReadOnlyList<BadgeDefinitionDto> GetBadgeDefinitions()
    {
        lock (_gate)
        {
            return _badgeDefinitions.Select(ToDto).ToList();
        }
    }

    public BadgeEligibilityDto GetBadgeEligibility(PurchaseBadgeRequest request)
    {
        var subjectType = NormalizeSubjectType(request.SubjectType);
        lock (_gate)
        {
            _ = FindBadgeDefinition(request.Level, subjectType);
            return EvaluateEligibilityNoLock(request with { SubjectType = subjectType });
        }
    }

    public BadgeFeatureAccessDto GetFeatureAccess(string subjectType, Guid subjectId)
    {
        var normalizedSubjectType = NormalizeSubjectType(subjectType);
        lock (_gate)
        {
            var activeAssignments = _assignments
                .Where(assignment =>
                    assignment.SubjectType.Equals(normalizedSubjectType, StringComparison.OrdinalIgnoreCase) &&
                    assignment.SubjectId == subjectId &&
                    assignment.Status == BadgeAssignmentStatus.Active &&
                    assignment.PaymentStatus == PaymentStatus.Captured &&
                    assignment.ExpiresAt > _timeProvider.GetUtcNow())
                .ToList();

            var activeLevel = activeAssignments.Count == 0
                ? BadgeLevel.Free
                : activeAssignments.Max(assignment => assignment.Level);
            var unlocked = FeatureSetFor(activeLevel);
            var allFeatures = FeatureSetFor(BadgeLevel.Wellness);

            return new BadgeFeatureAccessDto(
                normalizedSubjectType,
                subjectId,
                activeLevel,
                unlocked,
                allFeatures.Except(unlocked, StringComparer.OrdinalIgnoreCase).ToList());
        }
    }

    public IReadOnlyList<BadgeAssignmentDto> GetBadgeAssignments(string? subjectType = null, Guid? subjectId = null)
    {
        lock (_gate)
        {
            return _assignments
                .Where(assignment =>
                    (subjectType is null || assignment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase)) &&
                    (subjectId is null || assignment.SubjectId == subjectId))
                .Select(ToDto)
                .ToList();
        }
    }

    public IReadOnlyList<BadgeRenewalDto> GetRenewals(Guid? assignmentId = null)
    {
        lock (_gate)
        {
            return _renewals
                .Where(renewal => assignmentId is null || renewal.BadgeAssignmentId == assignmentId)
                .Select(ToDto)
                .ToList();
        }
    }

    public BadgeAssignmentDto PurchaseBadge(PurchaseBadgeRequest request)
    {
        var subjectType = NormalizeSubjectType(request.SubjectType);
        lock (_gate)
        {
            var definition = FindBadgeDefinition(request.Level, subjectType);
            var eligibility = EvaluateEligibilityNoLock(request with { SubjectType = subjectType });
            if (!eligibility.Eligible)
            {
                throw new InvalidOperationException($"Badge eligibility failed: {string.Join(" ", eligibility.MissingRequirements)}");
            }

            var price = ResolveBadgePrice(definition, subjectType, request.SubjectId, request.CampaignKey);
            var now = _timeProvider.GetUtcNow();

            var existing = _assignments.SingleOrDefault(assignment =>
                assignment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase) &&
                assignment.SubjectId == request.SubjectId &&
                assignment.Level == request.Level &&
                assignment.Status == BadgeAssignmentStatus.Active &&
                assignment.PaymentStatus == PaymentStatus.Captured &&
                assignment.ExpiresAt > now);

            if (existing is not null)
            {
                return ToDto(existing);
            }

            var isFree = request.Level == BadgeLevel.Free || price.Amount == 0m;
            var paymentStatus = isFree || request.PaymentSucceeded ? PaymentStatus.Captured : PaymentStatus.Failed;
            var assignmentStatus = paymentStatus == PaymentStatus.Captured ? BadgeAssignmentStatus.Active : BadgeAssignmentStatus.Suspended;
            var expiresAt = isFree ? DateTimeOffset.MaxValue : now.AddYears(1);

            var assignment = new BadgeAssignmentState(
                Guid.NewGuid(),
                definition.Id,
                definition.Key,
                definition.Level,
                subjectType,
                request.SubjectId,
                assignmentStatus,
                now,
                expiresAt,
                expiresAt,
                price.Amount,
                price.Currency,
                paymentStatus,
                paymentStatus == PaymentStatus.Captured
                    ? $"badge_{definition.Level.ToString().ToLowerInvariant()}_{request.SubjectId:N}"
                    : $"badge_failed_{definition.Level.ToString().ToLowerInvariant()}_{request.SubjectId:N}",
                definition.Unlocks);

            _assignments.Add(assignment);
            if (assignment.Status == BadgeAssignmentStatus.Active)
            {
                QueueRenewalNoLock(assignment, price.Amount, price.Currency);
            }

            return ToDto(assignment);
        }
    }

    public BadgeAssignmentDto PayRenewal(Guid assignmentId)
    {
        lock (_gate)
        {
            var assignment = _assignments.SingleOrDefault(item => item.Id == assignmentId)
                ?? throw new InvalidOperationException("Badge assignment not found.");
            if (assignment.Status != BadgeAssignmentStatus.Active)
            {
                throw new InvalidOperationException("Only active badge assignments can be renewed.");
            }

            var definition = _badgeDefinitions.Single(item => item.Id == assignment.BadgeDefinitionId);
            var price = ResolveBadgePrice(definition, assignment.SubjectType, assignment.SubjectId, null);
            var renewal = _renewals
                .Where(item => item.BadgeAssignmentId == assignmentId && item.PaymentStatus == PaymentStatus.Pending)
                .OrderBy(item => item.ReminderDueAt)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("No pending renewal exists for this badge assignment.");

            var now = _timeProvider.GetUtcNow();
            renewal.PaymentAttemptedAt = now;
            renewal.PaymentStatus = PaymentStatus.Captured;
            renewal.AmountDue = price.Amount;
            renewal.Currency = price.Currency;

            var nextExpiry = (assignment.ExpiresAt > now ? assignment.ExpiresAt : now).AddYears(1);
            assignment.PaidThrough = nextExpiry;
            assignment.ExpiresAt = nextExpiry;
            assignment.AmountCharged = price.Amount;
            assignment.Currency = price.Currency;
            assignment.PaymentStatus = PaymentStatus.Captured;
            assignment.PaymentReference = $"renewal_{assignment.Id:N}_{now:yyyyMMddHHmmss}";

            QueueRenewalNoLock(assignment, price.Amount, price.Currency);

            return ToDto(assignment);
        }
    }

    public BadgeAssignmentDto ExpireBadge(Guid assignmentId)
    {
        lock (_gate)
        {
            var assignment = _assignments.SingleOrDefault(item => item.Id == assignmentId)
                ?? throw new InvalidOperationException("Badge assignment not found.");
            assignment.Status = BadgeAssignmentStatus.Expired;
            assignment.ExpiresAt = _timeProvider.GetUtcNow().AddSeconds(-1);
            return ToDto(assignment);
        }
    }

    public BadgeAssignmentDto SuspendBadge(Guid assignmentId)
    {
        lock (_gate)
        {
            var assignment = _assignments.SingleOrDefault(item => item.Id == assignmentId)
                ?? throw new InvalidOperationException("Badge assignment not found.");
            assignment.Status = BadgeAssignmentStatus.Suspended;
            return ToDto(assignment);
        }
    }

    public IReadOnlyList<CampaignDto> GetCampaigns()
    {
        lock (_gate)
        {
            return _campaigns.Select(ToDto).ToList();
        }
    }

    public CampaignDto CreateCampaign(CreateCampaignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.CampaignType))
        {
            throw new InvalidOperationException("Campaign key, name, and type are required.");
        }

        if (request.OverrideAmount < 0)
        {
            throw new InvalidOperationException("Campaign override amount cannot be negative.");
        }

        if (request.CampaignType.Equals("BadgePriceOverride", StringComparison.OrdinalIgnoreCase) &&
            request.OverrideAmount is < 1m)
        {
            throw new InvalidOperationException("Badge campaign override amount must be at least 1.00.");
        }

        lock (_gate)
        {
            var existing = _campaigns.SingleOrDefault(item => KeyEquals(item.Key, request.Key));
            if (existing is not null)
            {
                existing.Name = request.Name.Trim();
                existing.CampaignType = request.CampaignType.Trim();
                existing.OverrideAmount = request.OverrideAmount;
                existing.AppliesTo = request.AppliesTo;
                existing.OpensAt = request.OpensAt;
                existing.ClosesAt = request.ClosesAt;
                existing.IsActive = request.IsActive;
                return ToDto(existing);
            }

            var campaign = new CampaignState(
                Guid.NewGuid(),
                request.Key.Trim(),
                request.Name.Trim(),
                request.CampaignType.Trim(),
                request.OverrideAmount,
                request.AppliesTo,
                request.OpensAt,
                request.ClosesAt,
                request.IsActive);
            _campaigns.Add(campaign);

            return ToDto(campaign);
        }
    }

    public CampaignEnrollmentDto EnrollCampaign(string campaignKey, EnrollCampaignRequest request)
    {
        var subjectType = NormalizeSubjectType(request.SubjectType);
        lock (_gate)
        {
            var campaign = _campaigns.SingleOrDefault(item => KeyEquals(item.Key, campaignKey))
                ?? throw new InvalidOperationException("Campaign not found.");

            if (!CampaignIsActive(campaign, _timeProvider.GetUtcNow()))
            {
                throw new InvalidOperationException("Campaign is not active.");
            }

            var existing = _campaignEnrollments.SingleOrDefault(enrollment =>
                KeyEquals(enrollment.CampaignKey, campaign.Key) &&
                enrollment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase) &&
                enrollment.SubjectId == request.SubjectId);

            if (existing is not null)
            {
                return ToDto(existing);
            }

            var enrollment = new CampaignEnrollmentState(Guid.NewGuid(), campaign.Key, subjectType, request.SubjectId, _timeProvider.GetUtcNow());
            _campaignEnrollments.Add(enrollment);
            return ToDto(enrollment);
        }
    }

    public FoundingBenefitDto UpsertFoundingBenefit(FoundingBenefitRequest request)
    {
        if (!request.IsEligible && request.Tier != FoundingTier.Standard)
        {
            throw new InvalidOperationException("Subject is not eligible for founding benefits.");
        }

        lock (_gate)
        {
            var existing = _foundingBenefits.SingleOrDefault(item => item.PropertyId == request.PropertyId);
            var guestFlatFee = NestyStayBusinessRules.ResolveFoundingGuestFlatFee(request.Tier);
            var isFoundingTier = request.Tier != FoundingTier.Standard;

            if (existing is null)
            {
                existing = new FoundingBenefitState(
                    request.PropertyId,
                    request.Tier,
                    guestFlatFee,
                    ResolveHostCommissionPercent(),
                    isFoundingTier,
                    isFoundingTier,
                    false);
                _foundingBenefits.Add(existing);
            }
            else
            {
                if (existing.Tier != FoundingTier.Standard &&
                    request.Tier != FoundingTier.Standard &&
                    existing.Tier != request.Tier &&
                    !existing.IsForfeited)
                {
                    throw new InvalidOperationException("Founding benefit has already been claimed for this property.");
                }

                existing.Tier = request.Tier;
                existing.GuestFlatFee = guestFlatFee;
                existing.HostCommissionPercent = ResolveHostCommissionPercent();
                existing.IsLifetimeGuestFee = isFoundingTier;
                existing.IsTransferableWithProperty = isFoundingTier;
                existing.IsForfeited = false;
            }

            return ToDto(existing);
        }
    }

    public FoundingBenefitDto? GetFoundingBenefit(Guid propertyId)
    {
        lock (_gate)
        {
            return _foundingBenefits.SingleOrDefault(item => item.PropertyId == propertyId) is { } benefit
                ? ToDto(benefit)
                : null;
        }
    }

    public FoundingTransferEvaluationDto EvaluateFoundingTransfer(FoundingTransferEvaluationRequest request)
    {
        var missing = new List<string>();
        if (!request.PreviousOwnerVerified)
        {
            missing.Add("Previous owner must be verified.");
        }

        if (!request.PreviousOwnerTrusted)
        {
            missing.Add("Previous owner must be trusted.");
        }

        if (!request.HasPropertyId)
        {
            missing.Add("Property ID is required.");
        }

        if (!request.HasCurrentTaxReceipt)
        {
            missing.Add("Current tax receipt is required.");
        }

        return new FoundingTransferEvaluationDto(
            NestyStayBusinessRules.CanTransferFoundingBenefit(
                request.PreviousOwnerVerified,
                request.PreviousOwnerTrusted,
                request.HasPropertyId,
                request.HasCurrentTaxReceipt),
            missing);
    }

    public CommissionQuoteDto QuoteCommission(CommissionQuoteRequest request)
    {
        if (request.BookingValue < 0)
        {
            throw new InvalidOperationException("Booking value cannot be negative.");
        }

        if (request.Nights <= 0)
        {
            throw new InvalidOperationException("Nights must be greater than zero.");
        }

        lock (_gate)
        {
            var hostCommissionPercent = ResolveHostCommissionPercent();
            var hostCommissionAmount = decimal.Round(request.BookingValue * hostCommissionPercent / 100m, 2);
            var foundingGuestFee = NestyStayBusinessRules.ResolveFoundingGuestFlatFee(request.Tier);
            var isFounding = request.Tier != FoundingTier.Standard;
            var guestFeeAmount = isFounding
                ? foundingGuestFee
                : decimal.Round(
                    request.BookingValue * NestyStayBusinessRules.ResolveStandardGuestFeePercent(request.BookingValue, request.Nights) / 100m,
                    2);

            return new CommissionQuoteDto(
                request.BookingValue,
                request.Nights,
                hostCommissionPercent,
                hostCommissionAmount,
                guestFeeAmount,
                isFounding ? $"{request.Tier} lifetime founding guest flat fee" : "Standard guest platform fee",
                decimal.Round(hostCommissionAmount + guestFeeAmount, 2));
        }
    }

    private void QueueRenewalNoLock(BadgeAssignmentState assignment, decimal amountDue, string currency)
    {
        if (amountDue <= 0 || assignment.ExpiresAt == DateTimeOffset.MaxValue)
        {
            return;
        }

        var reminderDueAt = assignment.ExpiresAt.AddDays(-30);
        if (_renewals.Any(renewal =>
                renewal.BadgeAssignmentId == assignment.Id &&
                renewal.ReminderDueAt == reminderDueAt &&
                renewal.PaymentStatus == PaymentStatus.Pending))
        {
            return;
        }

        _renewals.Add(new BadgeRenewalState(
            Guid.NewGuid(),
            assignment.Id,
            reminderDueAt,
            null,
            PaymentStatus.Pending,
            amountDue,
            currency));
    }

    private BadgeEligibilityDto EvaluateEligibilityNoLock(PurchaseBadgeRequest request)
    {
        var missing = new List<string>();
        var hasActiveVerified = HasActiveBadgeNoLock(request.SubjectType, request.SubjectId, BadgeLevel.Verified);

        switch (request.Level)
        {
            case BadgeLevel.Free:
                break;
            case BadgeLevel.Verified:
                if (!request.HostVerificationPassed)
                {
                    missing.Add("Host eKYC must pass before Verified badge activation.");
                }

                break;
            case BadgeLevel.Trusted:
                if (!hasActiveVerified)
                {
                    missing.Add("Trusted badge requires an active Verified badge.");
                }

                if (request.CompletedApprovedBookings < 3)
                {
                    missing.Add("Trusted badge requires at least 3 approved bookings.");
                }

                break;
            case BadgeLevel.Wellness:
                if (!hasActiveVerified)
                {
                    missing.Add("Wellness badge requires an active Verified badge.");
                }

                if (!request.HasPropertyAddress)
                {
                    missing.Add("Wellness badge requires a property address.");
                }

                if (!request.HasWellnessSubscription)
                {
                    missing.Add("Wellness badge requires an active wellness subscription.");
                }

                break;
            default:
                missing.Add("Unsupported badge level.");
                break;
        }

        return new BadgeEligibilityDto(request.Level, missing.Count == 0, missing);
    }

    private bool HasActiveBadgeNoLock(string subjectType, Guid subjectId, BadgeLevel level) =>
        _assignments.Any(assignment =>
            assignment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase) &&
            assignment.SubjectId == subjectId &&
            assignment.Level == level &&
            assignment.Status == BadgeAssignmentStatus.Active &&
            assignment.PaymentStatus == PaymentStatus.Captured &&
            assignment.ExpiresAt > _timeProvider.GetUtcNow());

    private static IReadOnlyList<string> FeatureSetFor(BadgeLevel level)
    {
        var features = new List<string>
        {
            "Listings",
            "Calendar",
            "Messaging",
            "QR",
            "Stripe",
            "InsuraGuest",
            "97% payout"
        };

        if (level >= BadgeLevel.Verified)
        {
            features.AddRange(["Verified badge", "Custodian directory", "Local business directory", "Guest verification upsell"]);
        }

        if (level >= BadgeLevel.Trusted)
        {
            features.AddRange(["Trades directory", "Search boost", "Referral program"]);
        }

        if (level >= BadgeLevel.Wellness)
        {
            features.AddRange(["Police directory", "Wellness visits", "Wellness badge", "Security verified filter"]);
        }

        return features.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private BadgeDefinitionState FindBadgeDefinition(BadgeLevel level, string subjectType) =>
        _badgeDefinitions.SingleOrDefault(definition =>
            definition.Level == level &&
            definition.AppliesTo.Equals(ToPluralSubjectType(subjectType), StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("Badge definition not found for the requested subject.");

    private PriceResolution ResolveBadgePrice(BadgeDefinitionState definition, string subjectType, Guid subjectId, string? campaignKey)
    {
        var pricebookKey = definition.PricebookKey;
        decimal? overrideAmount = null;

        if (!string.IsNullOrWhiteSpace(campaignKey))
        {
            var campaign = _campaigns.SingleOrDefault(item => KeyEquals(item.Key, campaignKey))
                ?? throw new InvalidOperationException("Campaign not found.");

            if (!CampaignIsActive(campaign, _timeProvider.GetUtcNow()))
            {
                throw new InvalidOperationException("Campaign is not active.");
            }

            var enrolled = _campaignEnrollments.Any(enrollment =>
                KeyEquals(enrollment.CampaignKey, campaign.Key) &&
                enrollment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase) &&
                enrollment.SubjectId == subjectId);

            if (!enrolled)
            {
                throw new InvalidOperationException("Subject is not enrolled in the requested campaign.");
            }

            if (definition.Level == BadgeLevel.Trusted && KeyEquals(campaign.Key, "trusted-host-pdf-campaign"))
            {
                pricebookKey = "trusted-host-pdf-campaign";
            }

            overrideAmount = campaign.OverrideAmount;
        }

        var pricebook = _pricebook.SingleOrDefault(item => KeyEquals(item.Key, pricebookKey) && item.IsActive)
            ?? throw new InvalidOperationException("Active badge pricebook item not found.");

        return new PriceResolution(overrideAmount ?? pricebook.Amount, pricebook.Currency);
    }

    private decimal ResolveHostCommissionPercent()
    {
        var item = _pricebook.SingleOrDefault(price => KeyEquals(price.Key, "host-commission") && price.IsActive)
            ?? throw new InvalidOperationException("Host commission pricebook item is missing.");
        return item.Amount;
    }

    private static bool CampaignIsActive(CampaignState campaign, DateTimeOffset now) =>
        campaign.IsActive &&
        (campaign.OpensAt is null || campaign.OpensAt <= now) &&
        (campaign.ClosesAt is null || campaign.ClosesAt >= now);

    private static string NormalizeSubjectType(string subjectType)
    {
        if (string.IsNullOrWhiteSpace(subjectType))
        {
            throw new InvalidOperationException("Subject type is required.");
        }

        return subjectType.Trim();
    }

    private static string ToPluralSubjectType(string subjectType) =>
        subjectType.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? subjectType : $"{subjectType}s";

    private static bool KeyEquals(string left, string right) =>
        left.Equals(right, StringComparison.OrdinalIgnoreCase);

    private PhaseTwoPricebookItemDto ToDto(PricebookEntryState item) =>
        new(item.Key, item.Label, item.Amount, item.Currency, item.Cadence, item.AppliesTo, item.IsConfigurable, item.IsActive, item.ActiveFrom, item.ActiveTo);

    private BadgeDefinitionDto ToDto(BadgeDefinitionState definition)
    {
        var price = _pricebook.Single(item => KeyEquals(item.Key, definition.PricebookKey));
        return new BadgeDefinitionDto(
            definition.Id,
            definition.Key,
            definition.Level,
            definition.AppliesTo,
            price.Amount,
            price.Currency,
            definition.Unlocks);
    }

    private static BadgeAssignmentDto ToDto(BadgeAssignmentState assignment) =>
        new(
            assignment.Id,
            assignment.BadgeKey,
            assignment.Level,
            assignment.SubjectType,
            assignment.SubjectId,
            assignment.Status.ToString(),
            assignment.EarnedAt,
            assignment.PaidThrough,
            assignment.ExpiresAt,
            assignment.AmountCharged,
            assignment.Currency,
            assignment.PaymentStatus.ToString().ToUpperInvariant(),
            assignment.PaymentReference,
            assignment.Unlocks);

    private static BadgeRenewalDto ToDto(BadgeRenewalState renewal) =>
        new(
            renewal.Id,
            renewal.BadgeAssignmentId,
            renewal.ReminderDueAt,
            renewal.PaymentAttemptedAt,
            renewal.PaymentStatus.ToString().ToUpperInvariant(),
            renewal.AmountDue,
            renewal.Currency);

    private static CampaignDto ToDto(CampaignState campaign) =>
        new(campaign.Id, campaign.Key, campaign.Name, campaign.CampaignType, campaign.OverrideAmount, campaign.AppliesTo, campaign.OpensAt, campaign.ClosesAt, campaign.IsActive);

    private static CampaignEnrollmentDto ToDto(CampaignEnrollmentState enrollment) =>
        new(enrollment.Id, enrollment.CampaignKey, enrollment.SubjectType, enrollment.SubjectId, enrollment.EnrolledAt);

    private static FoundingBenefitDto ToDto(FoundingBenefitState benefit) =>
        new(
            benefit.PropertyId,
            benefit.Tier,
            benefit.GuestFlatFee,
            benefit.HostCommissionPercent,
            benefit.IsLifetimeGuestFee,
            benefit.IsTransferableWithProperty,
            benefit.IsForfeited);

    private sealed class PricebookEntryState(
        string key,
        string label,
        decimal amount,
        string currency,
        string cadence,
        string appliesTo,
        bool isConfigurable,
        bool isActive,
        DateTimeOffset? activeFrom,
        DateTimeOffset? activeTo)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public decimal Amount { get; set; } = amount;
        public string Currency { get; set; } = currency;
        public string Cadence { get; set; } = cadence;
        public string AppliesTo { get; } = appliesTo;
        public bool IsConfigurable { get; } = isConfigurable;
        public bool IsActive { get; set; } = isActive;
        public DateTimeOffset? ActiveFrom { get; set; } = activeFrom;
        public DateTimeOffset? ActiveTo { get; set; } = activeTo;
    }

    private sealed record BadgeDefinitionState(
        Guid Id,
        string Key,
        BadgeLevel Level,
        string AppliesTo,
        string PricebookKey,
        IReadOnlyList<string> Unlocks);

    private sealed class BadgeAssignmentState(
        Guid id,
        Guid badgeDefinitionId,
        string badgeKey,
        BadgeLevel level,
        string subjectType,
        Guid subjectId,
        BadgeAssignmentStatus status,
        DateTimeOffset earnedAt,
        DateTimeOffset paidThrough,
        DateTimeOffset expiresAt,
        decimal amountCharged,
        string currency,
        PaymentStatus paymentStatus,
        string paymentReference,
        IReadOnlyList<string> unlocks)
    {
        public Guid Id { get; } = id;
        public Guid BadgeDefinitionId { get; } = badgeDefinitionId;
        public string BadgeKey { get; } = badgeKey;
        public BadgeLevel Level { get; } = level;
        public string SubjectType { get; } = subjectType;
        public Guid SubjectId { get; } = subjectId;
        public BadgeAssignmentStatus Status { get; set; } = status;
        public DateTimeOffset EarnedAt { get; } = earnedAt;
        public DateTimeOffset PaidThrough { get; set; } = paidThrough;
        public DateTimeOffset ExpiresAt { get; set; } = expiresAt;
        public decimal AmountCharged { get; set; } = amountCharged;
        public string Currency { get; set; } = currency;
        public PaymentStatus PaymentStatus { get; set; } = paymentStatus;
        public string PaymentReference { get; set; } = paymentReference;
        public IReadOnlyList<string> Unlocks { get; } = unlocks;
    }

    private sealed class BadgeRenewalState(
        Guid id,
        Guid badgeAssignmentId,
        DateTimeOffset reminderDueAt,
        DateTimeOffset? paymentAttemptedAt,
        PaymentStatus paymentStatus,
        decimal amountDue,
        string currency)
    {
        public Guid Id { get; } = id;
        public Guid BadgeAssignmentId { get; } = badgeAssignmentId;
        public DateTimeOffset ReminderDueAt { get; } = reminderDueAt;
        public DateTimeOffset? PaymentAttemptedAt { get; set; } = paymentAttemptedAt;
        public PaymentStatus PaymentStatus { get; set; } = paymentStatus;
        public decimal AmountDue { get; set; } = amountDue;
        public string Currency { get; set; } = currency;
    }

    private sealed class CampaignState(
        Guid id,
        string key,
        string name,
        string campaignType,
        decimal? overrideAmount,
        string? appliesTo,
        DateTimeOffset? opensAt,
        DateTimeOffset? closesAt,
        bool isActive)
    {
        public Guid Id { get; } = id;
        public string Key { get; } = key;
        public string Name { get; set; } = name;
        public string CampaignType { get; set; } = campaignType;
        public decimal? OverrideAmount { get; set; } = overrideAmount;
        public string? AppliesTo { get; set; } = appliesTo;
        public DateTimeOffset? OpensAt { get; set; } = opensAt;
        public DateTimeOffset? ClosesAt { get; set; } = closesAt;
        public bool IsActive { get; set; } = isActive;
    }

    private sealed record CampaignEnrollmentState(Guid Id, string CampaignKey, string SubjectType, Guid SubjectId, DateTimeOffset EnrolledAt);

    private sealed class FoundingBenefitState(
        Guid propertyId,
        FoundingTier tier,
        decimal guestFlatFee,
        decimal hostCommissionPercent,
        bool isLifetimeGuestFee,
        bool isTransferableWithProperty,
        bool isForfeited)
    {
        public Guid PropertyId { get; } = propertyId;
        public FoundingTier Tier { get; set; } = tier;
        public decimal GuestFlatFee { get; set; } = guestFlatFee;
        public decimal HostCommissionPercent { get; set; } = hostCommissionPercent;
        public bool IsLifetimeGuestFee { get; set; } = isLifetimeGuestFee;
        public bool IsTransferableWithProperty { get; set; } = isTransferableWithProperty;
        public bool IsForfeited { get; set; } = isForfeited;
    }

    private sealed record PriceResolution(decimal Amount, string Currency);
}
