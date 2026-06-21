using Microsoft.EntityFrameworkCore;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.Services;
using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class EfPhaseTwoStore(
    NestyStayDbContext db,
    IPricebookService pricebookService,
    TimeProvider timeProvider) : IPhaseTwoStore
{
    public IReadOnlyList<PhaseTwoPricebookItemDto> GetPricebook()
    {
        EnsureSeeded();
        return db.MilestonePricebookEntries
            .AsNoTracking()
            .OrderBy(item => item.Key)
            .ToList()
            .Select(ToDto)
            .ToList();
    }

    public PhaseTwoPricebookItemDto? GetPricebookItem(string key)
    {
        EnsureSeeded();
        return db.MilestonePricebookEntries
            .AsNoTracking()
            .ToList()
            .SingleOrDefault(item => KeyEquals(item.Key, key)) is { } item
            ? ToDto(item)
            : null;
    }

    public PhaseTwoPricebookItemDto UpdatePricebookItem(string key, UpdatePricebookItemRequest request)
    {
        EnsureSeeded();
        if (request.Amount < 0)
        {
            throw new InvalidOperationException("Pricebook amount cannot be negative.");
        }

        var item = db.MilestonePricebookEntries
            .ToList()
            .SingleOrDefault(entry => KeyEquals(entry.Key, key))
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
        db.SaveChanges();

        return ToDto(item);
    }

    public IReadOnlyList<BadgeDefinitionDto> GetBadgeDefinitions()
    {
        EnsureSeeded();
        var pricebook = db.MilestonePricebookEntries.AsNoTracking().ToList();
        return db.MilestoneBadgeDefinitions
            .AsNoTracking()
            .OrderBy(definition => definition.Level)
            .ToList()
            .Select(definition => ToDto(definition, pricebook))
            .ToList();
    }

    public BadgeEligibilityDto GetBadgeEligibility(PurchaseBadgeRequest request)
    {
        EnsureSeeded();
        var subjectType = NormalizeSubjectType(request.SubjectType);
        _ = FindBadgeDefinition(request.Level, subjectType);
        return EvaluateEligibility(request with { SubjectType = subjectType });
    }

    public BadgeFeatureAccessDto GetFeatureAccess(string subjectType, Guid subjectId)
    {
        EnsureSeeded();
        var normalizedSubjectType = NormalizeSubjectType(subjectType);
        var now = timeProvider.GetUtcNow();
        var activeAssignments = db.MilestoneBadgeAssignments
            .AsNoTracking()
            .ToList()
            .Where(assignment =>
                assignment.SubjectType.Equals(normalizedSubjectType, StringComparison.OrdinalIgnoreCase) &&
                assignment.SubjectId == subjectId &&
                assignment.Status == BadgeAssignmentStatus.Active &&
                assignment.PaymentStatus == PaymentStatus.Captured &&
                assignment.ExpiresAt > now)
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

    public IReadOnlyList<BadgeAssignmentDto> GetBadgeAssignments(string? subjectType = null, Guid? subjectId = null)
    {
        EnsureSeeded();
        return db.MilestoneBadgeAssignments
            .AsNoTracking()
            .ToList()
            .Where(assignment =>
                (subjectType is null || assignment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase)) &&
                (subjectId is null || assignment.SubjectId == subjectId))
            .Select(ToDto)
            .ToList();
    }

    public IReadOnlyList<BadgeRenewalDto> GetRenewals(Guid? assignmentId = null)
    {
        EnsureSeeded();
        return db.MilestoneBadgeRenewals
            .AsNoTracking()
            .Where(renewal => assignmentId == null || renewal.BadgeAssignmentId == assignmentId)
            .OrderBy(renewal => renewal.ReminderDueAt)
            .ToList()
            .Select(ToDto)
            .ToList();
    }

    public BadgeAssignmentDto PurchaseBadge(PurchaseBadgeRequest request)
    {
        EnsureSeeded();
        var subjectType = NormalizeSubjectType(request.SubjectType);
        var definition = FindBadgeDefinition(request.Level, subjectType);
        var eligibility = EvaluateEligibility(request with { SubjectType = subjectType });
        if (!eligibility.Eligible)
        {
            throw new InvalidOperationException($"Badge eligibility failed: {string.Join(" ", eligibility.MissingRequirements)}");
        }

        var price = ResolveBadgePrice(definition, subjectType, request.SubjectId, request.CampaignKey);
        var now = timeProvider.GetUtcNow();
        var existing = db.MilestoneBadgeAssignments
            .ToList()
            .SingleOrDefault(assignment =>
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

        var assignment = new MilestoneBadgeAssignment
        {
            Id = Guid.NewGuid(),
            BadgeDefinitionId = definition.Id,
            BadgeKey = definition.Key,
            Level = definition.Level,
            SubjectType = subjectType,
            SubjectId = request.SubjectId,
            Status = assignmentStatus,
            EarnedAt = now,
            PaidThrough = expiresAt,
            ExpiresAt = expiresAt,
            AmountCharged = price.Amount,
            Currency = price.Currency,
            PaymentStatus = paymentStatus,
            PaymentReference = paymentStatus == PaymentStatus.Captured
                ? $"badge_{definition.Level.ToString().ToLowerInvariant()}_{request.SubjectId:N}"
                : $"badge_failed_{definition.Level.ToString().ToLowerInvariant()}_{request.SubjectId:N}",
            UnlocksJson = definition.UnlocksJson
        };

        db.MilestoneBadgeAssignments.Add(assignment);
        if (assignment.Status == BadgeAssignmentStatus.Active)
        {
            QueueRenewal(assignment, price.Amount, price.Currency);
        }

        db.SaveChanges();
        return ToDto(assignment);
    }

    public BadgeAssignmentDto PayRenewal(Guid assignmentId)
    {
        EnsureSeeded();
        var assignment = db.MilestoneBadgeAssignments.SingleOrDefault(item => item.Id == assignmentId)
            ?? throw new InvalidOperationException("Badge assignment not found.");
        if (assignment.Status != BadgeAssignmentStatus.Active)
        {
            throw new InvalidOperationException("Only active badge assignments can be renewed.");
        }

        var definition = db.MilestoneBadgeDefinitions.Single(item => item.Id == assignment.BadgeDefinitionId);
        var price = ResolveBadgePrice(definition, assignment.SubjectType, assignment.SubjectId, null);
        var renewal = db.MilestoneBadgeRenewals
            .Where(item => item.BadgeAssignmentId == assignmentId && item.PaymentStatus == PaymentStatus.Pending)
            .OrderBy(item => item.ReminderDueAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No pending renewal exists for this badge assignment.");

        var now = timeProvider.GetUtcNow();
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

        QueueRenewal(assignment, price.Amount, price.Currency);
        db.SaveChanges();

        return ToDto(assignment);
    }

    public BadgeAssignmentDto ExpireBadge(Guid assignmentId)
    {
        EnsureSeeded();
        var assignment = db.MilestoneBadgeAssignments.SingleOrDefault(item => item.Id == assignmentId)
            ?? throw new InvalidOperationException("Badge assignment not found.");
        assignment.Status = BadgeAssignmentStatus.Expired;
        assignment.ExpiresAt = timeProvider.GetUtcNow().AddSeconds(-1);
        db.SaveChanges();
        return ToDto(assignment);
    }

    public BadgeAssignmentDto SuspendBadge(Guid assignmentId)
    {
        EnsureSeeded();
        var assignment = db.MilestoneBadgeAssignments.SingleOrDefault(item => item.Id == assignmentId)
            ?? throw new InvalidOperationException("Badge assignment not found.");
        assignment.Status = BadgeAssignmentStatus.Suspended;
        db.SaveChanges();
        return ToDto(assignment);
    }

    public IReadOnlyList<CampaignDto> GetCampaigns()
    {
        EnsureSeeded();
        return db.MilestoneCampaigns
            .AsNoTracking()
            .OrderBy(campaign => campaign.Key)
            .ToList()
            .Select(ToDto)
            .ToList();
    }

    public CampaignDto CreateCampaign(CreateCampaignRequest request)
    {
        EnsureSeeded();
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

        var existing = db.MilestoneCampaigns
            .ToList()
            .SingleOrDefault(item => KeyEquals(item.Key, request.Key));

        if (existing is not null)
        {
            existing.Name = request.Name.Trim();
            existing.CampaignType = request.CampaignType.Trim();
            existing.OverrideAmount = request.OverrideAmount;
            existing.AppliesTo = request.AppliesTo;
            existing.OpensAt = request.OpensAt;
            existing.ClosesAt = request.ClosesAt;
            existing.IsActive = request.IsActive;
            db.SaveChanges();
            return ToDto(existing);
        }

        var campaign = new MilestoneCampaign
        {
            Id = Guid.NewGuid(),
            Key = request.Key.Trim(),
            Name = request.Name.Trim(),
            CampaignType = request.CampaignType.Trim(),
            OverrideAmount = request.OverrideAmount,
            AppliesTo = request.AppliesTo,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt,
            IsActive = request.IsActive
        };

        db.MilestoneCampaigns.Add(campaign);
        db.SaveChanges();
        return ToDto(campaign);
    }

    public CampaignEnrollmentDto EnrollCampaign(string campaignKey, EnrollCampaignRequest request)
    {
        EnsureSeeded();
        var subjectType = NormalizeSubjectType(request.SubjectType);
        var campaign = db.MilestoneCampaigns
            .ToList()
            .SingleOrDefault(item => KeyEquals(item.Key, campaignKey))
            ?? throw new InvalidOperationException("Campaign not found.");

        if (!CampaignIsActive(campaign, timeProvider.GetUtcNow()))
        {
            throw new InvalidOperationException("Campaign is not active.");
        }

        var existing = db.MilestoneCampaignEnrollments
            .ToList()
            .SingleOrDefault(enrollment =>
                KeyEquals(enrollment.CampaignKey, campaign.Key) &&
                enrollment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase) &&
                enrollment.SubjectId == request.SubjectId);

        if (existing is not null)
        {
            return ToDto(existing);
        }

        var enrollment = new MilestoneCampaignEnrollment
        {
            Id = Guid.NewGuid(),
            CampaignKey = campaign.Key,
            SubjectType = subjectType,
            SubjectId = request.SubjectId,
            EnrolledAt = timeProvider.GetUtcNow()
        };

        db.MilestoneCampaignEnrollments.Add(enrollment);
        db.SaveChanges();
        return ToDto(enrollment);
    }

    public FoundingBenefitDto UpsertFoundingBenefit(FoundingBenefitRequest request)
    {
        EnsureSeeded();
        if (!request.IsEligible && request.Tier != FoundingTier.Standard)
        {
            throw new InvalidOperationException("Subject is not eligible for founding benefits.");
        }

        var existing = db.MilestoneFoundingBenefits.SingleOrDefault(item => item.PropertyId == request.PropertyId);
        var guestFlatFee = NestyStayBusinessRules.ResolveFoundingGuestFlatFee(request.Tier);
        var isFoundingTier = request.Tier != FoundingTier.Standard;

        if (existing is null)
        {
            existing = new MilestoneFoundingBenefit
            {
                Id = Guid.NewGuid(),
                PropertyId = request.PropertyId,
                Tier = request.Tier,
                GuestFlatFee = guestFlatFee,
                HostCommissionPercent = ResolveHostCommissionPercent(),
                IsLifetimeGuestFee = isFoundingTier,
                IsTransferableWithProperty = isFoundingTier,
                IsForfeited = false
            };
            db.MilestoneFoundingBenefits.Add(existing);
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

        db.SaveChanges();
        return ToDto(existing);
    }

    public FoundingBenefitDto? GetFoundingBenefit(Guid propertyId)
    {
        EnsureSeeded();
        return db.MilestoneFoundingBenefits
            .AsNoTracking()
            .SingleOrDefault(item => item.PropertyId == propertyId) is { } benefit
            ? ToDto(benefit)
            : null;
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
        EnsureSeeded();
        if (request.BookingValue < 0)
        {
            throw new InvalidOperationException("Booking value cannot be negative.");
        }

        if (request.Nights <= 0)
        {
            throw new InvalidOperationException("Nights must be greater than zero.");
        }

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

    private void EnsureSeeded()
    {
        var now = timeProvider.GetUtcNow();
        var changed = false;

        if (!db.MilestonePricebookEntries.Any())
        {
            db.MilestonePricebookEntries.AddRange(pricebookService.GetDefaultPricebook().Select(item => new MilestonePricebookEntry
            {
                Id = Guid.NewGuid(),
                Key = item.Key,
                Label = item.Label,
                Amount = item.Amount,
                Currency = item.Currency,
                Cadence = item.Cadence,
                AppliesTo = item.AppliesTo,
                IsConfigurable = item.IsConfigurable,
                IsActive = true,
                ActiveFrom = now
            }));
            changed = true;
        }

        if (!db.MilestoneBadgeDefinitions.Any())
        {
            db.MilestoneBadgeDefinitions.AddRange(DefaultBadgeDefinitions());
            changed = true;
        }

        if (!db.MilestoneCampaigns.Any(campaign => campaign.Key == "trusted-host-pdf-campaign"))
        {
            db.MilestoneCampaigns.Add(new MilestoneCampaign
            {
                Id = Guid.Parse("20000000-0000-4000-8000-000000000001"),
                Key = "trusted-host-pdf-campaign",
                Name = "Trusted host PDF campaign",
                CampaignType = "BadgePriceOverride",
                OverrideAmount = 49m,
                AppliesTo = "Hosts",
                OpensAt = now.AddDays(-1),
                ClosesAt = now.AddYears(1),
                IsActive = true
            });
            changed = true;
        }

        if (changed)
        {
            db.SaveChanges();
        }
    }

    private void QueueRenewal(MilestoneBadgeAssignment assignment, decimal amountDue, string currency)
    {
        if (amountDue <= 0 || assignment.ExpiresAt == DateTimeOffset.MaxValue)
        {
            return;
        }

        var reminderDueAt = assignment.ExpiresAt.AddDays(-30);
        if (db.MilestoneBadgeRenewals.Any(renewal =>
                renewal.BadgeAssignmentId == assignment.Id &&
                renewal.ReminderDueAt == reminderDueAt &&
                renewal.PaymentStatus == PaymentStatus.Pending))
        {
            return;
        }

        db.MilestoneBadgeRenewals.Add(new MilestoneBadgeRenewal
        {
            Id = Guid.NewGuid(),
            BadgeAssignmentId = assignment.Id,
            ReminderDueAt = reminderDueAt,
            PaymentAttemptedAt = null,
            PaymentStatus = PaymentStatus.Pending,
            AmountDue = amountDue,
            Currency = currency
        });
    }

    private BadgeEligibilityDto EvaluateEligibility(PurchaseBadgeRequest request)
    {
        var missing = new List<string>();
        var hasActiveVerified = HasActiveBadge(request.SubjectType, request.SubjectId, BadgeLevel.Verified);

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

    private bool HasActiveBadge(string subjectType, Guid subjectId, BadgeLevel level)
    {
        var now = timeProvider.GetUtcNow();
        return db.MilestoneBadgeAssignments
            .AsNoTracking()
            .ToList()
            .Any(assignment =>
                assignment.SubjectType.Equals(subjectType, StringComparison.OrdinalIgnoreCase) &&
                assignment.SubjectId == subjectId &&
                assignment.Level == level &&
                assignment.Status == BadgeAssignmentStatus.Active &&
                assignment.PaymentStatus == PaymentStatus.Captured &&
                assignment.ExpiresAt > now);
    }

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

    private MilestoneBadgeDefinition FindBadgeDefinition(BadgeLevel level, string subjectType) =>
        db.MilestoneBadgeDefinitions
            .ToList()
            .SingleOrDefault(definition =>
                definition.Level == level &&
                definition.AppliesTo.Equals(ToPluralSubjectType(subjectType), StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("Badge definition not found for the requested subject.");

    private PriceResolution ResolveBadgePrice(MilestoneBadgeDefinition definition, string subjectType, Guid subjectId, string? campaignKey)
    {
        var pricebookKey = definition.PricebookKey;
        decimal? overrideAmount = null;

        if (!string.IsNullOrWhiteSpace(campaignKey))
        {
            var campaign = db.MilestoneCampaigns
                .ToList()
                .SingleOrDefault(item => KeyEquals(item.Key, campaignKey))
                ?? throw new InvalidOperationException("Campaign not found.");

            if (!CampaignIsActive(campaign, timeProvider.GetUtcNow()))
            {
                throw new InvalidOperationException("Campaign is not active.");
            }

            var enrolled = db.MilestoneCampaignEnrollments
                .ToList()
                .Any(enrollment =>
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

        var pricebook = db.MilestonePricebookEntries
            .ToList()
            .SingleOrDefault(item => KeyEquals(item.Key, pricebookKey) && item.IsActive)
            ?? throw new InvalidOperationException("Active badge pricebook item not found.");

        return new PriceResolution(overrideAmount ?? pricebook.Amount, pricebook.Currency);
    }

    private decimal ResolveHostCommissionPercent()
    {
        var item = db.MilestonePricebookEntries
            .ToList()
            .SingleOrDefault(price => KeyEquals(price.Key, "host-commission") && price.IsActive)
            ?? throw new InvalidOperationException("Host commission pricebook item is missing.");
        return item.Amount;
    }

    private static bool CampaignIsActive(MilestoneCampaign campaign, DateTimeOffset now) =>
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

    private static PhaseTwoPricebookItemDto ToDto(MilestonePricebookEntry item) =>
        new(item.Key, item.Label, item.Amount, item.Currency, item.Cadence, item.AppliesTo, item.IsConfigurable, item.IsActive, item.ActiveFrom, item.ActiveTo);

    private BadgeDefinitionDto ToDto(MilestoneBadgeDefinition definition, IReadOnlyList<MilestonePricebookEntry>? pricebook = null)
    {
        pricebook ??= db.MilestonePricebookEntries.ToList();
        var price = pricebook.Single(item => KeyEquals(item.Key, definition.PricebookKey));
        return new BadgeDefinitionDto(
            definition.Id,
            definition.Key,
            definition.Level,
            definition.AppliesTo,
            price.Amount,
            price.Currency,
            MilestoneJson.DeserializeList<string>(definition.UnlocksJson));
    }

    private static BadgeAssignmentDto ToDto(MilestoneBadgeAssignment assignment) =>
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
            MilestoneJson.DeserializeList<string>(assignment.UnlocksJson));

    private static BadgeRenewalDto ToDto(MilestoneBadgeRenewal renewal) =>
        new(
            renewal.Id,
            renewal.BadgeAssignmentId,
            renewal.ReminderDueAt,
            renewal.PaymentAttemptedAt,
            renewal.PaymentStatus.ToString().ToUpperInvariant(),
            renewal.AmountDue,
            renewal.Currency);

    private static CampaignDto ToDto(MilestoneCampaign campaign) =>
        new(campaign.Id, campaign.Key, campaign.Name, campaign.CampaignType, campaign.OverrideAmount, campaign.AppliesTo, campaign.OpensAt, campaign.ClosesAt, campaign.IsActive);

    private static CampaignEnrollmentDto ToDto(MilestoneCampaignEnrollment enrollment) =>
        new(enrollment.Id, enrollment.CampaignKey, enrollment.SubjectType, enrollment.SubjectId, enrollment.EnrolledAt);

    private static FoundingBenefitDto ToDto(MilestoneFoundingBenefit benefit) =>
        new(
            benefit.PropertyId,
            benefit.Tier,
            benefit.GuestFlatFee,
            benefit.HostCommissionPercent,
            benefit.IsLifetimeGuestFee,
            benefit.IsTransferableWithProperty,
            benefit.IsForfeited);

    private static IReadOnlyList<MilestoneBadgeDefinition> DefaultBadgeDefinitions() =>
    [
        Badge(
            Guid.Parse("10000000-0000-4000-8000-000000000000"),
            "host-free",
            BadgeLevel.Free,
            "Hosts",
            "host-listing",
            ["Listings", "Calendar", "Messaging", "QR", "Stripe", "InsuraGuest", "97% payout"]),
        Badge(
            Guid.Parse("10000000-0000-4000-8000-000000000001"),
            "host-verified",
            BadgeLevel.Verified,
            "Hosts",
            "verified-host-standard-annual",
            ["Verified badge", "Custodian directory", "Local business directory", "Guest verification upsell"]),
        Badge(
            Guid.Parse("10000000-0000-4000-8000-000000000002"),
            "host-trusted",
            BadgeLevel.Trusted,
            "Hosts",
            "trusted-host-standard-annual",
            ["Trades directory", "Search boost", "Referral program"]),
        Badge(
            Guid.Parse("10000000-0000-4000-8000-000000000003"),
            "host-wellness",
            BadgeLevel.Wellness,
            "Hosts",
            "verified-host-standard-annual",
            ["Police directory", "Wellness visits", "Wellness badge", "Security verified filter"])
    ];

    private static MilestoneBadgeDefinition Badge(
        Guid id,
        string key,
        BadgeLevel level,
        string appliesTo,
        string pricebookKey,
        IReadOnlyList<string> unlocks) =>
        new()
        {
            Id = id,
            Key = key,
            Level = level,
            AppliesTo = appliesTo,
            PricebookKey = pricebookKey,
            UnlocksJson = MilestoneJson.Serialize(unlocks)
        };

    private sealed record PriceResolution(decimal Amount, string Currency);
}
