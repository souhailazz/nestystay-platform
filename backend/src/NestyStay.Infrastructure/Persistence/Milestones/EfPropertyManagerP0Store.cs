using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NestyStay.Domain;
using NestyStay.Application.PropertyManager;
using NestyStay.Domain.Notifications;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// Persistence and business rules for the P0 money/authority foundation.
/// P0 deliberately uses a separate aggregate from the legacy milestone ledger:
/// every posted journal is balanced, immutable and explicitly scoped to a
/// manager, owner and (when applicable) property.
/// </summary>
public sealed class EfPropertyManagerP0Store(
    NestyStayDbContext db,
    TimeProvider timeProvider) : IPropertyManagerP0Store
{
    private static readonly string[] Currencies = ["JMD", "USD"];
    private static readonly string[] OwnerStatuses = ["ACTIVE", "SUSPENDED", "ARCHIVED"];
    private static readonly string[] AgreementStatuses = ["DRAFT", "ACTIVE", "TERMINATED", "EXPIRED"];
    private static readonly string[] PayoutTerminalStatuses = ["PAID", "FAILED", "CANCELLED"];

    private DateTimeOffset Now => timeProvider.GetUtcNow();

    private async Task<IDbContextTransaction?> BeginP0TransactionAsync(CancellationToken cancellationToken)
    {
        // The API test fixture uses EF's in-memory provider, which does not
        // support transactions.  Production PostgreSQL uses a serializable
        // transaction for the multi-row money operations below; callers can
        // compose operations without attempting a nested transaction.
        if (!db.Database.IsRelational() || db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true || db.Database.CurrentTransaction is not null) return null;
        return await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    private async Task<IDbContextTransaction?> BeginP0LockedCommandTransactionAsync(CancellationToken cancellationToken)
    {
        // Advisory locks serialize these single-aggregate commands. Read
        // committed is intentional: after waiting for another API instance,
        // the next statement must observe the winner's committed result so an
        // identical command can replay instead of failing serialization.
        if (!db.Database.IsRelational() || db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true || db.Database.CurrentTransaction is not null) return null;
        return await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    private async Task<Guid> ResolveManagerAsync(
        P0Actor actor,
        bool finance = false,
        bool payoutApprover = false,
        Guid? ownerUserId = null,
        Guid? propertyId = null,
        CancellationToken cancellationToken = default)
    {
        if (!finance && ownerUserId.HasValue && actor.UserId == ownerUserId.Value)
        {
            var ownerManagers = await db.MilestoneManagerOwners.AsNoTracking().Where(x => x.OwnerUserId == actor.UserId && !x.IsDeleted).Select(x => x.ManagerUserId).Distinct().ToListAsync(cancellationToken);
            if (ownerManagers.Count == 1) return ownerManagers[0];
            if (ownerManagers.Count > 1) throw new InvalidOperationException("Select a manager portfolio before continuing.");
        }
        var membership = await db.MilestoneP0StaffMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.StaffUserId == actor.UserId && !x.IsDeleted &&
                                       (x.Status == "ACCEPTED" || x.Status == "ACTIVE"), cancellationToken);

        if (membership is not null)
        {
            if (finance && !membership.CanManageFinance)
                throw new UnauthorizedAccessException("This team member is not authorised for finance operations.");
            if (payoutApprover && !membership.CanApprovePayouts)
                throw new UnauthorizedAccessException("A separate authorised payout approver is required.");
            // An empty owner scope means "all owners" only when the member
            // has no property scope.  A property-only assignment must not be
            // able to address an unrelated owner-wide record (for example an
            // agreement or profile with no property dimension).
            var scopedOwnerIds = ParseIds(membership.OwnerScopeJson);
            var scopedPropertyIds = ParseIds(membership.PropertyScopeJson);
            if (ownerUserId.HasValue && scopedOwnerIds.Length > 0 && !scopedOwnerIds.Contains(ownerUserId.Value))
                throw new UnauthorizedAccessException("Owner is outside this team member's scope.");
            if (ownerUserId.HasValue && scopedPropertyIds.Length > 0 &&
                !await db.MilestoneManagerProperties.AsNoTracking().AnyAsync(
                    x => x.ManagerUserId == membership.ManagerUserId &&
                         scopedPropertyIds.Contains(x.Id) &&
                         x.OwnerUserId == ownerUserId.Value &&
                         !x.IsDeleted,
                    cancellationToken))
                throw new UnauthorizedAccessException("Owner is outside this team member's property scope.");
            if (propertyId.HasValue && !InScope(membership.PropertyScopeJson, propertyId.Value))
                throw new UnauthorizedAccessException("Property is outside this team member's scope.");
            return membership.ManagerUserId;
        }

        if (!actor.IsAdmin && !actor.IsPropertyManager)
            throw new UnauthorizedAccessException("A property manager or authorised team member is required.");

        await EnsureManagerAsync(actor.UserId, cancellationToken);
        return actor.UserId;
    }

    private sealed record P0Scope(Guid[] OwnerIds, Guid[] PropertyIds)
    {
        public bool IsRestricted => OwnerIds.Length > 0 || PropertyIds.Length > 0;

        public bool Allows(Guid? ownerUserId, Guid? propertyId)
        {
            var ownerAllowed = !ownerUserId.HasValue || OwnerIds.Length == 0 || OwnerIds.Contains(ownerUserId.Value);
            var propertyAllowed = !propertyId.HasValue || PropertyIds.Length == 0 || PropertyIds.Contains(propertyId.Value);
            return ownerAllowed && propertyAllowed;
        }
    }

    private async Task<P0Scope> ResolveScopeAsync(P0Actor actor, Guid managerId, CancellationToken cancellationToken)
    {
        if (actor.IsAdmin || actor.UserId == managerId) return new([], []);
        var membership = await db.MilestoneP0StaffMemberships.AsNoTracking().SingleOrDefaultAsync(
            x => x.ManagerUserId == managerId && x.StaffUserId == actor.UserId && !x.IsDeleted && (x.Status == "ACCEPTED" || x.Status == "ACTIVE"), cancellationToken);
        if (membership is not null)
        {
            var ownerIds = ParseIds(membership.OwnerScopeJson).ToHashSet();
            var propertyIds = ParseIds(membership.PropertyScopeJson);
            // A property-scoped staff member is also allowed to see owner-wide
            // records for the owners of those properties (for example an
            // agreement or owner-funds account without a property dimension).
            // Resolve that relationship once here so every read path applies
            // the same scope rule.
            if (propertyIds.Length > 0)
            {
                var propertyOwners = await db.MilestoneManagerProperties.AsNoTracking()
                    .Where(x => x.ManagerUserId == managerId && propertyIds.Contains(x.Id) && !x.IsDeleted)
                    .Select(x => x.OwnerUserId)
                    .ToListAsync(cancellationToken);
                ownerIds.UnionWith(propertyOwners);
            }
            return new(ownerIds.ToArray(), propertyIds);
        }

        // Owners use the same scoped read paths from the owner portal.  They
        // are never treated as staff; the scope is their own manager relation.
        if (await db.MilestoneManagerOwners.AsNoTracking().AnyAsync(
                x => x.ManagerUserId == managerId && x.OwnerUserId == actor.UserId && !x.IsDeleted,
                cancellationToken))
            return new([actor.UserId], []);

        throw new UnauthorizedAccessException("This team member is not authorised for the manager portfolio.");
    }

    private async Task EnsureStaffCapabilityAsync(P0Actor actor, Guid managerId, bool finance, bool payoutApprover, bool approvalDecision, CancellationToken cancellationToken)
    {
        if (actor.IsAdmin || actor.UserId == managerId) return;
        var membership = await db.MilestoneP0StaffMemberships.AsNoTracking().SingleOrDefaultAsync(
            x => x.ManagerUserId == managerId && x.StaffUserId == actor.UserId && !x.IsDeleted && (x.Status == "ACCEPTED" || x.Status == "ACTIVE"), cancellationToken)
            ?? throw new UnauthorizedAccessException("This team member is not authorised for the manager portfolio.");
        if (finance && !membership.CanManageFinance) throw new UnauthorizedAccessException("This team member is not authorised for finance operations.");
        if (payoutApprover && !membership.CanApprovePayouts) throw new UnauthorizedAccessException("A separate authorised payout approver is required.");
        if (approvalDecision && !(membership.CanManageFinance || membership.Role == "APPROVER")) throw new UnauthorizedAccessException("This team member is not authorised to decide owner approvals.");
    }

    private async Task EnsureRequestScopeAsync(P0Scope scope, Guid managerId, IEnumerable<P0JournalLineRequest> lines, CancellationToken cancellationToken)
    {
        var materialized = lines.ToList();
        if (scope.IsRestricted && !materialized.Any(line => line.OwnerUserId.HasValue || line.PropertyId.HasValue))
            throw new UnauthorizedAccessException("A scoped team member must post a journal against an owner or property.");
        foreach (var line in materialized)
        {
            var effectiveOwner = line.OwnerUserId;
            if (line.PropertyId.HasValue)
            {
                var property = await db.MilestoneManagerProperties.AsNoTracking().SingleOrDefaultAsync(
                    x => x.Id == line.PropertyId.Value && x.ManagerUserId == managerId && !x.IsDeleted,
                    cancellationToken)
                    ?? throw new InvalidOperationException("Journal property is not in this manager portfolio.");
                if (effectiveOwner.HasValue && effectiveOwner.Value != property.OwnerUserId)
                    throw new InvalidOperationException("Journal property is assigned to a different owner.");
                effectiveOwner ??= property.OwnerUserId;
            }

            if (scope.IsRestricted && !scope.Allows(effectiveOwner, line.PropertyId))
                throw new UnauthorizedAccessException("A journal line is outside this team member's owner or property scope.");
        }
    }

    private static bool InScope(string json, Guid id)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return true;
        try
        {
            var ids = JsonSerializer.Deserialize<Guid[]>(json) ?? [];
            return ids.Length == 0 || ids.Contains(id);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void RequireManagerMutation(P0Actor actor, Guid managerId)
    {
        if (!actor.IsAdmin && actor.UserId != managerId)
            throw new UnauthorizedAccessException("Only the portfolio manager or an administrator can change this record.");
    }

    private async Task EnsureStaffLimitAsync(P0Actor actor, Guid managerId, decimal amount, CancellationToken cancellationToken)
    {
        if (actor.IsAdmin || actor.UserId == managerId || amount <= 0) return;
        var membership = await db.MilestoneP0StaffMemberships.AsNoTracking().SingleOrDefaultAsync(
            x => x.ManagerUserId == managerId && x.StaffUserId == actor.UserId && !x.IsDeleted && (x.Status == "ACCEPTED" || x.Status == "ACTIVE"), cancellationToken);
        if (membership is null || membership.ApprovalLimit <= 0 || amount > membership.ApprovalLimit)
            throw new UnauthorizedAccessException("This team member's approval limit does not cover the requested amount.");
    }

    private async Task ValidateStaffScopesAsync(Guid managerId, IReadOnlyList<Guid>? ownerIds, IReadOnlyList<Guid>? propertyIds, CancellationToken cancellationToken)
    {
        var owners = (ownerIds ?? []).Distinct().ToArray();
        var properties = (propertyIds ?? []).Distinct().ToArray();
        if (owners.Length > 0 && await db.MilestoneManagerOwners.CountAsync(x => x.ManagerUserId == managerId && owners.Contains(x.OwnerUserId) && !x.IsDeleted, cancellationToken) != owners.Length)
            throw new InvalidOperationException("Every owner scope must belong to this manager portfolio.");
        if (properties.Length > 0 && await db.MilestoneManagerProperties.CountAsync(x => x.ManagerUserId == managerId && properties.Contains(x.Id) && !x.IsDeleted, cancellationToken) != properties.Length)
            throw new InvalidOperationException("Every property scope must belong to this manager portfolio.");
    }

    private async Task<MilestonePropertyManager> EnsureManagerAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        var manager = await db.MilestonePropertyManagers
            .SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (manager is not null) return manager;

        if (!await db.MilestoneUsers.AnyAsync(x => x.Id == managerUserId && !x.IsDeleted, cancellationToken))
        {
            db.MilestoneUsers.Add(new MilestoneUser { Id = managerUserId, Email = $"manager-{managerUserId:N}@nestystay.local", NormalizedEmail = $"MANAGER-{managerUserId:N}@NESTYSTAY.LOCAL", DisplayName = "NestyStay Property Manager", PasswordHash = "P0-managed-account" });
            await db.SaveChangesAsync(cancellationToken);
        }

        manager = new MilestonePropertyManager
        {
            ManagerUserId = managerUserId,
            BusinessName = "NestyStay Property Management",
            SubscriptionTier = "Portfolio",
            MonthlyAmount = 0m,
            SubscriptionStatus = "ACTIVE",
            BillingProviderStatus = "LOCAL_TEST",
            AutoRenew = true,
            NextBillingAt = Now.AddMonths(1)
        };
        db.MilestonePropertyManagers.Add(manager);
        await db.SaveChangesAsync(cancellationToken);
        return manager;
    }

    private async Task<MilestoneUser> EnsureOwnerAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var owner = await db.MilestoneUsers.SingleOrDefaultAsync(x => x.Id == ownerUserId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Owner user was not found.");
        var relation = await db.MilestoneManagerOwners.SingleOrDefaultAsync(
            x => x.ManagerUserId == managerUserId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        if (relation is null)
        {
            db.MilestoneManagerOwners.Add(new MilestoneManagerOwner
            {
                ManagerUserId = managerUserId,
                OwnerUserId = ownerUserId,
                DisplayName = owner.DisplayName,
                Email = owner.Email,
                VerificationStatus = "PENDING",
                InvitationStatus = "ACCEPTED"
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var currency in Currencies)
        {
            await EnsureAccountAsync(managerUserId, ownerUserId, null, currency, "OWNER_FUNDS", "Owner client funds", "LIABILITY", clientMoney: true, pmMoney: false, thirdParty: false, cancellationToken);
            await EnsureAccountAsync(managerUserId, ownerUserId, null, currency, "OWNER_RECEIVABLE", "Owner receivable", "ASSET", clientMoney: false, pmMoney: false, thirdParty: false, cancellationToken);
            await EnsureAccountAsync(managerUserId, ownerUserId, null, currency, "OWNER_INCOME", "Owner income", "LIABILITY", clientMoney: true, pmMoney: false, thirdParty: false, cancellationToken);
            await EnsureAccountAsync(managerUserId, ownerUserId, null, currency, "OWNER_EXPENSE", "Owner expense", "EXPENSE", clientMoney: true, pmMoney: false, thirdParty: false, cancellationToken);
            await EnsureAccountAsync(managerUserId, ownerUserId, null, currency, "OWNER_FUNDS_RESERVED", "Reserved owner funds", "LIABILITY", clientMoney: true, pmMoney: false, thirdParty: false, cancellationToken);
        }
        return owner;
    }

    private async Task EnsureOwnerScopeAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken) =>
        _ = await EnsureOwnerAsync(managerUserId, ownerUserId, cancellationToken);

    private async Task EnsurePropertyScopeAsync(Guid managerUserId, Guid propertyId, Guid? expectedOwnerId, CancellationToken cancellationToken)
    {
        var property = await db.MilestoneManagerProperties.SingleOrDefaultAsync(
            x => x.Id == propertyId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Property is not assigned to this manager.");
        if (expectedOwnerId.HasValue && property.OwnerUserId != expectedOwnerId.Value)
            throw new InvalidOperationException("Property is assigned to a different owner.");
    }

    private async Task AuditAsync(Guid actorUserId, string action, string subjectType, Guid? subjectId, string reason, object? metadata, CancellationToken cancellationToken)
    {
        db.MilestoneAuditEvents.Add(new MilestoneAuditEvent
        {
            ActorUserId = actorUserId,
            ActorRole = "PropertyManager",
            Action = action,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Reason = reason,
            MetadataJson = metadata is null ? "{}" : JsonSerializer.Serialize(metadata)
        });
        await Task.CompletedTask;
    }

    private async Task QueueNotificationAsync(Guid recipientUserId, string subject, string body, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (await db.NotificationQueue.AnyAsync(x => x.IdempotencyKey == idempotencyKey && !x.IsDeleted, cancellationToken)) return;
        var user = await db.MilestoneUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == recipientUserId && !x.IsDeleted, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;
        db.NotificationQueue.Add(new NotificationQueueItem
        {
            RecipientUserId = recipientUserId,
            Channel = "EMAIL",
            Recipient = user.Email,
            Subject = subject,
            Body = body,
            TextBody = body,
            Status = NotificationStatus.Queued,
            DeliveryStatus = "PENDING",
            AttemptCount = 0,
            NextAttemptAt = Now,
            IdempotencyKey = idempotencyKey
        });
    }

    private static void ValidateCurrency(string currency)
    {
        if (!Currencies.Contains(currency.Trim().ToUpperInvariant(), StringComparer.Ordinal))
            throw new InvalidOperationException("Currency must be JMD or USD. Balances never mix currencies.");
    }

    private static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (from > to) throw new InvalidOperationException("The period start must be on or before the period end.");
    }

    private async Task<MilestoneP0Account> EnsureAccountAsync(
        Guid managerUserId,
        Guid? ownerUserId,
        Guid? propertyId,
        string currency,
        string prefix,
        string name,
        string accountType,
        bool clientMoney,
        bool pmMoney,
        bool thirdParty,
        CancellationToken cancellationToken)
    {
        ValidateCurrency(currency);
        var code = prefix switch
        {
            "OWNER_FUNDS" => $"OWNER_FUNDS:{ownerUserId}:{currency}",
            "OWNER_FUNDS_RESERVED" => $"OWNER_FUNDS_RESERVED:{ownerUserId}:{currency}",
            "OWNER_RECEIVABLE" => $"OWNER_RECEIVABLE:{ownerUserId}:{currency}",
            "OWNER_INCOME" => $"OWNER_INCOME:{ownerUserId}:{propertyId?.ToString() ?? "ALL"}:{currency}",
            "OWNER_EXPENSE" => $"OWNER_EXPENSE:{ownerUserId}:{propertyId?.ToString() ?? "ALL"}:{currency}",
            "THIRD_PARTY_PAYABLE" => $"THIRD_PARTY_PAYABLE:{ownerUserId}:{propertyId?.ToString() ?? "ALL"}:{currency}",
            _ => $"{prefix}:{currency}"
        };
        var existing = await db.MilestoneP0Accounts.SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.Code == code && !x.IsDeleted, cancellationToken);
        if (existing is not null) return existing;
        var account = new MilestoneP0Account
        {
            ManagerUserId = managerUserId,
            OwnerUserId = ownerUserId,
            PropertyId = propertyId,
            Code = code,
            Name = name,
            AccountType = accountType,
            Currency = currency,
            IsClientMoney = clientMoney,
            IsPmMoney = pmMoney,
            IsThirdParty = thirdParty
        };
        db.MilestoneP0Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    private async Task EnsureBaseAccountsAsync(Guid managerUserId, string currency, CancellationToken cancellationToken)
    {
        await EnsureAccountAsync(managerUserId, null, null, currency, "CASH_CLEARING", "Cash clearing", "ASSET", false, true, false, cancellationToken);
        await EnsureAccountAsync(managerUserId, null, null, currency, "PM_FEE_REVENUE", "Property manager fee revenue", "REVENUE", false, true, false, cancellationToken);
        await EnsureAccountAsync(managerUserId, null, null, currency, "PM_OPERATING_EXPENSE", "Property manager operating expense", "EXPENSE", false, true, false, cancellationToken);
        await EnsureAccountAsync(managerUserId, null, null, currency, "SUSPENSE", "Unreconciled suspense", "LIABILITY", false, false, true, cancellationToken);
    }

    private static P0OwnerProfileDto ToDto(MilestoneP0OwnerProfile item) => new(item.Id, item.ManagerUserId, item.OwnerUserId, item.Status, item.LegalName, item.ContactEmail, item.ContactPhone, item.BillingAddress, item.PreferredCurrency, item.TimeZone, item.OperationalMetadataJson, item.Notes, item.Version, item.ActivatedAt, item.SuspendedAt, item.ArchivedAt, item.BillingMetadataJson, item.PaymentProviderCustomerReference);
    private static P0OwnerLifecycleEventDto ToDto(MilestoneP0OwnerLifecycleEvent item) => new(item.Id, item.OwnerUserId, item.EventType, item.FromStatus, item.ToStatus, item.Reason, item.ActorUserId, item.CreatedAt);
    private static P0AgreementDto ToDto(MilestoneP0ManagementAgreement item) => new(item.Id, item.ManagerUserId, item.OwnerUserId, item.PropertyId, item.Version, item.SupersedesAgreementId, item.Status, item.EffectiveFrom, item.EffectiveTo, item.Currency, item.TermsJson, item.FeeRuleJson, item.MaintenanceApprovalLimit, item.ExpenseApprovalLimit, item.DocumentId, item.DocumentKey, item.ActivatedAt, item.TerminatedAt, item.TerminationReason, item.RowVersion);
    private static P0FeeRuleDto ToDto(MilestoneP0ManagementFeeRule item) => new(item.Id, item.ManagerUserId, item.OwnerUserId, item.PropertyId, item.Category, item.RuleType, item.CalculationBasis, item.Currency, item.Percentage, item.FixedAmount, item.MinimumAmount, item.CleaningMarkup, item.MaintenanceMarkup, item.EffectiveFrom, item.EffectiveTo, item.IsActive, item.RowVersion);
    private static PropertyDto ToDto(MilestoneManagerProperty item) => new(item.Id, item.OwnerUserId, item.CommunityId, item.Title, item.UnitNumber, item.Address, item.Status, item.OccupancyStatus, item.RentalListingId);
    private static P0AccountDto ToDto(MilestoneP0Account item) => new(item.Id, item.Code, item.Name, item.AccountType, item.Currency, item.OwnerUserId, item.PropertyId, item.IsClientMoney, item.IsPmMoney, item.IsThirdParty, item.Status);
    private static P0ReconciliationDto ToDto(MilestoneP0Reconciliation item) => new(item.Id, item.JournalId, item.ExternalReference, item.Status, item.Amount, item.Currency, item.Reason, item.ActorUserId, item.ReconciledAt);

    public async Task<P0OwnerProfileDto> UpsertOwnerProfileAsync(P0Actor actor, Guid ownerUserId, UpsertP0OwnerProfileRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: ownerUserId, cancellationToken: cancellationToken);
        RequireManagerMutation(actor, managerId);
        ValidateCurrency(request.PreferredCurrency);
        if (string.IsNullOrWhiteSpace(request.LegalName) || string.IsNullOrWhiteSpace(request.ContactEmail)) throw new InvalidOperationException("Legal name and contact email are required.");
        _ = await EnsureOwnerAsync(managerId, ownerUserId, cancellationToken);
        var item = await db.MilestoneP0OwnerProfiles.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        var previous = item?.Status ?? "";
        var created = item is null;
        if (item is null)
        {
            item = new MilestoneP0OwnerProfile { ManagerUserId = managerId, OwnerUserId = ownerUserId, ActivatedAt = Now };
            db.MilestoneP0OwnerProfiles.Add(item);
        }
        item.LegalName = request.LegalName.Trim(); item.ContactEmail = request.ContactEmail.Trim(); item.ContactPhone = request.ContactPhone?.Trim() ?? string.Empty; item.BillingAddress = request.BillingAddress?.Trim() ?? string.Empty; item.PreferredCurrency = request.PreferredCurrency.Trim().ToUpperInvariant(); item.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "America/Jamaica" : request.TimeZone.Trim(); item.BillingMetadataJson = JsonOrObject(request.BillingMetadataJson ?? "{}"); item.PaymentProviderCustomerReference = string.IsNullOrWhiteSpace(request.PaymentProviderCustomerReference) ? null : request.PaymentProviderCustomerReference.Trim(); item.OperationalMetadataJson = string.IsNullOrWhiteSpace(request.OperationalMetadataJson) ? "{}" : request.OperationalMetadataJson; item.Notes = request.Notes?.Trim() ?? string.Empty; item.Status = item.Status is "" or null ? "ACTIVE" : item.Status; if (!created) item.Version++;
        if (previous != item.Status) db.MilestoneP0OwnerLifecycleEvents.Add(new MilestoneP0OwnerLifecycleEvent { ManagerUserId = managerId, OwnerUserId = ownerUserId, ActorUserId = actor.UserId, EventType = "PROFILE_CREATED", FromStatus = previous, ToStatus = item.Status, Reason = "Owner profile created" });
        await AuditAsync(actor.UserId, "P0OwnerProfileUpserted", "OwnerProfile", item.Id, "Owner profile saved", new { ownerUserId, managerId }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<P0OwnerProfileDto?> GetOwnerProfileAsync(P0Actor actor, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: ownerUserId, cancellationToken: cancellationToken);
        return (await db.MilestoneP0OwnerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken)) is { } item ? ToDto(item) : null;
    }

    public async Task<P0OwnerProfileDto?> ChangeOwnerStatusAsync(P0Actor actor, Guid ownerUserId, P0StatusChangeRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: ownerUserId, cancellationToken: cancellationToken);
        RequireManagerMutation(actor, managerId);
        var status = request.Status.Trim().ToUpperInvariant();
        if (!OwnerStatuses.Contains(status, StringComparer.Ordinal) || string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("Owner status and reason are required.");
        var item = await db.MilestoneP0OwnerProfiles.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        if (item is null) return null;
        var from = item.Status; if (from == status) return ToDto(item);
        item.Status = status; item.Version++; if (status == "ACTIVE") item.ActivatedAt = Now; if (status == "SUSPENDED") item.SuspendedAt = Now; if (status == "ARCHIVED") item.ArchivedAt = Now;
        db.MilestoneP0OwnerLifecycleEvents.Add(new MilestoneP0OwnerLifecycleEvent { ManagerUserId = managerId, OwnerUserId = ownerUserId, ActorUserId = actor.UserId, EventType = "STATUS_CHANGED", FromStatus = from, ToStatus = status, Reason = request.Reason.Trim() });
        await AuditAsync(actor.UserId, "P0OwnerStatusChanged", "OwnerProfile", item.Id, request.Reason.Trim(), new { from, status }, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<IReadOnlyList<P0OwnerLifecycleEventDto>> ListOwnerLifecycleAsync(P0Actor actor, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: ownerUserId, cancellationToken: cancellationToken);
        return (await db.MilestoneP0OwnerLifecycleEvents.AsNoTracking().Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<P0PortfolioDto> GetPortfolioAsync(P0Actor actor, P0PortfolioQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var page = Math.Max(1, query.Page); var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = from property in db.MilestoneManagerProperties.AsNoTracking()
                    join owner in db.MilestoneManagerOwners.AsNoTracking() on new { property.ManagerUserId, property.OwnerUserId } equals new { owner.ManagerUserId, owner.OwnerUserId }
                    where property.ManagerUserId == managerId && !property.IsDeleted && !owner.IsDeleted
                    select new { property, owner };
        if (query.OwnerUserId.HasValue) items = items.Where(x => x.property.OwnerUserId == query.OwnerUserId.Value);
        if (query.PropertyId.HasValue) items = items.Where(x => x.property.Id == query.PropertyId.Value);
        if (scope.OwnerIds.Length > 0) items = items.Where(x => scope.OwnerIds.Contains(x.property.OwnerUserId));
        if (scope.PropertyIds.Length > 0) items = items.Where(x => scope.PropertyIds.Contains(x.property.Id));
        if (!string.IsNullOrWhiteSpace(query.Status)) items = items.Where(x => x.property.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var search = query.Search.Trim(); items = items.Where(x => x.property.Title.Contains(search) || x.property.Address.Contains(search) || x.owner.DisplayName.Contains(search) || x.owner.Email.Contains(search)); }
        var total = await items.CountAsync(cancellationToken);
        var rows = await items.OrderBy(x => x.property.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ownerIds = rows.Select(x => x.property.OwnerUserId).Distinct().ToList();
        var statuses = await db.MilestoneP0OwnerProfiles.AsNoTracking()
            .Where(x => x.ManagerUserId == managerId && ownerIds.Contains(x.OwnerUserId) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.OwnerUserId, x => x.Status, cancellationToken);
        var result = rows.Select(x => new P0PortfolioRowDto(
            x.property.Id,
            x.property.OwnerUserId,
            x.owner.DisplayName,
            x.owner.Email,
            statuses.GetValueOrDefault(x.property.OwnerUserId, "ACTIVE"),
            x.property.Title,
            x.property.UnitNumber,
            x.property.Address,
            x.property.Status,
            x.property.UpdatedAt)).ToList();
        return new P0PortfolioDto(result, total, page, pageSize);
    }

    public async Task<IReadOnlyList<PropertyAssignmentHistoryDto>> GetAssignmentHistoryAsync(P0Actor actor, Guid? propertyId, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, propertyId: propertyId, cancellationToken: cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var rows = await db.MilestoneManagerPropertyAssignmentHistory.AsNoTracking()
            .Where(x => x.ManagerUserId == managerId && (!propertyId.HasValue || x.PropertyId == propertyId.Value) && !x.IsDeleted)
            .OrderByDescending(x => x.ChangedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        return rows
            .Where(x => !scope.IsRestricted || scope.Allows(x.NewOwnerUserId, x.PropertyId) || (x.PreviousOwnerUserId.HasValue && scope.Allows(x.PreviousOwnerUserId, x.PropertyId)))
            .Select(x => new PropertyAssignmentHistoryDto(x.Id, x.PropertyId, x.PreviousOwnerUserId, x.NewOwnerUserId, x.ActorUserId, x.Reason, x.BatchId, x.ChangedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<PropertyDto>> AssignPropertiesAsync(P0Actor actor, P0AssignPropertiesRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, finance: false, ownerUserId: request.OwnerUserId, cancellationToken: cancellationToken);
        RequireManagerMutation(actor, managerId);
        if (request.PropertyIds.Count == 0 || string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("Select at least one property and provide a reason.");
        await EnsureOwnerScopeAsync(managerId, request.OwnerUserId, cancellationToken);
        var batchId = request.ExpectedBatchId ?? Guid.NewGuid();
        var properties = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == managerId && request.PropertyIds.Contains(x.Id) && !x.IsDeleted).ToListAsync(cancellationToken);
        if (properties.Count != request.PropertyIds.Distinct().Count()) throw new InvalidOperationException("One or more properties are not in this manager portfolio.");
        foreach (var property in properties)
        {
            if (property.OwnerUserId == request.OwnerUserId) continue;
            var previous = property.OwnerUserId; property.OwnerUserId = request.OwnerUserId; property.UpdatedAt = Now; property.UpdatedByUserId = actor.UserId;
            db.MilestoneManagerPropertyAssignmentHistory.Add(new MilestoneManagerPropertyAssignmentHistory { ManagerUserId = managerId, PropertyId = property.Id, PreviousOwnerUserId = previous, NewOwnerUserId = request.OwnerUserId, ActorUserId = actor.UserId, Reason = request.Reason.Trim(), BatchId = batchId, ChangedAt = Now });
        }
        await AuditAsync(actor.UserId, "P0PropertiesAssigned", "PropertyAssignment", batchId, request.Reason.Trim(), new { request.PropertyIds, request.OwnerUserId }, cancellationToken); await db.SaveChangesAsync(cancellationToken); return properties.Select(ToDto).ToList();
    }

    public async Task<P0AgreementDto> CreateAgreementAsync(P0Actor actor, P0CreateAgreementRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(ownerUserId: request.OwnerUserId, propertyId: request.PropertyId, actor: actor, cancellationToken: cancellationToken);
        RequireManagerMutation(actor, managerId);
        ValidateCurrency(request.Currency); ValidatePeriod(request.EffectiveFrom, request.EffectiveTo ?? request.EffectiveFrom);
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom) throw new InvalidOperationException("Agreement end cannot precede its start.");
        await EnsureOwnerScopeAsync(managerId, request.OwnerUserId, cancellationToken);
        if (request.PropertyId.HasValue) await EnsurePropertyScopeAsync(managerId, request.PropertyId.Value, request.OwnerUserId, cancellationToken);
        var version = (await db.MilestoneP0ManagementAgreements.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && x.PropertyId == request.PropertyId && !x.IsDeleted).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;
        if (request.DocumentId.HasValue && !await db.MilestoneManagerDocuments.AnyAsync(x => x.Id == request.DocumentId.Value && x.ManagerUserId == managerId && (!x.OwnerUserId.HasValue || x.OwnerUserId == request.OwnerUserId) && (!x.PropertyId.HasValue || x.PropertyId == request.PropertyId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Agreement document is outside the manager, owner or property scope."); var item = new MilestoneP0ManagementAgreement { ManagerUserId = managerId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, Version = version, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, Currency = request.Currency.Trim().ToUpperInvariant(), TermsJson = JsonOrObject(request.TermsJson), FeeRuleJson = JsonOrArray(request.FeeRuleJson), MaintenanceApprovalLimit = Positive(request.MaintenanceApprovalLimit), ExpenseApprovalLimit = Positive(request.ExpenseApprovalLimit), DocumentId = request.DocumentId, DocumentKey = request.DocumentKey?.Trim(), Status = "DRAFT" };
        db.MilestoneP0ManagementAgreements.Add(item); await AuditAsync(actor.UserId, "P0AgreementCreated", "ManagementAgreement", item.Id, "Draft agreement created", new { request.OwnerUserId, request.PropertyId, item.Version }, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<P0AgreementDto?> GetAgreementAsync(P0Actor actor, Guid agreementId, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneP0ManagementAgreements.AsNoTracking().SingleOrDefaultAsync(x => x.Id == agreementId && !x.IsDeleted, cancellationToken); if (item is null) return null;
        var managerId = await ResolveManagerAsync(actor, ownerUserId: item.OwnerUserId, propertyId: item.PropertyId, cancellationToken: cancellationToken);
        if (item.ManagerUserId != managerId) throw new UnauthorizedAccessException("Agreement is outside your portfolio.");
        return ToDto(item);
    }

    public async Task<IReadOnlyList<P0AgreementDto>> ListAgreementsAsync(P0Actor actor, P0AgreementQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var q = db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted);
        if (query.OwnerUserId.HasValue) q = q.Where(x => x.OwnerUserId == query.OwnerUserId.Value); if (query.PropertyId.HasValue) q = q.Where(x => x.PropertyId == query.PropertyId.Value); if (scope.OwnerIds.Length > 0) q = q.Where(x => scope.OwnerIds.Contains(x.OwnerUserId)); if (scope.PropertyIds.Length > 0) q = q.Where(x => x.PropertyId == null || scope.PropertyIds.Contains(x.PropertyId.Value)); if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(x => x.Status == query.Status.Trim().ToUpperInvariant());
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        return (await q.OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Version).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<P0AgreementDto?> UpdateAgreementDraftAsync(P0Actor actor, Guid agreementId, P0UpdateAgreementRequest request, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneP0ManagementAgreements.SingleOrDefaultAsync(x => x.Id == agreementId && !x.IsDeleted, cancellationToken); if (item is null) return null;
        var managerId = await ResolveManagerAsync(actor, ownerUserId: item.OwnerUserId, propertyId: item.PropertyId, cancellationToken: cancellationToken); if (managerId != item.ManagerUserId) throw new UnauthorizedAccessException("Agreement is outside your portfolio.");
        RequireManagerMutation(actor, managerId);
        if (item.Status != "DRAFT") throw new InvalidOperationException("Only draft agreements can be edited."); if (item.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Agreement changed; reload before editing."); ValidateCurrency(request.Currency); if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom) throw new InvalidOperationException("Agreement end cannot precede its start.");
        if (request.DocumentId.HasValue && !await db.MilestoneManagerDocuments.AnyAsync(x => x.Id == request.DocumentId.Value && x.ManagerUserId == item.ManagerUserId && (!x.OwnerUserId.HasValue || x.OwnerUserId == item.OwnerUserId) && (!x.PropertyId.HasValue || x.PropertyId == item.PropertyId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Agreement document is outside the manager, owner or property scope."); item.EffectiveFrom = request.EffectiveFrom; item.EffectiveTo = request.EffectiveTo; item.Currency = request.Currency.Trim().ToUpperInvariant(); item.TermsJson = JsonOrObject(request.TermsJson); item.FeeRuleJson = JsonOrArray(request.FeeRuleJson); item.MaintenanceApprovalLimit = Positive(request.MaintenanceApprovalLimit); item.ExpenseApprovalLimit = Positive(request.ExpenseApprovalLimit); item.DocumentId = request.DocumentId; item.DocumentKey = request.DocumentKey?.Trim(); item.RowVersion++;
        await AuditAsync(actor.UserId, "P0AgreementDraftUpdated", "ManagementAgreement", item.Id, "Draft agreement updated", null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<P0AgreementDto?> ActivateAgreementAsync(P0Actor actor, Guid agreementId, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneP0ManagementAgreements.SingleOrDefaultAsync(x => x.Id == agreementId && !x.IsDeleted, cancellationToken); if (item is null) return null; var managerId = await ResolveManagerAsync(actor, ownerUserId: item.OwnerUserId, propertyId: item.PropertyId, cancellationToken: cancellationToken); RequireManagerMutation(actor, managerId); if (item.Status != "DRAFT") throw new InvalidOperationException("Only draft agreements can be activated.");
        // Keep the date-range predicate in managed code.  Npgsql cannot
        // translate the reusable Overlaps helper, and evaluating the already
        // scope-filtered candidate set preserves the same conflict rule on
        // PostgreSQL and the in-memory test provider.
        var activeAgreementCandidates = await db.MilestoneP0ManagementAgreements
            .Where(x => x.Id != item.Id && x.ManagerUserId == item.ManagerUserId && x.OwnerUserId == item.OwnerUserId && x.PropertyId == item.PropertyId && x.Status == "ACTIVE" && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (activeAgreementCandidates.Any(x => Overlaps(item.EffectiveFrom, item.EffectiveTo, x.EffectiveFrom, x.EffectiveTo))) throw new InvalidOperationException("An active agreement already covers this effective period.");
        item.Status = "ACTIVE"; item.ActivatedAt = Now; item.RowVersion++; await AuditAsync(actor.UserId, "P0AgreementActivated", "ManagementAgreement", item.Id, "Agreement activated", null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<P0AgreementDto?> RenewAgreementAsync(P0Actor actor, Guid agreementId, P0RenewAgreementRequest request, CancellationToken cancellationToken)
    {
        var active = await db.MilestoneP0ManagementAgreements.SingleOrDefaultAsync(x => x.Id == agreementId && !x.IsDeleted, cancellationToken); if (active is null) return null; var managerId = await ResolveManagerAsync(actor, ownerUserId: active.OwnerUserId, propertyId: active.PropertyId, cancellationToken: cancellationToken); RequireManagerMutation(actor, managerId); if (active.Status != "ACTIVE") throw new InvalidOperationException("Only active agreements can be renewed.");
        if (request.EffectiveFrom <= active.EffectiveFrom) throw new InvalidOperationException("Renewal must start after the current agreement begins.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom) throw new InvalidOperationException("Renewal end cannot precede its start.");
        if (active.EffectiveTo.HasValue && request.EffectiveFrom < active.EffectiveTo.Value) throw new InvalidOperationException("Renewal starts before the current agreement ends.");
        // Close an open-ended active period immediately before the renewal so
        // the subsequent activation cannot overlap its superseded version.
        if (!active.EffectiveTo.HasValue || active.EffectiveTo.Value >= request.EffectiveFrom)
        {
            active.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            active.RowVersion++;
            await AuditAsync(actor.UserId, "P0AgreementPeriodClosedForRenewal", "ManagementAgreement", active.Id, request.Reason ?? "Agreement renewal", new { renewalEffectiveFrom = request.EffectiveFrom }, cancellationToken);
        }
        var item = new MilestoneP0ManagementAgreement { ManagerUserId = active.ManagerUserId, OwnerUserId = active.OwnerUserId, PropertyId = active.PropertyId, Version = active.Version + 1, SupersedesAgreementId = active.Id, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, Currency = active.Currency, TermsJson = active.TermsJson, FeeRuleJson = active.FeeRuleJson, MaintenanceApprovalLimit = active.MaintenanceApprovalLimit, ExpenseApprovalLimit = active.ExpenseApprovalLimit, DocumentId = active.DocumentId, DocumentKey = active.DocumentKey, Status = "DRAFT" };
        db.MilestoneP0ManagementAgreements.Add(item); await AuditAsync(actor.UserId, "P0AgreementRenewalDrafted", "ManagementAgreement", item.Id, request.Reason ?? "Agreement renewal", new { supersedes = active.Id }, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<P0AgreementDto?> TerminateAgreementAsync(P0Actor actor, Guid agreementId, P0TerminateAgreementRequest request, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneP0ManagementAgreements.SingleOrDefaultAsync(x => x.Id == agreementId && !x.IsDeleted, cancellationToken); if (item is null) return null; var managerId = await ResolveManagerAsync(actor, ownerUserId: item.OwnerUserId, propertyId: item.PropertyId, cancellationToken: cancellationToken); RequireManagerMutation(actor, managerId); if (item.Status != "ACTIVE") throw new InvalidOperationException("Only active agreements can be terminated."); if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A termination reason is required.");
        item.Status = "TERMINATED"; item.TerminatedAt = Now; item.TerminationReason = request.Reason.Trim(); item.RowVersion++; await AuditAsync(actor.UserId, "P0AgreementTerminated", "ManagementAgreement", item.Id, item.TerminationReason, null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<P0FeeRuleDto> CreateFeeRuleAsync(P0Actor actor, P0CreateFeeRuleRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: request.OwnerUserId, propertyId: request.PropertyId, cancellationToken: cancellationToken); ValidateCurrency(request.Currency); ValidatePeriod(request.EffectiveFrom, request.EffectiveTo ?? request.EffectiveFrom); if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom) throw new InvalidOperationException("Fee-rule end cannot precede its start.");
        await using var transaction = await BeginP0TransactionAsync(cancellationToken);
        RequireManagerMutation(actor, managerId);
        var type = request.RuleType.Trim().ToUpperInvariant(); var basis = request.CalculationBasis.Trim().ToUpperInvariant(); var category = request.Category.Trim().ToUpperInvariant(); if (!new[] { "PERCENTAGE", "FIXED", "MINIMUM", "MARKUP", "COMBINED" }.Contains(type)) throw new InvalidOperationException("Unsupported fee-rule type."); if (!new[] { "COLLECTED_RENT", "INVOICED_RENT", "EXPENSE" }.Contains(basis)) throw new InvalidOperationException("Unsupported fee basis."); if (request.Percentage < 0 || request.Percentage > 100 || request.FixedAmount < 0 || request.MinimumAmount < 0 || request.CleaningMarkup < 0 || request.MaintenanceMarkup < 0) throw new InvalidOperationException("Fee values must be non-negative.");
        await EnsureOwnerScopeAsync(managerId, request.OwnerUserId, cancellationToken); if (request.PropertyId.HasValue) await EnsurePropertyScopeAsync(managerId, request.PropertyId.Value, request.OwnerUserId, cancellationToken);
        var feeRuleCandidates = await db.MilestoneP0ManagementFeeRules
            .Where(x => x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && x.PropertyId == request.PropertyId && x.Category == category && x.Currency == request.Currency.Trim().ToUpperInvariant() && x.IsActive && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (feeRuleCandidates.Any(x => Overlaps(x.EffectiveFrom, x.EffectiveTo, request.EffectiveFrom, request.EffectiveTo))) throw new InvalidOperationException("Fee-rule effective periods overlap for this scope.");
         var item = new MilestoneP0ManagementFeeRule { ManagerUserId = managerId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, Category = category, RuleType = type, CalculationBasis = basis, Currency = request.Currency.Trim().ToUpperInvariant(), Percentage = request.Percentage, FixedAmount = request.FixedAmount, MinimumAmount = request.MinimumAmount, CleaningMarkup = request.CleaningMarkup, MaintenanceMarkup = request.MaintenanceMarkup, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo }; db.MilestoneP0ManagementFeeRules.Add(item); await AuditAsync(actor.UserId, "P0FeeRuleCreated", "ManagementFeeRule", item.Id, "Fee rule created", null, cancellationToken); await db.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return ToDto(item);
    }

    public async Task<IReadOnlyList<P0FeeRuleDto>> ListFeeRulesAsync(P0Actor actor, P0FeeRuleQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken); var scope = await ResolveScopeAsync(actor, managerId, cancellationToken); var q = db.MilestoneP0ManagementFeeRules.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted); if (query.OwnerUserId.HasValue) q = q.Where(x => x.OwnerUserId == query.OwnerUserId.Value); if (query.PropertyId.HasValue) q = q.Where(x => x.PropertyId == query.PropertyId.Value); if (scope.OwnerIds.Length > 0) q = q.Where(x => scope.OwnerIds.Contains(x.OwnerUserId)); if (scope.PropertyIds.Length > 0) q = q.Where(x => x.PropertyId == null || scope.PropertyIds.Contains(x.PropertyId.Value)); if (!string.IsNullOrWhiteSpace(query.Currency)) q = q.Where(x => x.Currency == query.Currency.Trim().ToUpperInvariant()); if (!string.IsNullOrWhiteSpace(query.Category)) q = q.Where(x => x.Category == query.Category.Trim().ToUpperInvariant()); if (query.On.HasValue) q = q.Where(x => x.EffectiveFrom <= query.On.Value && (x.EffectiveTo == null || x.EffectiveTo >= query.On.Value)); return (await q.OrderBy(x => x.Category).ThenBy(x => x.EffectiveFrom).Take(300).ToListAsync(cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<P0FeeCalculationDto> CalculateFeeAsync(P0Actor actor, P0CalculateFeeRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: request.OwnerUserId, propertyId: request.PropertyId, cancellationToken: cancellationToken); ValidateCurrency(request.Currency); if (request.BaseAmount < 0) throw new InvalidOperationException("Base amount cannot be negative.");
        var rule = await SelectFeeRuleAsync(managerId, request.OwnerUserId, request.PropertyId, request.Category, request.Currency, request.On, cancellationToken); return Calculate(rule, request);
    }

    public async Task<P0FeeCalculationDto> PostFeeAsync(P0Actor actor, P0PostFeeRequest request, CancellationToken cancellationToken)
    {
        // Authorise the mutation before returning a zero-value calculation;
        // otherwise an unauthorised staff member could probe fee rules through
        // the posting endpoint without meeting the finance capability check.
        var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: request.OwnerUserId, propertyId: request.PropertyId, cancellationToken: cancellationToken);
        var calc = await CalculateFeeAsync(actor, new P0CalculateFeeRequest(request.OwnerUserId, request.PropertyId, request.Category, request.Currency, request.BaseAmount, request.On, request.SourceId), cancellationToken); if (calc.CalculatedAmount <= 0) return calc;
        var ownerFunds = await EnsureAccountAsync(managerId, request.OwnerUserId, null, request.Currency, "OWNER_FUNDS", "Owner client funds", "LIABILITY", true, false, false, cancellationToken); var feeRevenue = await EnsureAccountAsync(managerId, null, null, request.Currency, "PM_FEE_REVENUE", "Property manager fee revenue", "REVENUE", false, true, false, cancellationToken);
        var journal = await PostJournalCoreAsync(actor, managerId, new P0PostJournalRequest("MANAGEMENT_FEE", request.SourceId, request.IdempotencyKey, request.Currency, request.On, $"Management fee: {request.Category}", [new P0JournalLineRequest(ownerFunds.Code, calc.CalculatedAmount, 0, request.OwnerUserId, request.PropertyId, "Owner funds charged"), new P0JournalLineRequest(feeRevenue.Code, 0, calc.CalculatedAmount, null, null, "PM fee revenue")], false, null, null), cancellationToken); return calc with { Posted = true, JournalId = journal.Id };
    }

    private async Task<MilestoneP0ManagementFeeRule?> SelectFeeRuleAsync(Guid managerId, Guid ownerId, Guid? propertyId, string category, string currency, DateOnly on, CancellationToken cancellationToken)
    {
        var normalized = category.Trim().ToUpperInvariant(); var cur = currency.Trim().ToUpperInvariant(); var rules = await db.MilestoneP0ManagementFeeRules.AsNoTracking().Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerId && x.Currency == cur && x.Category == normalized && x.IsActive && !x.IsDeleted && x.EffectiveFrom <= on && (x.EffectiveTo == null || x.EffectiveTo >= on) && (x.PropertyId == propertyId || x.PropertyId == null)).OrderByDescending(x => x.PropertyId.HasValue).ThenByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken); return rules.FirstOrDefault();
    }

    private static P0FeeCalculationDto Calculate(MilestoneP0ManagementFeeRule? rule, P0CalculateFeeRequest request)
    {
        if (rule is null) return new P0FeeCalculationDto(null, request.Category, request.Currency.ToUpperInvariant(), request.BaseAmount, 0, 0, 0, 0, 0, request.On, request.SourceId, false, null);
        var percentage = rule.RuleType is "PERCENTAGE" or "COMBINED" ? Round(request.BaseAmount * rule.Percentage / 100m) : 0m; var fixedAmount = rule.RuleType is "FIXED" or "COMBINED" ? rule.FixedAmount : 0m; var markup = rule.RuleType is "MARKUP" or "COMBINED" ? Round(request.BaseAmount * (rule.CleaningMarkup + rule.MaintenanceMarkup) / 100m) : 0m; var beforeMinimum = percentage + fixedAmount + markup; var minimumTopUp = rule.MinimumAmount > beforeMinimum ? rule.MinimumAmount - beforeMinimum : 0m; return new P0FeeCalculationDto(rule.Id, rule.Category, rule.Currency, request.BaseAmount, Round(beforeMinimum + minimumTopUp), percentage, fixedAmount, minimumTopUp, markup, request.On, request.SourceId, false, null);
    }

    public async Task<IReadOnlyList<P0AccountDto>> ListAccountsAsync(P0Actor actor, string? currency, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, cancellationToken: cancellationToken); var scope = await ResolveScopeAsync(actor, managerId, cancellationToken); if (!string.IsNullOrWhiteSpace(currency)) ValidateCurrency(currency);
        var q = db.MilestoneP0Accounts.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted); if (scope.OwnerIds.Length > 0) q = q.Where(x => x.OwnerUserId == null || scope.OwnerIds.Contains(x.OwnerUserId.Value)); if (scope.PropertyIds.Length > 0) q = q.Where(x => x.PropertyId == null || scope.PropertyIds.Contains(x.PropertyId.Value)); if (!string.IsNullOrWhiteSpace(currency)) q = q.Where(x => x.Currency == currency.Trim().ToUpperInvariant());
        var current = await q.OrderBy(x => x.Currency).ThenBy(x => x.Code).ToListAsync(cancellationToken); if (current.Count == 0) { await EnsureBaseAccountsAsync(managerId, currency?.Trim().ToUpperInvariant() ?? "JMD", cancellationToken); current = await q.OrderBy(x => x.Currency).ThenBy(x => x.Code).ToListAsync(cancellationToken); } return current.Select(ToDto).ToList();
    }

    public async Task<P0JournalDto> PostJournalAsync(P0Actor actor, P0PostJournalRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, finance: true, cancellationToken: cancellationToken);
        return await PostJournalCoreAsync(actor, managerId, request, cancellationToken);
    }

    private async Task<P0JournalDto> PostJournalCoreAsync(P0Actor actor, Guid managerId, P0PostJournalRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginP0TransactionAsync(cancellationToken);
        ValidateCurrency(request.Currency); if (string.IsNullOrWhiteSpace(request.SourceType) || string.IsNullOrWhiteSpace(request.Memo)) throw new InvalidOperationException("Journal source and memo are required."); if (request.Lines is null || request.Lines.Count is < 2 or > 50) throw new InvalidOperationException("A journal requires between two and fifty lines.");
        var source = request.SourceType.Trim().ToUpperInvariant();
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        await EnsureRequestScopeAsync(scope, managerId, request.Lines, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var duplicate = await db.MilestoneP0Journals.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.IdempotencyKey == request.IdempotencyKey.Trim() && !x.IsDeleted, cancellationToken); if (duplicate is not null) { if (!await JournalPayloadMatchesAsync(duplicate, request, source, cancellationToken)) throw new InvalidOperationException("The idempotency key was already used for a different journal request."); return await ToDtoAsync(duplicate, cancellationToken); }
        }
        await EnsureBaseAccountsAsync(managerId, request.Currency.Trim().ToUpperInvariant(), cancellationToken);
        var accountRows = new List<(P0JournalLineRequest Request, MilestoneP0Account Account)>(); decimal debit = 0, credit = 0;
        foreach (var line in request.Lines)
        {
            if (line.Debit < 0 || line.Credit < 0 || (line.Debit > 0 && line.Credit > 0) || (line.Debit == 0 && line.Credit == 0)) throw new InvalidOperationException("Each journal line must contain either a positive debit or a positive credit.");
            if (line.Debit != decimal.Round(line.Debit, 2) || line.Credit != decimal.Round(line.Credit, 2)) throw new InvalidOperationException("Journal amounts support at most two decimal places.");
            var account = await FindOrCreateAccountByCodeAsync(managerId, line.AccountCode, request.Currency, line.OwnerUserId, line.PropertyId, cancellationToken);
            if (account.Currency != request.Currency.Trim().ToUpperInvariant()) throw new InvalidOperationException("Journal lines cannot mix currencies.");
            if (account.OwnerUserId.HasValue && account.OwnerUserId != line.OwnerUserId) throw new InvalidOperationException("Owner dimension does not match account.");
            if (account.PropertyId.HasValue && account.PropertyId != line.PropertyId) throw new InvalidOperationException("Property dimension does not match account.");
            if (line.OwnerUserId.HasValue) { await EnsureOwnerScopeAsync(managerId, line.OwnerUserId.Value, cancellationToken); if (line.PropertyId.HasValue) await EnsurePropertyScopeAsync(managerId, line.PropertyId.Value, line.OwnerUserId, cancellationToken); }
            debit += line.Debit; credit += line.Credit; accountRows.Add((line, account));
        }
        debit = Round(debit); credit = Round(credit); if (debit <= 0 || debit != credit) throw new InvalidOperationException("Journal debits and credits must balance to a positive amount.");
        await EnsureStaffLimitAsync(actor, managerId, debit, cancellationToken);
        if (source is "OWNER_EXPENSE" or "THIRD_PARTY_EXPENSE" or "MAINTENANCE_EXPENSE" or "UTILITY_EXPENSE")
        {
            var owner = request.Lines.Select(x => x.OwnerUserId).FirstOrDefault(x => x.HasValue); if (!owner.HasValue) throw new InvalidOperationException("Owner expense journals require an owner dimension.");
            var property = request.Lines.Select(x => x.PropertyId).FirstOrDefault(x => x.HasValue); var threshold = await GetApprovalThresholdAsync(managerId, owner.Value, property, source, request.AccountingDate, cancellationToken);
            if (debit > threshold)
            {
                if (!request.ApprovalId.HasValue) throw new InvalidOperationException("An approved owner approval is required before posting this expense.");
                var approval = await db.MilestoneP0Approvals.SingleOrDefaultAsync(x => x.Id == request.ApprovalId && x.ManagerUserId == managerId && x.OwnerUserId == owner.Value && x.Status == "APPROVED" && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Approval is missing or not approved for this expense.");
                if (approval.Amount < debit || approval.Currency != request.Currency.Trim().ToUpperInvariant()) throw new InvalidOperationException("Approval amount or currency does not cover this expense.");
            }
        }
        var journal = new MilestoneP0Journal { ManagerUserId = managerId, JournalNumber = $"P0-{Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..24], SourceType = source, SourceId = request.SourceId, IdempotencyKey = request.IdempotencyKey?.Trim(), Currency = request.Currency.Trim().ToUpperInvariant(), AccountingDate = request.AccountingDate, Memo = request.Memo.Trim(), Status = "POSTED", ReconciliationStatus = request.Reconcile ? "RECONCILED" : "UNRECONCILED", TotalDebit = debit, TotalCredit = credit, ReversalOfJournalId = source == "REVERSAL" ? request.SourceId : null, PostedByUserId = actor.UserId, PostedAt = Now };
        db.MilestoneP0Journals.Add(journal); foreach (var (line, account) in accountRows) db.MilestoneP0JournalLines.Add(new MilestoneP0JournalLine { JournalId = journal.Id, AccountId = account.Id, OwnerUserId = line.OwnerUserId ?? account.OwnerUserId, PropertyId = line.PropertyId ?? account.PropertyId, Debit = line.Debit, Credit = line.Credit, Description = line.Description?.Trim() ?? string.Empty, SourceReference = request.SourceId?.ToString() ?? journal.JournalNumber });
         if (request.Reconcile) db.MilestoneP0Reconciliations.Add(new MilestoneP0Reconciliation { ManagerUserId = managerId, JournalId = journal.Id, ExternalReference = request.ReconciliationReference?.Trim() ?? journal.JournalNumber, Status = "MATCHED", Amount = debit, Currency = request.Currency.Trim().ToUpperInvariant(), Reason = "Reconciled at posting", ActorUserId = actor.UserId, ReconciledAt = Now });
         await AuditAsync(actor.UserId, "P0JournalPosted", "Journal", journal.Id, journal.Memo, new { source, debit, credit, currency = journal.Currency }, cancellationToken); await db.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return await ToDtoAsync(journal, cancellationToken);
    }

    private async Task<bool> JournalPayloadMatchesAsync(MilestoneP0Journal existing, P0PostJournalRequest request, string source, CancellationToken cancellationToken)
    {
        if (existing.SourceType != source || existing.SourceId != request.SourceId || existing.Currency != request.Currency.Trim().ToUpperInvariant() || existing.AccountingDate != request.AccountingDate || existing.Memo != request.Memo.Trim()) return false;
        var persisted = await (from line in db.MilestoneP0JournalLines.AsNoTracking()
                               join account in db.MilestoneP0Accounts.AsNoTracking() on line.AccountId equals account.Id
                               where line.JournalId == existing.Id && !line.IsDeleted
                               select new { account.Code, line.OwnerUserId, line.PropertyId, line.Debit, line.Credit }).ToListAsync(cancellationToken);
        var requested = request.Lines.Select(line => new { Code = line.AccountCode.Trim().ToUpperInvariant(), line.OwnerUserId, line.PropertyId, line.Debit, line.Credit }).ToList();
        if (persisted.Count != requested.Count || request.Reconcile != (existing.ReconciliationStatus == "RECONCILED" || await db.MilestoneP0Reconciliations.AnyAsync(x => x.JournalId == existing.Id && !x.IsDeleted, cancellationToken))) return false;
        return persisted.OrderBy(x => x.Code).ThenBy(x => x.OwnerUserId).ThenBy(x => x.PropertyId).ThenBy(x => x.Debit).ThenBy(x => x.Credit).Select(x => $"{x.Code}|{x.OwnerUserId}|{x.PropertyId}|{x.Debit:0.00}|{x.Credit:0.00}")
            .SequenceEqual(requested.OrderBy(x => x.Code).ThenBy(x => x.OwnerUserId).ThenBy(x => x.PropertyId).ThenBy(x => x.Debit).ThenBy(x => x.Credit).Select(x => $"{x.Code}|{x.OwnerUserId}|{x.PropertyId}|{x.Debit:0.00}|{x.Credit:0.00}"));
    }

    private async Task<MilestoneP0Account> FindOrCreateAccountByCodeAsync(Guid managerId, string code, string currency, Guid? ownerId, Guid? propertyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Account code is required."); var normalized = code.Trim().ToUpperInvariant(); var existing = await db.MilestoneP0Accounts.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.Code == normalized && !x.IsDeleted, cancellationToken); if (existing is not null) return existing;
        var parts = normalized.Split(':', StringSplitOptions.RemoveEmptyEntries); if (parts.Length == 0) throw new InvalidOperationException("Unknown account code."); var prefix = parts[0]; if (parts[^1] != currency.Trim().ToUpperInvariant()) throw new InvalidOperationException("Account currency does not match journal currency.");
        Guid? accountOwner = ownerId; Guid? accountProperty = propertyId; string name; string type; bool client = false, pm = false, third = false;
        if (prefix is "CASH_CLEARING" or "PM_FEE_REVENUE" or "PM_OPERATING_EXPENSE" or "SUSPENSE") { if (parts.Length != 2) throw new InvalidOperationException("Malformed manager account code."); name = prefix switch { "CASH_CLEARING" => "Cash clearing", "PM_FEE_REVENUE" => "Property manager fee revenue", "PM_OPERATING_EXPENSE" => "Property manager operating expense", _ => "Unreconciled suspense" }; type = prefix == "PM_FEE_REVENUE" ? "REVENUE" : prefix == "PM_OPERATING_EXPENSE" ? "EXPENSE" : prefix == "SUSPENSE" ? "LIABILITY" : "ASSET"; pm = prefix != "SUSPENSE"; third = prefix == "SUSPENSE"; accountOwner = null; accountProperty = null; }
        else if (prefix is "OWNER_FUNDS" or "OWNER_FUNDS_RESERVED" or "OWNER_RECEIVABLE") { if (parts.Length != 3 || !Guid.TryParse(parts[1], out var parsedOwner)) throw new InvalidOperationException("Malformed owner account code."); accountOwner = parsedOwner; accountProperty = null; name = prefix == "OWNER_RECEIVABLE" ? "Owner receivable" : prefix == "OWNER_FUNDS_RESERVED" ? "Reserved owner funds" : "Owner client funds"; type = prefix == "OWNER_RECEIVABLE" ? "ASSET" : "LIABILITY"; client = prefix != "OWNER_RECEIVABLE"; }
        else if (prefix is "OWNER_INCOME" or "OWNER_EXPENSE" or "THIRD_PARTY_PAYABLE") { if (parts.Length != 4 || !Guid.TryParse(parts[1], out var parsedOwner)) throw new InvalidOperationException("Malformed owner account code."); accountOwner = parsedOwner; if (parts[2] != "ALL") { if (!Guid.TryParse(parts[2], out var parsedProperty)) throw new InvalidOperationException("Malformed property account code."); accountProperty = parsedProperty; } name = prefix == "OWNER_INCOME" ? "Owner income" : prefix == "OWNER_EXPENSE" ? "Owner expense" : "Third-party payable"; type = prefix == "OWNER_INCOME" ? "LIABILITY" : prefix == "OWNER_EXPENSE" ? "EXPENSE" : "LIABILITY"; client = prefix != "THIRD_PARTY_PAYABLE"; third = prefix == "THIRD_PARTY_PAYABLE"; }
        else throw new InvalidOperationException("Unknown account code.");
        if (ownerId.HasValue && accountOwner.HasValue && ownerId != accountOwner) throw new InvalidOperationException("Account owner does not match line owner."); if (propertyId.HasValue && accountProperty.HasValue && propertyId != accountProperty) throw new InvalidOperationException("Account property does not match line property."); if (accountOwner.HasValue) await EnsureOwnerScopeAsync(managerId, accountOwner.Value, cancellationToken); if (accountProperty.HasValue) await EnsurePropertyScopeAsync(managerId, accountProperty.Value, accountOwner, cancellationToken);
        return await EnsureAccountAsync(managerId, accountOwner, accountProperty, currency, prefix, name, type, client, pm, third, cancellationToken);
    }

    public async Task<IReadOnlyList<P0JournalDto>> ListJournalsAsync(P0Actor actor, P0JournalQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var q = db.MilestoneP0Journals.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted);
        if (query.OwnerUserId.HasValue) q = q.Where(x => db.MilestoneP0JournalLines.Any(l => l.JournalId == x.Id && l.OwnerUserId == query.OwnerUserId.Value && !l.IsDeleted));
        if (query.PropertyId.HasValue) q = q.Where(x => db.MilestoneP0JournalLines.Any(l => l.JournalId == x.Id && l.PropertyId == query.PropertyId.Value && !l.IsDeleted));
        if (!string.IsNullOrWhiteSpace(query.Currency)) q = q.Where(x => x.Currency == query.Currency.Trim().ToUpperInvariant());
        if (query.From.HasValue) q = q.Where(x => x.AccountingDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(x => x.AccountingDate <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(x => x.Status == query.Status.Trim().ToUpperInvariant());
        // Load a bounded, manager-scoped window before applying staff scope to
        // the journal lines.  This keeps line-level dimensions private while
        // preserving correct page semantics for restricted team members.
        var rows = await q.OrderByDescending(x => x.AccountingDate).ThenByDescending(x => x.CreatedAt).Take(500).ToListAsync(cancellationToken);
        var result = new List<P0JournalDto>();
        foreach (var row in rows)
        {
            var dto = await ToDtoAsync(row, cancellationToken, scope);
            if (!scope.IsRestricted || dto.Lines.Count > 0) result.Add(dto);
        }
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        return result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public async Task<P0JournalDto?> ReverseJournalAsync(P0Actor actor, Guid journalId, P0ReverseJournalRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginP0LockedCommandTransactionAsync(cancellationToken);
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-p0:journal-reversal:{journalId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken); }
        var original = await db.MilestoneP0Journals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == journalId && !x.IsDeleted, cancellationToken); if (original is null) return null; var managerId = await ResolveManagerAsync(actor, finance: true, cancellationToken: cancellationToken); if (original.ManagerUserId != managerId) throw new UnauthorizedAccessException("Journal is outside your portfolio."); await EnsureStaffLimitAsync(actor, managerId, original.TotalDebit, cancellationToken); if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("A reversal reason and idempotency key are required."); if (original.SourceType == "REVERSAL") throw new InvalidOperationException("A reversal journal cannot itself be reversed.");
        var existingReversal = await db.MilestoneP0Journals.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.ReversalOfJournalId == journalId && !x.IsDeleted, cancellationToken);
        if (existingReversal is not null)
        {
            if (existingReversal.IdempotencyKey != request.IdempotencyKey.Trim() || existingReversal.Memo != request.Reason.Trim()) throw new InvalidOperationException("This journal has already been reversed by a different request.");
            return await ToDtoAsync(existingReversal, cancellationToken);
        }
        var lines = await db.MilestoneP0JournalLines.AsNoTracking().Where(x => x.JournalId == journalId && !x.IsDeleted).Join(db.MilestoneP0Accounts, line => line.AccountId, account => account.Id, (line, account) => new P0JournalLineRequest(account.Code, line.Credit, line.Debit, line.OwnerUserId ?? account.OwnerUserId, line.PropertyId ?? account.PropertyId, $"Reversal: {line.Description}")).ToListAsync(cancellationToken); var result = await PostJournalCoreAsync(actor, managerId, new P0PostJournalRequest("REVERSAL", journalId, request.IdempotencyKey, original.Currency, original.AccountingDate, request.Reason.Trim(), lines, false, null, null), cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return result;
    }

    public async Task<P0ReconciliationDto> ReconcileJournalAsync(P0Actor actor, P0ReconcileJournalRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, finance: true, cancellationToken: cancellationToken);
        await using var transaction = await BeginP0TransactionAsync(cancellationToken);
        ValidateCurrency(request.Currency);
        if (string.IsNullOrWhiteSpace(request.ExternalReference) || string.IsNullOrWhiteSpace(request.Reason) || request.Amount <= 0)
            throw new InvalidOperationException("A positive amount, external reference and reconciliation reason are required.");
        var journal = await db.MilestoneP0Journals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.JournalId && x.ManagerUserId == managerId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Journal not found.");
        if (journal.Currency != request.Currency.Trim().ToUpperInvariant()) throw new InvalidOperationException("Reconciliation currency does not match journal.");
        if (request.Amount != journal.TotalDebit) throw new InvalidOperationException("Reconciliation amount must equal the balanced journal total.");
        var journalLines = await db.MilestoneP0JournalLines.AsNoTracking().Where(x => x.JournalId == journal.Id && !x.IsDeleted).Join(db.MilestoneP0Accounts, line => line.AccountId, account => account.Id, (line, account) => new P0JournalLineRequest(account.Code, line.Debit, line.Credit, line.OwnerUserId ?? account.OwnerUserId, line.PropertyId ?? account.PropertyId)).ToListAsync(cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        await EnsureRequestScopeAsync(scope, managerId, journalLines, cancellationToken);
        await EnsureStaffLimitAsync(actor, managerId, request.Amount, cancellationToken);
        if (await db.MilestoneP0Reconciliations.AnyAsync(x => x.JournalId == journal.Id && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Journal is already reconciled.");
        var item = new MilestoneP0Reconciliation { ManagerUserId = managerId, JournalId = journal.Id, ExternalReference = request.ExternalReference.Trim(), Status = "MATCHED", Amount = request.Amount, Currency = request.Currency.Trim().ToUpperInvariant(), Reason = request.Reason.Trim(), ActorUserId = actor.UserId, ReconciledAt = Now };
        db.MilestoneP0Reconciliations.Add(item);
        await AuditAsync(actor.UserId, "P0JournalReconciled", "Journal", journal.Id, request.Reason.Trim(), new { request.ExternalReference }, cancellationToken); await db.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return ToDto(item);
    }

    public async Task<IReadOnlyList<P0ReconciliationDto>> ListReconciliationsAsync(P0Actor actor, P0JournalQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var q = db.MilestoneP0Reconciliations.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted);
        if (query.OwnerUserId.HasValue)
            q = q.Where(x => db.MilestoneP0JournalLines.Any(line => line.JournalId == x.JournalId && line.OwnerUserId == query.OwnerUserId.Value && !line.IsDeleted));
        if (query.PropertyId.HasValue)
            q = q.Where(x => db.MilestoneP0JournalLines.Any(line => line.JournalId == x.JournalId && line.PropertyId == query.PropertyId.Value && !line.IsDeleted));
        if (!string.IsNullOrWhiteSpace(query.Currency)) q = q.Where(x => x.Currency == query.Currency.Trim().ToUpperInvariant());
        var rows = await q.OrderByDescending(x => x.ReconciledAt).Take(500).ToListAsync(cancellationToken);
        if (!scope.IsRestricted) return rows.Select(ToDto).ToList();
        var journalIds = rows.Select(x => x.JournalId).ToList();
        var visibleJournalIds = (await (from line in db.MilestoneP0JournalLines.AsNoTracking()
                                        join account in db.MilestoneP0Accounts.AsNoTracking() on line.AccountId equals account.Id
                                        where journalIds.Contains(line.JournalId) && !line.IsDeleted
                                        select new { line.JournalId, OwnerUserId = line.OwnerUserId ?? account.OwnerUserId, PropertyId = line.PropertyId ?? account.PropertyId }).ToListAsync(cancellationToken))
            .Where(x => (x.OwnerUserId.HasValue || x.PropertyId.HasValue) && scope.Allows(x.OwnerUserId, x.PropertyId))
            .Select(x => x.JournalId)
            .ToHashSet();
        return rows.Where(x => visibleJournalIds.Contains(x.JournalId)).Select(ToDto).ToList();
    }

    private async Task<List<(MilestoneP0Journal Journal, List<(MilestoneP0JournalLine Line, MilestoneP0Account Account)> Lines, bool Reconciled)>> LoadJournalBundlesAsync(Guid managerId, Guid? ownerId, Guid? propertyId, string currency, DateOnly? from, DateOnly? to, CancellationToken cancellationToken, P0Scope? scope = null)
    {
        var query = db.MilestoneP0Journals.AsNoTracking()
            .Where(x => x.ManagerUserId == managerId && x.Currency == currency && x.Status == "POSTED" && !x.IsDeleted);
        if (from.HasValue) query = query.Where(x => x.AccountingDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.AccountingDate <= to.Value);

        var journals = await query
            .OrderBy(x => x.AccountingDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var journalIds = journals.Select(x => x.Id).ToList();
        var lines = await db.MilestoneP0JournalLines.AsNoTracking()
            .Where(x => journalIds.Contains(x.JournalId) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        var accountIds = lines.Select(x => x.AccountId).Distinct().ToList();
        var accounts = await db.MilestoneP0Accounts.AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var reconciled = await db.MilestoneP0Reconciliations.AsNoTracking()
            .Where(x => journalIds.Contains(x.JournalId) && !x.IsDeleted)
            .Select(x => x.JournalId)
            .ToHashSetAsync(cancellationToken);

        var result = new List<(MilestoneP0Journal, List<(MilestoneP0JournalLine, MilestoneP0Account)>, bool)>();
        foreach (var journal in journals)
        {
            var journalLines = lines
                .Where(x => x.JournalId == journal.Id)
                .Select(x => (Line: x, Account: accounts[x.AccountId]))
                .ToList();

            // A manager-level cash/fee/suspense line has no owner dimension.
            // Include it only when this journal also contains a dimensional
            // line matching the requested owner/property.  This prevents a
            // statement for owner A from inheriting PM fees or cash from owner B.
            var hasMatchingDimension = !ownerId.HasValue && !propertyId.HasValue
                ? true
                : journalLines.Any(item =>
                {
                    var lineOwner = item.Line.OwnerUserId ?? item.Account.OwnerUserId;
                    var lineProperty = item.Line.PropertyId ?? item.Account.PropertyId;
                    var ownerMatches = !ownerId.HasValue || lineOwner == ownerId.Value;
                    var propertyMatches = !propertyId.HasValue || lineProperty == propertyId.Value;
                    return (lineOwner.HasValue || lineProperty.HasValue) && ownerMatches && propertyMatches;
                });
            if (!hasMatchingDimension) continue;

            var selected = journalLines
                .Where(item =>
                {
                    var lineOwner = item.Line.OwnerUserId ?? item.Account.OwnerUserId;
                    var lineProperty = item.Line.PropertyId ?? item.Account.PropertyId;
                    var dimensional = lineOwner.HasValue || lineProperty.HasValue;
                    if (!dimensional) return true;
                    var ownerMatches = !ownerId.HasValue || lineOwner == ownerId.Value;
                    var propertyMatches = !propertyId.HasValue || lineProperty == propertyId.Value;
                    return ownerMatches && propertyMatches;
                })
                .ToList();

            if (scope is { IsRestricted: true })
            {
                var visibleDimensional = selected
                    .Where(item =>
                    {
                        var lineOwner = item.Line.OwnerUserId ?? item.Account.OwnerUserId;
                        var lineProperty = item.Line.PropertyId ?? item.Account.PropertyId;
                        return (lineOwner.HasValue || lineProperty.HasValue) && scope.Allows(lineOwner, lineProperty);
                    })
                    .ToList();
                if (visibleDimensional.Count == 0) continue;

                selected = selected
                    .Where(item =>
                    {
                        var lineOwner = item.Line.OwnerUserId ?? item.Account.OwnerUserId;
                        var lineProperty = item.Line.PropertyId ?? item.Account.PropertyId;
                        return !(lineOwner.HasValue || lineProperty.HasValue) || scope.Allows(lineOwner, lineProperty);
                    })
                    .ToList();
            }

            if (selected.Count > 0) result.Add((journal, selected, reconciled.Contains(journal.Id)));
        }

        return result;
    }

    private async Task<bool> HasUnresolvedSuspenseAsync(Guid managerId, string currency, DateOnly through, CancellationToken cancellationToken)
    {
        // Suspense is manager-global: even an owner/property statement must
        // stop while an unidentified cash balance remains unresolved.  Do not
        // apply the owner filter here, otherwise a legacy suspense journal
        // with no dimensions would disappear from the check.
        var bundles = await LoadJournalBundlesAsync(managerId, null, null, currency, null, through, cancellationToken);
        // Suspense is unresolved while its signed balance is non-zero.  A
        // later, auditable reclassification journal debits the suspense
        // account and therefore clears the queue without mutating history.
        var balance = bundles
            .SelectMany(bundle => bundle.Lines)
            .Where(item => item.Account.Code.StartsWith("SUSPENSE:", StringComparison.Ordinal))
            .Sum(item => item.Line.Debit - item.Line.Credit);
        return Round(balance) != 0m;
    }

    private static (decimal Income, decimal Expenses, decimal Fees, decimal Payouts, decimal Net, string Description) SummarizeBundle(MilestoneP0Journal journal, IReadOnlyList<(MilestoneP0JournalLine Line, MilestoneP0Account Account)> lines)
    {
        decimal income = 0, expenses = 0, fees = 0, payouts = 0;
        foreach (var (line, account) in lines)
        {
            var amount = line.Debit > 0 ? line.Debit : line.Credit;
            if (account.Code.StartsWith("OWNER_INCOME:", StringComparison.Ordinal) || (journal.SourceType is "RENT_RECEIPT" or "OWNER_INCOME") && line.OwnerUserId.HasValue) income += line.Credit - line.Debit;
            else if (account.Code.StartsWith("OWNER_EXPENSE:", StringComparison.Ordinal) || (journal.SourceType is "OWNER_EXPENSE" or "THIRD_PARTY_EXPENSE" or "MAINTENANCE_EXPENSE" or "UTILITY_EXPENSE") && line.OwnerUserId.HasValue) expenses += line.Debit - line.Credit;
            else if (account.Code.StartsWith("PM_FEE_REVENUE", StringComparison.Ordinal) || journal.SourceType == "MANAGEMENT_FEE") fees += line.Credit - line.Debit;
            else if ((journal.SourceType == "OWNER_PAYOUT" || journal.SourceType == "REVERSAL") && account.Code.StartsWith("OWNER_FUNDS", StringComparison.Ordinal)) payouts += line.Debit - line.Credit;
            _ = amount;
        }
        if (journal.SourceType == "MANAGEMENT_FEE")
            fees = lines.Where(x => x.Account.Code.StartsWith("PM_FEE_REVENUE", StringComparison.Ordinal)).Sum(x => x.Line.Credit - x.Line.Debit);
        // Keep signed values.  A reversal carries the opposite side of the
        // original account and must reduce income, expense, fee or payout
        // totals rather than being clamped to zero.
        income = Round(income); expenses = Round(expenses); fees = Round(fees); payouts = Round(payouts); return (income, expenses, fees, payouts, Round(income - expenses - fees - payouts), journal.Memo);
    }

    public async Task<P0StatementDto> BuildStatementAsync(P0Actor actor, P0StatementQuery query, CancellationToken cancellationToken)
    {
        ValidateCurrency(query.Currency); ValidatePeriod(query.From, query.To); var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken); var scope = await ResolveScopeAsync(actor, managerId, cancellationToken); var previous = await LoadJournalBundlesAsync(managerId, query.OwnerUserId, query.PropertyId, query.Currency.Trim().ToUpperInvariant(), null, query.From.AddDays(-1), cancellationToken, scope); var current = await LoadJournalBundlesAsync(managerId, query.OwnerUserId, query.PropertyId, query.Currency.Trim().ToUpperInvariant(), query.From, query.To, cancellationToken, scope); decimal opening = 0; foreach (var bundle in previous) { var s = SummarizeBundle(bundle.Journal, bundle.Lines); opening += s.Net; } decimal income = 0, expenses = 0, fees = 0, payouts = 0; var entries = new List<P0StatementEntryDto>(); foreach (var bundle in current) { var s = SummarizeBundle(bundle.Journal, bundle.Lines); income += s.Income; expenses += s.Expenses; fees += s.Fees; payouts += s.Payouts; entries.Add(new P0StatementEntryDto(bundle.Journal.AccountingDate, bundle.Journal.SourceType, bundle.Journal.SourceId, s.Description, query.Currency.Trim().ToUpperInvariant(), s.Income, s.Expenses, s.Fees, s.Payouts, s.Net, bundle.Journal.Id)); } var hasUnresolvedSuspense = await HasUnresolvedSuspenseAsync(managerId, query.Currency.Trim().ToUpperInvariant(), query.To, cancellationToken); return new P0StatementDto(null, managerId, query.OwnerUserId, query.PropertyId, query.Currency.Trim().ToUpperInvariant(), query.From, query.To, "PREVIEW", Round(opening), Round(income), Round(expenses), Round(fees), Round(payouts), Round(opening + income - expenses - fees - payouts), hasUnresolvedSuspense, entries, null);
    }

    public async Task<P0StatementDto> FinalizeStatementAsync(P0Actor actor, P0FinalizeStatementRequest request, CancellationToken cancellationToken)
    {
        ValidateCurrency(request.Currency); ValidatePeriod(request.From, request.To); if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("Statement idempotency key is required."); var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: request.OwnerUserId, propertyId: request.PropertyId, cancellationToken: cancellationToken); await using var transaction = await BeginP0TransactionAsync(cancellationToken); var key = request.IdempotencyKey.Trim(); var keyed = await db.MilestoneP0StatementSnapshots.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.IdempotencyKey == key && !x.IsDeleted, cancellationToken); if (keyed is not null) { if (keyed.OwnerUserId != request.OwnerUserId || keyed.PropertyId != request.PropertyId || keyed.Currency != request.Currency.Trim().ToUpperInvariant() || keyed.PeriodFrom != request.From || keyed.PeriodTo != request.To) throw new InvalidOperationException("The statement idempotency key was already used for a different statement."); return SnapshotToDto(keyed); } var existing = await db.MilestoneP0StatementSnapshots.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && x.PropertyId == request.PropertyId && x.Currency == request.Currency.Trim().ToUpperInvariant() && x.PeriodFrom == request.From && x.PeriodTo == request.To && !x.IsDeleted, cancellationToken); if (existing is not null) return SnapshotToDto(existing);
        var preview = await BuildStatementAsync(actor, new P0StatementQuery(request.OwnerUserId, request.PropertyId, request.Currency, request.From, request.To), cancellationToken); if (preview.HasUnresolvedSuspense) throw new InvalidOperationException("Resolve suspense/reconciliation items before finalizing a statement."); var canonical = JsonSerializer.Serialize(preview with { SnapshotId = null, Status = "FINAL" }, new JsonSerializerOptions { WriteIndented = false }); var snapshot = new MilestoneP0StatementSnapshot { ManagerUserId = managerId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, Currency = request.Currency.Trim().ToUpperInvariant(), PeriodFrom = request.From, PeriodTo = request.To, Status = "FINAL", OpeningBalance = preview.OpeningBalance, Income = preview.Income, Expenses = preview.Expenses, ManagementFees = preview.ManagementFees, Payouts = preview.Payouts, ClosingBalance = preview.ClosingBalance, IdempotencyKey = key, SnapshotJson = canonical, ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(), FinalizedByUserId = actor.UserId, FinalizedAt = Now }; db.MilestoneP0StatementSnapshots.Add(snapshot); await AuditAsync(actor.UserId, "P0StatementFinalized", "StatementSnapshot", snapshot.Id, "Statement finalized", new { request.OwnerUserId, request.PropertyId, request.From, request.To }, cancellationToken); await db.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return SnapshotToDto(snapshot);
    }

    private static P0StatementDto SnapshotToDto(MilestoneP0StatementSnapshot item)
    {
        try { var parsed = JsonSerializer.Deserialize<P0StatementDto>(item.SnapshotJson); if (parsed is not null) return parsed with { SnapshotId = item.Id, Status = item.Status, ContentHash = item.ContentHash }; } catch (JsonException) { }
        return new P0StatementDto(item.Id, item.ManagerUserId, item.OwnerUserId, item.PropertyId, item.Currency, item.PeriodFrom, item.PeriodTo, item.Status, item.OpeningBalance, item.Income, item.Expenses, item.ManagementFees, item.Payouts, item.ClosingBalance, false, [], item.ContentHash);
    }

    public async Task<P0StatementExportDto> ExportStatementAsync(P0Actor actor, Guid snapshotId, string format, CancellationToken cancellationToken)
    {
        var snapshot = await db.MilestoneP0StatementSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == snapshotId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Statement snapshot not found."); var managerId = await ResolveManagerAsync(actor, ownerUserId: snapshot.OwnerUserId, propertyId: snapshot.PropertyId, cancellationToken: cancellationToken); if (snapshot.ManagerUserId != managerId) throw new UnauthorizedAccessException("Statement is outside your portfolio."); var dto = SnapshotToDto(snapshot); var normalized = (format ?? "csv").Trim().ToLowerInvariant(); if (normalized is not ("csv" or "json")) throw new InvalidOperationException("Statement format must be csv or json."); var content = normalized == "json" ? snapshot.SnapshotJson : BuildCsv(dto); var bytes = Encoding.UTF8.GetBytes(content); return new P0StatementExportDto(normalized, $"statement-{snapshot.PeriodFrom:yyyyMMdd}-{snapshot.PeriodTo:yyyyMMdd}.{normalized}", "text/" + normalized, Convert.ToBase64String(bytes), snapshot.Id);
    }

    private static string BuildCsv(P0StatementDto statement)
    {
        var builder = new StringBuilder("date,source,description,income,expenses,fees,payouts,net\n"); foreach (var entry in statement.Entries) builder.Append(entry.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',').Append(entry.SourceType).Append(',').Append('"').Append(entry.Description.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"').Append(',').Append(entry.Income.ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append(entry.Expenses.ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append(entry.ManagementFees.ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append(entry.Payouts.ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append(entry.Net.ToString("0.00", CultureInfo.InvariantCulture)).Append('\n'); return builder.ToString();
    }

    public async Task<P0ProfitabilityDto> GetProfitabilityAsync(P0Actor actor, P0ProfitabilityQuery query, CancellationToken cancellationToken)
    {
        ValidateCurrency(query.Currency);
        ValidatePeriod(query.From, query.To);
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var bundles = await LoadJournalBundlesAsync(managerId, query.OwnerUserId, query.PropertyId, query.Currency.Trim().ToUpperInvariant(), query.From, query.To, cancellationToken, scope);
        decimal income = 0, expenses = 0, fees = 0, payouts = 0, cash = 0, unreconciled = 0;
        var rows = new Dictionary<(Guid OwnerUserId, Guid? PropertyId), (decimal Income, decimal Expenses, decimal Fees)>();

        foreach (var bundle in bundles)
        {
            var summary = SummarizeBundle(bundle.Journal, bundle.Lines);
            income += summary.Income;
            expenses += summary.Expenses;
            fees += summary.Fees;
            payouts += summary.Payouts;

            // Cash belongs to the manager clearing account, not to an owner
            // dimension.  It must still be visible in manager profitability
            // and payout eligibility, including when the cash line has no
            // OwnerUserId.
            foreach (var (line, account) in bundle.Lines)
            {
                if (account.Code.StartsWith("CASH_CLEARING:", StringComparison.Ordinal))
                {
                    var value = line.Debit - line.Credit;
                    if (bundle.Reconciled) cash += value;
                    else unreconciled += value;
                }
            }

            var ownerKeys = bundle.Lines
                .Where(item => item.Line.OwnerUserId.HasValue)
                .Select(item => (item.Line.OwnerUserId!.Value, item.Line.PropertyId))
                .Distinct()
                .ToList();
            foreach (var key in ownerKeys)
            {
                var scoped = bundle.Lines.Where(item => item.Line.OwnerUserId == key.Value && item.Line.PropertyId == key.PropertyId).ToList();
                decimal rowIncome = 0, rowExpenses = 0, rowFees = 0;
                foreach (var (line, account) in scoped)
                {
                    if (account.Code.StartsWith("OWNER_INCOME:", StringComparison.Ordinal) ||
                        ((bundle.Journal.SourceType is "RENT_RECEIPT" or "OWNER_INCOME") && line.Credit > line.Debit))
                        rowIncome += line.Credit - line.Debit;
                    else if (account.Code.StartsWith("OWNER_EXPENSE:", StringComparison.Ordinal) ||
                             (bundle.Journal.SourceType is "OWNER_EXPENSE" or "THIRD_PARTY_EXPENSE" or "MAINTENANCE_EXPENSE" or "UTILITY_EXPENSE" && line.Debit > line.Credit))
                        rowExpenses += line.Debit - line.Credit;
                }

                // A management-fee journal has an owner-funds debit and a
                // manager fee-revenue credit.  Attribute the fee to the
                // owner dimension carried by that debit without duplicating
                // the fee across unrelated owner rows.
                if ((bundle.Journal.SourceType == "MANAGEMENT_FEE" &&
                     scoped.Any(item => item.Account.Code.StartsWith("OWNER_FUNDS:", StringComparison.Ordinal))) ||
                    (bundle.Journal.SourceType == "OWNER_BILLING" &&
                     scoped.Any(item => item.Account.Code.StartsWith("OWNER_RECEIVABLE:", StringComparison.Ordinal))))
                    rowFees = summary.Fees;

                rows.TryGetValue(key, out var current);
                rows[key] = (current.Income + rowIncome, current.Expenses + rowExpenses, current.Fees + rowFees);
            }
        }

        var dtoRows = rows.Select(item => new P0ProfitabilityRowDto(
            item.Key.OwnerUserId,
            item.Key.PropertyId,
            Round(item.Value.Income),
            Round(item.Value.Expenses),
            Round(item.Value.Fees),
            Round(item.Value.Income - item.Value.Expenses - item.Value.Fees),
            Round(item.Value.Fees))).ToList();
        return new P0ProfitabilityDto(query.Currency.Trim().ToUpperInvariant(), query.From, query.To, Round(income), Round(expenses), Round(fees), Round(payouts), Round(income - expenses - fees - payouts), Round(fees), Round(cash), Round(unreconciled), dtoRows);
    }

    public async Task<P0ApprovalDto> CreateApprovalAsync(P0Actor actor, P0CreateApprovalRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: request.OwnerUserId, propertyId: request.PropertyId, cancellationToken: cancellationToken);
        await EnsureStaffCapabilityAsync(actor, managerId, finance: true, payoutApprover: false, approvalDecision: false, cancellationToken);
        await using var transaction = await BeginP0TransactionAsync(cancellationToken);
        ValidateCurrency(request.Currency);
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Description)) throw new InvalidOperationException("Approval description and a positive amount are required.");
        if (request.ExpiresAt is { } expiry && expiry <= Now) throw new InvalidOperationException("Approval expiry must be in the future.");
        await EnsureStaffLimitAsync(actor, managerId, request.Amount, cancellationToken);
        await EnsureOwnerScopeAsync(managerId, request.OwnerUserId, cancellationToken);
        if (request.PropertyId.HasValue) await EnsurePropertyScopeAsync(managerId, request.PropertyId.Value, request.OwnerUserId, cancellationToken);
        if (request.AgreementId.HasValue && !await db.MilestoneP0ManagementAgreements.AnyAsync(x => x.Id == request.AgreementId.Value && x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId || x.PropertyId == null) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Agreement linkage is outside the manager, owner or property scope.");
        if (request.FeeRuleId.HasValue && !await db.MilestoneP0ManagementFeeRules.AnyAsync(x => x.Id == request.FeeRuleId.Value && x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId || x.PropertyId == null) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Fee-rule linkage is outside the manager, owner or property scope.");

        var sourceType = string.IsNullOrWhiteSpace(request.SourceType) ? null : request.SourceType.Trim().ToUpperInvariant();
        if (sourceType is not null && sourceType is not ("MAINTENANCE" or "WORK_ORDER" or "EXPENSE")) throw new InvalidOperationException("Approval source type must be MAINTENANCE, WORK_ORDER or EXPENSE.");
        if (sourceType is not null && request.SourceId is null) throw new InvalidOperationException("A related record is required for this approval source.");
        if (sourceType == "MAINTENANCE" && !await db.MilestonePmMaintenanceCases.AnyAsync(x => x.Id == request.SourceId && x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Maintenance source is outside the approval scope.");
        if (sourceType == "WORK_ORDER" && !await db.MilestoneWorkOrders.AnyAsync(x => x.Id == request.SourceId && x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Work-order source is outside the approval scope.");

        var evidence = (request.EvidenceDocumentIds ?? []).Distinct().ToArray();
        if (evidence.Length > 20) throw new InvalidOperationException("An approval can contain at most twenty evidence documents.");
        if (evidence.Length > 0)
        {
            var validCount = await db.MilestoneManagerDocuments.CountAsync(x => evidence.Contains(x.Id) && x.ManagerUserId == managerId && (!x.OwnerUserId.HasValue || x.OwnerUserId == request.OwnerUserId) && (!x.PropertyId.HasValue || x.PropertyId == request.PropertyId) && !x.IsDeleted && !x.IsArchived, cancellationToken);
            if (validCount != evidence.Length) throw new InvalidOperationException("Every evidence document must be active and in the manager, owner and property scope.");
        }
        var evidenceJson = JsonSerializer.Serialize(evidence);
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        if (idempotencyKey is not null)
        {
            var duplicate = await db.MilestoneP0Approvals.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.RequestIdempotencyKey == idempotencyKey && !x.IsDeleted, cancellationToken);
            if (duplicate is not null)
            {
                var matches = duplicate.OwnerUserId == request.OwnerUserId && duplicate.PropertyId == request.PropertyId && duplicate.AgreementId == request.AgreementId && duplicate.FeeRuleId == request.FeeRuleId && duplicate.ApprovalType == request.ApprovalType.Trim().ToUpperInvariant() && duplicate.Description == request.Description.Trim() && duplicate.Amount == Round(request.Amount) && duplicate.Currency == request.Currency.Trim().ToUpperInvariant() && duplicate.EvidenceJson == evidenceJson && duplicate.SourceType == sourceType && duplicate.SourceId == request.SourceId && duplicate.ExpiresAt == request.ExpiresAt?.ToUniversalTime();
                if (!matches) throw new InvalidOperationException("The idempotency key was already used for a different approval request.");
                return await ApprovalToDtoAsync(duplicate, cancellationToken);
            }
        }

        var threshold = await GetApprovalThresholdAsync(managerId, request.OwnerUserId, request.PropertyId, request.ApprovalType, DateOnly.FromDateTime(Now.UtcDateTime), cancellationToken);
        var status = request.Amount <= threshold && threshold > 0 ? "DELEGATED" : "REQUIRED";
        var item = new MilestoneP0Approval
        {
            ManagerUserId = managerId,
            OwnerUserId = request.OwnerUserId,
            PropertyId = request.PropertyId,
            AgreementId = request.AgreementId,
            FeeRuleId = request.FeeRuleId,
            ApprovalType = request.ApprovalType.Trim().ToUpperInvariant(),
            Description = request.Description.Trim(),
            Amount = Round(request.Amount),
            Threshold = Round(threshold),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Status = status,
            EvidenceJson = evidenceJson,
            SourceType = sourceType,
            SourceId = request.SourceId,
            ExpiresAt = request.ExpiresAt?.ToUniversalTime(),
            RequestIdempotencyKey = idempotencyKey
        };
        db.MilestoneP0Approvals.Add(item);
        db.MilestoneP0ApprovalEvents.Add(new MilestoneP0ApprovalEvent { ApprovalId = item.Id, ActorUserId = actor.UserId, EventType = "CREATED", FromStatus = "", ToStatus = status, Reason = item.Description, EvidenceJson = item.EvidenceJson, IdempotencyKey = idempotencyKey is null ? null : $"request:{idempotencyKey}" });
        await AuditAsync(actor.UserId, "P0ApprovalCreated", "Approval", item.Id, item.Description, new { request.Amount, threshold, status, sourceType, request.SourceId }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await QueueNotificationAsync(request.OwnerUserId, "Approval requested", item.Description, $"p0-approval:{item.Id}:created", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return await ApprovalToDtoAsync(item, cancellationToken);
    }

    public async Task<IReadOnlyList<P0ApprovalDto>> ListApprovalsAsync(P0Actor actor, P0ApprovalQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, ownerUserId: query.OwnerUserId, propertyId: query.PropertyId, cancellationToken: cancellationToken);
        var expiring = await db.MilestoneP0Approvals.Where(x => x.ManagerUserId == managerId && x.Status == "REQUIRED" && x.ExpiresAt.HasValue && x.ExpiresAt <= Now && !x.IsDeleted).ToListAsync(cancellationToken);
        foreach (var item in expiring)
        {
            item.Status = "EXPIRED";
            item.RowVersion++;
            db.MilestoneP0ApprovalEvents.Add(new MilestoneP0ApprovalEvent { ApprovalId = item.Id, ActorUserId = actor.UserId, EventType = "EXPIRED", FromStatus = "REQUIRED", ToStatus = "EXPIRED", Reason = "Approval request expired", EvidenceJson = item.EvidenceJson });
        }
        if (expiring.Count > 0) await db.SaveChangesAsync(cancellationToken);
        var scope = await ResolveScopeAsync(actor, managerId, cancellationToken);
        var q = db.MilestoneP0Approvals.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted);
        if (query.OwnerUserId.HasValue) q = q.Where(x => x.OwnerUserId == query.OwnerUserId);
        if (query.PropertyId.HasValue) q = q.Where(x => x.PropertyId == query.PropertyId);
        if (scope.OwnerIds.Length > 0) q = q.Where(x => scope.OwnerIds.Contains(x.OwnerUserId));
        if (scope.PropertyIds.Length > 0) q = q.Where(x => x.PropertyId == null || scope.PropertyIds.Contains(x.PropertyId.Value));
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(x => x.Status == query.Status.Trim().ToUpperInvariant());
        var rows = await q.OrderByDescending(x => x.CreatedAt).Skip(Math.Max(0, query.Page - 1) * Math.Clamp(query.PageSize, 1, 200)).Take(Math.Clamp(query.PageSize, 1, 200)).ToListAsync(cancellationToken);
        var result = new List<P0ApprovalDto>();
        foreach (var row in rows) result.Add(await ApprovalToDtoAsync(row, cancellationToken));
        return result;
    }

    public async Task<P0ApprovalDto?> DecideApprovalAsync(P0Actor actor, Guid approvalId, P0ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status) || string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A decision status and reason are required.");
        var status = request.Status.Trim().ToUpperInvariant();
        if (status is not ("APPROVED" or "REJECTED" or "CHANGES_REQUESTED")) throw new InvalidOperationException("Approval decision must be approved, rejected or changes_requested.");
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"decision:{approvalId:N}:{status}" : request.IdempotencyKey.Trim();
        await using var transaction = await BeginP0LockedCommandTransactionAsync(cancellationToken);
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:approval:{approvalId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken); }
        var item = await db.MilestoneP0Approvals.SingleOrDefaultAsync(x => x.Id == approvalId && !x.IsDeleted, cancellationToken);
        if (item is null) return null;
        var isOwner = actor.UserId == item.OwnerUserId;
        var managerId = await ResolveManagerAsync(actor, finance: false, ownerUserId: item.OwnerUserId, propertyId: item.PropertyId, cancellationToken: cancellationToken);
        if (managerId != item.ManagerUserId) throw new UnauthorizedAccessException("Approval is outside your portfolio.");
        if (!isOwner && !actor.IsAdmin) throw new UnauthorizedAccessException("Only the linked owner can decide this approval request.");
        var duplicate = await db.MilestoneP0ApprovalEvents.AsNoTracking().SingleOrDefaultAsync(x => x.ApprovalId == item.Id && x.IdempotencyKey == idempotencyKey && !x.IsDeleted, cancellationToken);
        if (duplicate is not null)
        {
            if (duplicate.ToStatus != status || duplicate.Reason != request.Reason.Trim()) throw new InvalidOperationException("The idempotency key was already used for a different approval decision.");
            return await ApprovalToDtoAsync(item, cancellationToken);
        }
        if (item.ExpiresAt is { } expiry && expiry <= Now)
        {
            item.Status = "EXPIRED";
            item.RowVersion++;
            db.MilestoneP0ApprovalEvents.Add(new MilestoneP0ApprovalEvent { ApprovalId = item.Id, ActorUserId = actor.UserId, EventType = "EXPIRED", FromStatus = "REQUIRED", ToStatus = "EXPIRED", Reason = "Approval request expired", EvidenceJson = item.EvidenceJson });
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            throw new InvalidOperationException("This approval request has expired.");
        }
        if (item.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Approval changed; reload before deciding.");
        if (item.Status != "REQUIRED") throw new InvalidOperationException("This approval has already been decided.");
        var from = item.Status;
        item.Status = status;
        item.DecisionReason = request.Reason.Trim();
        item.DecidedByUserId = actor.UserId;
        item.DecidedAt = Now;
        item.RowVersion++;
        db.MilestoneP0ApprovalEvents.Add(new MilestoneP0ApprovalEvent { ApprovalId = item.Id, ActorUserId = actor.UserId, EventType = "DECIDED", FromStatus = from, ToStatus = status, Reason = item.DecisionReason, EvidenceJson = item.EvidenceJson, IdempotencyKey = idempotencyKey });
        await AuditAsync(actor.UserId, "P0ApprovalDecided", "Approval", item.Id, item.DecisionReason, new { from, status }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await QueueNotificationAsync(item.ManagerUserId, "Approval decision", $"Approval {item.Description}: {status}", $"p0-approval:{item.Id}:{status}", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return await ApprovalToDtoAsync(item, cancellationToken);
    }

    private async Task<P0ApprovalDto> ApprovalToDtoAsync(MilestoneP0Approval item, CancellationToken cancellationToken)
    {
        var history = await db.MilestoneP0ApprovalEvents.AsNoTracking().Where(x => x.ApprovalId == item.Id && !x.IsDeleted).OrderBy(x => x.CreatedAt).Select(x => new P0ApprovalEventDto(x.Id, x.ActorUserId, x.EventType, x.FromStatus, x.ToStatus, x.Reason, x.CreatedAt, x.IdempotencyKey)).ToListAsync(cancellationToken);
        Guid[] evidenceIds;
        try { evidenceIds = JsonSerializer.Deserialize<Guid[]>(item.EvidenceJson) ?? []; }
        catch (JsonException) { evidenceIds = []; }
        var evidence = await db.MilestoneManagerDocuments.AsNoTracking().Where(x => evidenceIds.Contains(x.Id) && x.ManagerUserId == item.ManagerUserId && !x.IsDeleted).OrderBy(x => x.Title).Select(x => new P0ApprovalEvidenceDto(x.Id, x.Title, x.FileName, x.ContentType, x.SizeBytes, x.IsArchived ? "ARCHIVED" : "ACTIVE")).ToListAsync(cancellationToken);
        return new P0ApprovalDto(item.Id, item.ManagerUserId, item.OwnerUserId, item.PropertyId, item.AgreementId, item.FeeRuleId, item.ApprovalType, item.Description, item.Amount, item.Threshold, item.Currency, item.Status, item.EvidenceJson, item.DecisionReason, item.DecidedByUserId, item.DecidedAt, item.RowVersion, history, evidence, item.SourceType, item.SourceId, item.ExpiresAt, item.RequestIdempotencyKey);
    }

    public async Task<P0StaffMembershipDto> InviteStaffAsync(P0Actor actor, P0InviteStaffRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, cancellationToken: cancellationToken);
        RequireManagerMutation(actor, managerId);
        var staff = await db.MilestoneUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.StaffUserId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Staff user was not found."); if (request.StaffUserId == managerId) throw new InvalidOperationException("Manager cannot invite itself as staff."); var existing = await db.MilestoneP0StaffMemberships.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.StaffUserId == request.StaffUserId && !x.IsDeleted, cancellationToken); if (existing is not null && existing.Status is "ACCEPTED" or "ACTIVE") throw new InvalidOperationException("Staff member is already active."); var role = string.IsNullOrWhiteSpace(request.Role) ? "OPERATIONS" : request.Role.Trim().ToUpperInvariant(); if (role is not ("OPERATIONS" or "FINANCE" or "APPROVER")) throw new InvalidOperationException("Staff role is invalid."); await ValidateStaffScopesAsync(managerId, request.OwnerIds, request.PropertyIds, cancellationToken); var approvalLimit = Positive(request.ApprovalLimit); if ((request.CanManageFinance || request.CanApprovePayouts) && approvalLimit <= 0) throw new InvalidOperationException("A positive approval limit is required when finance or payout permissions are enabled."); var item = existing ?? new MilestoneP0StaffMembership { ManagerUserId = managerId, StaffUserId = request.StaffUserId }; if (existing is null) db.MilestoneP0StaffMemberships.Add(item); item.Role = role; item.PropertyScopeJson = JsonSerializer.Serialize((request.PropertyIds ?? []).Distinct()); item.OwnerScopeJson = JsonSerializer.Serialize((request.OwnerIds ?? []).Distinct()); item.CanManageFinance = request.CanManageFinance; item.CanApprovePayouts = request.CanApprovePayouts; item.ApprovalLimit = approvalLimit; item.Status = "INVITED"; item.RowVersion++; db.MilestoneP0StaffEvents.Add(new MilestoneP0StaffEvent { MembershipId = item.Id, ActorUserId = actor.UserId, EventType = "INVITED", FromStatus = existing?.Status ?? "", ToStatus = "INVITED", Reason = "Team invitation" }); await AuditAsync(actor.UserId, "P0StaffInvited", "StaffMembership", item.Id, "Team invitation", new { request.StaffUserId }, cancellationToken); await db.SaveChangesAsync(cancellationToken); await QueueNotificationAsync(staff.Id, "NestyStay team invitation", "You have been invited to the property management team.", $"p0-staff:{item.Id}:invite", cancellationToken); await db.SaveChangesAsync(cancellationToken); return await StaffToDtoAsync(item, cancellationToken);
    }

    public async Task<IReadOnlyList<P0StaffMembershipDto>> ListStaffAsync(P0Actor actor, CancellationToken cancellationToken)
    {
        // A newly invited team member must be able to see their pending
        // invitation before they have an accepted membership.  Do not route
        // this read through the manager fallback (which would create a
        // separate synthetic portfolio for a staff user's claim).
        var invitations = await db.MilestoneP0StaffMemberships.AsNoTracking()
            .Where(x => x.StaffUserId == actor.UserId && x.Status == "INVITED" && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        if (invitations.Count > 0)
        {
            var invitedResult = new List<P0StaffMembershipDto>();
            foreach (var invitation in invitations) invitedResult.Add(await StaffToDtoAsync(invitation, cancellationToken));
            return invitedResult;
        }

        var managerId = await ResolveManagerAsync(actor, cancellationToken: cancellationToken);
        var rows = await db.MilestoneP0StaffMemberships.AsNoTracking()
            .Where(x => x.ManagerUserId == managerId && !x.IsDeleted)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        if (!actor.IsAdmin && actor.UserId != managerId)
            rows = rows.Where(x => x.StaffUserId == actor.UserId).ToList();
        var result = new List<P0StaffMembershipDto>();
        foreach (var row in rows) result.Add(await StaffToDtoAsync(row, cancellationToken));
        return result;
    }

    public async Task<IReadOnlyList<P0StaffEventDto>> ListStaffHistoryAsync(P0Actor actor, Guid membershipId, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, cancellationToken: cancellationToken);
        var membership = await db.MilestoneP0StaffMemberships.AsNoTracking().SingleOrDefaultAsync(x => x.Id == membershipId && x.ManagerUserId == managerId && !x.IsDeleted, cancellationToken);
        if (membership is null || (!actor.IsAdmin && actor.UserId != managerId && membership.StaffUserId != actor.UserId))
            throw new UnauthorizedAccessException("Team membership is outside your portfolio.");
        return await db.MilestoneP0StaffEvents.AsNoTracking()
            .Where(x => x.MembershipId == membershipId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new P0StaffEventDto(x.Id, x.MembershipId, x.ActorUserId, x.EventType, x.FromStatus, x.ToStatus, x.Reason, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<P0StaffMembershipDto?> AcceptStaffAsync(P0Actor actor, Guid membershipId, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneP0StaffMemberships.SingleOrDefaultAsync(x => x.Id == membershipId && x.StaffUserId == actor.UserId && !x.IsDeleted, cancellationToken); if (item is null) return null; if (item.Status != "INVITED") throw new InvalidOperationException("Invitation is no longer pending."); var from = item.Status; item.Status = "ACCEPTED"; item.AcceptedAt = Now; item.RowVersion++; db.MilestoneP0StaffEvents.Add(new MilestoneP0StaffEvent { MembershipId = item.Id, ActorUserId = actor.UserId, EventType = "ACCEPTED", FromStatus = from, ToStatus = item.Status, Reason = "Invitation accepted" }); await AuditAsync(actor.UserId, "P0StaffAccepted", "StaffMembership", item.Id, "Invitation accepted", null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await StaffToDtoAsync(item, cancellationToken);
    }

    public async Task<P0StaffMembershipDto?> UpdateStaffAsync(P0Actor actor, Guid membershipId, P0UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, cancellationToken: cancellationToken); RequireManagerMutation(actor, managerId); var item = await db.MilestoneP0StaffMemberships.SingleOrDefaultAsync(x => x.Id == membershipId && x.ManagerUserId == managerId && !x.IsDeleted, cancellationToken); if (item is null) return null; if (item.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Staff membership changed; reload before editing."); if (string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.Status)) throw new InvalidOperationException("Team role and status are required."); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("INVITED" or "ACCEPTED" or "ACTIVE" or "SUSPENDED" or "REVOKED")) throw new InvalidOperationException("Invalid team-member status."); if (status is "ACCEPTED" or "ACTIVE" && item.Status == "INVITED") throw new InvalidOperationException("The invitee must accept the invitation before activation."); var role = request.Role.Trim().ToUpperInvariant(); if (role is not ("OPERATIONS" or "FINANCE" or "APPROVER")) throw new InvalidOperationException("Staff role is invalid."); await ValidateStaffScopesAsync(managerId, request.OwnerIds, request.PropertyIds, cancellationToken); var approvalLimit = Positive(request.ApprovalLimit); if ((request.CanManageFinance || request.CanApprovePayouts) && approvalLimit <= 0) throw new InvalidOperationException("A positive approval limit is required when finance or payout permissions are enabled."); var from = item.Status; item.Role = role; item.PropertyScopeJson = JsonSerializer.Serialize((request.PropertyIds ?? []).Distinct()); item.OwnerScopeJson = JsonSerializer.Serialize((request.OwnerIds ?? []).Distinct()); item.CanManageFinance = request.CanManageFinance; item.CanApprovePayouts = request.CanApprovePayouts; item.ApprovalLimit = approvalLimit; item.Status = status; if (status == "SUSPENDED") item.SuspendedAt = Now; if (status == "REVOKED") item.RevokedAt = Now; item.RowVersion++; db.MilestoneP0StaffEvents.Add(new MilestoneP0StaffEvent { MembershipId = item.Id, ActorUserId = actor.UserId, EventType = "UPDATED", FromStatus = from, ToStatus = status, Reason = "Team membership updated" }); await AuditAsync(actor.UserId, "P0StaffUpdated", "StaffMembership", item.Id, "Team membership updated", null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await StaffToDtoAsync(item, cancellationToken);
    }

    public async Task<P0StaffMembershipDto?> RevokeStaffAsync(P0Actor actor, Guid membershipId, P0StatusChangeRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, cancellationToken: cancellationToken); RequireManagerMutation(actor, managerId); var item = await db.MilestoneP0StaffMemberships.SingleOrDefaultAsync(x => x.Id == membershipId && x.ManagerUserId == managerId && !x.IsDeleted, cancellationToken); if (item is null) return null; if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A revocation reason is required."); var from = item.Status; item.Status = "REVOKED"; item.RevokedAt = Now; item.RowVersion++; db.MilestoneP0StaffEvents.Add(new MilestoneP0StaffEvent { MembershipId = item.Id, ActorUserId = actor.UserId, EventType = "REVOKED", FromStatus = from, ToStatus = "REVOKED", Reason = request.Reason.Trim() }); await AuditAsync(actor.UserId, "P0StaffRevoked", "StaffMembership", item.Id, request.Reason.Trim(), null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await StaffToDtoAsync(item, cancellationToken);
    }

    private async Task<P0StaffMembershipDto> StaffToDtoAsync(MilestoneP0StaffMembership item, CancellationToken cancellationToken) => new(item.Id, item.ManagerUserId, item.StaffUserId, item.Role, ParseIds(item.PropertyScopeJson), ParseIds(item.OwnerScopeJson), item.CanManageFinance, item.CanApprovePayouts, item.ApprovalLimit, item.Status, item.AcceptedAt, item.SuspendedAt, item.RevokedAt, item.RowVersion);

    public async Task<P0PayoutAvailabilityDto> GetPayoutAvailabilityAsync(P0Actor actor, Guid ownerUserId, string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        ValidateCurrency(currency); ValidatePeriod(from, to); var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: ownerUserId, cancellationToken: cancellationToken); await EnsureOwnerScopeAsync(managerId, ownerUserId, cancellationToken); var bundles = await LoadJournalBundlesAsync(managerId, ownerUserId, null, currency.Trim().ToUpperInvariant(), from, to, cancellationToken); decimal income = 0, expenses = 0, fees = 0, existingPayouts = 0, cash = 0; foreach (var bundle in bundles) { var s = SummarizeBundle(bundle.Journal, bundle.Lines); income += s.Income; expenses += s.Expenses; fees += s.Fees; existingPayouts += s.Payouts; foreach (var (line, account) in bundle.Lines) if (bundle.Reconciled && account.Code.StartsWith("CASH_CLEARING:", StringComparison.Ordinal)) cash += line.Debit - line.Credit; }
        var suspense = await HasUnresolvedSuspenseAsync(managerId, currency.Trim().ToUpperInvariant(), to, cancellationToken);
        var reserved = await db.MilestoneP0PayoutBatches.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && x.Currency == currency.Trim().ToUpperInvariant() && x.PeriodFrom == from && x.PeriodTo == to && !PayoutTerminalStatuses.Contains(x.Status) && !x.IsDeleted).SumAsync(x => x.ReservedAmount, cancellationToken); var payable = suspense ? 0 : Math.Max(0, Math.Min(Math.Max(0, income - expenses - fees - existingPayouts), cash) - reserved); return new P0PayoutAvailabilityDto(managerId, ownerUserId, currency.Trim().ToUpperInvariant(), from, to, Round(income), Round(expenses), Round(fees), Round(existingPayouts), Round(cash), Round(reserved), Round(payable), suspense);
    }

    public async Task<P0PayoutBatchDto> CreatePayoutBatchAsync(P0Actor actor, P0CreatePayoutBatchRequest request, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: request.OwnerUserId, cancellationToken: cancellationToken);
        await using var transaction = await BeginP0TransactionAsync(cancellationToken);
        ValidateCurrency(request.Currency);
        ValidatePeriod(request.PeriodFrom, request.PeriodTo);
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("Payout idempotency key is required.");
        var currency = request.Currency.Trim().ToUpperInvariant();
        var duplicate = await db.MilestoneP0PayoutBatches.SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.IdempotencyKey == request.IdempotencyKey.Trim() && !x.IsDeleted, cancellationToken);
        if (duplicate is not null)
        {
            if (duplicate.OwnerUserId != request.OwnerUserId || duplicate.Currency != currency || duplicate.PeriodFrom != request.PeriodFrom || duplicate.PeriodTo != request.PeriodTo || duplicate.StatementSnapshotId != request.StatementSnapshotId)
                throw new InvalidOperationException("The idempotency key was already used for a different payout request.");
            return await PayoutToDtoAsync(duplicate, cancellationToken);
        }
        if (request.StatementSnapshotId.HasValue)
        {
            var snapshot = await db.MilestoneP0StatementSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.StatementSnapshotId.Value && !x.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Statement snapshot was not found.");
            if (snapshot.ManagerUserId != managerId || snapshot.OwnerUserId != request.OwnerUserId || snapshot.Currency != currency || snapshot.PeriodFrom != request.PeriodFrom || snapshot.PeriodTo != request.PeriodTo || snapshot.Status != "FINAL")
                throw new InvalidOperationException("Payout statement linkage does not match the owner, currency or period.");
        }
        var availability = await GetPayoutAvailabilityAsync(actor, request.OwnerUserId, currency, request.PeriodFrom, request.PeriodTo, cancellationToken);
        if (availability.PayableAmount <= 0) throw new InvalidOperationException("No reconciled payable owner balance is available.");
        await EnsureStaffLimitAsync(actor, managerId, availability.PayableAmount, cancellationToken);
        var batch = new MilestoneP0PayoutBatch { ManagerUserId = managerId, OwnerUserId = request.OwnerUserId, CreatedByUserId = actor.UserId, Currency = currency, PeriodFrom = request.PeriodFrom, PeriodTo = request.PeriodTo, Amount = availability.PayableAmount, ReservedAmount = availability.PayableAmount, Status = "PENDING_APPROVAL", IdempotencyKey = request.IdempotencyKey.Trim(), StatementSnapshotId = request.StatementSnapshotId };
        db.MilestoneP0PayoutBatches.Add(batch);
        db.MilestoneP0PayoutItems.Add(new MilestoneP0PayoutItem { BatchId = batch.Id, OwnerUserId = request.OwnerUserId, Amount = batch.Amount, StatementSnapshotId = request.StatementSnapshotId, SourceJson = JsonSerializer.Serialize(availability) });
        db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "CREATED", FromStatus = "", ToStatus = batch.Status, Reason = "Payout batch created" });
        await AuditAsync(actor.UserId, "P0PayoutCreated", "PayoutBatch", batch.Id, "Payout batch created", new { batch.Amount, batch.Currency }, cancellationToken);
         await db.SaveChangesAsync(cancellationToken);
         await QueueNotificationAsync(request.OwnerUserId, "Payout pending approval", $"A payout of {batch.Amount:0.00} {batch.Currency} is pending approval.", $"p0-payout:{batch.Id}:created", cancellationToken);
         await db.SaveChangesAsync(cancellationToken);
         if (transaction is not null) await transaction.CommitAsync(cancellationToken);
         return await PayoutToDtoAsync(batch, cancellationToken);
    }

    public async Task<IReadOnlyList<P0PayoutBatchDto>> ListPayoutBatchesAsync(P0Actor actor, P0PayoutQuery query, CancellationToken cancellationToken)
    {
        var managerId = await ResolveManagerAsync(actor, finance: false, ownerUserId: query.OwnerUserId, cancellationToken: cancellationToken); var scope = await ResolveScopeAsync(actor, managerId, cancellationToken); var q = db.MilestoneP0PayoutBatches.AsNoTracking().Where(x => x.ManagerUserId == managerId && !x.IsDeleted); if (query.OwnerUserId.HasValue) q = q.Where(x => x.OwnerUserId == query.OwnerUserId); if (scope.OwnerIds.Length > 0) q = q.Where(x => scope.OwnerIds.Contains(x.OwnerUserId)); if (!string.IsNullOrWhiteSpace(query.Currency)) q = q.Where(x => x.Currency == query.Currency.Trim().ToUpperInvariant()); if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(x => x.Status == query.Status.Trim().ToUpperInvariant()); var rows = await q.OrderByDescending(x => x.CreatedAt).Skip(Math.Max(0, query.Page - 1) * Math.Clamp(query.PageSize, 1, 200)).Take(Math.Clamp(query.PageSize, 1, 200)).ToListAsync(cancellationToken); var result = new List<P0PayoutBatchDto>(); foreach (var row in rows) result.Add(await PayoutToDtoAsync(row, cancellationToken)); return result;
    }

    public async Task<P0PayoutBatchDto?> ApprovePayoutBatchAsync(P0Actor actor, Guid batchId, P0PayoutDecisionRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.MilestoneP0PayoutBatches.SingleOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted, cancellationToken); if (batch is null) return null; var managerId = await ResolveManagerAsync(actor, finance: true, payoutApprover: true, ownerUserId: batch.OwnerUserId, cancellationToken: cancellationToken); if (batch.ManagerUserId != managerId) throw new UnauthorizedAccessException("Payout is outside your portfolio."); if (batch.CreatedByUserId == actor.UserId) throw new UnauthorizedAccessException("A separate person must approve a payout."); if (batch.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Payout changed; reload before approving."); if (batch.Status != "PENDING_APPROVAL") throw new InvalidOperationException("Only pending payouts can be approved."); if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("An approval reason is required."); await EnsureStaffLimitAsync(actor, managerId, batch.Amount, cancellationToken); var from = batch.Status; batch.Status = "APPROVED"; batch.ApprovedByUserId = actor.UserId; batch.ApprovedAt = Now; batch.RowVersion++; db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "APPROVED", FromStatus = from, ToStatus = batch.Status, Reason = request.Reason.Trim() }); await AuditAsync(actor.UserId, "P0PayoutApproved", "PayoutBatch", batch.Id, request.Reason.Trim(), null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await PayoutToDtoAsync(batch, cancellationToken);
    }

    public async Task<P0PayoutBatchDto?> ProcessPayoutBatchAsync(P0Actor actor, Guid batchId, P0PayoutProcessRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.MilestoneP0PayoutBatches.SingleOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted, cancellationToken); if (batch is null) return null;
        var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: batch.OwnerUserId, cancellationToken: cancellationToken); if (batch.ManagerUserId != managerId) throw new UnauthorizedAccessException("Payout is outside your portfolio."); await EnsureStaffLimitAsync(actor, managerId, batch.Amount, cancellationToken);
        if (batch.Status == "PAID") return await PayoutToDtoAsync(batch, cancellationToken);
        if (batch.Status != "APPROVED") throw new InvalidOperationException("Only approved payouts can be processed.");
        await using var transaction = await BeginP0TransactionAsync(cancellationToken);
        var from = batch.Status; batch.Status = "PROCESSING"; batch.RowVersion++; db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "PROCESSING", FromStatus = from, ToStatus = batch.Status, Reason = "Payout submitted" }); await db.SaveChangesAsync(cancellationToken);
        if (request.SimulateFailure) { batch.Status = "FAILED"; batch.FailureReason = string.IsNullOrWhiteSpace(request.FailureReason) ? "Local provider failure" : request.FailureReason.Trim(); batch.RowVersion++; db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "FAILED", FromStatus = "PROCESSING", ToStatus = batch.Status, Reason = batch.FailureReason }); await AuditAsync(actor.UserId, "P0PayoutFailed", "PayoutBatch", batch.Id, batch.FailureReason, null, cancellationToken); await db.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return await PayoutToDtoAsync(batch, cancellationToken); }
        var providerReference = string.IsNullOrWhiteSpace(request.ProviderReference) ? $"LOCAL-PAYOUT-{batch.Id:N}" : request.ProviderReference.Trim();
        var ownerFunds = await EnsureAccountAsync(managerId, batch.OwnerUserId, null, batch.Currency, "OWNER_FUNDS", "Owner client funds", "LIABILITY", true, false, false, cancellationToken); var cash = await EnsureAccountAsync(managerId, null, null, batch.Currency, "CASH_CLEARING", "Cash clearing", "ASSET", false, true, false, cancellationToken);
        await PostJournalCoreAsync(actor, managerId, new P0PostJournalRequest("OWNER_PAYOUT", batch.Id, $"payout:{batch.Id}", batch.Currency, batch.PeriodTo, $"Owner payout {batch.Id}", [new P0JournalLineRequest(ownerFunds.Code, batch.Amount, 0, batch.OwnerUserId, null, "Owner payout"), new P0JournalLineRequest(cash.Code, 0, batch.Amount, null, null, "Cash paid" )], true, providerReference, null), cancellationToken);
         batch.Status = "PAID"; batch.ProviderReference = providerReference; batch.ProcessedAt = Now; batch.RowVersion++; db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "PAID", FromStatus = "PROCESSING", ToStatus = batch.Status, Reason = "Payout completed", ProviderReference = batch.ProviderReference }); await AuditAsync(actor.UserId, "P0PayoutPaid", "PayoutBatch", batch.Id, "Payout completed", new { batch.ProviderReference }, cancellationToken); await db.SaveChangesAsync(cancellationToken); await QueueNotificationAsync(batch.OwnerUserId, "Payout paid", $"Your payout of {batch.Amount:0.00} {batch.Currency} was paid.", $"p0-payout:{batch.Id}:paid", cancellationToken); await db.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); return await PayoutToDtoAsync(batch, cancellationToken);
    }

    public async Task<P0PayoutBatchDto?> CancelPayoutBatchAsync(P0Actor actor, Guid batchId, P0PayoutDecisionRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.MilestoneP0PayoutBatches.SingleOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted, cancellationToken); if (batch is null) return null; var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: batch.OwnerUserId, cancellationToken: cancellationToken); if (batch.ManagerUserId != managerId) throw new UnauthorizedAccessException("Payout is outside your portfolio."); if (batch.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Payout changed; reload before cancelling."); if (batch.Status is not ("DRAFT" or "PENDING_APPROVAL" or "APPROVED")) throw new InvalidOperationException("This payout cannot be cancelled."); if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A cancellation reason is required."); var from = batch.Status; batch.Status = "CANCELLED"; batch.CancelledAt = Now; batch.ReservedAmount = 0; batch.RowVersion++; db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "CANCELLED", FromStatus = from, ToStatus = batch.Status, Reason = request.Reason.Trim() }); await AuditAsync(actor.UserId, "P0PayoutCancelled", "PayoutBatch", batch.Id, request.Reason.Trim(), null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await PayoutToDtoAsync(batch, cancellationToken);
    }

    public async Task<P0PayoutBatchDto?> RetryPayoutBatchAsync(P0Actor actor, Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await db.MilestoneP0PayoutBatches.SingleOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted, cancellationToken); if (batch is null) return null; var managerId = await ResolveManagerAsync(actor, finance: true, ownerUserId: batch.OwnerUserId, cancellationToken: cancellationToken); if (batch.ManagerUserId != managerId) throw new UnauthorizedAccessException("Payout is outside your portfolio."); if (batch.Status != "FAILED") throw new InvalidOperationException("Only failed payouts can be retried."); var from = batch.Status; batch.Status = "PENDING_APPROVAL"; batch.FailureReason = null; batch.ProviderReference = null; batch.RowVersion++; db.MilestoneP0PayoutEvents.Add(new MilestoneP0PayoutEvent { BatchId = batch.Id, ActorUserId = actor.UserId, EventType = "RETRY", FromStatus = from, ToStatus = batch.Status, Reason = "Payout retry requested" }); await AuditAsync(actor.UserId, "P0PayoutRetried", "PayoutBatch", batch.Id, "Payout retry requested", null, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await PayoutToDtoAsync(batch, cancellationToken);
    }

    public async Task<P0OwnerPortalDto> GetOwnerPortalAsync(P0Actor actor, Guid? managerUserId, CancellationToken cancellationToken)
    {
        if (actor.UserId == Guid.Empty) throw new UnauthorizedAccessException("Signed-in owner required.");
        var memberships = await db.MilestoneManagerOwners.AsNoTracking().Where(x => x.OwnerUserId == actor.UserId && !x.IsDeleted).Select(x => x.ManagerUserId).Distinct().ToListAsync(cancellationToken); if (managerUserId.HasValue) { if (!memberships.Contains(managerUserId.Value)) throw new UnauthorizedAccessException("Owner is not linked to this manager."); } else if (memberships.Count != 1) throw new InvalidOperationException(memberships.Count == 0 ? "Owner is not linked to a property manager." : "Select a manager portfolio."); var managerId = managerUserId ?? memberships[0]; var profile = await db.MilestoneP0OwnerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == actor.UserId && !x.IsDeleted, cancellationToken); var properties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerId && x.OwnerUserId == actor.UserId && !x.IsDeleted).ToListAsync(cancellationToken); var agreements = (await db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == managerId && x.OwnerUserId == actor.UserId && !x.IsDeleted).OrderByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken)).Select(ToDto).ToList(); var approvals = await db.MilestoneP0Approvals.AsNoTracking().Where(x => x.ManagerUserId == managerId && x.OwnerUserId == actor.UserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken); var approvalDtos = new List<P0ApprovalDto>(); foreach (var item in approvals) approvalDtos.Add(await ApprovalToDtoAsync(item, cancellationToken)); var currency = profile?.PreferredCurrency ?? "JMD"; var statement = await BuildStatementAsync(actor, new P0StatementQuery(actor.UserId, null, currency, DateOnly.FromDateTime(Now.UtcDateTime).AddMonths(-1), DateOnly.FromDateTime(Now.UtcDateTime)), cancellationToken); var journals = await ListJournalsAsync(actor, new P0JournalQuery(actor.UserId, null, currency, null, null, null, 1, 100), cancellationToken); var payouts = await ListPayoutBatchesAsync(actor, new P0PayoutQuery(actor.UserId, currency, null, 1, 100), cancellationToken); var profitability = await GetProfitabilityAsync(actor, new P0ProfitabilityQuery(currency, DateOnly.FromDateTime(Now.UtcDateTime).AddMonths(-1), DateOnly.FromDateTime(Now.UtcDateTime), actor.UserId), cancellationToken); return new P0OwnerPortalDto(managerId, profile is null ? null : ToDto(profile), properties.Select(ToDto).ToList(), agreements, approvalDtos, statement, journals, payouts, profitability.Rows);
    }

    private async Task<P0PayoutBatchDto> PayoutToDtoAsync(MilestoneP0PayoutBatch item, CancellationToken cancellationToken)
    {
        var items = await db.MilestoneP0PayoutItems.AsNoTracking().Where(x => x.BatchId == item.Id && !x.IsDeleted).Select(x => new P0PayoutItemDto(x.Id, x.OwnerUserId, x.PropertyId, x.Amount, x.StatementSnapshotId, x.SourceJson)).ToListAsync(cancellationToken); var history = await db.MilestoneP0PayoutEvents.AsNoTracking().Where(x => x.BatchId == item.Id && !x.IsDeleted).OrderBy(x => x.CreatedAt).Select(x => new P0PayoutEventDto(x.Id, x.ActorUserId, x.EventType, x.FromStatus, x.ToStatus, x.Reason, x.ProviderReference, x.CreatedAt)).ToListAsync(cancellationToken); return new P0PayoutBatchDto(item.Id, item.ManagerUserId, item.OwnerUserId, item.Currency, item.PeriodFrom, item.PeriodTo, item.Amount, item.ReservedAmount, item.Status, item.IdempotencyKey, item.ProviderReference, item.FailureReason, item.ApprovedByUserId, item.ApprovedAt, item.ProcessedAt, item.CancelledAt, item.StatementSnapshotId, item.RowVersion, items, history);
    }

    private async Task<P0JournalDto> ToDtoAsync(MilestoneP0Journal item, CancellationToken cancellationToken, P0Scope? scope = null)
    {
        var rows = await (from line in db.MilestoneP0JournalLines.AsNoTracking()
                          join account in db.MilestoneP0Accounts.AsNoTracking() on line.AccountId equals account.Id
                          where line.JournalId == item.Id && !line.IsDeleted
                          select new { line, account }).ToListAsync(cancellationToken);
        var hasVisibleDimension = scope is not { IsRestricted: true } || rows.Any(row =>
        {
            var owner = row.line.OwnerUserId ?? row.account.OwnerUserId;
            var property = row.line.PropertyId ?? row.account.PropertyId;
            return (owner.HasValue || property.HasValue) && scope!.Allows(owner, property);
        });
        var lines = rows
            .Where(row => scope is null || !scope.IsRestricted || (hasVisibleDimension &&
                (!(row.line.OwnerUserId ?? row.account.OwnerUserId).HasValue && !(row.line.PropertyId ?? row.account.PropertyId).HasValue ||
                 scope.Allows(row.line.OwnerUserId ?? row.account.OwnerUserId, row.line.PropertyId ?? row.account.PropertyId))))
            .Select(row => new P0JournalLineDto(row.line.Id, row.line.AccountId, row.account.Code, row.line.OwnerUserId ?? row.account.OwnerUserId, row.line.PropertyId ?? row.account.PropertyId, row.line.Debit, row.line.Credit, row.line.Description, row.line.SourceReference))
            .ToList();
        var reconciled = await db.MilestoneP0Reconciliations.AnyAsync(x => x.JournalId == item.Id && !x.IsDeleted, cancellationToken);
        return new P0JournalDto(item.Id, item.ManagerUserId, item.JournalNumber, item.SourceType, item.SourceId, item.IdempotencyKey, item.Currency, item.AccountingDate, item.Memo, item.Status, reconciled ? "RECONCILED" : item.ReconciliationStatus, item.TotalDebit, item.TotalCredit, item.ReversalOfJournalId, item.PostedAt, lines);
    }

    private async Task<decimal> GetApprovalThresholdAsync(Guid managerId, Guid ownerId, Guid? propertyId, string approvalType, DateOnly on, CancellationToken cancellationToken)
    {
        var q = db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerId && x.Status == "ACTIVE" && x.EffectiveFrom <= on && (x.EffectiveTo == null || x.EffectiveTo >= on) && (x.PropertyId == propertyId || x.PropertyId == null) && !x.IsDeleted).OrderByDescending(x => x.PropertyId.HasValue).ThenByDescending(x => x.EffectiveFrom); var agreement = await q.FirstOrDefaultAsync(cancellationToken); if (agreement is null) return 0m; return approvalType.ToUpperInvariant().Contains("MAINTENANCE", StringComparison.Ordinal) ? agreement.MaintenanceApprovalLimit : agreement.ExpenseApprovalLimit;
    }

    private static bool Overlaps(DateOnly fromA, DateOnly? toA, DateOnly fromB, DateOnly? toB) => fromA <= (toB ?? DateOnly.MaxValue) && fromB <= (toA ?? DateOnly.MaxValue);
    private static decimal Positive(decimal value) => value < 0 ? throw new InvalidOperationException("Amount cannot be negative.") : Round(value);
    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string JsonOrObject(string value) { if (string.IsNullOrWhiteSpace(value)) return "{}"; try { using var document = JsonDocument.Parse(value); return document.RootElement.ValueKind == JsonValueKind.Object ? value : throw new InvalidOperationException("Expected a JSON object."); } catch (JsonException) { throw new InvalidOperationException("Invalid JSON object."); } }
    private static string JsonOrArray(string value) { if (string.IsNullOrWhiteSpace(value)) return "[]"; try { using var document = JsonDocument.Parse(value); return document.RootElement.ValueKind == JsonValueKind.Array ? value : throw new InvalidOperationException("Expected a JSON array."); } catch (JsonException) { throw new InvalidOperationException("Invalid JSON array."); } }
    private static Guid[] ParseIds(string json) { try { return JsonSerializer.Deserialize<Guid[]>(json) ?? []; } catch (JsonException) { return []; } }
}
