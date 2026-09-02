using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class EfPropertyManagerStore(
    NestyStayDbContext db,
    IPaymentGateway paymentGateway,
    IStorageProvider storageProvider,
    TimeProvider timeProvider) : IPropertyManagerStore
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
        var proposalDtos = new List<ProposalDto>();
        foreach (var proposal in proposals) proposalDtos.Add(await BuildProposalDtoAsync(proposal, cancellationToken));
        return new PropertyManagerDashboardDto(
            ToDto(manager), owners.Count, properties.Count,
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
        await AuditAsync(managerUserId, "OwnerInvited", "ManagerOwner", owner.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
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
        await AuditAsync(managerUserId, $"OwnerVerification{normalized}", "ManagerOwner", owner.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(owner);
    }

    public async Task<ManagerProfileDto> RenewSubscriptionAsync(Guid managerUserId, CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(managerUserId, cancellationToken);
        manager.SubscriptionStatus = "ACTIVE";
        manager.NextBillingAt = timeProvider.GetUtcNow().AddMonths(1);
        manager.SubscriptionTier = "Portfolio";
        manager.MonthlyAmount = 0m; // the signed agreement leaves tier prices configurable
        await AuditAsync(managerUserId, "ManagerSubscriptionRenewed", "PropertyManager", manager.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(manager);
    }

    public async Task<PropertyDto> AddPropertyAsync(Guid managerUserId, AddPropertyRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken);
        await RequireOwnerScopeAsync(managerUserId, request.OwnerUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.UnitNumber)) throw new InvalidOperationException("Property title and unit number are required.");
        var property = new MilestoneManagerProperty { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, CommunityId = request.CommunityId, Title = request.Title.Trim(), UnitNumber = request.UnitNumber.Trim(), Address = request.Address.Trim(), Status = "ACTIVE", OccupancyStatus = "VACANT" };
        db.MilestoneManagerProperties.Add(property);
        await AuditAsync(managerUserId, "PropertyAssigned", "ManagerProperty", property.Id, cancellationToken);
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

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await db.MilestoneManagerInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken);
        if (invoice is null) return null;
        if (!isAdmin && invoice.ManagerUserId != actorUserId && invoice.OwnerUserId != actorUserId) return null;
        var lines = await db.MilestoneManagerInvoiceLines.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).ToListAsync(cancellationToken);
        return ToDto(invoice, lines);
    }

    public async Task<InvoiceDto?> PayInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, PayInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("A positive amount and idempotency key are required.");
        await PaymentGate.WaitAsync(cancellationToken);
        try
        {
            var invoice = await db.MilestoneManagerInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Invoice not found.");
            if (!isAdmin && invoice.ManagerUserId != actorUserId && invoice.OwnerUserId != actorUserId) return null!;
            if (await db.MilestoneManagerPayments.AnyAsync(x => x.ManagerUserId == invoice.ManagerUserId && x.IdempotencyKey == request.IdempotencyKey && !x.IsDeleted, cancellationToken)) return ToDto(invoice, await db.MilestoneManagerInvoiceLines.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).ToListAsync(cancellationToken));
            if (request.Amount > invoice.Balance) throw new InvalidOperationException("Payment cannot exceed invoice balance.");
            var auth = await paymentGateway.AuthorizeAsync(new PaymentAuthorizationRequest(invoice.Id, request.Amount, invoice.Currency, $"Property manager invoice {invoice.InvoiceNumber}", request.IdempotencyKey), cancellationToken);
            if (auth.Status is not (NestyStay.Domain.PaymentStatus.Authorized or NestyStay.Domain.PaymentStatus.Captured)) throw new InvalidOperationException("Payment authorization failed.");
            var payment = new MilestoneManagerPayment { ManagerUserId = invoice.ManagerUserId, OwnerUserId = invoice.OwnerUserId, InvoiceId = invoice.Id, Amount = request.Amount, IdempotencyKey = request.IdempotencyKey, Provider = auth.ProviderName, ProviderReference = auth.AuthorizationReference, Status = "CAPTURED" };
            db.MilestoneManagerPayments.Add(payment);
            invoice.AmountPaid += request.Amount; invoice.Balance = invoice.Total - invoice.AmountPaid; invoice.Status = invoice.Balance <= 0 ? "PAID" : "PARTIALLY_PAID"; invoice.UpdatedAt = timeProvider.GetUtcNow();
            db.MilestoneManagerLedgerEntries.Add(new MilestoneManagerLedgerEntry { ManagerUserId = invoice.ManagerUserId, OwnerUserId = invoice.OwnerUserId, PropertyId = invoice.PropertyId, InvoiceId = invoice.Id, EntryType = "PAYMENT", Description = $"Payment for {invoice.InvoiceNumber}", Amount = -request.Amount, OccurredOn = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime) });
            await AuditAsync(actorUserId, "InvoicePaymentCaptured", "ManagerInvoice", invoice.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
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
        var item = new MilestoneManagerMaintenance { ManagerUserId = managerId, OwnerUserId = ownerId, PropertyId = property.Id, Title = request.Title.Trim(), Description = request.Description.Trim(), Category = request.Category.Trim(), Urgency = request.Urgency.Trim().ToUpperInvariant(), Status = "OPEN" }; db.MilestoneManagerMaintenances.Add(item); await AuditAsync(actorUserId, "MaintenanceCreated", "Maintenance", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<MaintenanceDto?> UpdateMaintenanceAsync(Guid managerUserId, Guid maintenanceId, UpdateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerMaintenances.SingleOrDefaultAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) return null;
        if (request.VendorId is { } vendor && !await db.MilestoneManagerVendors.AnyAsync(x => x.Id == vendor && x.ManagerUserId == managerUserId && x.IsActive && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Vendor is not in the manager portfolio.");
        item.Status = request.Status.Trim().ToUpperInvariant(); item.VendorId = request.VendorId; item.ScheduledAt = request.ScheduledAt; item.Cost = request.Cost; item.Notes = request.Notes.Trim(); item.UpdatedAt = timeProvider.GetUtcNow(); await AuditAsync(managerUserId, "MaintenanceUpdated", "Maintenance", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item);
    }

    public async Task<VendorDto> CreateVendorAsync(Guid managerUserId, CreateVendorRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = new MilestoneManagerVendor { ManagerUserId = managerUserId, Name = request.Name.Trim(), Category = request.Category.Trim(), Contact = request.Contact.Trim(), Notes = request.Notes.Trim(), VerificationStatus = "PENDING" }; db.MilestoneManagerVendors.Add(item); await AuditAsync(managerUserId, "VendorAdded", "Vendor", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<NoticeDto> CreateNoticeAsync(Guid managerUserId, CreateNoticeRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.TargetOwnerUserId is { } owner) await RequireOwnerScopeAsync(managerUserId, owner, cancellationToken); var item = new MilestoneManagerNotice { ManagerUserId = managerUserId, CommunityId = request.CommunityId, TargetOwnerUserId = request.TargetOwnerUserId, Title = request.Title.Trim(), Body = request.Body.Trim(), PublishAt = timeProvider.GetUtcNow(), ExpiresAt = request.ExpiresAt, IsPinned = request.IsPinned }; db.MilestoneManagerNotices.Add(item); await AuditAsync(managerUserId, "CommunityNoticePublished", "Notice", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    { if (isAdmin) return (await db.MilestoneManagerNotices.Where(x => !x.IsDeleted && !x.IsArchived).OrderByDescending(x => x.PublishAt).ToListAsync(cancellationToken)).Select(ToDto).ToList(); var managerIds = await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == actorUserId && !x.IsDeleted).Select(x => x.ManagerUserId).ToListAsync(cancellationToken); return (await db.MilestoneManagerNotices.Where(x => managerIds.Contains(x.ManagerUserId) && !x.IsDeleted && !x.IsArchived && (x.TargetOwnerUserId == null || x.TargetOwnerUserId == actorUserId) && (x.ExpiresAt == null || x.ExpiresAt > timeProvider.GetUtcNow())).OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.PublishAt).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

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

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    { if (isAdmin) return (await db.MilestoneManagerDocuments.Where(x => !x.IsDeleted && !x.IsArchived).ToListAsync(cancellationToken)).Select(ToDto).ToList(); var managerIds = await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == actorUserId && !x.IsDeleted).Select(x => x.ManagerUserId).ToListAsync(cancellationToken); return (await db.MilestoneManagerDocuments.Where(x => managerIds.Contains(x.ManagerUserId) && !x.IsDeleted && !x.IsArchived && (x.OwnerUserId == null || x.OwnerUserId == actorUserId)).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }

    public async Task<DocumentDto> AddDocumentAsync(Guid managerUserId, AddDocumentRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.OwnerUserId is { } owner) await RequireOwnerScopeAsync(managerUserId, owner, cancellationToken); if (request.PropertyId is { } documentProperty && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == documentProperty && x.ManagerUserId == managerUserId && (!request.OwnerUserId.HasValue || x.OwnerUserId == request.OwnerUserId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is not in the manager portfolio."); if (request.SizeBytes <= 0 || request.SizeBytes > 25 * 1024 * 1024) throw new InvalidOperationException("Document size must be between 1 byte and 25 MB."); var safeName = Path.GetFileName(request.FileName); if (safeName != request.FileName || safeName.Contains("..", StringComparison.Ordinal)) throw new InvalidOperationException("Document filename is invalid."); var allowed = request.ContentType.ToLowerInvariant() is "application/pdf" or "image/jpeg" or "image/png"; if (!allowed) throw new InvalidOperationException("Only PDF, JPEG, and PNG documents are accepted."); var key = $"property-manager/{managerUserId:N}/{Guid.NewGuid():N}/{safeName}"; if (!string.IsNullOrWhiteSpace(request.ContentBase64)) { byte[] bytes; try { bytes = Convert.FromBase64String(request.ContentBase64); } catch { throw new InvalidOperationException("Document content is not valid base64."); } if (bytes.LongLength != request.SizeBytes) throw new InvalidOperationException("Document size does not match content."); await using var stream = new MemoryStream(bytes); await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(key, request.ContentType, 25 * 1024 * 1024), stream, cancellationToken); } var item = new MilestoneManagerDocument { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, Title = request.Title.Trim(), Category = request.Category.Trim(), FileName = safeName, ContentType = request.ContentType, SizeBytes = request.SizeBytes, StorageKey = key, AccessScope = request.OwnerUserId is null ? "COMMUNITY" : "OWNER" }; db.MilestoneManagerDocuments.Add(item); await AuditAsync(managerUserId, "DocumentStored", "Document", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<DocumentDownloadDto?> GetDocumentDownloadAsync(Guid actorUserId, bool isAdmin, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await db.MilestoneManagerDocuments.SingleOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted && !x.IsArchived, cancellationToken);
        if (document is null || (!isAdmin && document.ManagerUserId != actorUserId && document.OwnerUserId != actorUserId)) return null;
        var expiresAt = timeProvider.GetUtcNow().AddHours(24);
        var url = await storageProvider.CreateDownloadUrlAsync(document.StorageKey, expiresAt, cancellationToken);
        return new DocumentDownloadDto(document.Id, document.FileName, document.ContentType, document.SizeBytes, url, expiresAt);
    }

    public async Task<GateMessageDto> CreateGateMessageAsync(Guid managerUserId, CreateGateMessageRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.PropertyId is { } gateProperty && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == gateProperty && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is not in the manager portfolio."); if (request.ValidUntil <= request.ValidFrom) throw new InvalidOperationException("Gate message expiry must be after its start time."); var item = new MilestoneManagerGateMessage { ManagerUserId = managerUserId, CommunityId = request.CommunityId, PropertyId = request.PropertyId, Recipient = request.Recipient.Trim(), Message = request.Message.Trim(), VisitorType = request.VisitorType.Trim(), ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil }; db.MilestoneManagerGateMessages.Add(item); await AuditAsync(managerUserId, "GateMessageCreated", "GateMessage", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return ToDto(item); }

    public async Task<QrIssueDto> IssueQrAsync(Guid managerUserId, IssueQrRequest request, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); if (request.OwnerUserId is { } qrOwner) await RequireOwnerScopeAsync(managerUserId, qrOwner, cancellationToken); if (request.PropertyId is { } property && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == property && x.ManagerUserId == managerUserId && (!request.OwnerUserId.HasValue || x.OwnerUserId == request.OwnerUserId) && !x.IsDeleted, cancellationToken)) throw new InvalidOperationException("Property is not in the manager portfolio."); if (request.ValidUntil <= request.ValidFrom) throw new InvalidOperationException("QR expiry must be after its start time."); var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); var item = new MilestoneManagerQrAccess { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, SubjectType = request.SubjectType.Trim().ToUpperInvariant(), TokenHash = HashToken(token), ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil }; db.MilestoneManagerQrAccesses.Add(item); await AuditAsync(managerUserId, "QrCreated", "ManagerQr", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return new QrIssueDto(item.Id, token, item.SubjectType, item.PropertyId, item.ValidFrom, item.ValidUntil); }

    public async Task<QrValidationDto> ValidateQrAsync(string token, Guid? propertyId, Guid? gateGuardUserId, CancellationToken cancellationToken)
    { if (string.IsNullOrWhiteSpace(token)) return new QrValidationDto("INVALID", "Invalid", null, "", null, null, "QR token is required."); var item = await db.MilestoneManagerQrAccesses.SingleOrDefaultAsync(x => x.TokenHash == HashToken(token) && !x.IsDeleted, cancellationToken); var now = timeProvider.GetUtcNow(); var result = item is null ? "INVALID" : item.IsRevoked ? "REVOKED" : item.ValidFrom > now ? "NOT_YET_VALID" : item.ValidUntil <= now ? "EXPIRED" : propertyId is not null && item.PropertyId != propertyId ? "WRONG_PROPERTY" : "VALID"; if (item is not null) { item.ValidationCount++; item.LastValidatedAt = now; db.MilestoneManagerQrScans.Add(new MilestoneManagerQrScan { QrAccessId = item.Id, GateGuardUserId = gateGuardUserId, PropertyId = propertyId, Result = result }); await db.SaveChangesAsync(cancellationToken); } return new QrValidationDto(result, result.Replace('_', ' '), item?.PropertyId, item?.SubjectType ?? "", item?.ValidUntil, item?.Id, result == "VALID" ? "Access approved." : "Access denied."); }

    public async Task<QrValidationDto> RevokeQrAsync(Guid managerUserId, Guid qrId, CancellationToken cancellationToken)
    { await EnsureManagerAsync(managerUserId, cancellationToken); var item = await db.MilestoneManagerQrAccesses.SingleOrDefaultAsync(x => x.Id == qrId && x.ManagerUserId == managerUserId && !x.IsDeleted, cancellationToken); if (item is null) throw new InvalidOperationException("QR access code not found."); item.IsRevoked = true; await AuditAsync(managerUserId, "QrRevoked", "ManagerQr", item.Id, cancellationToken); await db.SaveChangesAsync(cancellationToken); return new QrValidationDto("REVOKED", "Revoked", item.PropertyId, item.SubjectType, item.ValidUntil, item.Id, "Access revoked."); }

    public async Task<OwnerPortalDto> GetOwnerPortalAsync(Guid ownerUserId, CancellationToken cancellationToken)
    { var managers = await db.MilestoneManagerOwners.Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => x.ManagerUserId).ToListAsync(cancellationToken); if (managers.Count == 0) throw new InvalidOperationException("Owner is not linked to a property manager."); var managerId = managers[0]; var properties = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var invoices = await db.MilestoneManagerInvoices.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var lines = await db.MilestoneManagerInvoiceLines.Where(x => invoices.Select(i => i.Id).Contains(x.InvoiceId) && !x.IsDeleted).ToListAsync(cancellationToken); var utilities = await db.MilestoneManagerUtilityCharges.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var maintenance = await db.MilestoneManagerMaintenances.Where(x => x.ManagerUserId == managerId && x.OwnerUserId == ownerUserId && !x.IsDeleted).ToListAsync(cancellationToken); var notices = await GetNoticesAsync(ownerUserId, false, cancellationToken); var proposals = await db.MilestoneManagerProposals.Where(x => x.ManagerUserId == managerId && !x.IsDeleted).ToListAsync(cancellationToken); var proposalDtos = new List<ProposalDto>(); foreach (var proposal in proposals) proposalDtos.Add(await BuildProposalDtoAsync(proposal, cancellationToken)); var documents = (await GetDocumentsAsync(ownerUserId, false, cancellationToken)).ToList(); return new OwnerPortalDto(ownerUserId, properties.Select(ToDto).ToList(), invoices.Select(x => ToDto(x, lines.Where(l => l.InvoiceId == x.Id))).ToList(), await GetStatementAsync(ownerUserId, false, ownerUserId, null, null, cancellationToken), utilities.Select(ToDto).ToList(), maintenance.Select(ToDto).ToList(), notices, proposalDtos, documents); }

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
        manager = new MilestonePropertyManager { ManagerUserId = managerUserId, BusinessName = "NestyStay Property Management", SubscriptionTier = "Portfolio", MonthlyAmount = 0m, SubscriptionStatus = "ACTIVE", NextBillingAt = timeProvider.GetUtcNow().AddMonths(1) };
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
    private static ManagerProfileDto ToDto(MilestonePropertyManager x) => new(x.ManagerUserId, x.BusinessName, x.SubscriptionTier, x.MonthlyAmount, x.SubscriptionStatus, x.NextBillingAt);
    private static OwnerDto ToDto(MilestoneManagerOwner x) => new(x.Id, x.OwnerUserId, x.DisplayName, x.Email, x.VerificationStatus, x.InvitationStatus, x.CommunityId);
    private static PropertyDto ToDto(MilestoneManagerProperty x) => new(x.Id, x.OwnerUserId, x.CommunityId, x.Title, x.UnitNumber, x.Address, x.Status, x.OccupancyStatus);
    private static InvoiceDto ToDto(MilestoneManagerInvoice x, IEnumerable<MilestoneManagerInvoiceLine> lines) => new(x.Id, x.OwnerUserId, x.PropertyId, x.InvoiceNumber, x.IssueDate, x.DueDate, x.Subtotal, x.Tax, x.Total, x.AmountPaid, x.Balance, x.Currency, x.Status, lines.Select(l => new InvoiceLineDto(l.Id, l.Description, l.Quantity, l.UnitAmount, l.Amount)).ToList());
    private static UtilityChargeDto ToDto(MilestoneManagerUtilityCharge x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.UtilityType, x.BillingPeriod, x.Usage, x.Rate, x.Amount, x.InvoiceId, x.Status);
    private static MaintenanceDto ToDto(MilestoneManagerMaintenance x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.VendorId, x.Title, x.Description, x.Category, x.Urgency, x.Status, x.ScheduledAt, x.Cost, x.Notes);
    private static VendorDto ToDto(MilestoneManagerVendor x) => new(x.Id, x.Name, x.Category, x.Contact, x.VerificationStatus, x.IsActive, x.Notes);
    private static NoticeDto ToDto(MilestoneManagerNotice x) => new(x.Id, x.CommunityId, x.TargetOwnerUserId, x.Title, x.Body, x.PublishAt, x.ExpiresAt, x.IsPinned, x.IsArchived);
    private static ProposalDto ToDto(MilestoneManagerProposal x, int eligible, int votes) { var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); return new(x.Id, x.CommunityId, x.Title, x.Description, x.OpensAt, x.ClosesAt, x.Status, x.IsAnonymous, x.Quorum, eligible, votes, results); }
    private static ProxyDto ToDto(MilestoneManagerProxy x) => new(x.Id, x.ProposalId, x.OwnerUserId, x.ProxyUserId, x.Status, x.ValidUntil, x.AcceptedAt);
    private static DocumentDto ToDto(MilestoneManagerDocument x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.Title, x.Category, x.FileName, x.ContentType, x.SizeBytes, x.AccessScope, x.IsArchived, x.CreatedAt);
    private static GateMessageDto ToDto(MilestoneManagerGateMessage x) => new(x.Id, x.CommunityId, x.PropertyId, x.Recipient, x.Message, x.VisitorType, x.ValidFrom, x.ValidUntil);
}
