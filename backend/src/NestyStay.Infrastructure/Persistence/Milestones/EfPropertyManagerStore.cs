using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PropertyManager;
using NestyStay.Application.SpecCompletion;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class EfPropertyManagerStore(
    NestyStayDbContext db,
    IPaymentGateway paymentGateway,
    IStorageProvider storageProvider,
    TimeProvider timeProvider,
    ISpecCompletionStore specCompletionStore) : IPropertyManagerStore
{
    private static readonly SemaphoreSlim PaymentGate = new(1, 1);
    private static readonly SemaphoreSlim GovernanceGate = new(1, 1);

    public async Task<PropertyManagerDashboardDto> GetDashboardAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(actorUserId, cancellationToken);
        var owners = await db.MilestoneManagerOwners.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var properties = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var invoices = await db.MilestoneManagerInvoices.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var invoiceLines = await db.MilestoneManagerInvoiceLines.Where(x => invoices.Select(i => i.Id).Contains(x.InvoiceId) && !x.IsDeleted).ToListAsync(cancellationToken);
        var maintenance = await db.MilestoneManagerMaintenances.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var utilities = await db.MilestoneManagerUtilityCharges.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var vendors = await db.MilestoneManagerVendors.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var notices = await db.MilestoneManagerNotices.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).OrderByDescending(x => x.PublishAt).Take(20).ToListAsync(cancellationToken);
        var proposals = await db.MilestoneManagerProposals.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(cancellationToken);
        var documents = await db.MilestoneManagerDocuments.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted && !x.IsArchived).OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(cancellationToken);
        var gateMessages = await db.MilestoneManagerGateMessages.Where(x => x.ManagerUserId == actorUserId && !x.IsDeleted).OrderByDescending(x => x.ValidFrom).Take(20).ToListAsync(cancellationToken);
        if (ApplyDueSubscriptionChange(manager)) await db.SaveChangesAsync(cancellationToken);
        var proposalDtos = new List<ProposalDto>();
        foreach (var proposal in proposals) proposalDtos.Add(await BuildProposalDtoAsync(proposal, cancellationToken));
        return new PropertyManagerDashboardDto(
            ToDto(manager, properties.Count), owners.Count, properties.Count,
            invoices.Sum(x => x.Balance), invoices.Count(x => x.Status is "ISSUED" or "OVERDUE" && x.Balance > 0),
            maintenance.Count(x => x.Status is not ("COMPLETED" or "CANCELLED")), owners.Count(x => x.VerificationStatus == "PENDING"),
            await db.MilestoneManagerQrScans.CountAsync(x => x.CreatedAt >= timeProvider.GetUtcNow().AddDays(-30) && !x.IsDeleted, cancellationToken),
            owners.Select(ToDto).ToList(), properties.Select(ToDto).ToList(), invoices.Select(x => ToDto(x, invoiceLines.Where(l => l.InvoiceId == x.Id))).ToList(), maintenance.Select(ToDto).ToList(), utilities.Select(ToDto).ToList(), vendors.Select(ToDto).ToList(), notices.Select(ToDto).ToList(), proposalDtos, documents.Select(ToDto).ToList(), gateMessages.Select(ToDto).ToList());
    }

    public async Task<OwnerDto> InviteOwnerAsync(Guid managerUserId, InviteOwnerRequest request, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName)) throw new InvalidOperationException("Owner email and display name are required.");
        var email = request.Email.Trim().ToLowerInvariant();
        var user = request.OwnerUserId is { } requested
            ? await db.MilestoneUsers.SingleOrDefaultAsync(x => x.Id == requested && !x.IsDeleted, cancellationToken)
            : await db.MilestoneUsers.SingleOrDefaultAsync(x => x.NormalizedEmail == email && !x.IsDeleted, cancellationToken);
        if (user is null) throw new InvalidOperationException("Register the owner account first, then invite it by email.");
        var existing = await db.MilestoneManagerOwners.SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.OwnerUserId == user.Id && !x.IsDeleted, cancellationToken);
        if (existing is not null) return ToDto(existing);
        var owner = new MilestoneManagerOwner { ManagerUserId = managerUserId, OwnerUserId = user.Id, DisplayName = user.DisplayName, Email = user.Email, CommunityId = request.CommunityId, VerificationStatus = "PENDING", InvitationStatus = "INVITED" };
        db.MilestoneManagerOwners.Add(owner);
        db.MilestoneManagerInvitationEvents.Add(new MilestoneManagerInvitationEvent { ManagerUserId = managerUserId, OwnerUserId = user.Id, EventType = "INVITED" });
        await AuditAsync(managerUserId, "OwnerInvited", "ManagerOwner", owner.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await specCompletionStore.StartAuthFlowAsync(
            // Use a stable manager-scoped bucket rather than the literal
            // string "manager".  Otherwise every local/test manager shares
            // one IP bucket and an unrelated test run can throttle all owner
            // invitations.  The auth-flow store still applies account and
            // destination limits, while this keeps network throttling scoped
            // to the authenticated manager.
            new StartAuthFlowRequest(user.Id, "OwnerInvitation", user.Email, RequestIp: $"manager:{managerUserId:N}"),
            cancellationToken);
        return ToDto(owner);
    }

    public async Task<OwnerDto?> ReviewOwnerAsync(Guid managerUserId, Guid ownerUserId, string status, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var owner = await db.MilestoneManagerOwners.SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        if (owner is null) return null;
        var normalized = status.Trim().ToUpperInvariant();
        if (normalized is not ("PENDING" or "VERIFIED" or "REJECTED" or "SUSPENDED")) throw new InvalidOperationException("Owner status is invalid.");
        owner.VerificationStatus = normalized;
        db.MilestoneManagerOwnerVerifications.Add(new MilestoneManagerOwnerVerification { ManagerUserId = managerUserId, OwnerUserId = ownerUserId, Requirement = "ACCOUNT", Status = normalized == "VERIFIED" ? "APPROVED" : normalized, Reason = normalized, ActorUserId = managerUserId });
        await AuditAsync(managerUserId, $"OwnerVerification{normalized}", "ManagerOwner", owner.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(owner);
    }

    public async Task<ManagerProfileDto> RenewSubscriptionAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        ApplyDueSubscriptionChange(manager);
        manager.SubscriptionStatus = "ACTIVE";
        manager.NextBillingAt = timeProvider.GetUtcNow().AddMonths(1);
        manager.BillingProviderStatus = "LOCAL_TEST_CAPTURED";
        manager.CancellationReason = null;
        manager.AutoRenew = true;
        manager.PendingSubscriptionTier = null;
        manager.PendingSubscriptionEffectiveAt = null;
        db.MilestoneManagerSubscriptionEvents.Add(new MilestoneManagerSubscriptionEvent { ManagerUserId = managerUserId, EventType = "RENEW", FromTier = manager.SubscriptionTier, ToTier = manager.SubscriptionTier, Status = "COMPLETED", Reason = "Subscription renewed by manager.", EffectiveAt = timeProvider.GetUtcNow() });
        await AuditAsync(managerUserId, "ManagerSubscriptionRenewed", "PropertyManager", manager.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var unitsUsed = await db.MilestoneManagerProperties.CountAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        return ToDto(manager, unitsUsed);
    }

    public async Task<PropertyDto> AddPropertyAsync(Guid managerUserId, AddPropertyRequest request, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        ApplyDueSubscriptionChange(manager);
        var unitLimit = SubscriptionUnitLimit(manager.SubscriptionTier);
        var unitsUsed = await db.MilestoneManagerProperties.CountAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (unitLimit is { } limit && unitsUsed >= limit)
            throw new InvalidOperationException($"The {manager.SubscriptionTier} plan allows {limit} units. Upgrade the subscription to add another property.");
        await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.UnitNumber)) throw new InvalidOperationException("Property title and unit number are required.");
        if (request.RentalListingId is { } listingId)
        {
            var listing = await db.MilestoneProperties.AsNoTracking().SingleOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Rental listing is not available.");
            if (listing.HostUserId != request.OwnerUserId) throw new UnauthorizedAccessException("Rental listing is not owned by this owner.");
            if (await db.MilestoneManagerProperties.AnyAsync(x => x.RentalListingId == listingId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Rental listing is already linked to a managed property.");
        }
        var property = new MilestoneManagerProperty { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, CommunityId = request.CommunityId, Title = request.Title.Trim(), UnitNumber = request.UnitNumber.Trim(), Address = request.Address.Trim(), Status = "ACTIVE", OccupancyStatus = "VACANT", RentalListingId = request.RentalListingId, RentalListingLinkedAt = request.RentalListingId.HasValue ? timeProvider.GetUtcNow() : null };
        db.MilestoneManagerProperties.Add(property);
        await AuditAsync(managerUserId, "PropertyAssigned", "ManagerProperty", property.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(property);
    }

    public async Task<PropertyDto?> LinkRentalListingAsync(Guid managerUserId, Guid propertyId, LinkRentalListingRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var property = await db.MilestoneManagerProperties.SingleOrDefaultAsync(x => x.Id == propertyId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (property is null) return null;
        if (request.RentalListingId is { } listingId)
        {
            var listing = await db.MilestoneProperties.AsNoTracking().SingleOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Rental listing is not available.");
            if (listing.HostUserId != property.OwnerUserId) throw new UnauthorizedAccessException("Rental listing is not owned by this owner.");
            if (await db.MilestoneManagerProperties.AnyAsync(x => x.Id != propertyId && x.RentalListingId == listingId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Rental listing is already linked to another managed property.");
        }
        property.RentalListingId = request.RentalListingId;
        property.RentalListingLinkedAt = request.RentalListingId.HasValue ? timeProvider.GetUtcNow() : null;
        await AuditAsync(managerUserId, request.RentalListingId.HasValue ? "PropertyListingLinked" : "PropertyListingUnlinked", "ManagerProperty", property.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(property);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(Guid managerUserId, CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        if (request.PropertyId is { } invoiceProperty && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == invoiceProperty && x.ManagerUserId == managerUserId && x.OwnerUserId == request.OwnerUserId && !x.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Property is not in the owner's manager portfolio.");
        if (request.Lines is null || request.Lines.Count == 0) throw new InvalidOperationException("At least one invoice line is required.");
        var lines = request.Lines.Select(x => new MilestoneManagerInvoiceLine { Description = x.Description.Trim(), Quantity = x.Quantity, UnitAmount = x.UnitAmount, Amount = decimal.Round(x.Quantity * x.UnitAmount, 2, MidpointRounding.AwayFromZero) }).ToList();
        if (lines.Any(x => x.Quantity <= 0 || x.UnitAmount < 0 || string.IsNullOrWhiteSpace(x.Description)) || request.Tax < 0) throw new InvalidOperationException("Invoice quantities, descriptions, tax and amounts must be valid.");
        var invoice = new MilestoneManagerInvoice { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, InvoiceNumber = $"PM-{timeProvider.GetUtcNow():yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}", IssueDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), DueDate = request.DueDate, Subtotal = lines.Sum(x => x.Amount), Tax = decimal.Round(request.Tax, 2, MidpointRounding.AwayFromZero), Currency = "USD", Status = "ISSUED" };
        invoice.Total = invoice.Subtotal + invoice.Tax; invoice.Balance = invoice.Total;
        db.MilestoneManagerInvoices.Add(invoice);
        foreach (var line in lines) { line.InvoiceId = invoice.Id; db.MilestoneManagerInvoiceLines.Add(line); }
        db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, InvoiceId = invoice.Id, EntryType = "CHARGE", Description = $"Invoice {invoice.InvoiceNumber}", Amount = invoice.Total, OccurredOn = invoice.IssueDate });
        await AuditAsync(managerUserId, "InvoiceIssued", "ManagerInvoice", invoice.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(invoice, lines);
    }

    public async Task<IReadOnlyList<InvoiceDto>> BulkIssueInvoicesAsync(Guid managerUserId, BulkIssueInvoicesRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var ids = (request.InvoiceIds ?? []).Distinct().Take(250).ToArray();
        if (ids.Length == 0) throw new InvalidOperationException("Select at least one invoice to issue.");
        var rows = await db.MilestoneManagerInvoices.Where(x => ids.Contains(x.Id) && x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        if (rows.Count != ids.Length) throw new InvalidOperationException("One or more invoices are outside the manager portfolio.");
        foreach (var row in rows)
        {
            if (row.Status is "PAID" or "CANCELLED") continue;
            row.Status = row.Balance > 0 && row.DueDate < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime) ? "OVERDUE" : "ISSUED";
            row.UpdatedAt = timeProvider.GetUtcNow();
            await AuditAsync(managerUserId, "InvoiceBulkIssued", "ManagerInvoice", row.Id, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        var lines = await db.MilestoneManagerInvoiceLines.Where(x => ids.Contains(x.InvoiceId) && !x.IsDeleted).ToListAsync(cancellationToken);
        return rows.Select(row => ToDto(row, lines.Where(line => line.InvoiceId == row.Id))).ToList();
    }

    public async Task<IReadOnlyList<InvoiceDto>> MarkOverdueInvoicesAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var rows = await db.MilestoneManagerInvoices.Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.Balance > 0 && x.DueDate < today && x.Status != "CANCELLED").ToListAsync(cancellationToken);
        foreach (var row in rows) { row.Status = "OVERDUE"; row.UpdatedAt = timeProvider.GetUtcNow(); await AuditAsync(managerUserId, "InvoiceOverdueReminderQueued", "ManagerInvoice", row.Id, cancellationToken); }
        await db.SaveChangesAsync(cancellationToken);
        var lines = await db.MilestoneManagerInvoiceLines.Where(x => rows.Select(row => row.Id).Contains(x.InvoiceId) && !x.IsDeleted).ToListAsync(cancellationToken);
        return rows.Select(row => ToDto(row, lines.Where(line => line.InvoiceId == row.Id))).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await db.MilestoneManagerInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken);
        if (invoice is null) return null;
        if (!isAdmin && invoice.ManagerUserId != actorUserId && invoice.OwnerUserId != actorUserId) return null;
        var lines = await db.MilestoneManagerInvoiceLines.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).ToListAsync(cancellationToken);
        return ToDto(invoice, lines);
    }

    public async Task<InvoiceDto?> UpdateInvoiceAsync(Guid managerUserId, Guid invoiceId, UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await using var transaction = await LockRecordAsync($"invoice:{invoiceId}", cancellationToken);
        var invoice = await db.MilestoneManagerInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (invoice is null) return null;
        if (invoice.AmountPaid > 0 || invoice.Status is "PAID" or "CANCELLED")
            throw new InvalidOperationException("Only unpaid invoices can be edited.");
        if (request.Lines is null || request.Lines.Count == 0 || request.Tax < 0)
            throw new InvalidOperationException("At least one valid invoice line and non-negative tax are required.");
        var lines = request.Lines.Select(x => new MilestoneManagerInvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = x.Description.Trim(),
            Quantity = x.Quantity,
            UnitAmount = x.UnitAmount,
            Amount = decimal.Round(x.Quantity * x.UnitAmount, 2, MidpointRounding.AwayFromZero)
        }).ToList();
        if (lines.Any(x => x.Quantity <= 0 || x.UnitAmount < 0 || string.IsNullOrWhiteSpace(x.Description)))
            throw new InvalidOperationException("Invoice quantities, descriptions and amounts must be valid.");
        var previousLines = await db.MilestoneManagerInvoiceLines.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).ToListAsync(cancellationToken);
        foreach (var line in previousLines) line.IsDeleted = true;
        var previousTotal = invoice.Total;
        invoice.DueDate = request.DueDate;
        invoice.Subtotal = lines.Sum(x => x.Amount);
        invoice.Tax = decimal.Round(request.Tax, 2, MidpointRounding.AwayFromZero);
        invoice.Total = invoice.Subtotal + invoice.Tax;
        invoice.Balance = invoice.Total - invoice.AmountPaid;
        invoice.Status = "ISSUED";
        invoice.UpdatedAt = timeProvider.GetUtcNow();
        if (invoice.Total != previousTotal)
            db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry
            {
                ManagerUserId = managerUserId, OwnerUserId = invoice.OwnerUserId,
                PropertyId = invoice.PropertyId, InvoiceId = invoice.Id,
                EntryType = "ADJUSTMENT", Description = $"Invoice {invoice.InvoiceNumber} amended from {previousTotal} to {invoice.Total}",
                Amount = invoice.Total - previousTotal,
                OccurredOn = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)
            });
        db.MilestoneManagerInvoiceLines.AddRange(lines);
        await AuditAsync(managerUserId, "InvoiceUpdated", "ManagerInvoice", invoice.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ToDto(invoice, lines);
    }

    public async Task<InvoiceDto?> PayInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, PayInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("A positive amount and idempotency key are required.");
        await PaymentGate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await LockRecordAsync($"invoice:{invoiceId}", cancellationToken);
            var invoice = await db.MilestoneManagerInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Invoice not found.");
            if (!isAdmin && invoice.ManagerUserId != actorUserId && invoice.OwnerUserId != actorUserId) return null!;
            var existing = await db.MilestoneManagerPayments.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == invoice.ManagerUserId && x.IdempotencyKey == request.IdempotencyKey && !x.IsDeleted, cancellationToken);
            if (existing is not null)
            {
                if (existing.InvoiceId != invoiceId || existing.Amount != request.Amount)
                    throw new InvalidOperationException("This idempotency key belongs to another payment request.");
                return ToDto(invoice, await db.MilestoneManagerInvoiceLines.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).ToListAsync(cancellationToken));
            }
            if (request.Amount > invoice.Balance) throw new InvalidOperationException("Payment cannot exceed invoice balance.");
            var auth = await paymentGateway.AuthorizeAsync(new PaymentAuthorizationRequest(invoice.Id, request.Amount, invoice.Currency, $"Property manager invoice {invoice.InvoiceNumber}", request.IdempotencyKey), cancellationToken);
            if (auth.Status is not (NestyStay.Domain.PaymentStatus.Authorized or NestyStay.Domain.PaymentStatus.Captured)) throw new InvalidOperationException("Payment authorization failed.");
            var reference = auth.AuthorizationReference;
            if (auth.Status == NestyStay.Domain.PaymentStatus.Authorized)
            {
                var captured = await paymentGateway.CaptureAsync(new PaymentCaptureRequest(reference, request.Amount, invoice.Currency, $"pm-capture:{request.IdempotencyKey}"), cancellationToken);
                if (captured.Status != NestyStay.Domain.PaymentStatus.Captured || captured.CapturedAmount != request.Amount || captured.Currency != invoice.Currency)
                    throw new InvalidOperationException("Payment capture has not been confirmed. No invoice balance was changed.");
                reference = captured.CaptureReference;
            }
            var payment = new MilestoneManagerPayment { ManagerUserId = invoice.ManagerUserId, OwnerUserId = invoice.OwnerUserId, InvoiceId = invoice.Id, Amount = request.Amount, IdempotencyKey = request.IdempotencyKey, Provider = auth.ProviderName, ProviderReference = reference, Status = "CAPTURED" };
            db.MilestoneManagerPayments.Add(payment);
            invoice.AmountPaid += request.Amount; invoice.Balance = invoice.Total - invoice.AmountPaid; invoice.Status = invoice.Balance <= 0 ? "PAID" : "PARTIALLY_PAID"; invoice.UpdatedAt = timeProvider.GetUtcNow();
            db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry { ManagerUserId = invoice.ManagerUserId, OwnerUserId = invoice.OwnerUserId, PropertyId = invoice.PropertyId, InvoiceId = invoice.Id, EntryType = "PAYMENT", Description = $"Payment for {invoice.InvoiceNumber}", Amount = -request.Amount, OccurredOn = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime) });
            await AuditAsync(actorUserId, "InvoicePaymentCaptured", "ManagerInvoice", invoice.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return ToDto(invoice, await db.MilestoneManagerInvoiceLines.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).ToListAsync(cancellationToken));
        }
        finally { PaymentGate.Release(); }
    }

    public async Task<StatementDto> GetStatementAsync(Guid actorUserId, bool isAdmin, Guid ownerUserId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!isAdmin && actorUserId != ownerUserId) await RequireOwnerScopeAsync(actorUserId, ownerUserId, cancellationToken);
        var managerId = isAdmin
            ? (await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => (Guid?)x.ManagerUserId).FirstOrDefaultAsync(cancellationToken) ?? actorUserId)
            : await db.MilestoneManagerOwners.Where(x => x.ManagerUserId == actorUserId && x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => (Guid?)x.ManagerUserId).FirstOrDefaultAsync(cancellationToken)
              ?? await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => (Guid?)x.ManagerUserId).FirstOrDefaultAsync(cancellationToken)
              ?? actorUserId;
        var start = from ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddMonths(-12)); var end = to ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddMonths(1));
        var entries = await db.MilestoneManagerLedgerEntries.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && x.OccurredOn >= start && x.OccurredOn <= end && !x.IsDeleted).OrderBy(x => x.OccurredOn).ToListAsync(cancellationToken);
        var opening = await db.MilestoneManagerLedgerEntries.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && x.OccurredOn < start && !x.IsDeleted).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var invoiceRows = await db.MilestoneManagerInvoices.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var invoiceLines = await db.MilestoneManagerInvoiceLines.Where(x => invoiceRows.Select(i => i.Id).Contains(x.InvoiceId) && !x.IsDeleted).ToListAsync(cancellationToken);
        var paymentRows = await db.MilestoneManagerPayments.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return new StatementDto(ownerUserId, start, end, opening, entries.Select(x => new StatementEntryDto(x.OccurredOn, x.EntryType, x.Description, x.Amount, x.InvoiceId)).ToList(), opening + entries.Sum(x => x.Amount), invoiceRows.Select(x => ToDto(x, invoiceLines.Where(l => l.InvoiceId == x.Id))).ToList(), paymentRows.Select(x => new PaymentDto(x.Id, x.InvoiceId, x.Amount, x.Provider, x.ProviderReference, x.Status, x.CreatedAt)).ToList());
    }

    public async Task<UtilityChargeDto> CreateUtilityAsync(Guid managerUserId, CreateUtilityRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken); await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        var property = await db.MilestoneManagerProperties.SingleOrDefaultAsync(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && x.OwnerUserId == request.OwnerUserId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Property is not in the manager portfolio.");
        if (request.Usage < 0 || request.Rate < 0) throw new InvalidOperationException("Utility usage and rate cannot be negative.");
        var amount = decimal.Round(request.Usage * request.Rate, 2, MidpointRounding.AwayFromZero);
        var charge = new MilestoneManagerUtilityCharge { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = property.Id, UtilityType = request.UtilityType.Trim(), BillingPeriod = request.BillingPeriod.Trim(), Usage = request.Usage, Rate = request.Rate, Amount = amount };
        // Utility allocations are billable charges, so create the invoice association
        // in the same transaction as the utility and ledger rows. This keeps the
        // owner portal, statement, and payment flow consistent for every charge.
        var invoice = new MilestoneManagerInvoice
        {
            ManagerUserId = managerUserId,
            OwnerUserId = request.OwnerUserId,
            PropertyId = property.Id,
            InvoiceNumber = $"UTIL-{timeProvider.GetUtcNow():yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
            IssueDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            DueDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(30)),
            Subtotal = amount,
            Tax = 0m,
            Total = amount,
            Balance = amount,
            Currency = "USD",
            Status = "ISSUED"
        };
        var invoiceLine = new MilestoneManagerInvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = $"{charge.UtilityType} utility {charge.BillingPeriod}",
            Quantity = request.Usage,
            UnitAmount = request.Rate,
            Amount = amount
        };
        charge.InvoiceId = invoice.Id;
        db.MilestoneManagerUtilityCharges.Add(charge);
        db.MilestoneManagerInvoices.Add(invoice);
        db.MilestoneManagerInvoiceLines.Add(invoiceLine);
        db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = property.Id, InvoiceId = invoice.Id, EntryType = "UTILITY", Description = $"{charge.UtilityType} utility {charge.BillingPeriod}", Amount = amount, OccurredOn = invoice.IssueDate });
        await AuditAsync(managerUserId, "UtilityChargeAllocated", "UtilityCharge", charge.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(charge);
    }

    public async Task<MaintenanceDto> CreateMaintenanceAsync(Guid actorUserId, bool isAdmin, CreateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var ownerId = request.OwnerUserId;
        var isManagerActor = !isAdmin && await db.MilestonePropertyManagers.AnyAsync(x => x.ManagerUserId == actorUserId && !x.IsDeleted, cancellationToken);
        var managerId = isAdmin
            ? await db.MilestoneManagerProperties.Where(x => x.Id == request.PropertyId && !x.IsDeleted).Select(x => x.ManagerUserId).FirstOrDefaultAsync(cancellationToken)
            : isManagerActor
                ? actorUserId
                : await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == actorUserId && !x.IsDeleted).Select(x => x.ManagerUserId).FirstOrDefaultAsync(cancellationToken);
        if (managerId == Guid.Empty) throw new InvalidOperationException("Property is not in a manager portfolio.");
        if (!isAdmin && actorUserId != ownerId) await RequireOwnerScopeAsync(actorUserId, ownerId, cancellationToken); else await RequireOwnerScopeAsync(managerId, ownerId, cancellationToken);
        var property = await db.MilestoneManagerProperties.SingleOrDefaultAsync(x => x.Id == request.PropertyId && x.ManagerUserId == managerId && x.OwnerUserId == ownerId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Property is not in the manager portfolio.");
        var item = new MilestoneManagerMaintenance { ManagerUserId = managerId, OwnerUserId = ownerId, PropertyId = property.Id, Title = request.Title.Trim(), Description = request.Description.Trim(), Category = request.Category.Trim(), Urgency = request.Urgency.Trim().ToUpperInvariant(), Status = "OPEN", SlaDueAt = timeProvider.GetUtcNow().AddHours(request.Urgency.Trim().Equals("URGENT", StringComparison.OrdinalIgnoreCase) ? 4 : 48) }; db.MilestoneManagerMaintenances.Add(item); db.MilestoneManagerMaintenanceActivities.Add(new MilestoneManagerMaintenanceActivity { ManagerUserId = managerId, MaintenanceId = item.Id, ActorUserId = actorUserId, Action = "CREATED", Details = item.Title }); await AuditAsync(actorUserId, "MaintenanceCreated", "Maintenance", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<MaintenanceDto?> UpdateMaintenanceAsync(Guid managerUserId, Guid maintenanceId, UpdateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerMaintenances.SingleOrDefaultAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) return null;
        if (request.VendorId is { } vendor && !await db.MilestoneManagerVendors.AnyAsync(x => x.Id == vendor && x.ManagerUserId == managerUserId && x.IsActive && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Vendor is not in the manager portfolio.");
        item.Status = request.Status.Trim().ToUpperInvariant(); item.VendorId = request.VendorId; item.ScheduledAt = request.ScheduledAt; item.Cost = request.Cost; item.Notes = request.Notes.Trim(); item.ClosedAt = item.Status is "COMPLETED" or "CANCELLED" ? timeProvider.GetUtcNow() : null; item.UpdatedAt = timeProvider.GetUtcNow(); db.MilestoneManagerMaintenanceActivities.Add(new MilestoneManagerMaintenanceActivity { ManagerUserId = managerUserId, MaintenanceId = item.Id, ActorUserId = managerUserId, Action = item.Status, Details = request.Notes.Trim() }); await AuditAsync(managerUserId, "MaintenanceUpdated", "Maintenance", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<VendorDto> CreateVendorAsync(Guid managerUserId, CreateVendorRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = new MilestoneManagerVendor { ManagerUserId = managerUserId, Name = request.Name.Trim(), Category = request.Category.Trim(), Contact = request.Contact.Trim(), Notes = request.Notes.Trim(), ServiceAreasJson = JsonSerializer.Serialize(request.ServiceAreas ?? []), AvailabilityJson = request.AvailabilityJson ?? "{}", Rate = request.Rate, VerificationStatus = "PENDING" }; db.MilestoneManagerVendors.Add(item); await AuditAsync(managerUserId, "VendorAdded", "Vendor", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<NoticeDto> CreateNoticeAsync(Guid managerUserId, CreateNoticeRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body)) throw new InvalidOperationException("Notice title and body are required.");
        if (request.TargetOwnerUserId is { } owner) await RequireOwnerScopeAsync(managerUserId, owner, cancellationToken);
        if (request.ExpiresAt is { } expires && expires <= (request.PublishAt ?? timeProvider.GetUtcNow())) throw new InvalidOperationException("Notice expiry must be after its publish time.");
        var publishAt = request.PublishAt ?? timeProvider.GetUtcNow();
        var roles = (request.AudienceRoles ?? []).Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var owners = (request.AudienceOwnerIds ?? []).Distinct().ToArray();
        foreach (var audienceOwner in owners) await RequireOwnerScopeAsync(managerUserId, audienceOwner, cancellationToken);
        var item = new MilestoneManagerNotice
        {
            ManagerUserId = managerUserId,
            CommunityId = request.CommunityId,
            TargetOwnerUserId = request.TargetOwnerUserId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            PublishAt = publishAt,
            ExpiresAt = request.ExpiresAt,
            IsPinned = request.IsPinned,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "GENERAL" : request.Category.Trim().ToUpperInvariant(),
            AudienceRolesJson = JsonSerializer.Serialize(roles),
            AudienceOwnerIdsJson = JsonSerializer.Serialize(owners),
            AcknowledgementDueAt = request.AcknowledgementDueAt
        };
        db.MilestoneManagerNotices.Add(item);
        await AuditAsync(managerUserId, publishAt > timeProvider.GetUtcNow() ? "CommunityNoticeScheduled" : "CommunityNoticePublished", "Notice", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var managerIds = await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == actorUserId && !x.IsDeleted).Select(x => x.ManagerUserId).ToListAsync(cancellationToken);
        var rows = await db.MilestoneManagerNotices.AsNoTracking().Where(x => !x.IsDeleted && !x.IsArchived && (isAdmin || x.ManagerUserId == actorUserId || managerIds.Contains(x.ManagerUserId))).OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.PublishAt).ToListAsync(cancellationToken);
        var visible = new List<NoticeDto>();
        foreach (var row in rows)
            if (isAdmin || row.ManagerUserId == actorUserId || await CanOwnerReadNoticeAsync(actorUserId, row, cancellationToken)) visible.Add(ToDto(row));
        return visible;
    }

    private async Task<bool> CanOwnerReadNoticeAsync(Guid ownerId, MilestoneManagerNotice notice, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        if (notice.IsDeleted || notice.IsArchived || notice.PublishAt > now || notice.ExpiresAt <= now || (notice.TargetOwnerUserId is { } target && target != ownerId)) return false;
        var membership = await db.MilestoneManagerOwners.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == notice.ManagerUserId && x.OwnerUserId == ownerId && !x.IsDeleted, ct);
        if (membership is null || (notice.CommunityId is { } community && membership.CommunityId != community)) return false;
        var owners = JsonSerializer.Deserialize<Guid[]>(notice.AudienceOwnerIdsJson) ?? [];
        var roles = JsonSerializer.Deserialize<string[]>(notice.AudienceRolesJson) ?? [];
        return (owners.Length == 0 || owners.Contains(ownerId)) && (roles.Length == 0 || roles.Contains("Owner", StringComparer.OrdinalIgnoreCase));
    }

    public async Task<ProposalDto> CreateProposalAsync(Guid managerUserId, CreateProposalRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description) || request.ClosesAt <= request.OpensAt || request.Quorum is < 0) throw new InvalidOperationException("Proposal title, description, window and quorum must be valid."); var owners = await db.MilestoneManagerOwners.Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).Select(x => x.OwnerUserId).ToListAsync(cancellationToken); var item = new MilestoneManagerProposal { ManagerUserId = managerUserId, CommunityId = request.CommunityId, Title = request.Title.Trim(), Description = request.Description.Trim(), OpensAt = request.OpensAt, ClosesAt = request.ClosesAt, IsAnonymous = request.IsAnonymous, Quorum = request.Quorum }; db.MilestoneManagerProposals.Add(item); foreach (var owner in owners) db.MilestoneManagerEligibleVoters.Add(new MilestoneManagerEligibleVoter { ProposalId = item.Id, OwnerUserId = owner }); await AuditAsync(managerUserId, "ProposalCreated", "Proposal", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item, owners.Count, 0); }

    public async Task<ProposalDto> VoteAsync(Guid actorUserId, bool isAdmin, Guid proposalId, VoteRequest request, CancellationToken cancellationToken)
    {
        await GovernanceGate.WaitAsync(cancellationToken);
        try
        {
            var proposal = await db.MilestoneManagerProposals.SingleOrDefaultAsync(x => x.Id == proposalId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Proposal not found.");
            var now = timeProvider.GetUtcNow();
            if (isAdmin) throw new InvalidOperationException("Administrators cannot cast owner ballots.");
            if (now < proposal.OpensAt || now >= proposal.ClosesAt || proposal.Status is not "OPEN") throw new InvalidOperationException("This proposal is not open for voting.");
            var voter = await db.MilestoneManagerEligibleVoters.SingleOrDefaultAsync(x => x.ProposalId == proposalId && x.OwnerUserId == actorUserId && !x.IsDeleted, cancellationToken);
            var proxy = request.ProxyId is { } proxyId ? await db.MilestoneManagerProxies.SingleOrDefaultAsync(x => x.Id == proxyId && x.ProposalId == proposalId && x.ProxyUserId == actorUserId && x.Status == "ACCEPTED" && x.ValidUntil > now && !x.IsDeleted, cancellationToken) : null;
            if (voter is null && proxy is null) throw new InvalidOperationException("You are not eligible to vote on this proposal.");
            if (voter is not null && voter.HasVoted) throw new InvalidOperationException("A ballot has already been recorded for this owner.");
            if (proxy is not null && await db.MilestoneManagerVotes.AnyAsync(x => x.ProposalId == proposalId && x.ProxyId == proxy.Id && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("This proxy has already been used.");
            var choice = request.Choice.Trim().ToUpperInvariant();
            if (choice is not ("YES" or "NO" or "ABSTAIN")) throw new InvalidOperationException("Choice must be YES, NO, or ABSTAIN.");
            if (voter is not null) voter.HasVoted = true;
            var ballot = new MilestoneManagerVote { ProposalId = proposalId, BallotHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{proposalId:N}:{Guid.NewGuid():N}"))).ToLowerInvariant(), Choice = choice, CastByProxy = proxy is not null, ProxyId = proxy?.Id };
            db.MilestoneManagerVotes.Add(ballot);
            await AuditAsync(actorUserId, "VoteSubmitted", "Proposal", proposalId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return await BuildProposalDtoAsync(proposal, cancellationToken);
        }
        finally { GovernanceGate.Release(); }
    }

    public async Task<ProxyDto> CreateProxyAsync(Guid ownerUserId, CreateProxyRequest request, CancellationToken cancellationToken)
    { var proposal = await db.MilestoneManagerProposals.SingleOrDefaultAsync(x => x.Id == request.ProposalId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Proposal not found."); var eligible = await db.MilestoneManagerEligibleVoters.AnyAsync(x => x.ProposalId == request.ProposalId && x.OwnerUserId == ownerUserId && !x.HasVoted && !x.IsDeleted, cancellationToken); if (!eligible) throw new InvalidOperationException("Owner is not eligible or has already voted."); if (!await db.MilestoneManagerOwners.AnyAsync(x => x.ManagerUserId == proposal.ManagerUserId && x.OwnerUserId == request.ProxyUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Proxy must be an eligible portfolio owner."); var existing = await db.MilestoneManagerProxies.SingleOrDefaultAsync(x => x.ProposalId == request.ProposalId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken); if (existing is not null) return ToDto(existing); var item = new MilestoneManagerProxy { ProposalId = request.ProposalId, OwnerUserId = ownerUserId, ProxyUserId = request.ProxyUserId, ValidUntil = request.ValidUntil, Status = "ACCEPTED", AcceptedAt = timeProvider.GetUtcNow() }; db.MilestoneManagerProxies.Add(item); await AuditAsync(ownerUserId, "ProxyGranted", "Proxy", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<ProxyDto>> ListProxiesAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var items = await db.MilestoneManagerProxies.AsNoTracking().Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return items.Select(item => item.Status == "ACCEPTED" && item.ValidUntil <= now ? new ProxyDto(item.Id, item.ProposalId, item.OwnerUserId, item.ProxyUserId, "EXPIRED", item.ValidUntil, item.AcceptedAt) : ToDto(item)).ToList();
    }

    public async Task<ProxyDto?> RevokeProxyAsync(Guid ownerUserId, Guid proxyId, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneManagerProxies.SingleOrDefaultAsync(x => x.Id == proxyId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        if (item is null) return null;
        if (item.Status == "REVOKED") return ToDto(item);
        item.Status = "REVOKED";
        item.UpdatedAt = timeProvider.GetUtcNow();
        await AuditAsync(ownerUserId, "ProxyRevoked", "Proxy", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    { if (isAdmin) return (await db.MilestoneManagerDocuments.Where(x => !x.IsDeleted && !x.IsArchived).ToListAsync(cancellationToken)).Select(ToDto).ToList(); var managerIds = await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == actorUserId && !x.IsDeleted).Select(x => x.ManagerUserId).ToListAsync(cancellationToken); return (await db.MilestoneManagerDocuments.Where(x => managerIds.Contains(x.ManagerUserId) && !x.IsDeleted && !x.IsArchived && (x.OwnerUserId == null || x.OwnerUserId == actorUserId)).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

    public async Task<DocumentDto> AddDocumentAsync(Guid managerUserId, AddDocumentRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.OwnerUserId is { } owner) await RequireOwnerScopeAsync(managerUserId, owner, cancellationToken); if (request.PropertyId is { } documentProperty && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == documentProperty && x.ManagerUserId == managerUserId && (!request.OwnerUserId.HasValue || x.OwnerUserId == request.OwnerUserId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is not in the manager portfolio."); if (request.SizeBytes <= 0 || request.SizeBytes > 25 * 1024 * 1024) throw new InvalidOperationException("Document size must be between 1 byte and 25 MB."); if (request.ExpiresOn is { } expiresOn && expiresOn < DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("Document expiry must be today or later."); var safeName = Path.GetFileName(request.FileName); if (safeName != request.FileName || safeName.Contains("..", StringComparison.Ordinal)) throw new InvalidOperationException("Document filename is invalid."); var allowed = request.ContentType.ToLowerInvariant() is "application/pdf" or "image/jpeg" or "image/png"; if (!allowed) throw new InvalidOperationException("Only PDF, JPEG, and PNG documents are accepted."); var key = $"property-manager/{managerUserId:N}/{Guid.NewGuid():N}/{safeName}"; if (!string.IsNullOrWhiteSpace(request.ContentBase64)) { byte[] bytes; try { bytes = Convert.FromBase64String(request.ContentBase64); } catch { throw new InvalidOperationException("Document content is not valid base64."); } if (bytes.LongLength != request.SizeBytes) throw new InvalidOperationException("Document size does not match content."); await using var stream = new MemoryStream(bytes); await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(key, request.ContentType, 25 * 1024 * 1024), stream, cancellationToken); } var item = new MilestoneManagerDocument { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, Title = request.Title.Trim(), Category = request.Category.Trim(), FileName = safeName, ContentType = request.ContentType, SizeBytes = request.SizeBytes, StorageKey = key, AccessScope = request.OwnerUserId is null ? "COMMUNITY" : "OWNER", ExpiresOn = request.ExpiresOn }; db.MilestoneManagerDocuments.Add(item); db.MilestoneManagerDocumentVersions.Add(new MilestoneManagerDocumentVersion { DocumentId = item.Id, ManagerUserId = managerUserId, Version = 1, FileName = safeName, ContentType = request.ContentType, SizeBytes = request.SizeBytes, StorageKey = key, CreatedByUserId = managerUserId }); await AuditAsync(managerUserId, "DocumentStored", "Document", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<DocumentDownloadDto?> GetDocumentDownloadAsync(Guid actorUserId, bool isAdmin, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await db.MilestoneManagerDocuments.SingleOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted && !x.IsArchived, cancellationToken);
        if (document is null || (!isAdmin && document.ManagerUserId != actorUserId && document.OwnerUserId != actorUserId)) return null;
        var expiresAt = timeProvider.GetUtcNow().AddHours(24);
        var url = await storageProvider.CreateDownloadUrlAsync(document.StorageKey, expiresAt, cancellationToken);
        db.MilestoneManagerDocumentAccessEvents.Add(new MilestoneManagerDocumentAccessEvent { DocumentId = document.Id, ActorUserId = actorUserId, Action = "DOWNLOAD" });
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentDownloadDto(document.Id, document.FileName, document.ContentType, document.SizeBytes, url, expiresAt);
    }

    public async Task<DocumentExportDto> CreateDocumentExportAsync(Guid managerUserId, CreateDocumentExportRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var ids = request.DocumentIds?.Distinct().ToArray() ?? [];
        if (ids.Length is 0 or > 100) throw new InvalidOperationException("Select between 1 and 100 documents for an export.");
        var available = await db.MilestoneManagerDocuments
            .Where(x => x.ManagerUserId == managerUserId && ids.Contains(x.Id) && !x.IsDeleted && !x.IsArchived)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (available.Count != ids.Length) throw new InvalidOperationException("One or more documents are not available in this manager portfolio.");
        var item = new MilestoneManagerDocumentExport
        {
            ManagerUserId = managerUserId,
            DocumentIdsJson = JsonSerializer.Serialize(ids),
            Status = "QUEUED"
        };
        db.MilestoneManagerDocumentExports.Add(item);
        await AuditAsync(managerUserId, "DocumentExportQueued", "DocumentExport", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item, null);
    }

    public async Task<DocumentExportDto?> GetDocumentExportAsync(Guid managerUserId, Guid exportId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var item = await db.MilestoneManagerDocumentExports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == exportId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (item is null) return null;
        string? url = null;
        if (item.Status == "COMPLETED" && !string.IsNullOrWhiteSpace(item.ObjectKey) && item.ExpiresAt is { } expiresAt && expiresAt > timeProvider.GetUtcNow())
        {
            url = await storageProvider.CreateDownloadUrlAsync(item.ObjectKey, expiresAt, cancellationToken);
        }
        return ToDto(item, url);
    }

    public async Task<DocumentExportFile?> OpenDocumentExportAsync(Guid managerUserId, Guid exportId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var item = await db.MilestoneManagerDocumentExports.SingleOrDefaultAsync(x => x.Id == exportId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (item is null || !item.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(item.ObjectKey) || item.ExpiresAt is not { } expiresAt || expiresAt <= timeProvider.GetUtcNow())
        {
            return null;
        }

        var content = await storageProvider.OpenReadAsync(item.ObjectKey, cancellationToken);
        await AuditAsync(managerUserId, "DocumentExportDownloaded", "DocumentExport", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentExportFile(content, item.FileName ?? "nesty-documents.zip", "application/zip", expiresAt);
    }

    public async Task<GateMessageDto> CreateGateMessageAsync(Guid managerUserId, CreateGateMessageRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.PropertyId is { } gateProperty && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == gateProperty && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is not in the manager portfolio."); if (request.ValidUntil <= request.ValidFrom) throw new InvalidOperationException("Gate message expiry must be after its start time."); var item = new MilestoneManagerGateMessage { ManagerUserId = managerUserId, CommunityId = request.CommunityId, PropertyId = request.PropertyId, Recipient = request.Recipient.Trim(), Message = request.Message.Trim(), VisitorType = request.VisitorType.Trim(), ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil, IdempotencyKey = $"gate-{Guid.NewGuid():N}" }; db.MilestoneManagerGateMessages.Add(item); db.MilestoneManagerGateDeliveryAttempts.Add(new MilestoneManagerGateDeliveryAttempt { GateMessageId = item.Id, Recipient = item.Recipient, Status = "QUEUED", AttemptNumber = 1 }); await AuditAsync(managerUserId, "GateMessageCreated", "GateMessage", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<GateDeliveryAttemptDto>> ListGateDeliveryAttemptsAsync(Guid managerUserId, Guid gateMessageId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        if (!await db.MilestoneManagerGateMessages.AnyAsync(x => x.Id == gateMessageId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) return [];
        return await db.MilestoneManagerGateDeliveryAttempts.AsNoTracking().Where(x => x.GateMessageId == gateMessageId && !x.IsDeleted).OrderByDescending(x => x.AttemptNumber).Select(x => new GateDeliveryAttemptDto(x.Id, x.GateMessageId, x.Recipient, x.Status, x.ProviderReference, x.AttemptNumber, x.FailureReason, x.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<GateDeliveryAttemptDto?> RetryGateDeliveryAsync(Guid managerUserId, Guid gateMessageId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var message = await db.MilestoneManagerGateMessages.SingleOrDefaultAsync(x => x.Id == gateMessageId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (message is null) return null;
        var last = await db.MilestoneManagerGateDeliveryAttempts.Where(x => x.GateMessageId == gateMessageId && !x.IsDeleted).OrderByDescending(x => x.AttemptNumber).FirstOrDefaultAsync(cancellationToken);
        if (last is not null && last.Status == "DELIVERED") throw new InvalidOperationException("Gate message is already delivered.");
        var attempt = new MilestoneManagerGateDeliveryAttempt { GateMessageId = gateMessageId, Recipient = message.Recipient, AttemptNumber = (last?.AttemptNumber ?? 0) + 1, Status = "QUEUED" };
        db.MilestoneManagerGateDeliveryAttempts.Add(attempt);
        await AuditAsync(managerUserId, "GateMessageDeliveryRetried", "GateMessage", gateMessageId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new GateDeliveryAttemptDto(attempt.Id, attempt.GateMessageId, attempt.Recipient, attempt.Status, attempt.ProviderReference, attempt.AttemptNumber, attempt.FailureReason, attempt.CreatedAt);
    }

    public async Task<QrIssueDto> IssueQrAsync(Guid managerUserId, IssueQrRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.OwnerUserId is { } qrOwner) await RequireOwnerScopeAsync(managerUserId, qrOwner, cancellationToken); if (request.PropertyId is { } property && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == property && x.ManagerUserId == managerUserId && (!request.OwnerUserId.HasValue || x.OwnerUserId == request.OwnerUserId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is not in the manager portfolio."); if (request.ValidUntil <= request.ValidFrom) throw new InvalidOperationException("QR expiry must be after its start time."); var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); var item = new MilestoneManagerQrAccess { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, SubjectType = request.SubjectType.Trim().ToUpperInvariant(), TokenHash = HashToken(token), ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil }; db.MilestoneManagerQrAccesses.Add(item); await AuditAsync(managerUserId, "QrCreated", "ManagerQr", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return new QrIssueDto(item.Id, token, item.SubjectType, item.PropertyId, item.ValidFrom, item.ValidUntil); }

    public async Task<IReadOnlyList<QrAccessRecordDto>> ListQrAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var items = await db.MilestoneManagerQrAccesses.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return items.Select(item => new QrAccessRecordDto(item.Id, item.SubjectType, item.PropertyId, item.ValidFrom, item.ValidUntil, item.IsRevoked, item.ValidationCount, item.LastValidatedAt, item.IsRevoked ? "REVOKED" : item.ValidUntil <= now ? "EXPIRED" : item.ValidFrom > now ? "NOT_YET_VALID" : "ACTIVE")).ToList();
    }

    public async Task<IReadOnlyList<QrScanDto>> ListQrHistoryAsync(Guid managerUserId, Guid qrId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        if (!await db.MilestoneManagerQrAccesses.AnyAsync(x => x.Id == qrId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new KeyNotFoundException("QR access code not found.");
        return await db.MilestoneManagerQrScans.AsNoTracking().Where(x => x.QrAccessId == qrId && !x.IsDeleted).OrderByDescending(x => x.ScannedAt).Take(200).Select(x => new QrScanDto(x.Id, x.QrAccessId, x.GateGuardUserId, x.PropertyId, x.Result, x.ScannedAt)).ToListAsync(cancellationToken);
    }

    public async Task<QrValidationDto> ValidateQrAsync(string token, Guid? propertyId, Guid? gateGuardUserId, CancellationToken cancellationToken)
    { if (string.IsNullOrWhiteSpace(token)) return new QrValidationDto("INVALID", "Invalid", null, "", null, null, "QR token is required."); var item = await db.MilestoneManagerQrAccesses.SingleOrDefaultAsync(x => x.TokenHash == HashToken(token) && !x.IsDeleted, cancellationToken); var now = timeProvider.GetUtcNow(); var result = item is null ? "INVALID" : item.IsRevoked ? "REVOKED" : item.ValidFrom > now ? "NOT_YET_VALID" : item.ValidUntil <= now ? "EXPIRED" : propertyId is not null && item.PropertyId != propertyId ? "WRONG_PROPERTY" : "VALID"; if (item is not null) { item.ValidationCount++; item.LastValidatedAt = now; db.MilestoneManagerQrScans.Add(new MilestoneManagerQrScan { QrAccessId = item.Id, GateGuardUserId = gateGuardUserId, PropertyId = propertyId, Result = result }); await db.SaveChangesAsync(cancellationToken); } return new QrValidationDto(result, result.Replace('_', ' '), item?.PropertyId, item?.SubjectType ?? "", item?.ValidUntil, item?.Id, result == "VALID" ? "Access approved." : "Access denied."); }

    public async Task<QrValidationDto> RevokeQrAsync(Guid managerUserId, Guid qrId, string? reason, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerQrAccesses.SingleOrDefaultAsync(x => x.Id == qrId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) throw new InvalidOperationException("QR access code not found."); item.IsRevoked = true; item.RevokeReason = string.IsNullOrWhiteSpace(reason) ? "Manager initiated revoke" : reason.Trim(); await AuditAsync(managerUserId, "QrRevoked", "ManagerQr", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return new QrValidationDto("REVOKED", "Revoked", item.PropertyId, item.SubjectType, item.ValidUntil, item.Id, "Access revoked."); }

    public async Task<OwnerPortalDto> GetOwnerPortalAsync(Guid ownerUserId, CancellationToken cancellationToken)
    { var managers = await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => x.ManagerUserId).ToListAsync(cancellationToken); if (managers.Count == 0) throw new InvalidOperationException("Owner is not linked to a property manager."); var managerId = managers[0]; var properties = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var invoices = await db.MilestoneManagerInvoices.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var lines = await db.MilestoneManagerInvoiceLines.Where(x => invoices.Select(i => i.Id).Contains(x.InvoiceId) && !x.IsDeleted).ToListAsync(cancellationToken); var utilities = await db.MilestoneManagerUtilityCharges.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var maintenance = await db.MilestoneManagerMaintenances.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var notices = await GetNoticesAsync(ownerUserId, false, cancellationToken); var proposals = await db.MilestoneManagerProposals.Where(x => x.ManagerUserId == managerId && !x.IsDeleted).ToListAsync(cancellationToken); var proposalDtos = new List<ProposalDto>(); foreach (var proposal in proposals) proposalDtos.Add(await BuildProposalDtoAsync(proposal, cancellationToken)); var documents = (await GetDocumentsAsync(ownerUserId, false, cancellationToken)).ToList(); return new OwnerPortalDto(ownerUserId, properties.Select(ToDto).ToList(), invoices.Select(x => ToDto(x, lines.Where(l => l.InvoiceId == x.Id))).ToList(), await GetStatementAsync(ownerUserId, false, ownerUserId, null, null, cancellationToken), utilities.Select(ToDto).ToList(), maintenance.Select(ToDto).ToList(), notices, proposalDtos, documents); }

    private static readonly SemaphoreSlim AssignmentGate = new(1, 1);

    public async Task<IReadOnlyList<PropertyDto>> BulkAssignPropertiesAsync(Guid managerUserId, BulkAssignPropertiesRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        if (request.PropertyIds is null || request.PropertyIds.Count == 0) throw new InvalidOperationException("Select at least one property.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A reassignment reason is required.");
        var ids = request.PropertyIds.Distinct().ToArray();
        await AssignmentGate.WaitAsync(cancellationToken);
        try
        {
            var rows = await db.MilestoneManagerProperties.Where(x => ids.Contains(x.Id) && x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(cancellationToken);
            if (rows.Count != ids.Length) throw new InvalidOperationException("One or more selected properties are no longer in this portfolio.");
            var batchId = request.BatchId.GetValueOrDefault(Guid.NewGuid());
            var now = timeProvider.GetUtcNow();
            foreach (var row in rows)
            {
                var previous = row.OwnerUserId;
                if (row.OwnerUserId != request.OwnerUserId) row.OwnerUserId = request.OwnerUserId;
                row.UpdatedAt = now;
                db.MilestoneManagerPropertyAssignmentHistory.Add(new MilestoneManagerPropertyAssignmentHistory { ManagerUserId = managerUserId, PropertyId = row.Id, PreviousOwnerUserId = previous, NewOwnerUserId = request.OwnerUserId, ActorUserId = managerUserId, Reason = request.Reason.Trim(), BatchId = batchId, ChangedAt = now });
                await AuditAsync(managerUserId, "PropertyBulkReassigned", "ManagerProperty", row.Id, cancellationToken);
            }
            await db.SaveChangesAsync(cancellationToken);
            return rows.Select(ToDto).ToList();
        }
        finally { AssignmentGate.Release(); }
    }

    public async Task<IReadOnlyList<PropertyAssignmentHistoryDto>> GetPropertyAssignmentHistoryAsync(Guid managerUserId, Guid? propertyId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        return await db.MilestoneManagerPropertyAssignmentHistory.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && (!propertyId.HasValue || x.PropertyId == propertyId.Value) && !x.IsDeleted).OrderByDescending(x => x.ChangedAt).Take(500).Select(x => new PropertyAssignmentHistoryDto(x.Id, x.PropertyId, x.PreviousOwnerUserId, x.NewOwnerUserId, x.ActorUserId, x.Reason, x.BatchId, x.ChangedAt)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentOperationDto>> ListPaymentsAsync(Guid actorUserId, bool isAdmin, PaymentQuery query, CancellationToken cancellationToken)
    {
        var rows = db.MilestoneManagerPayments.AsNoTracking().Where(x => !x.IsDeleted);
        if (!isAdmin) rows = rows.Where(x => x.ManagerUserId == actorUserId || x.OwnerUserId == actorUserId);
        if (query.OwnerUserId.HasValue) rows = rows.Where(x => x.OwnerUserId == query.OwnerUserId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status)) rows = rows.Where(x => x.Status == query.Status.Trim().ToUpperInvariant());
        if (query.From.HasValue) rows = rows.Where(x => x.CreatedAt >= query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (query.To.HasValue) rows = rows.Where(x => x.CreatedAt < query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        return await rows.OrderByDescending(x => x.CreatedAt).Take(500).Select(x => new PaymentOperationDto(x.Id, x.InvoiceId, x.OwnerUserId, x.Amount, x.RefundedAmount, x.Provider, x.ProviderReference, x.Status, x.ReconciliationStatus, x.ReconciliationReference, x.RefundReason, x.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NestyStay.Application.PropertyManager.PaymentMethodDto>> ListPaymentMethodsAsync(Guid actorUserId, bool isAdmin, Guid? ownerUserId, CancellationToken cancellationToken)
    {
        var rows = db.MilestoneManagerPaymentMethods.AsNoTracking().Where(x => !x.IsDeleted);
        if (!isAdmin) rows = rows.Where(x => x.ManagerUserId == actorUserId || x.OwnerUserId == actorUserId);
        if (ownerUserId.HasValue) rows = rows.Where(x => x.OwnerUserId == ownerUserId.Value);
        return await rows.OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.CreatedAt).Select(x => new NestyStay.Application.PropertyManager.PaymentMethodDto(x.Id, x.OwnerUserId, x.Provider, x.Brand, x.Last4, x.ExpMonth, x.ExpYear, x.IsDefault)).ToListAsync(cancellationToken);
    }

    public async Task<NestyStay.Application.PropertyManager.PaymentMethodDto> SavePaymentMethodAsync(Guid actorUserId, bool isAdmin, NestyStay.Application.PropertyManager.SavePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var managerId = isAdmin ? (await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == request.OwnerUserId && !x.IsDeleted).Select(x => (Guid?)x.ManagerUserId).FirstOrDefaultAsync(cancellationToken) ?? actorUserId) : actorUserId;
        if (!isAdmin && actorUserId != request.OwnerUserId) await RequireOwnerScopeAsync(actorUserId, request.OwnerUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.ProviderReference) || request.Last4.Length != 4 || request.ExpMonth is < 1 or > 12 || request.ExpYear < DateTime.UtcNow.Year) throw new InvalidOperationException("Payment method details are invalid.");
        if (request.IsDefault) foreach (var old in await db.MilestoneManagerPaymentMethods.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == request.OwnerUserId && !x.IsDeleted).ToListAsync(cancellationToken)) old.IsDefault = false;
        var row = new MilestoneManagerPaymentMethod { ManagerUserId = managerId, OwnerUserId = request.OwnerUserId, Provider = request.Provider.Trim(), ProviderReference = request.ProviderReference.Trim(), Brand = request.Brand.Trim(), Last4 = request.Last4.Trim(), ExpMonth = request.ExpMonth, ExpYear = request.ExpYear, IsDefault = request.IsDefault };
        db.MilestoneManagerPaymentMethods.Add(row); await AuditAsync(actorUserId, "PaymentMethodSaved", "PaymentMethod", row.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken);
        return new NestyStay.Application.PropertyManager.PaymentMethodDto(row.Id, row.OwnerUserId, row.Provider, row.Brand, row.Last4, row.ExpMonth, row.ExpYear, row.IsDefault);
    }

    public async Task<PaymentOperationDto?> RefundPaymentAsync(Guid actorUserId, bool isAdmin, Guid paymentId, RefundPaymentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("Refund reason and idempotency key are required.");
        await PaymentGate.WaitAsync(cancellationToken);
        try
        {
            var scope = await db.MilestoneManagerPayments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId && !x.IsDeleted, cancellationToken);
            if (scope is null || (!isAdmin && scope.ManagerUserId != actorUserId)) return null;
            await using var transaction = await LockRecordAsync($"invoice:{scope.InvoiceId}", cancellationToken);
            var payment = await db.MilestoneManagerPayments.SingleOrDefaultAsync(x => x.Id == paymentId && !x.IsDeleted, cancellationToken);
            if (payment is null || (!isAdmin && payment.ManagerUserId != actorUserId)) return null;
            if (await db.MilestoneManagerPaymentAttempts.AnyAsync(x => x.PaymentId == paymentId && x.IdempotencyKey == request.IdempotencyKey && !x.IsDeleted, cancellationToken)) return ToPaymentDto(payment);
            var refundable = payment.Amount - payment.RefundedAmount;
            var amount = request.Amount ?? refundable;
            if (payment.Status is not ("CAPTURED" or "PARTIALLY_REFUNDED"))
                throw new InvalidOperationException("Only confirmed payments can be refunded.");
            if (amount <= 0 || amount > refundable) throw new InvalidOperationException("Refund amount is outside the refundable balance.");
            var refund = await paymentGateway.RefundAsync(new PaymentRefundRequest(payment.ProviderReference, amount, payment.Currency, request.Reason.Trim(), request.IdempotencyKey), cancellationToken);
            var attempt = new MilestoneManagerPaymentAttempt { PaymentId = payment.Id, InvoiceId = payment.InvoiceId, ManagerUserId = payment.ManagerUserId, Status = refund.Status.ToString().ToUpperInvariant(), ProviderReference = refund.RefundReference, AttemptNumber = 1, IdempotencyKey = request.IdempotencyKey };
            db.MilestoneManagerPaymentAttempts.Add(attempt);
            if (refund.Status != NestyStay.Domain.PaymentStatus.Refunded
                || refund.RefundedAmount != amount)
            {
                await AuditAsync(actorUserId, "PaymentRefundUnconfirmed", "Payment", payment.Id, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                throw new InvalidOperationException("The provider has not confirmed this refund. No balance was changed.");
            }
            payment.RefundedAmount += refund.RefundedAmount; payment.RefundReason = request.Reason.Trim(); payment.RefundedAt = refund.RefundedAt; payment.Status = payment.RefundedAmount >= payment.Amount ? "REFUNDED" : "PARTIALLY_REFUNDED"; payment.ReconciliationStatus = "PENDING";
            var invoice = await db.MilestoneManagerInvoices.SingleAsync(x => x.Id == payment.InvoiceId, cancellationToken); invoice.AmountPaid = Math.Max(0, invoice.AmountPaid - refund.RefundedAmount); invoice.Balance = invoice.Total - invoice.AmountPaid; invoice.Status = invoice.Balance <= 0 ? "PAID" : invoice.AmountPaid > 0 ? "PARTIALLY_PAID" : "ISSUED";
            db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry { ManagerUserId = payment.ManagerUserId, OwnerUserId = payment.OwnerUserId, InvoiceId = payment.InvoiceId, EntryType = "REFUND", Description = $"Refund for {invoice.InvoiceNumber}: {request.Reason.Trim()}", Amount = refund.RefundedAmount, OccurredOn = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime) });
            await AuditAsync(actorUserId, "PaymentRefunded", "Payment", payment.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return ToPaymentDto(payment);
        }
        finally { PaymentGate.Release(); }
    }

    public async Task<PaymentOperationDto?> RetryPaymentAsync(Guid actorUserId, bool isAdmin, Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await db.MilestoneManagerPayments.SingleOrDefaultAsync(x => x.Id == paymentId && !x.IsDeleted, cancellationToken);
        if (payment is null || (!isAdmin && payment.ManagerUserId != actorUserId && payment.OwnerUserId != actorUserId)) return null;
        if (payment.Status is not ("FAILED" or "RETRYABLE")) throw new InvalidOperationException("Only failed payments can be retried.");
        var invoice = await db.MilestoneManagerInvoices.SingleAsync(x => x.Id == payment.InvoiceId, cancellationToken);
        var result = await PayInvoiceAsync(actorUserId, isAdmin, invoice.Id, new PayInvoiceRequest(Math.Min(invoice.Balance, payment.Amount), $"retry-{payment.Id:N}-{Guid.NewGuid():N}"), cancellationToken);
        return result is null ? null : (await ListPaymentsAsync(actorUserId, isAdmin, new PaymentQuery(), cancellationToken)).FirstOrDefault(x => x.InvoiceId == invoice.Id && x.CreatedAt >= payment.CreatedAt);
    }

    public async Task<MeterReadingDto> RecordMeterReadingAsync(Guid managerUserId, RecordMeterReadingRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken); await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        var property = await db.MilestoneManagerProperties.SingleOrDefaultAsync(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && x.OwnerUserId == request.OwnerUserId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Property is not in the manager portfolio.");
        if (request.CurrentReading < request.PreviousReading || string.IsNullOrWhiteSpace(request.UtilityType) || string.IsNullOrWhiteSpace(request.BillingPeriod)) throw new InvalidOperationException("Meter readings must be non-negative and current must not be below previous.");
        var duplicate = await db.MilestoneManagerMeterReadings.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == property.Id && x.UtilityType == request.UtilityType.Trim() && x.BillingPeriod == request.BillingPeriod.Trim() && !x.IsDeleted, cancellationToken);
        if (duplicate) throw new InvalidOperationException("A meter reading already exists for this period.");
        var last = await db.MilestoneManagerMeterReadings.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == property.Id && x.UtilityType == request.UtilityType.Trim() && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (last is not null && request.PreviousReading != last.CurrentReading) throw new InvalidOperationException($"Previous reading must match the last recorded reading ({last.CurrentReading}).");
        var usage = request.CurrentReading - request.PreviousReading;
        var history = await db.MilestoneManagerMeterReadings.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == property.Id && x.UtilityType == request.UtilityType.Trim() && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(6).Select(x => x.Usage).ToListAsync(cancellationToken);
        var average = history.Count == 0 ? 0 : history.Average();
        var anomaly = average > 0 && usage > average * 1.3m;
        var reading = new MilestoneManagerMeterReading { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = property.Id, UtilityType = request.UtilityType.Trim(), BillingPeriod = request.BillingPeriod.Trim(), PreviousReading = request.PreviousReading, CurrentReading = request.CurrentReading, Usage = usage, IsAnomaly = anomaly, ReadingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{property.Id:N}|{request.UtilityType}|{request.BillingPeriod}|{request.PreviousReading}|{request.CurrentReading}"))).ToLowerInvariant() };
        db.MilestoneManagerMeterReadings.Add(reading);
        var schedule = await db.MilestoneManagerUtilitySchedules.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == property.Id && x.UtilityType == reading.UtilityType && x.IsActive && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (request.Rate is > 0) { if (schedule is null) { schedule = new MilestoneManagerUtilitySchedule { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = property.Id, UtilityType = reading.UtilityType, Rate = request.Rate.Value }; db.MilestoneManagerUtilitySchedules.Add(schedule); } else schedule.Rate = request.Rate.Value; }
        var rate = request.Rate ?? schedule?.Rate ?? 0m;
        if (rate > 0) await CreateUtilityChargeFromReadingAsync(managerUserId, request.OwnerUserId, property.Id, reading.UtilityType, reading.BillingPeriod, usage, rate, cancellationToken);
        await AuditAsync(managerUserId, anomaly ? "UtilityAnomalyDetected" : "MeterReadingRecorded", "MeterReading", reading.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken);
        return ToDto(reading);
    }

    private async Task CreateUtilityChargeFromReadingAsync(Guid managerUserId, Guid ownerUserId, Guid propertyId, string utilityType, string period, decimal usage, decimal rate, CancellationToken cancellationToken)
    {
        var amount = decimal.Round(usage * rate, 2, MidpointRounding.AwayFromZero);
        var invoice = new MilestoneManagerInvoice { ManagerUserId = managerUserId, OwnerUserId = ownerUserId, PropertyId = propertyId, InvoiceNumber = $"UTIL-{timeProvider.GetUtcNow():yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}", IssueDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), DueDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(30)), Subtotal = amount, Total = amount, Balance = amount, Currency = "USD", Status = "ISSUED" };
        var charge = new MilestoneManagerUtilityCharge { ManagerUserId = managerUserId, OwnerUserId = ownerUserId, PropertyId = propertyId, UtilityType = utilityType, BillingPeriod = period, Usage = usage, Rate = rate, Amount = amount, InvoiceId = invoice.Id };
        db.MilestoneManagerInvoices.Add(invoice); db.MilestoneManagerUtilityCharges.Add(charge); db.MilestoneManagerInvoiceLines.Add(new MilestoneManagerInvoiceLine { InvoiceId = invoice.Id, Description = $"{utilityType} utility {period}", Quantity = usage, UnitAmount = rate, Amount = amount }); db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry { ManagerUserId = managerUserId, OwnerUserId = ownerUserId, PropertyId = propertyId, InvoiceId = invoice.Id, EntryType = "UTILITY", Description = $"{utilityType} utility {period}", Amount = amount, OccurredOn = invoice.IssueDate });
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MeterReadingDto>> ListMeterReadingsAsync(Guid managerUserId, Guid propertyId, string? utilityType, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var rows = await db.MilestoneManagerMeterReadings.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.PropertyId == propertyId && (utilityType == null || x.UtilityType == utilityType) && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(cancellationToken); return rows.Select(ToDto).ToList(); }

    public async Task<UtilityScheduleDto> SaveUtilityScheduleAsync(Guid managerUserId, SaveUtilityScheduleRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequirePropertyScopeAsync(managerUserId, request.OwnerUserId, request.PropertyId, cancellationToken);
        if (request.DayOfMonth is < 1 or > 28 || request.Rate < 0 || string.IsNullOrWhiteSpace(request.UtilityType))
            throw new InvalidOperationException("Utility schedule is invalid.");
        var type = request.UtilityType.Trim().ToUpperInvariant();
        var row = await db.MilestoneManagerUtilitySchedules.SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.UtilityType == type && !x.IsDeleted, cancellationToken);
        if (row is null)
        {
            row = new MilestoneManagerUtilitySchedule { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, UtilityType = type };
            db.MilestoneManagerUtilitySchedules.Add(row);
        }
        row.OwnerUserId = request.OwnerUserId;
        row.Rate = request.Rate; row.DayOfMonth = request.DayOfMonth; row.IsActive = true;
        await AuditAsync(managerUserId, "UtilityScheduleSaved", "UtilitySchedule", row.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
    }

    public async Task<IReadOnlyList<UtilityDisputeDto>> ListUtilityDisputesAsync(Guid managerUserId, CancellationToken cancellationToken) { var rows = await db.MilestoneManagerUtilityDisputes.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken); return rows.Select(ToDto).ToList(); }

    public async Task<UtilityDisputeDto> CreateUtilityDisputeAsync(Guid actorUserId, bool isAdmin, CreateUtilityDisputeRequest request, CancellationToken cancellationToken)
    { var charge = await db.MilestoneManagerUtilityCharges.SingleOrDefaultAsync(x => x.Id == request.UtilityChargeId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Utility charge not found."); if (!isAdmin && actorUserId != charge.OwnerUserId && actorUserId != charge.ManagerUserId) throw new UnauthorizedAccessException("Utility charge is outside your scope."); var dispute = new MilestoneManagerUtilityDispute { ManagerUserId = charge.ManagerUserId, OwnerUserId = charge.OwnerUserId, UtilityChargeId = charge.Id, Reason = request.Reason.Trim(), Status = "OPEN" }; db.MilestoneManagerUtilityDisputes.Add(dispute); await AuditAsync(actorUserId, "UtilityDisputeOpened", "UtilityDispute", dispute.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(dispute); }

    public async Task<UtilityDisputeDto?> DecideUtilityDisputeAsync(Guid managerUserId, Guid disputeId, DecideUtilityDisputeRequest request, CancellationToken cancellationToken)
    { var dispute = await db.MilestoneManagerUtilityDisputes.SingleOrDefaultAsync(x => x.Id == disputeId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (dispute is null) return null; if (request.Status is not ("APPROVED" or "REJECTED")) throw new InvalidOperationException("Dispute status must be APPROVED or REJECTED."); dispute.Status = request.Status; dispute.Decision = request.Decision.Trim(); dispute.AdjustmentAmount = request.AdjustmentAmount; dispute.DecidedByUserId = managerUserId; dispute.DecidedAt = timeProvider.GetUtcNow(); await AuditAsync(managerUserId, "UtilityDisputeDecided", "UtilityDispute", dispute.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(dispute); }

    public async Task<IReadOnlyList<MaintenanceActivityDto>> ListMaintenanceActivityAsync(Guid actorUserId, bool isAdmin, Guid maintenanceId, CancellationToken cancellationToken)
    { var item = await db.MilestoneManagerMaintenances.AsNoTracking().SingleOrDefaultAsync(x => x.Id == maintenanceId && !x.IsDeleted, cancellationToken); if (item is null || (!isAdmin && item.ManagerUserId != actorUserId && item.OwnerUserId != actorUserId)) return []; return await db.MilestoneManagerMaintenanceActivities.AsNoTracking().Where(x => x.MaintenanceId == maintenanceId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Select(x => new MaintenanceActivityDto(x.Id, x.MaintenanceId, x.ActorUserId, x.Action, x.Details, x.CreatedAt)).ToListAsync(cancellationToken); }

    public async Task<MaintenanceAttachmentDto> AddMaintenanceAttachmentAsync(Guid managerUserId, AddMaintenanceAttachmentRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var legacy = await db.MilestoneManagerMaintenances.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.MaintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); var professional = await db.MilestonePmMaintenanceCases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.MaintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (legacy is null && professional is null) throw new InvalidOperationException("Maintenance request not found."); var safe = Path.GetFileName(request.FileName); var contentType = request.ContentType.Trim().ToLowerInvariant(); if (safe != request.FileName || string.IsNullOrWhiteSpace(request.ContentBase64) || contentType is not ("application/pdf" or "image/jpeg" or "image/png")) throw new InvalidOperationException("Attachment is invalid."); byte[] bytes; try { bytes = Convert.FromBase64String(request.ContentBase64); } catch { throw new InvalidOperationException("Attachment content is not valid base64."); } if (bytes.Length == 0 || bytes.Length > 25 * 1024 * 1024) throw new InvalidOperationException("Attachment must be between 1 byte and 25 MB."); var key = $"property-manager/{managerUserId:N}/maintenance/{request.MaintenanceId:N}/{Guid.NewGuid():N}-{safe}"; await using var stream = new MemoryStream(bytes); await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(key, contentType, 25 * 1024 * 1024), stream, cancellationToken); var attachment = new MilestoneManagerMaintenanceAttachment { ManagerUserId = managerUserId, MaintenanceId = request.MaintenanceId, FileName = safe, ContentType = contentType, StorageKey = key }; db.MilestoneManagerMaintenanceAttachments.Add(attachment); await AuditAsync(managerUserId, "MaintenanceAttachmentAdded", "Maintenance", request.MaintenanceId, cancellationToken); await db.SaveChangesAsync(cancellationToken); return new MaintenanceAttachmentDto(attachment.Id, attachment.MaintenanceId, attachment.FileName, attachment.ContentType, attachment.Status, attachment.CreatedAt); }
    public async Task<IReadOnlyList<MaintenanceAttachmentDto>> ListMaintenanceAttachmentsAsync(Guid managerUserId, Guid maintenanceId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        if (!await db.MilestoneManagerMaintenances.AnyAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken) && !await db.MilestonePmMaintenanceCases.AnyAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Maintenance request not found.");
        return (await db.MilestoneManagerMaintenanceAttachments.AsNoTracking().Where(x => x.MaintenanceId == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken))
            .Select(x => new MaintenanceAttachmentDto(x.Id, x.MaintenanceId, x.FileName, x.ContentType, x.Status, x.CreatedAt)).ToList();
    }

    public async Task<DocumentDownloadDto?> GetMaintenanceAttachmentDownloadAsync(Guid managerUserId, Guid maintenanceId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var attachment = await db.MilestoneManagerMaintenanceAttachments.SingleOrDefaultAsync(x => x.Id == attachmentId && x.MaintenanceId == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (attachment is null) return null;
        var expiresAt = timeProvider.GetUtcNow().AddHours(1);
        var url = await storageProvider.CreateDownloadUrlAsync(attachment.StorageKey, expiresAt, cancellationToken);
        await AuditAsync(managerUserId, "MaintenanceAttachmentDownloaded", "Maintenance", maintenanceId, cancellationToken);
        return new DocumentDownloadDto(attachment.Id, attachment.FileName, attachment.ContentType, 0, url, expiresAt);
    }

    public async Task<VendorDto?> UpdateVendorAsync(Guid managerUserId, Guid vendorId, UpdateVendorRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerVendors.SingleOrDefaultAsync(x => x.Id == vendorId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) return null; if (request.Contact is not null) item.Contact = request.Contact.Trim(); if (request.Notes is not null) item.Notes = request.Notes.Trim(); if (request.ServiceAreas is not null) item.ServiceAreasJson = JsonSerializer.Serialize(request.ServiceAreas); if (request.AvailabilityJson is not null) item.AvailabilityJson = request.AvailabilityJson; if (request.Rate is not null) item.Rate = request.Rate; if (request.Rating is not null) item.Rating = Math.Clamp(request.Rating.Value, 0, 5); if (request.IsPreferred is not null) item.IsPreferred = request.IsPreferred.Value; if (request.IsSuspended is not null) item.IsSuspended = request.IsSuspended.Value; if (request.IsActive is not null) item.IsActive = request.IsActive.Value; item.UpdatedAt = timeProvider.GetUtcNow(); await AuditAsync(managerUserId, "VendorUpdated", "Vendor", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<VendorDocumentDto> AddVendorDocumentAsync(Guid managerUserId, AddVendorDocumentRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var vendor = await db.MilestoneManagerVendors.SingleOrDefaultAsync(x => x.Id == request.VendorId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Vendor not found."); var safe = Path.GetFileName(request.FileName); if (safe != request.FileName || string.IsNullOrWhiteSpace(request.ContentBase64)) throw new InvalidOperationException("Vendor document is invalid."); byte[] bytes; try { bytes = Convert.FromBase64String(request.ContentBase64); } catch { throw new InvalidOperationException("Vendor document content is not valid base64."); } if (bytes.Length > 25 * 1024 * 1024) throw new InvalidOperationException("Vendor document is too large."); var key = $"property-manager/{managerUserId:N}/vendors/{vendor.Id:N}/{Guid.NewGuid():N}-{safe}"; await using var stream = new MemoryStream(bytes); await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(key, request.ContentType, 25 * 1024 * 1024), stream, cancellationToken); var item = new MilestoneManagerVendorDocument { ManagerUserId = managerUserId, VendorId = vendor.Id, DocumentType = request.DocumentType.Trim().ToUpperInvariant(), FileName = safe, ContentType = request.ContentType, StorageKey = key, ExpiresOn = request.ExpiresOn }; db.MilestoneManagerVendorDocuments.Add(item); await AuditAsync(managerUserId, "VendorDocumentAdded", "Vendor", vendor.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<VendorDocumentDto>> ListVendorDocumentsAsync(Guid managerUserId, Guid vendorId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (!await db.MilestoneManagerVendors.AnyAsync(x => x.Id == vendorId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) return []; return (await db.MilestoneManagerVendorDocuments.AsNoTracking().Where(x => x.VendorId == vendorId && x.ManagerUserId == managerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

    public async Task<NoticeInteractionDto> CommentOnNoticeAsync(Guid actorUserId, bool isAdmin, CommentNoticeRequest request, CancellationToken cancellationToken)
    {
        var notice = await db.MilestoneManagerNotices.SingleOrDefaultAsync(x => x.Id == request.NoticeId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Notice not found.");
        if (!isAdmin && notice.ManagerUserId != actorUserId && !await CanOwnerReadNoticeAsync(actorUserId, notice, cancellationToken)) throw new UnauthorizedAccessException("Notice is outside your scope.");
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 4000) throw new InvalidOperationException("A comment of up to 4000 characters is required.");
        var item = new MilestoneManagerNoticeComment { NoticeId = notice.Id, AuthorUserId = actorUserId, Body = request.Body.Trim() };
        db.MilestoneManagerNoticeComments.Add(item);
        await AuditAsync(actorUserId, "NoticeCommented", "Notice", notice.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new NoticeInteractionDto(item.Id, item.NoticeId, item.AuthorUserId, "COMMENT", item.Body, item.CreatedAt);
    }

    public async Task<NoticeInteractionDto> AcknowledgeNoticeAsync(Guid ownerUserId, Guid noticeId, CancellationToken cancellationToken)
    {
        var notice = await db.MilestoneManagerNotices.SingleOrDefaultAsync(x => x.Id == noticeId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Notice not found.");
        if (!await CanOwnerReadNoticeAsync(ownerUserId, notice, cancellationToken)) throw new UnauthorizedAccessException("Notice is outside your scope.");
        var existing = await db.MilestoneManagerNoticeAcknowledgements.SingleOrDefaultAsync(x => x.NoticeId == noticeId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        if (existing is null)
        {
            existing = new MilestoneManagerNoticeAcknowledgement { NoticeId = noticeId, OwnerUserId = ownerUserId, AcknowledgedAt = timeProvider.GetUtcNow() };
            db.MilestoneManagerNoticeAcknowledgements.Add(existing);
            await AuditAsync(ownerUserId, "NoticeAcknowledged", "Notice", noticeId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        return new NoticeInteractionDto(existing.Id, existing.NoticeId, existing.OwnerUserId, "ACKNOWLEDGEMENT", "Acknowledged", existing.AcknowledgedAt);
    }

    public async Task<ProposalDiscussionDto> AddProposalDiscussionAsync(Guid actorUserId, bool isAdmin, AddProposalDiscussionRequest request, CancellationToken cancellationToken)
    { var proposal = await db.MilestoneManagerProposals.SingleOrDefaultAsync(x => x.Id == request.ProposalId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Proposal not found."); var eligible = await db.MilestoneManagerEligibleVoters.AnyAsync(x => x.ProposalId == proposal.Id && x.OwnerUserId == actorUserId && !x.IsDeleted, cancellationToken); if (!isAdmin && !eligible && proposal.ManagerUserId != actorUserId) throw new UnauthorizedAccessException("You cannot discuss this proposal."); if (string.IsNullOrWhiteSpace(request.Body)) throw new InvalidOperationException("Discussion body is required."); var item = new MilestoneManagerProposalDiscussion { ProposalId = proposal.Id, AuthorUserId = actorUserId, Body = request.Body.Trim() }; db.MilestoneManagerProposalDiscussions.Add(item); await AuditAsync(actorUserId, "ProposalDiscussionAdded", "Proposal", proposal.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return new ProposalDiscussionDto(item.Id, item.ProposalId, item.AuthorUserId, item.Body, item.CreatedAt); }

    public async Task<ProposalDto?> CloseProposalAsync(Guid managerUserId, Guid proposalId, CancellationToken cancellationToken)
    { await GovernanceGate.WaitAsync(cancellationToken); try { var proposal = await db.MilestoneManagerProposals.SingleOrDefaultAsync(x => x.Id == proposalId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (proposal is null) return null; var votes = await db.MilestoneManagerVotes.Where(x => x.ProposalId == proposal.Id && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken); var result = votes.GroupBy(x => x.Choice).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase); var canonical = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }); proposal.ResultJson = canonical; proposal.ResultProofHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(); proposal.Status = "CLOSED"; proposal.ClosedAt = timeProvider.GetUtcNow(); await AuditAsync(managerUserId, "ProposalClosed", "Proposal", proposal.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return await BuildProposalDtoAsync(proposal, cancellationToken); } finally { GovernanceGate.Release(); } }

    public async Task<DocumentDto?> ArchiveDocumentAsync(Guid managerUserId, Guid documentId, bool restore, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerDocuments.SingleOrDefaultAsync(x => x.Id == documentId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) return null; item.IsArchived = !restore; await AuditAsync(managerUserId, restore ? "DocumentRestored" : "DocumentArchived", "Document", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<DocumentVersionDto>> ListDocumentVersionsAsync(Guid actorUserId, bool isAdmin, Guid documentId, CancellationToken cancellationToken)
    { var document = await db.MilestoneManagerDocuments.SingleOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted, cancellationToken); if (document is null || (!isAdmin && document.ManagerUserId != actorUserId && document.OwnerUserId != actorUserId)) return []; return (await db.MilestoneManagerDocumentVersions.AsNoTracking().Where(x => x.DocumentId == documentId && !x.IsDeleted).OrderByDescending(x => x.Version).ToListAsync(cancellationToken)).Select(x => new DocumentVersionDto(x.Id, x.DocumentId, x.Version, x.FileName, x.ContentType, x.SizeBytes, x.CreatedByUserId, x.CreatedAt)).ToList(); }

    public async Task<DocumentVersionDto> AddDocumentVersionAsync(Guid managerUserId, AddDocumentVersionRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        var document = await db.MilestoneManagerDocuments.SingleOrDefaultAsync(x => x.Id == request.DocumentId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Document not found.");
        if (document.IsArchived) throw new InvalidOperationException("Archived documents cannot receive new versions.");
        var safe = Path.GetFileName(request.FileName);
        if (safe != request.FileName || string.IsNullOrWhiteSpace(safe)) throw new InvalidOperationException("Document filename is invalid.");
        if (request.SizeBytes <= 0 || request.SizeBytes > 25 * 1024 * 1024) throw new InvalidOperationException("Document size must be between 1 byte and 25 MB.");
        if (request.ContentType.ToLowerInvariant() is not ("application/pdf" or "image/jpeg" or "image/png")) throw new InvalidOperationException("Only PDF, JPEG, and PNG documents are accepted.");
        byte[] bytes; try { bytes = Convert.FromBase64String(request.ContentBase64); } catch { throw new InvalidOperationException("Document content is not valid base64."); }
        if (bytes.LongLength != request.SizeBytes) throw new InvalidOperationException("Document size does not match content.");
        var key = $"property-manager/{managerUserId:N}/documents/{document.Id:N}/{Guid.NewGuid():N}/{safe}";
        await using (var stream = new MemoryStream(bytes)) await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(key, request.ContentType, 25 * 1024 * 1024), stream, cancellationToken);
        var version = document.CurrentVersion + 1;
        document.CurrentVersion = version; document.FileName = safe; document.ContentType = request.ContentType; document.SizeBytes = request.SizeBytes; document.StorageKey = key; document.UpdatedAt = timeProvider.GetUtcNow();
        var row = new MilestoneManagerDocumentVersion { DocumentId = document.Id, ManagerUserId = managerUserId, Version = version, FileName = safe, ContentType = request.ContentType, SizeBytes = request.SizeBytes, StorageKey = key, CreatedByUserId = managerUserId };
        db.MilestoneManagerDocumentVersions.Add(row);
        await AuditAsync(managerUserId, "DocumentVersionAdded", "Document", document.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentVersionDto(row.Id, row.DocumentId, row.Version, row.FileName, row.ContentType, row.SizeBytes, row.CreatedByUserId, row.CreatedAt);
    }

    public async Task<ManagerProfileDto> ChangeSubscriptionAsync(Guid managerUserId, ChangeSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        ApplyDueSubscriptionChange(manager);
        var action = request.Action.Trim().ToUpperInvariant();
        if (action is not ("PAUSE" or "RESUME" or "UPGRADE" or "DOWNGRADE" or "CANCEL" or "REACTIVATE" or "AUTO_RENEW"))
            throw new InvalidOperationException("Unsupported subscription action.");

        var now = timeProvider.GetUtcNow();
        var from = manager.SubscriptionTier;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var toTier = manager.SubscriptionTier;
        var status = "COMPLETED";

        if (action is "UPGRADE" or "DOWNGRADE")
        {
            var requestedTier = NormalizeSubscriptionTier(request.TargetTier, action == "UPGRADE" ? "Enterprise" : "Starter");
            var currentRank = SubscriptionTierRank(manager.SubscriptionTier);
            var targetRank = SubscriptionTierRank(requestedTier);
            if (action == "UPGRADE" && targetRank <= currentRank)
                throw new InvalidOperationException("An upgrade must select a higher subscription tier.");
            if (action == "DOWNGRADE" && targetRank >= currentRank)
                throw new InvalidOperationException("A downgrade must select a lower subscription tier.");

            if (action == "DOWNGRADE")
            {
                manager.PendingSubscriptionTier = requestedTier;
                manager.PendingSubscriptionEffectiveAt = manager.NextBillingAt > now ? manager.NextBillingAt : now.AddMonths(1);
                toTier = requestedTier;
                status = "SCHEDULED";
            }
            else
            {
                manager.SubscriptionTier = requestedTier;
                manager.PendingSubscriptionTier = null;
                manager.PendingSubscriptionEffectiveAt = null;
                manager.NextBillingAt = now.AddMonths(1);
                toTier = requestedTier;
            }
        }
        else if (action == "PAUSE")
        {
            manager.SubscriptionStatus = "PAUSED";
            manager.BillingProviderStatus = "PAUSED_LOCAL";
        }
        else if (action is "RESUME" or "REACTIVATE")
        {
            manager.SubscriptionStatus = "ACTIVE";
            manager.BillingProviderStatus = "LOCAL_TEST_READY";
            manager.CancellationReason = null;
            if (action == "REACTIVATE") manager.AutoRenew = true;
        }
        else if (action == "CANCEL")
        {
            manager.SubscriptionStatus = "CANCELLED";
            manager.AutoRenew = false;
            manager.CancellationReason = reason ?? "Cancelled by manager.";
            manager.BillingProviderStatus = "CANCELLED_LOCAL";
        }
        else if (action == "AUTO_RENEW")
        {
            if (request.AutoRenew is null) throw new InvalidOperationException("Auto-renew preference is required.");
            manager.AutoRenew = request.AutoRenew.Value;
            if (manager.AutoRenew && manager.SubscriptionStatus == "CANCELLED")
            {
                manager.SubscriptionStatus = "ACTIVE";
                manager.BillingProviderStatus = "LOCAL_TEST_READY";
                manager.CancellationReason = null;
            }
        }

        db.MilestoneManagerSubscriptionEvents.Add(new MilestoneManagerSubscriptionEvent
        {
            ManagerUserId = managerUserId,
            EventType = action,
            FromTier = from,
            ToTier = toTier,
            Status = status,
            Reason = reason,
            EffectiveAt = status == "SCHEDULED" ? manager.PendingSubscriptionEffectiveAt!.Value : now
        });
        await AuditAsync(managerUserId, $"Subscription{action}", "PropertyManager", manager.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var unitsUsed = await db.MilestoneManagerProperties.CountAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        return ToDto(manager, unitsUsed);
    }

    public async Task<IReadOnlyList<SubscriptionEventDto>> ListSubscriptionEventsAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        if (ApplyDueSubscriptionChange(manager)) await db.SaveChangesAsync(cancellationToken);
        return (await db.MilestoneManagerSubscriptionEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).OrderByDescending(x => x.EffectiveAt).ToListAsync(cancellationToken)).Select(x => new SubscriptionEventDto(x.Id, x.EventType, x.FromTier, x.ToTier, x.Status, x.Reason, x.EffectiveAt)).ToList();
    }

    public async Task<int> ApplyDueSubscriptionChangesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var managers = await db.MilestonePropertyManagers
            .Where(x => !x.IsDeleted && x.PendingSubscriptionTier != null && x.PendingSubscriptionEffectiveAt <= now)
            .OrderBy(x => x.PendingSubscriptionEffectiveAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        var applied = 0;
        foreach (var manager in managers)
        {
            if (!ApplyDueSubscriptionChange(manager)) continue;
            await AuditAsync(manager.ManagerUserId, "SubscriptionDowngradeApplied", "PropertyManager", manager.Id, cancellationToken);
            applied++;
        }
        if (applied > 0) await db.SaveChangesAsync(cancellationToken);
        return applied;
    }

    public async Task<SubscriptionEventDto> RetrySubscriptionPaymentAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        ApplyDueSubscriptionChange(manager);
        manager.BillingProviderStatus = "RETRY_QUEUED";
        var item = new MilestoneManagerSubscriptionEvent { ManagerUserId = managerUserId, EventType = "PAYMENT_RETRY", FromTier = manager.SubscriptionTier, ToTier = manager.SubscriptionTier, Status = "RETRY_QUEUED", Reason = "Manager requested a billing retry.", EffectiveAt = timeProvider.GetUtcNow() };
        db.MilestoneManagerSubscriptionEvents.Add(item);
        await AuditAsync(managerUserId, "SubscriptionPaymentRetryQueued", "PropertyManager", manager.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new SubscriptionEventDto(item.Id, item.EventType, item.FromTier, item.ToTier, item.Status, item.Reason, item.EffectiveAt);
    }

    public async Task<IReadOnlyList<InvitationEventDto>> ListInvitationEventsAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); return (await db.MilestoneManagerInvitationEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.OwnerUserId == ownerUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(x => new InvitationEventDto(x.Id, x.OwnerUserId, x.EventType, x.ProviderReference, x.CreatedAt)).ToList(); }

    public async Task<IReadOnlyList<OwnerVerificationDto>> ListOwnerVerificationAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); await RequireOwnerScopeAsync(managerUserId, ownerUserId, cancellationToken); return (await db.MilestoneManagerOwnerVerifications.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.OwnerUserId == ownerUserId && !x.IsDeleted).OrderBy(x => x.Requirement).ToListAsync(cancellationToken)).Select(x => new OwnerVerificationDto(x.Id, x.OwnerUserId, x.Requirement, x.Status, x.Reason, x.DocumentKey, x.CreatedAt)).ToList(); }

    public async Task<OwnerVerificationDto> DecideOwnerVerificationAsync(Guid managerUserId, DecideOwnerVerificationRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        var status = request.Status.Trim().ToUpperInvariant();
        if (status is not ("PENDING" or "APPROVED" or "REJECTED" or "CHANGES_REQUESTED") || string.IsNullOrWhiteSpace(request.Requirement) || string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("A valid verification status, requirement and decision reason are required.");
        await RequireDocumentScopeAsync(managerUserId, request.OwnerUserId, request.DocumentKey, cancellationToken);
        // Decisions are events, not a mutable current-row projection.
        var item = new MilestoneManagerOwnerVerification { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, Requirement = request.Requirement.Trim().ToUpperInvariant(), ActorUserId = managerUserId, Status = status, Reason = request.Reason.Trim(), DocumentKey = request.DocumentKey };
        db.MilestoneManagerOwnerVerifications.Add(item);
        if (item.Requirement == "ACCOUNT")
        {
            var owner = await db.MilestoneManagerOwners.SingleAsync(x => x.ManagerUserId == managerUserId && x.OwnerUserId == request.OwnerUserId && !x.IsDeleted, cancellationToken);
            owner.VerificationStatus = status == "APPROVED" ? "VERIFIED" : status;
        }
        await AuditAsync(managerUserId, $"OwnerRequirement{status}", "OwnerVerification", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new OwnerVerificationDto(item.Id, item.OwnerUserId, item.Requirement, item.Status, item.Reason, item.DocumentKey, item.CreatedAt);
    }

    public async Task<DashboardPreferenceDto> GetDashboardPreferenceAsync(Guid managerUserId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerDashboardPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken) ?? new MilestoneManagerDashboardPreference { ManagerUserId = managerUserId }; return ToDto(item); }

    public async Task<DashboardPreferenceDto> SaveDashboardPreferenceAsync(Guid managerUserId, SaveDashboardPreferenceRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        ValidateJson(request.SavedFiltersJson ?? "{}", JsonValueKind.Object);
        var item = await db.MilestoneManagerDashboardPreferences.SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (item is null)
        {
            item = new MilestoneManagerDashboardPreference { ManagerUserId = managerUserId };
            db.MilestoneManagerDashboardPreferences.Add(item);
        }
        item.KpiOrderJson = JsonSerializer.Serialize(request.KpiOrder ?? []);
        item.VisibleKpisJson = JsonSerializer.Serialize(request.VisibleKpis ?? []);
        item.SavedFiltersJson = request.SavedFiltersJson ?? "{}";
        item.SavedViewsJson = JsonSerializer.Serialize(request.SavedViews ?? []);
        await AuditAsync(managerUserId, "DashboardPreferencesSaved", "DashboardPreference", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<AgreementDto> SaveAgreementAsync(Guid managerUserId, SaveAgreementRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequirePropertyScopeAsync(managerUserId, request.OwnerUserId, request.PropertyId, cancellationToken);
        if (request.EffectiveTo < request.EffectiveFrom || request.MaintenanceApprovalLimit < 0 || request.ExpenseApprovalLimit < 0)
            throw new InvalidOperationException("Agreement dates and authority limits are invalid.");
        ValidateJson(request.FeeRuleJson, JsonValueKind.Object);
        await RequireDocumentScopeAsync(managerUserId, request.OwnerUserId, request.SignedDocumentKey, cancellationToken);
        var previousVersion = await db.MilestoneManagementAgreements.Where(x => x.ManagerUserId == managerUserId && x.OwnerUserId == request.OwnerUserId && x.PropertyId == request.PropertyId).Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0;
        var item = new MilestoneManagementAgreement
        {
            ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId,
            Version = previousVersion + 1, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            FeeRuleJson = request.FeeRuleJson, MaintenanceApprovalLimit = request.MaintenanceApprovalLimit,
            ExpenseApprovalLimit = request.ExpenseApprovalLimit, SignedDocumentKey = request.SignedDocumentKey, Status = "DRAFT"
        };
        db.MilestoneManagementAgreements.Add(item);
        await AuditAsync(managerUserId, "AgreementDraftSaved", "ManagementAgreement", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<FeeRuleDto> SaveFeeRuleAsync(Guid managerUserId, SaveFeeRuleRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.Percentage < 0 || request.Percentage > 100 || request.FixedAmount < 0) throw new InvalidOperationException("Fee rule values are invalid."); var item = new MilestoneManagementFeeRule { ManagerUserId = managerUserId, PropertyId = request.PropertyId, RuleType = request.RuleType.Trim().ToUpperInvariant(), Percentage = request.Percentage, FixedAmount = request.FixedAmount, CleaningMarkup = request.CleaningMarkup, MaintenanceMarkup = request.MaintenanceMarkup, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo }; db.MilestoneManagementFeeRules.Add(item); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<OwnerPayoutDto>> ListOwnerPayoutsAsync(Guid managerUserId, Guid? ownerUserId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); return (await db.MilestoneOwnerPayouts.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && (!ownerUserId.HasValue || x.OwnerUserId == ownerUserId) && !x.IsDeleted).OrderByDescending(x => x.PeriodTo).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

    public async Task<OwnerPayoutDto> CreateOwnerPayoutAsync(Guid managerUserId, CreateOwnerPayoutRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken); if (request.Amount < 0 || request.PeriodTo < request.PeriodFrom) throw new InvalidOperationException("Payout period or amount is invalid."); var item = new MilestoneOwnerPayout { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PeriodFrom = request.PeriodFrom, PeriodTo = request.PeriodTo, Amount = request.Amount, Status = "PAYABLE" }; db.MilestoneOwnerPayouts.Add(item); await AuditAsync(managerUserId, "OwnerPayoutCreated", "OwnerPayout", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<OwnerApprovalDto> CreateOwnerApprovalAsync(Guid managerUserId, CreateOwnerApprovalRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequirePropertyScopeAsync(managerUserId, request.OwnerUserId, request.PropertyId, cancellationToken);
        if (request.Amount < 0 || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.ApprovalType))
            throw new InvalidOperationException("Approval type, description and a non-negative amount are required.");
        // No client-controlled threshold may waive an owner's decision. Activation
        // and threshold delegation are separate agreement operations.
        var item = new MilestoneOwnerApproval { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, ApprovalType = request.ApprovalType.Trim().ToUpperInvariant(), Description = request.Description.Trim(), Amount = request.Amount, Limit = 0, Status = "REQUIRED" };
        db.MilestoneOwnerApprovals.Add(item);
        await AuditAsync(managerUserId, "OwnerApprovalCreated", "OwnerApproval", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<OwnerApprovalDto?> DecideOwnerApprovalAsync(Guid ownerUserId, Guid approvalId, DecideOwnerApprovalRequest request, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneOwnerApprovals.SingleOrDefaultAsync(x => x.Id == approvalId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken);
        if (item is null) return null;
        if (item.Status != "REQUIRED") throw new InvalidOperationException("This approval already has a decision.");
        if (request.Status is not ("APPROVED" or "REJECTED" or "CHANGES_REQUESTED") || string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("A valid decision and reason are required.");
        item.Status = request.Status; item.DecisionReason = request.Reason.Trim(); item.DecidedByUserId = ownerUserId;
        await AuditAsync(ownerUserId, $"OwnerApproval{request.Status}", "OwnerApproval", item.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<StaffDto> InviteStaffAsync(Guid managerUserId, InviteStaffRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.Role is null || string.IsNullOrWhiteSpace(request.Role)) throw new InvalidOperationException("Staff role is required."); var item = new MilestoneManagerStaff { ManagerUserId = managerUserId, StaffUserId = request.StaffUserId, Role = request.Role.Trim().ToUpperInvariant(), PropertyScopeJson = JsonSerializer.Serialize(request.PropertyIds ?? []), OwnerScopeJson = JsonSerializer.Serialize(request.OwnerIds ?? []), CanManageFinance = request.CanManageFinance, ApprovalLimit = request.ApprovalLimit, Status = "INVITED" }; db.MilestoneManagerStaff.Add(item); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<StaffDto>> ListStaffAsync(Guid managerUserId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); return (await db.MilestoneManagerStaff.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

    public async Task<CalendarEventDto> CreateCalendarEventAsync(Guid managerUserId, CreateCalendarEventRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.EndsAt <= request.StartsAt) throw new InvalidOperationException("Calendar event end must be after start."); if (request.PropertyId is { } property && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == property && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is outside manager portfolio."); var item = new MilestoneManagerCalendarEvent { ManagerUserId = managerUserId, PropertyId = request.PropertyId, OwnerUserId = request.OwnerUserId, EventType = request.EventType.Trim(), Title = request.Title.Trim(), StartsAt = request.StartsAt.ToUniversalTime(), EndsAt = request.EndsAt.ToUniversalTime(), Status = request.Status.Trim().ToUpperInvariant(), SourceType = "MANUAL" }; db.MilestoneManagerCalendarEvents.Add(item); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<CalendarEventDto>> ListCalendarEventsAsync(Guid managerUserId, CalendarEventQuery query, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var rows = db.MilestoneManagerCalendarEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (query.From is { } from) rows = rows.Where(x => x.EndsAt >= from); if (query.To is { } to) rows = rows.Where(x => x.StartsAt <= to); if (query.PropertyId is { } property) rows = rows.Where(x => x.PropertyId == property); if (query.OwnerUserId is { } owner) rows = rows.Where(x => x.OwnerUserId == owner); if (!string.IsNullOrWhiteSpace(query.EventType)) rows = rows.Where(x => x.EventType == query.EventType); return (await rows.OrderBy(x => x.StartsAt).Take(500).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

    public async Task<WorkOrderDto> CreateWorkOrderAsync(Guid managerUserId, CreateWorkOrderRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken); if (!await db.MilestoneManagerProperties.AnyAsync(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && x.OwnerUserId == request.OwnerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is outside manager portfolio."); if (request.VendorId is { } vendor && !await db.MilestoneManagerVendors.AnyAsync(x => x.Id == vendor && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Vendor is outside manager portfolio."); var item = new MilestoneWorkOrder { ManagerUserId = managerUserId, PropertyId = request.PropertyId, OwnerUserId = request.OwnerUserId, VendorId = request.VendorId, WorkOrderNumber = $"WO-{timeProvider.GetUtcNow():yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000,9999)}", Scope = request.Scope.Trim(), QuoteAmount = request.QuoteAmount, SlaDueAt = request.SlaDueAt, Status = "REQUEST" }; db.MilestoneWorkOrders.Add(item); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<WorkOrderDto?> UpdateWorkOrderAsync(Guid managerUserId, Guid workOrderId, UpdateWorkOrderRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) return null; item.Status = request.Status.Trim().ToUpperInvariant(); item.VendorId = request.VendorId ?? item.VendorId; item.ApprovedAmount = request.ApprovedAmount ?? item.ApprovedAmount; item.LaborAmount = request.LaborAmount; item.PartsAmount = request.PartsAmount; item.ScheduledAt = request.ScheduledAt; await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<CleaningTaskDto> CreateCleaningTaskAsync(Guid managerUserId, CreateCleaningTaskRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (!await db.MilestoneManagerProperties.AnyAsync(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is outside manager portfolio."); var item = new MilestoneCleaningTask { ManagerUserId = managerUserId, PropertyId = request.PropertyId, DueAt = request.DueAt.ToUniversalTime(), AssignedStaffUserId = request.AssignedStaffUserId, Notes = request.Notes?.Trim() ?? "" }; db.MilestoneCleaningTasks.Add(item); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<InspectionDto> CreateInspectionAsync(Guid managerUserId, CreateInspectionRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (!await db.MilestoneManagerProperties.AnyAsync(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is outside manager portfolio."); var item = new MilestoneInspection { ManagerUserId = managerUserId, PropertyId = request.PropertyId, ChecklistJson = string.IsNullOrWhiteSpace(request.ChecklistJson) ? "[]" : request.ChecklistJson }; db.MilestoneInspections.Add(item); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<InspectionDto?> CompleteInspectionAsync(Guid managerUserId, Guid inspectionId, CompleteInspectionRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneInspections.SingleOrDefaultAsync(x => x.Id == inspectionId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) return null; item.Status = request.Status.Trim().ToUpperInvariant(); item.IssuesJson = request.IssuesJson; item.CompletedAt = timeProvider.GetUtcNow(); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<PmsReportDto> GetPmsReportAsync(Guid managerUserId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        if (to < from) throw new InvalidOperationException("Report end must not precede its start.");
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
        var invoices = await db.MilestoneManagerInvoices.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.IssueDate >= from && x.IssueDate <= to && x.Status != "DRAFT" && x.Status != "CANCELLED" && !x.IsDeleted).ToListAsync(cancellationToken);
        var maint = await db.MilestoneManagerMaintenances.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(cancellationToken);
        var utilities = await db.MilestoneManagerUtilityCharges.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.CreatedAt >= start && x.CreatedAt <= end && !x.IsDeleted).ToListAsync(cancellationToken);
        var ledger = await db.MilestoneManagerLedgerEntries.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.OccurredOn >= from && x.OccurredOn <= to && !x.IsDeleted).ToListAsync(cancellationToken);
        var owners = await db.MilestoneManagerOwners.CountAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        var props = await db.MilestoneManagerProperties.CountAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        var openWork = await db.MilestoneWorkOrders.CountAsync(x => x.ManagerUserId == managerUserId && x.Status != "COMPLETED" && x.Status != "CANCELLED" && !x.IsDeleted, cancellationToken);
        var postedMaintenance = maint.Where(x => x.Status == "COMPLETED" && (x.ClosedAt ?? x.UpdatedAt) >= start && (x.ClosedAt ?? x.UpdatedAt) <= end).ToList();
        // Cash is sourced from immutable payment/refund postings. Fee revenue is
        // posted fees only; invoice totals are not assumed to be company income.
        var payments = -ledger.Where(x => x.EntryType is "PAYMENT" or "REFUND").Sum(x => x.Amount);
        var fees = ledger.Where(x => x.EntryType == "MANAGEMENT_FEE").Sum(x => x.Amount);
        return new PmsReportDto(from, to, props, owners, maint.Count(x => x.Status is not ("COMPLETED" or "CANCELLED")), openWork, invoices.Sum(x => x.Total), payments, postedMaintenance.Sum(x => x.Cost), utilities.Sum(x => x.Amount), invoices.Sum(x => x.Balance), fees, invoices.Select(x => x.Id).ToList(), postedMaintenance.Select(x => x.Id).ToList());
    }

    private async Task RequirePropertyScopeAsync(Guid managerId, Guid ownerId, Guid? propertyId, CancellationToken ct)
    {
        await RequireOwnerScopeAsync(managerId, ownerId, ct);
        if (propertyId is { } id && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == id && x.ManagerUserId == managerId && x.OwnerUserId == ownerId && !x.IsDeleted, ct))
            throw new InvalidOperationException("Property is outside this owner's manager portfolio.");
    }

    private async Task<IDbContextTransaction?> LockRecordAsync(string resource, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return null;
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var key = $"nesty-pm:{resource}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
            return transaction;
        }
        catch { await transaction.DisposeAsync(); throw; }
    }

    private async Task RequireDocumentScopeAsync(Guid managerId, Guid ownerId, string? key, CancellationToken ct)
    {
        if (key is null) return;
        if (!await db.MilestoneManagerDocuments.AnyAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerId && x.StorageKey == key && !x.IsDeleted && !x.IsArchived, ct))
            throw new InvalidOperationException("Select an uploaded document belonging to this owner.");
    }

    private static void ValidateJson(string value, JsonValueKind kind)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != kind || value.Length > 20000)
                throw new InvalidOperationException("The structured data has an invalid type or size.");
        }
        catch (JsonException) { throw new InvalidOperationException("The structured data must be valid JSON."); }
    }

    private async Task<MilestonePropertyManager> EnsureManagerAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        // API contract tests and trusted service callers can provide a signed
        // manager id without first creating an interactive account.  Maintain a
        // disabled parent user so the database FK remains valid while ensuring
        // the placeholder can never authenticate or appear as a real person.
        if (!await db.MilestoneUsers.AnyAsync(x => x.Id == managerUserId && !x.IsDeleted, cancellationToken))
        {
            db.MilestoneUsers.Add(new MilestoneUser
            {
                Id = managerUserId,
                Email = $"manager-{managerUserId:N}@system.invalid",
                NormalizedEmail = $"MANAGER-{managerUserId:N}@SYSTEM.INVALID",
                PasswordHash = "disabled",
                DisplayName = "Property Manager (service account)",
                RolesJson = "[\"PropertyManager\"]",
                Status = "Disabled",
                IsTwoFactorEnabled = false,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        var manager = await db.MilestonePropertyManagers.SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken);
        if (manager is not null) return manager;
        manager = new MilestonePropertyManager { ManagerUserId = managerUserId, BusinessName = "NestyStay Property Management", SubscriptionTier = "Portfolio", MonthlyAmount = 0m, SubscriptionStatus = "ACTIVE", BillingProviderStatus = "LOCAL_TEST_READY", AutoRenew = true, NextBillingAt = timeProvider.GetUtcNow().AddMonths(1) };
        db.MilestonePropertyManagers.Add(manager); await db.SaveChangesAsync(cancellationToken); return manager;
    }
    private async Task RequireOwnerScopeAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken) { if (!await db.MilestoneManagerOwners.AnyAsync(x => x.ManagerUserId == managerUserId && x.OwnerUserId == ownerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Owner is not assigned to this manager."); }
    private async Task AuditAsync(Guid actor, string action, string type, Guid target, CancellationToken cancellationToken) { db.MilestoneAuditEvents.Add(new MilestoneAuditEvent { ActorUserId = actor, ActorRole = "PropertyManager", Action = action, SubjectType = type, SubjectId = target, Reason = action, MetadataJson = JsonSerializer.Serialize(new { source = "property-manager" }) }); await Task.CompletedTask; }
    private async Task<ProposalDto> BuildProposalDtoAsync(MilestoneManagerProposal proposal, CancellationToken cancellationToken)
    {
        var eligible = await db.MilestoneManagerEligibleVoters.CountAsync(x => x.ProposalId == proposal.Id && !x.IsDeleted, cancellationToken);
        var votes = await db.MilestoneManagerVotes.Where(x => x.ProposalId == proposal.Id && !x.IsDeleted).ToListAsync(cancellationToken);
        var results = votes.GroupBy(x => x.Choice).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var now = timeProvider.GetUtcNow();
        if (proposal.Status == "OPEN" && now >= proposal.ClosesAt) proposal.Status = "CLOSED";
        proposal.ResultJson = JsonSerializer.Serialize(results);
        return new ProposalDto(proposal.Id, proposal.CommunityId, proposal.Title, proposal.Description, proposal.OpensAt, proposal.ClosesAt, proposal.Status, proposal.IsAnonymous, proposal.Quorum, eligible, votes.Count, results);
    }
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private static ManagerProfileDto ToDto(MilestonePropertyManager x, int unitsUsed = 0) => new(
        x.ManagerUserId,
        x.BusinessName,
        x.SubscriptionTier,
        x.MonthlyAmount,
        x.SubscriptionStatus,
        x.NextBillingAt,
        x.PendingSubscriptionTier,
        x.PendingSubscriptionEffectiveAt,
        x.AutoRenew,
        x.BillingProviderStatus,
        SubscriptionUnitLimit(x.SubscriptionTier),
        unitsUsed,
        x.CancellationReason);

    private static int? SubscriptionUnitLimit(string tier) => tier.Trim().ToUpperInvariant() switch
    {
        "STARTER" or "STANDARD" => 10,
        "PROFESSIONAL" => 50,
        "ENTERPRISE" or "PORTFOLIO" => null,
        _ => 10
    };

    private static int SubscriptionTierRank(string tier) => tier.Trim().ToUpperInvariant() switch
    {
        "STARTER" => 0,
        "STANDARD" => 1,
        "PROFESSIONAL" => 2,
        "ENTERPRISE" => 3,
        "PORTFOLIO" => 3,
        _ => -1
    };

    private static string NormalizeSubscriptionTier(string? tier, string fallback) =>
        (tier ?? fallback).Trim().ToUpperInvariant() switch
        {
            "STARTER" => "Starter",
            "STANDARD" => "Standard",
            "PROFESSIONAL" => "Professional",
            "ENTERPRISE" => "Enterprise",
            "PORTFOLIO" => "Portfolio",
            _ => throw new InvalidOperationException("Subscription tier must be Starter, Standard, Professional, or Enterprise.")
        };

    private bool ApplyDueSubscriptionChange(MilestonePropertyManager manager)
    {
        if (string.IsNullOrWhiteSpace(manager.PendingSubscriptionTier) || manager.PendingSubscriptionEffectiveAt is not { } effectiveAt || effectiveAt > timeProvider.GetUtcNow()) return false;
        var previousTier = manager.SubscriptionTier;
        var nextTier = manager.PendingSubscriptionTier!;
        manager.SubscriptionTier = nextTier;
        manager.PendingSubscriptionTier = null;
        manager.PendingSubscriptionEffectiveAt = null;
        manager.SubscriptionStatus = manager.AutoRenew ? "ACTIVE" : "CANCELLED";
        manager.BillingProviderStatus = manager.AutoRenew ? "LOCAL_TEST_READY" : "CANCELLED_LOCAL";
        db.MilestoneManagerSubscriptionEvents.Add(new MilestoneManagerSubscriptionEvent
        {
            ManagerUserId = manager.ManagerUserId,
            EventType = "DOWNGRADE_APPLIED",
            FromTier = previousTier,
            ToTier = nextTier,
            Status = "COMPLETED",
            Reason = "Scheduled downgrade reached the end of the paid period.",
            EffectiveAt = effectiveAt
        });
        return true;
    }
    private static OwnerDto ToDto(MilestoneManagerOwner x) => new(x.Id, x.OwnerUserId, x.DisplayName, x.Email, x.VerificationStatus, x.InvitationStatus, x.CommunityId);
    private static PropertyDto ToDto(MilestoneManagerProperty x) => new(x.Id, x.OwnerUserId, x.CommunityId, x.Title, x.UnitNumber, x.Address, x.Status, x.OccupancyStatus, x.RentalListingId);
    private static InvoiceDto ToDto(MilestoneManagerInvoice x, IEnumerable<MilestoneManagerInvoiceLine> lines) => new(x.Id, x.OwnerUserId, x.PropertyId, x.InvoiceNumber, x.IssueDate, x.DueDate, x.Subtotal, x.Tax, x.Total, x.AmountPaid, x.Balance, x.Currency, x.Status, lines.Select(l => new InvoiceLineDto(l.Id, l.Description, l.Quantity, l.UnitAmount, l.Amount)).ToList());
    private static UtilityChargeDto ToDto(MilestoneManagerUtilityCharge x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.UtilityType, x.BillingPeriod, x.Usage, x.Rate, x.Amount, x.InvoiceId, x.Status);
    private static MaintenanceDto ToDto(MilestoneManagerMaintenance x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.VendorId, x.Title, x.Description, x.Category, x.Urgency, x.Status, x.ScheduledAt, x.Cost, x.Notes);
    private static VendorDto ToDto(MilestoneManagerVendor x) => new(x.Id, x.Name, x.Category, x.Contact, x.VerificationStatus, x.IsActive, x.Notes, ParseStringList(x.ServiceAreasJson), x.Rate, x.Rating, x.IsPreferred, x.IsSuspended, x.CompletedJobCount, x.SpendTotal);
    private static NoticeDto ToDto(MilestoneManagerNotice x)
    {
        var roles = JsonSerializer.Deserialize<string[]>(x.AudienceRolesJson) ?? [];
        var owners = JsonSerializer.Deserialize<Guid[]>(x.AudienceOwnerIdsJson) ?? [];
        return new NoticeDto(x.Id, x.CommunityId, x.TargetOwnerUserId, x.Title, x.Body, x.PublishAt, x.ExpiresAt, x.IsPinned, x.IsArchived, x.Category, roles, owners, x.AcknowledgementDueAt);
    }
    private static ProposalDto ToDto(MilestoneManagerProposal x, int eligible, int votes) { var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); return new(x.Id, x.CommunityId, x.Title, x.Description, x.OpensAt, x.ClosesAt, x.Status, x.IsAnonymous, x.Quorum, eligible, votes, results); }
    private static ProxyDto ToDto(MilestoneManagerProxy x) => new(x.Id, x.ProposalId, x.OwnerUserId, x.ProxyUserId, x.Status, x.ValidUntil, x.AcceptedAt);
    private static DocumentDto ToDto(MilestoneManagerDocument x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.Title, x.Category, x.FileName, x.ContentType, x.SizeBytes, x.AccessScope, x.IsArchived, x.ExpiresOn, x.CreatedAt);
    private static DocumentExportDto ToDto(MilestoneManagerDocumentExport x, string? url) => new(x.Id, x.Status, ParseDocumentIds(x.DocumentIdsJson).Count, x.FileName, url, x.Error, x.CreatedAt, x.CompletedAt, x.ExpiresAt);
    private static IReadOnlyList<Guid> ParseDocumentIds(string value) => JsonSerializer.Deserialize<Guid[]>(value) ?? [];
    private static GateMessageDto ToDto(MilestoneManagerGateMessage x) => new(x.Id, x.CommunityId, x.PropertyId, x.Recipient, x.Message, x.VisitorType, x.ValidFrom, x.ValidUntil);
    private static NestyStay.Application.PropertyManager.PaymentMethodDto ToDto(MilestoneManagerPaymentMethod x) => new(x.Id, x.OwnerUserId, x.Provider, x.Brand, x.Last4, x.ExpMonth, x.ExpYear, x.IsDefault);
    private static PaymentOperationDto ToPaymentDto(MilestoneManagerPayment x) => new(x.Id, x.InvoiceId, x.OwnerUserId, x.Amount, x.RefundedAmount, x.Provider, x.ProviderReference, x.Status, x.ReconciliationStatus, x.ReconciliationReference, x.RefundReason, x.CreatedAt);
    private static MeterReadingDto ToDto(MilestoneManagerMeterReading x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.UtilityType, x.BillingPeriod, x.PreviousReading, x.CurrentReading, x.Usage, x.IsAnomaly, x.Status, x.CreatedAt);
    private static UtilityScheduleDto ToDto(MilestoneManagerUtilitySchedule x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.UtilityType, x.Rate, x.DayOfMonth, x.IsActive, x.LastRunAt);
    private static UtilityDisputeDto ToDto(MilestoneManagerUtilityDispute x) => new(x.Id, x.UtilityChargeId, x.OwnerUserId, x.Reason, x.Status, x.Decision, x.AdjustmentAmount, x.CreatedAt);
    private static VendorDocumentDto ToDto(MilestoneManagerVendorDocument x) => new(x.Id, x.VendorId, x.DocumentType, x.FileName, x.ExpiresOn, x.Status, x.CreatedAt);
    private static DashboardPreferenceDto ToDto(MilestoneManagerDashboardPreference x) => new(x.ManagerUserId, ParseStringList(x.KpiOrderJson), ParseStringList(x.VisibleKpisJson), x.SavedFiltersJson, ParseStringList(x.SavedViewsJson));
    private static AgreementDto ToDto(MilestoneManagementAgreement x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.EffectiveFrom, x.EffectiveTo, x.FeeRuleJson, x.MaintenanceApprovalLimit, x.ExpenseApprovalLimit, x.SignedDocumentKey, x.Status, x.Version);
    private static FeeRuleDto ToDto(MilestoneManagementFeeRule x) => new(x.Id, x.PropertyId, x.RuleType, x.Percentage, x.FixedAmount, x.CleaningMarkup, x.MaintenanceMarkup, x.EffectiveFrom, x.EffectiveTo);
    private static OwnerPayoutDto ToDto(MilestoneOwnerPayout x) => new(x.Id, x.OwnerUserId, x.PeriodFrom, x.PeriodTo, x.Amount, x.Status, x.ProviderReference, x.FailureReason);
    private static OwnerApprovalDto ToDto(MilestoneOwnerApproval x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.ApprovalType, x.Description, x.Amount, x.Limit, x.Status, x.DecisionReason);
    private static StaffDto ToDto(MilestoneManagerStaff x) => new(x.Id, x.StaffUserId, x.Role, ParseGuidList(x.PropertyScopeJson), ParseGuidList(x.OwnerScopeJson), x.CanManageFinance, x.ApprovalLimit, x.Status);
    private static CalendarEventDto ToDto(MilestoneManagerCalendarEvent x) => new(x.Id, x.PropertyId, x.OwnerUserId, x.EventType, x.Title, x.StartsAt, x.EndsAt, x.Status, x.SourceType);
    private static WorkOrderDto ToDto(MilestoneWorkOrder x) => new(x.Id, x.PropertyId, x.OwnerUserId, x.VendorId, x.WorkOrderNumber, x.Scope, x.Status, x.QuoteAmount, x.ApprovedAmount, x.LaborAmount, x.PartsAmount, x.SlaDueAt, x.ScheduledAt);
    private static CleaningTaskDto ToDto(MilestoneCleaningTask x) => new(x.Id, x.PropertyId, x.Status, x.DueAt, x.AssignedStaffUserId, x.Notes);
    private static InspectionDto ToDto(MilestoneInspection x) => new(x.Id, x.PropertyId, x.Status, x.ChecklistJson, x.IssuesJson, x.CompletedAt);
    private static List<string> ParseStringList(string? json) { try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? []; } catch { return []; } }
    private static List<Guid> ParseGuidList(string? json) { try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<Guid>>(json) ?? []; } catch { return []; } }
}
