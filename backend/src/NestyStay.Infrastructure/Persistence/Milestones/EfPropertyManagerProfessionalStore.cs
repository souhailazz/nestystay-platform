using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// M5 operational workflows.  Every query is scoped to the manager portfolio;
/// state transitions append an event and use an EF concurrency token.
/// </summary>
public sealed class EfPropertyManagerProfessionalStore(NestyStayDbContext db, TimeProvider timeProvider, IPropertyManagerP0Store p0Store, IPhaseOneStore phaseOneStore) : IPropertyManagerProfessionalStore
{
    private static readonly string[] MaintenanceStates = ["REQUESTED", "TRIAGED", "QUOTING", "OWNER_APPROVAL", "ASSIGNED", "SCHEDULED", "IN_PROGRESS", "COMPLETED", "CLOSED", "CANCELLED"];
    private static readonly IReadOnlyDictionary<string, string[]> MaintenanceTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["REQUESTED"] = ["TRIAGED", "CANCELLED"], ["TRIAGED"] = ["QUOTING", "OWNER_APPROVAL", "CANCELLED"], ["QUOTING"] = ["OWNER_APPROVAL", "ASSIGNED", "CANCELLED"],
        ["OWNER_APPROVAL"] = ["ASSIGNED", "CANCELLED"], ["ASSIGNED"] = ["SCHEDULED", "CANCELLED"], ["SCHEDULED"] = ["IN_PROGRESS", "CANCELLED"], ["IN_PROGRESS"] = ["COMPLETED"], ["COMPLETED"] = ["CLOSED"], ["CLOSED"] = [], ["CANCELLED"] = []
    };

    private async Task EnsureManagerAsync(Guid managerUserId, CancellationToken ct)
    {
        if (await db.MilestonePropertyManagers.AnyAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) return;
        if (!await db.MilestoneUsers.AnyAsync(x => x.Id == managerUserId && !x.IsDeleted, ct))
            db.MilestoneUsers.Add(new MilestoneUser { Id = managerUserId, Email = $"manager-{managerUserId:N}@system.invalid", NormalizedEmail = $"MANAGER-{managerUserId:N}@SYSTEM.INVALID", PasswordHash = "disabled", DisplayName = "Property Manager", RolesJson = "[\"PropertyManager\"]", Status = "Disabled", IsTwoFactorEnabled = false });
        db.MilestonePropertyManagers.Add(new MilestonePropertyManager { ManagerUserId = managerUserId, BusinessName = "NestyStay Property Management", SubscriptionTier = "Portfolio", SubscriptionStatus = "ACTIVE", BillingProviderStatus = "LOCAL_TEST_READY", AutoRenew = true, NextBillingAt = timeProvider.GetUtcNow().AddMonths(1) });
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsurePropertyAsync(Guid managerUserId, Guid propertyId, Guid? ownerId, CancellationToken ct)
    {
        var property = await db.MilestoneManagerProperties.SingleOrDefaultAsync(x => x.Id == propertyId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Property is outside the manager portfolio.");
        if (ownerId is { } owner && property.OwnerUserId != owner) throw new InvalidOperationException("Property is not assigned to this owner.");
    }

    public async Task<IReadOnlyList<PmOwnerBlockDto>> ListOwnerBlocksAsync(Guid managerUserId, DateTimeOffset? from, DateTimeOffset? to, Guid? propertyId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var q = db.MilestonePmOwnerBlocks.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted);
        if (from is { } start) q = q.Where(x => x.EndsAt >= start.ToUniversalTime());
        if (to is { } end) q = q.Where(x => x.StartsAt <= end.ToUniversalTime());
        if (propertyId is { } property) q = q.Where(x => x.PropertyId == property);
        return (await q.OrderBy(x => x.StartsAt).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public Task<PmOwnerBlockDto> CreateOwnerBlockAsync(Guid managerUserId, CreatePmOwnerBlockRequest request, CancellationToken ct) => CreateOwnerBlockCoreAsync(managerUserId, managerUserId, request, ct);

    public async Task<IReadOnlyList<PmOwnerBlockDto>> ListOwnerBlocksForOwnerAsync(Guid ownerUserId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var propertyIds = db.MilestoneManagerProperties.AsNoTracking().Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => x.Id);
        var q = db.MilestonePmOwnerBlocks.AsNoTracking().Where(x => x.OwnerUserId == ownerUserId && propertyIds.Contains(x.PropertyId) && !x.IsDeleted);
        if (from is { } start) q = q.Where(x => x.EndsAt >= start.ToUniversalTime());
        if (to is { } end) q = q.Where(x => x.StartsAt <= end.ToUniversalTime());
        return (await q.OrderBy(x => x.StartsAt).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<PmOwnerBlockDto> CreateOwnerBlockForOwnerAsync(Guid ownerUserId, CreatePmOwnerBlockRequest request, CancellationToken ct)
    {
        var managerUserId = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.Id == request.PropertyId && x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => x.ManagerUserId).SingleOrDefaultAsync(ct);
        if (managerUserId == Guid.Empty) throw new UnauthorizedAccessException("Property is not owned by the current user.");
        return await CreateOwnerBlockCoreAsync(managerUserId, ownerUserId, request with { OwnerUserId = ownerUserId }, ct);
    }

    public async Task<PmOwnerBlockDto?> CancelOwnerBlockForOwnerAsync(Guid ownerUserId, Guid id, string reason, long rowVersion, CancellationToken ct)
    {
        var managerUserId = await db.MilestonePmOwnerBlocks.AsNoTracking().Where(x => x.Id == id && x.OwnerUserId == ownerUserId && !x.IsDeleted).Select(x => x.ManagerUserId).SingleOrDefaultAsync(ct);
        if (managerUserId == Guid.Empty) return null;
        return await CancelOwnerBlockCoreAsync(managerUserId, ownerUserId, id, reason, rowVersion, ct);
    }

    private async Task<PmOwnerBlockDto> CreateOwnerBlockCoreAsync(Guid managerUserId, Guid auditActorUserId, CreatePmOwnerBlockRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, request.OwnerUserId, ct);
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:block:{request.PropertyId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var starts = request.StartsAt.ToUniversalTime(); var ends = request.EndsAt.ToUniversalTime();
        if (ends <= starts) throw new InvalidOperationException("Block end must be after start.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A block reason is required.");
        var overlap = await db.MilestonePmOwnerBlocks.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status == "ACTIVE" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        var calendarConflict = await db.MilestoneManagerCalendarEvents.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.StartsAt < ends && x.EndsAt > starts && !x.IsDeleted, ct);
        var maintenanceConflict = await db.MilestonePmMaintenanceCases.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.ScheduledAt >= starts && x.ScheduledAt <= ends && !x.IsDeleted, ct);
        var cleaningConflict = await db.MilestonePmCleaningReadiness.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.DueAt >= starts && x.DueAt <= ends && !x.IsDeleted, ct);
        var inspectionConflict = await db.MilestonePmInspectionRecords.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.ScheduledAt >= starts && x.ScheduledAt <= ends && !x.IsDeleted, ct);
        if (overlap || calendarConflict || maintenanceConflict || cleaningConflict || inspectionConflict)
            throw new InvalidOperationException("The requested owner block conflicts with an existing calendar event.");
        var rentalListingId = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && !x.IsDeleted).Select(x => x.RentalListingId).SingleOrDefaultAsync(ct);
        var bookingOverlap = await db.MilestoneBookings.AnyAsync(x => (x.PropertyId == request.PropertyId || (rentalListingId.HasValue && x.PropertyId == rentalListingId.Value)) && !x.IsDeleted && x.CheckIn < DateOnly.FromDateTime(ends.UtcDateTime) && x.CheckOut > DateOnly.FromDateTime(starts.UtcDateTime) && x.Status != NestyStay.Domain.BookingStatus.Cancelled && x.Status != NestyStay.Domain.BookingStatus.Rejected, ct);
        if (bookingOverlap) throw new InvalidOperationException("The requested owner block conflicts with a reservation.");
        var row = new MilestonePmOwnerBlock { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, StartsAt = starts, EndsAt = ends, TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "America/Jamaica" : request.TimeZone.Trim(), Category = NormalizeBlockCategory(request.Category), Reason = request.Reason.Trim(), Notes = request.Notes?.Trim() ?? string.Empty, BookingId = request.BookingId };
        db.MilestonePmOwnerBlocks.Add(row); AddAudit(auditActorUserId, "OwnerBlockCreated", "PmOwnerBlock", row.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return ToDto(row);
    }

    public Task<PmOwnerBlockDto?> CancelOwnerBlockAsync(Guid managerUserId, Guid id, string reason, long rowVersion, CancellationToken ct) => CancelOwnerBlockCoreAsync(managerUserId, managerUserId, id, reason, rowVersion, ct);

    private async Task<PmOwnerBlockDto?> CancelOwnerBlockCoreAsync(Guid managerUserId, Guid auditActorUserId, Guid id, string reason, long rowVersion, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null; if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:block:{id}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); } var row = await db.MilestonePmOwnerBlocks.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null;
        if (row.RowVersion != rowVersion) throw new DbUpdateConcurrencyException("Block changed; reload before cancelling.");
        if (row.Status == "CANCELLED") return ToDto(row); row.Status = "CANCELLED"; row.Reason = string.IsNullOrWhiteSpace(reason) ? row.Reason : reason.Trim(); row.RowVersion++;
        AddAudit(auditActorUserId, "OwnerBlockCancelled", "PmOwnerBlock", row.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return ToDto(row);
    }

    public async Task<IReadOnlyList<PmOwnerBlockHistoryDto>> ListOwnerBlockHistoryAsync(Guid managerUserId, Guid id, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (!await db.MilestonePmOwnerBlocks.AnyAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Owner block not found.");
        return (await db.MilestoneAuditEvents.AsNoTracking().Where(x => x.SubjectType == "PmOwnerBlock" && x.SubjectId == id && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct)).Select(x => new PmOwnerBlockHistoryDto(x.Id, id, x.ActorUserId ?? Guid.Empty, x.Action, x.Reason, x.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<PmReservationDto>> ListReservationsAsync(Guid managerUserId, string? search, string? status, Guid? propertyId, Guid? ownerUserId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var portfolioProperties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var propertyIds = portfolioProperties.Select(x => x.Id).ToHashSet();
        var listingIds = portfolioProperties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToHashSet();
        var selected = propertyId is { } selectedProperty ? portfolioProperties.SingleOrDefault(x => x.Id == selectedProperty) ?? throw new InvalidOperationException("Property is outside the manager portfolio.") : null;
        var q = db.MilestoneBookings.AsNoTracking().Where(x => (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)) && !x.IsDeleted);
        if (selected is not null)
        {
            var bookingPropertyId = selected.RentalListingId ?? selected.Id;
            q = q.Where(x => x.PropertyId == bookingPropertyId);
        }
        if (ownerUserId is { } owner)
        {
            var ownerProperties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.OwnerUserId == owner && !x.IsDeleted).ToListAsync(ct);
            var ownerPropertyIds = ownerProperties.Select(x => x.Id).ToArray();
            var ownerListingIds = ownerProperties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
            q = q.Where(x => ownerPropertyIds.Contains(x.PropertyId) || ownerListingIds.Contains(x.PropertyId));
        }
        if (!string.IsNullOrWhiteSpace(status) && TryParseBookingStatus(status, out var parsed)) q = q.Where(x => x.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.GuestEmail.Contains(search) || x.GuestName.Contains(search) || x.PropertyTitle!.Contains(search));
        var rows = await q.OrderByDescending(x => x.CheckIn).Take(500).ToListAsync(ct); var ids = rows.Select(x => x.Id).ToList();
        var notes = await db.MilestonePmReservationNotes.AsNoTracking().Where(x => ids.Contains(x.BookingId) && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        return rows.Select(x =>
        {
            var managedProperty = portfolioProperties.FirstOrDefault(p => p.Id == x.PropertyId || p.RentalListingId == x.PropertyId);
            return new PmReservationDto(x.Id, managedProperty?.Id ?? x.PropertyId, x.GuestUserId, x.HostUserId, new DateTimeOffset(x.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), new DateTimeOffset(x.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), x.Status.ToString(), x.PaymentStatus.ToString(), x.TotalAmount, x.Currency, notes.Where(n => n.BookingId == x.Id).Select(ToDto).ToList(), x.GuestName, x.GuestEmail, x.PropertyTitle, x.UpdatedAt);
        }).ToList();
    }

    public async Task<PmReservationDto?> UpdateReservationAsync(Guid managerUserId, Guid bookingId, UpdatePmReservationRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:reservation:{bookingId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var managedProperties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var managedPropertyIds = managedProperties.Select(p => p.Id).ToArray();
        var managedListingIds = managedProperties.Where(p => p.RentalListingId.HasValue).Select(p => p.RentalListingId!.Value).ToArray();
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(x => x.Id == bookingId && (managedPropertyIds.Contains(x.PropertyId) || managedListingIds.Contains(x.PropertyId)) && !x.IsDeleted, ct); if (booking is null) return null;
        var managedProperty = managedProperties.First(p => p.Id == booking.PropertyId || p.RentalListingId == booking.PropertyId);
        if (request.ExpectedUpdatedTicks is { } expected && booking.UpdatedAt.UtcTicks != expected) throw new DbUpdateConcurrencyException("Reservation changed; reload before updating.");
        if (!TryParseBookingStatus(request.Status, out var next)) throw new InvalidOperationException("Unsupported reservation status.");
        if (next is NestyStay.Domain.BookingStatus.PaymentCaptured or NestyStay.Domain.BookingStatus.Confirmed)
            throw new InvalidOperationException("Payment capture and confirmation must use the verified payment workflow.");
        if (next == NestyStay.Domain.BookingStatus.Cancelled)
            throw new InvalidOperationException("Cancellation requires a reason and must use the cancellation action.");
        if (next == NestyStay.Domain.BookingStatus.Approved && booking.VerificationStatus != NestyStay.Domain.VerificationStatus.Passed)
            throw new InvalidOperationException("A reservation can be approved only after identity verification passes.");
        if (next == NestyStay.Domain.BookingStatus.Rejected && booking.VerificationStatus is not (NestyStay.Domain.VerificationStatus.Failed or NestyStay.Domain.VerificationStatus.Expired))
            throw new InvalidOperationException("A reservation can be rejected only after identity verification fails or expires.");
        if (next != booking.Status) BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, next, "property_manager_update");
        var checkIn = request.CheckIn ?? booking.CheckIn; var checkOut = request.CheckOut ?? booking.CheckOut; if (checkOut <= checkIn) throw new InvalidOperationException("Check-out must be after check-in.");
        if (checkIn != booking.CheckIn || checkOut != booking.CheckOut)
        {
            var proposedNights = checkOut.DayNumber - checkIn.DayNumber;
            var proposedTotal = decimal.Round(booking.NightlyRate * proposedNights + (booking.GuestPlatformFee / Math.Max(1, booking.Nights) * proposedNights), 2, MidpointRounding.AwayFromZero);
            if (booking.PaymentStatus is (NestyStay.Domain.PaymentStatus.Authorized or NestyStay.Domain.PaymentStatus.Captured) && proposedTotal != booking.TotalAmount)
                throw new InvalidOperationException("Paid reservations cannot be repriced in place. Cancel and rebook with the replacement dates.");
            var conflict = await db.MilestoneBookings.AnyAsync(x => x.Id != booking.Id && x.PropertyId == booking.PropertyId && !x.IsDeleted && x.CheckIn < checkOut && checkIn < x.CheckOut && (x.Status == NestyStay.Domain.BookingStatus.PendingVerification || x.Status == NestyStay.Domain.BookingStatus.Approved || x.Status == NestyStay.Domain.BookingStatus.PaymentCaptured || x.Status == NestyStay.Domain.BookingStatus.Confirmed), ct);
            var blockConflict = await db.MilestonePmOwnerBlocks.AnyAsync(x => x.PropertyId == managedProperty.Id && x.Status == "ACTIVE" && !x.IsDeleted && x.StartsAt < new DateTimeOffset(checkOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) && x.EndsAt > new DateTimeOffset(checkIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), ct);
            if (conflict || blockConflict) throw new InvalidOperationException("The revised reservation conflicts with another reservation or owner block."); booking.CheckIn = checkIn; booking.CheckOut = checkOut; booking.Nights = checkOut.DayNumber - checkIn.DayNumber;
        }
        var previousStatus = booking.Status; booking.Status = next; booking.UpdatedAt = timeProvider.GetUtcNow();
        if (previousStatus != next) db.MilestonePmReservationEvents.Add(new MilestonePmReservationEvent { ManagerUserId = managerUserId, BookingId = booking.Id, ActorUserId = managerUserId, EventType = "STATUS_CHANGED", FromStatus = previousStatus.ToString(), ToStatus = next.ToString(), Reason = "Property manager reservation update" });
        AddAudit(managerUserId, "ReservationUpdated", "Booking", booking.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct);
        return await ToReservationDtoAsync(booking, managedProperties, ct);
    }

    public async Task<PmReservationDateChangePreviewDto?> PreviewReservationDateChangeAsync(Guid managerUserId, Guid bookingId, PreviewPmReservationDateChangeRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var properties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var propertyIds = properties.Select(x => x.Id).ToArray();
        var listingIds = properties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        var booking = await db.MilestoneBookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == bookingId && !x.IsDeleted && (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)), ct);
        if (booking is null) return null;
        if (request.CheckOut <= request.CheckIn) throw new InvalidOperationException("Check-out must be after check-in.");
        var proposedNights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
        var proposedTotal = decimal.Round(booking.NightlyRate * proposedNights + (booking.GuestPlatformFee / Math.Max(1, booking.Nights) * proposedNights), 2, MidpointRounding.AwayFromZero);
        var conflict = await db.MilestoneBookings.AsNoTracking().AnyAsync(x => x.Id != booking.Id && x.PropertyId == booking.PropertyId && !x.IsDeleted && x.CheckIn < request.CheckOut && request.CheckIn < x.CheckOut && (x.Status == NestyStay.Domain.BookingStatus.PendingVerification || x.Status == NestyStay.Domain.BookingStatus.Approved || x.Status == NestyStay.Domain.BookingStatus.PaymentCaptured || x.Status == NestyStay.Domain.BookingStatus.Confirmed), ct);
        var managed = properties.First(x => x.Id == booking.PropertyId || x.RentalListingId == booking.PropertyId);
        var starts = new DateTimeOffset(request.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var ends = new DateTimeOffset(request.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var blockConflict = await db.MilestonePmOwnerBlocks.AsNoTracking().AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == managed.Id && x.Status == "ACTIVE" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        var priceChanged = proposedTotal != booking.TotalAmount;
        var paid = booking.PaymentStatus is NestyStay.Domain.PaymentStatus.Authorized or NestyStay.Domain.PaymentStatus.Captured;
        var reason = conflict ? "The proposed dates overlap another reservation." : blockConflict ? "The proposed dates overlap an active owner block." : paid && priceChanged ? "Paid reservations with a price change must be cancelled and rebooked." : null;
        return new PmReservationDateChangePreviewDto(booking.Id, booking.CheckIn, booking.CheckOut, request.CheckIn, request.CheckOut, booking.Nights, proposedNights, booking.TotalAmount, proposedTotal, booking.Currency, reason is null, reason);
    }

    public async Task<PmReservationNoteDto> AddReservationNoteAsync(Guid managerUserId, Guid bookingId, AddPmReservationNoteRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var bookingPropertyId = await db.MilestoneBookings.Where(x => x.Id == bookingId && !x.IsDeleted).Select(x => x.PropertyId).SingleOrDefaultAsync(ct); var managedPropertyId = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && (x.Id == bookingPropertyId || x.RentalListingId == bookingPropertyId)).Select(x => x.Id).SingleOrDefaultAsync(ct); await EnsurePropertyAsync(managerUserId, managedPropertyId, null, ct);
        var propertyId = managedPropertyId;
        if (string.IsNullOrWhiteSpace(request.Body)) throw new InvalidOperationException("Note text is required.");
        var row = new MilestonePmReservationNote { ManagerUserId = managerUserId, BookingId = bookingId, PropertyId = propertyId, AuthorUserId = managerUserId, Body = request.Body.Trim(), Visibility = request.Visibility.Trim().ToUpperInvariant() };
        db.MilestonePmReservationNotes.Add(row); AddAudit(managerUserId, "ReservationNoteAdded", "Booking", bookingId); await db.SaveChangesAsync(ct); return ToDto(row);
    }

    public async Task<PmReservationDto?> CancelReservationAsync(Guid managerUserId, Guid bookingId, CancelPmReservationRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A cancellation reason is required.");
        var managedProperties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var propertyIds = managedProperties.Select(x => x.Id).ToArray(); var listingIds = managedProperties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:reservation:{bookingId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"pm-cancel:{bookingId:N}" : request.IdempotencyKey.Trim();
        var prior = await db.MilestonePmReservationEvents.AsNoTracking().FirstOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == idempotencyKey && !x.IsDeleted, ct);
        if (prior is not null)
        {
            if (prior.BookingId != bookingId) throw new InvalidOperationException("Idempotency key was already used for another reservation.");
            var priorBooking = await db.MilestoneBookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == prior.BookingId && !x.IsDeleted, ct);
            return priorBooking is null ? null : await ToReservationDtoAsync(priorBooking, managedProperties, ct);
        }
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(x => x.Id == bookingId && (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)) && !x.IsDeleted, ct);
        if (booking is null) return null;
        if (request.ExpectedUpdatedTicks is { } expected && booking.UpdatedAt.UtcTicks != expected) throw new DbUpdateConcurrencyException("Reservation changed; reload before cancelling.");
        if (booking.Status == NestyStay.Domain.BookingStatus.Cancelled) throw new InvalidOperationException("Reservation is already cancelled; use its existing cancellation history.");
        BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, NestyStay.Domain.BookingStatus.Cancelled, "property_manager_cancel");
        var previousStatus = booking.Status;
        if (booking.PaymentStatus is NestyStay.Domain.PaymentStatus.Captured or NestyStay.Domain.PaymentStatus.Authorized)
        {
            if (booking.PaymentStatus == NestyStay.Domain.PaymentStatus.Authorized)
            {
                // The local/test adapter can void an authorization without a refund.
                // A live provider must perform the corresponding authorization release.
                booking.PaymentStatus = NestyStay.Domain.PaymentStatus.Cancelled;
            }
            else
            {
                var refund = await phaseOneStore.RefundPaymentAsync(booking.Id, new RefundBookingRequest(null, request.Reason.Trim(), idempotencyKey), ct);
                if (refund is null || !string.Equals(refund.PaymentStatus, NestyStay.Domain.PaymentStatus.Refunded.ToString(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Payment refund is pending or failed; reservation was not cancelled.");
                booking.PaymentStatus = NestyStay.Domain.PaymentStatus.Refunded;
            }
        }
        booking.Status = NestyStay.Domain.BookingStatus.Cancelled; booking.UpdatedAt = timeProvider.GetUtcNow();
        db.MilestonePmReservationEvents.Add(new MilestonePmReservationEvent { ManagerUserId = managerUserId, BookingId = booking.Id, ActorUserId = managerUserId, EventType = "CANCELLED", FromStatus = previousStatus.ToString(), ToStatus = booking.Status.ToString(), Reason = request.Reason.Trim(), IdempotencyKey = idempotencyKey });
        AddAudit(managerUserId, "ReservationCancelled", "Booking", booking.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct);
        return await ToReservationDtoAsync(booking, managedProperties, ct);
    }

    public async Task<IReadOnlyList<PmReservationEventDto>> ListReservationHistoryAsync(Guid managerUserId, Guid bookingId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var managedPropertyIds = db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).Select(x => x.Id);
        var managedListingIds = db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.RentalListingId.HasValue && !x.IsDeleted).Select(x => x.RentalListingId!.Value);
        if (!await db.MilestoneBookings.AnyAsync(x => x.Id == bookingId && !x.IsDeleted && (managedPropertyIds.Contains(x.PropertyId) || managedListingIds.Contains(x.PropertyId)), ct)) throw new InvalidOperationException("Reservation is outside the manager portfolio.");
        return await db.MilestonePmReservationEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.BookingId == bookingId && !x.IsDeleted).OrderBy(x => x.CreatedAt).Select(x => new PmReservationEventDto(x.Id, x.BookingId, x.ActorUserId, x.EventType, x.FromStatus, x.ToStatus, x.Reason, x.CreatedAt)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PmCalendarItemDto>> ListMasterCalendarAsync(Guid managerUserId, DateTimeOffset from, DateTimeOffset to, Guid? propertyId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (to <= from) throw new InvalidOperationException("Calendar end must be after start.");
        var portfolioProperties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var pids = portfolioProperties.Select(x => x.Id).ToArray();
        var listingIds = portfolioProperties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        Guid? requestedBookingProperty = null;
        if (propertyId is { } requested)
        {
            var selected = portfolioProperties.SingleOrDefault(x => x.Id == requested) ?? throw new InvalidOperationException("Property is outside the manager portfolio.");
            requestedBookingProperty = selected.RentalListingId ?? selected.Id;
        }
        var result = new List<PmCalendarItemDto>();
        var bookings = await db.MilestoneBookings.AsNoTracking().Where(x => (pids.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)) && !x.IsDeleted && x.CheckIn < DateOnly.FromDateTime(to.UtcDateTime) && x.CheckOut > DateOnly.FromDateTime(from.UtcDateTime)).ToListAsync(ct);
        result.AddRange(bookings.Where(x => requestedBookingProperty is null || x.PropertyId == requestedBookingProperty).Select(x =>
        {
            var managed = portfolioProperties.FirstOrDefault(p => p.Id == x.PropertyId || p.RentalListingId == x.PropertyId);
            return new PmCalendarItemDto("RESERVATION", x.Id, managed?.Id ?? x.PropertyId, new DateTimeOffset(x.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), new DateTimeOffset(x.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), x.PropertyTitle ?? "Reservation", x.Status.ToString(), managed?.OwnerUserId.ToString());
        }));
        var blocks = await db.MilestonePmOwnerBlocks.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.Status == "ACTIVE" && x.StartsAt < to && x.EndsAt > from && (propertyId == null || x.PropertyId == propertyId)).ToListAsync(ct);
        result.AddRange(blocks.Select(x => new PmCalendarItemDto("OWNER_BLOCK", x.Id, x.PropertyId, x.StartsAt, x.EndsAt, x.Reason, x.Status, x.OwnerUserId.ToString())));
        var events = await db.MilestoneManagerCalendarEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.StartsAt < to && x.EndsAt > from && (propertyId == null || x.PropertyId == propertyId)).ToListAsync(ct);
        result.AddRange(events.Select(x => new PmCalendarItemDto(x.EventType, x.Id, x.PropertyId ?? Guid.Empty, x.StartsAt, x.EndsAt, x.Title, x.Status, x.OwnerUserId?.ToString())));
        var maintenance = await db.MilestonePmMaintenanceCases.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.ScheduledAt.HasValue && x.ScheduledAt.Value < to && x.ScheduledAt.Value > from && (propertyId == null || x.PropertyId == propertyId)).ToListAsync(ct);
        result.AddRange(maintenance.Select(x => new PmCalendarItemDto("MAINTENANCE", x.Id, x.PropertyId, x.ScheduledAt!.Value, x.ScheduledAt!.Value.AddHours(1), x.Title, x.Status, x.OwnerUserId.ToString())));
        var cleaning = await db.MilestonePmCleaningReadiness.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.DueAt >= from && x.DueAt <= to && (propertyId == null || x.PropertyId == propertyId)).ToListAsync(ct);
        result.AddRange(cleaning.Select(x => new PmCalendarItemDto("CLEANING", x.Id, x.PropertyId, x.DueAt, x.DueAt.AddHours(2), "Cleaning / readiness", x.Status)));
        var inspections = await db.MilestonePmInspectionRecords.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.ScheduledAt >= from && x.ScheduledAt <= to && (propertyId == null || x.PropertyId == propertyId)).ToListAsync(ct);
        result.AddRange(inspections.Select(x => new PmCalendarItemDto("INSPECTION", x.Id, x.PropertyId, x.ScheduledAt, x.ScheduledAt.AddHours(1), $"{x.InspectionType} inspection", x.Status, x.AssignedUserId?.ToString())));
        return result.OrderBy(x => x.StartsAt).ToList();
    }

    public async Task<PmOperationalDashboardDto> GetOperationalDashboardAsync(Guid managerUserId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var properties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var propertyIds = properties.Select(x => x.Id).ToArray();
        var listingIds = properties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var horizon = today.AddDays(30);
        var reservations = await db.MilestoneBookings.AsNoTracking().Where(x => (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)) && !x.IsDeleted && x.CheckOut > today && x.CheckIn < horizon && x.Status != NestyStay.Domain.BookingStatus.Cancelled && x.Status != NestyStay.Domain.BookingStatus.Rejected).ToListAsync(ct);
        var occupied = reservations.Sum(x => Math.Max(0, Math.Min(x.CheckOut.DayNumber, horizon.DayNumber) - Math.Max(x.CheckIn.DayNumber, today.DayNumber)));
        var portfolioNights = properties.Count * 30;
        var openMaintenance = await db.MilestonePmMaintenanceCases.CountAsync(x => x.ManagerUserId == managerUserId && !x.IsDeleted && x.Status != "CLOSED" && x.Status != "CANCELLED", ct);
        var openWorkOrders = await db.MilestoneWorkOrders.CountAsync(x => x.ManagerUserId == managerUserId && propertyIds.Contains(x.PropertyId) && !x.IsDeleted && x.Status != "COMPLETED" && x.Status != "CANCELLED", ct);
        var notReady = await db.MilestonePmCleaningReadiness.CountAsync(x => x.ManagerUserId == managerUserId && propertyIds.Contains(x.PropertyId) && !x.IsDeleted && x.Status != "READY" && x.Status != "CANCELLED", ct);
        var upcomingInspections = await db.MilestonePmInspectionRecords.CountAsync(x => x.ManagerUserId == managerUserId && propertyIds.Contains(x.PropertyId) && !x.IsDeleted && x.ScheduledAt >= timeProvider.GetUtcNow() && x.ScheduledAt <= timeProvider.GetUtcNow().AddDays(30) && x.Status != "CANCELLED", ct);
        var openIncidents = await db.MilestonePmIncidents.CountAsync(x => x.ManagerUserId == managerUserId && propertyIds.Contains(x.PropertyId) && !x.IsDeleted && x.Status == "OPEN", ct);
        var anomalousUtilities = await db.MilestoneManagerMeterReadings.CountAsync(x => x.ManagerUserId == managerUserId && propertyIds.Contains(x.PropertyId) && x.IsAnomaly && !x.IsDeleted, ct);
        var pendingApprovals = await db.MilestoneP0Approvals.CountAsync(x => x.ManagerUserId == managerUserId && x.Status == "REQUIRED" && !x.IsDeleted, ct);
        var activeVendors = await db.MilestoneManagerVendors.CountAsync(x => x.ManagerUserId == managerUserId && x.IsActive && !x.IsSuspended && !x.IsDeleted, ct);
        var overdueActions = await db.MilestoneWorkOrders.CountAsync(x => x.ManagerUserId == managerUserId && propertyIds.Contains(x.PropertyId) && x.SlaDueAt < timeProvider.GetUtcNow() && x.Status != "COMPLETED" && x.Status != "CANCELLED" && !x.IsDeleted, ct);
        var occupancyPercent = portfolioNights == 0 ? 0m : decimal.Round(occupied * 100m / portfolioNights, 2);
        return new PmOperationalDashboardDto(reservations.Count, occupied, portfolioNights, occupancyPercent, openMaintenance, openWorkOrders, notReady, upcomingInspections, openIncidents, anomalousUtilities, pendingApprovals, activeVendors, overdueActions);
    }

    public async Task<IReadOnlyList<PmTimelineEventDto>> ListOperationalTimelineAsync(Guid managerUserId, Guid? propertyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var query = db.MilestoneAuditEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted);
        if (from is { } start) query = query.Where(x => x.CreatedAt >= start);
        if (to is { } end) query = query.Where(x => x.CreatedAt <= end);
        // Property filtering is applied through the manager-owned entity ids so
        // an audit entry can never reveal another portfolio's history.
        if (propertyId is { } property)
        {
            await EnsurePropertyAsync(managerUserId, property, null, ct);
            var subjects = await db.MilestonePmMaintenanceCases.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == property && !x.IsDeleted).Select(x => x.Id).ToListAsync(ct);
            subjects.AddRange(await db.MilestonePmCleaningReadiness.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == property && !x.IsDeleted).Select(x => x.Id).ToListAsync(ct));
            subjects.AddRange(await db.MilestonePmInspectionRecords.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == property && !x.IsDeleted).Select(x => x.Id).ToListAsync(ct));
            query = query.Where(x => x.SubjectId.HasValue && subjects.Contains(x.SubjectId.Value));
        }
        return await query.OrderByDescending(x => x.CreatedAt).Take(500).Select(x => new PmTimelineEventDto(x.Id, x.ActorUserId, x.ActorRole, x.Action, x.SubjectType, x.SubjectId, x.Reason, x.MetadataJson, x.CreatedAt)).ToListAsync(ct);
    }

    public async Task<PmMaintenanceCaseDto> CreateMaintenanceAsync(Guid managerUserId, CreatePmMaintenanceRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, request.OwnerUserId, ct); if (string.IsNullOrWhiteSpace(request.Title)) throw new InvalidOperationException("Maintenance title is required.");
        var row = new MilestonePmMaintenanceCase { ManagerUserId = managerUserId, OwnerUserId = request.OwnerUserId, PropertyId = request.PropertyId, VendorId = request.VendorId, Number = $"PM-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Random.Shared.Next(100,999)}", Title = request.Title.Trim(), Description = request.Description.Trim(), Priority = request.Priority.Trim().ToUpperInvariant(), SelectedQuoteAmount = request.QuoteAmount, Currency = request.Currency.Trim().ToUpperInvariant() };
        db.MilestonePmMaintenanceCases.Add(row); db.MilestonePmMaintenanceEvents.Add(new MilestonePmMaintenanceEvent { ManagerUserId = managerUserId, MaintenanceId = row.Id, ActorUserId = managerUserId, EventType = "CREATED", ToStatus = row.Status, Details = row.Title }); AddAudit(managerUserId, "MaintenanceCreated", "PmMaintenance", row.Id); await db.SaveChangesAsync(ct); return ToDto(row);
    }

    public async Task<IReadOnlyList<PmMaintenanceCaseDto>> ListMaintenanceAsync(Guid managerUserId, string? status, Guid? propertyId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmMaintenanceCases.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.ToUpperInvariant()); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p);
        return (await q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<PmMaintenanceCaseDto?> TransitionMaintenanceAsync(Guid managerUserId, Guid id, TransitionPmMaintenanceRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var row = await db.MilestonePmMaintenanceCases.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct);
        if (row is null) return null;
        var next = request.Status.Trim().ToUpperInvariant();
        if (!MaintenanceStates.Contains(next)) throw new InvalidOperationException("Unsupported maintenance state.");
        if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Maintenance changed; reload before updating.");
        var previous = row.Status;
        if (next != previous && (!MaintenanceTransitions.TryGetValue(previous, out var allowed) || !allowed.Contains(next, StringComparer.OrdinalIgnoreCase))) throw new InvalidOperationException($"Maintenance cannot transition from {previous} to {next}.");

        var amount = request.ExpenseAmount ?? request.ApprovedAmount ?? row.ExpenseAmount;
        if (amount < 0) throw new InvalidOperationException("Maintenance cost cannot be negative.");
        if (row.FinanciallyPosted && ((request.ExpenseAmount.HasValue && request.ExpenseAmount.Value != row.ExpenseAmount) || (request.OwnerCharge.HasValue && request.OwnerCharge.Value != row.OwnerCharge)))
            throw new InvalidOperationException("Posted maintenance costs are immutable; create a reasoned reversal and replacement entry.");
        MilestonePmMaintenanceQuote? selectedQuote = null;
        if (request.VendorId is { } vendorId && (request.ApprovedAmount ?? row.SelectedQuoteAmount) is { } quoteAmount)
        {
            selectedQuote = await db.MilestonePmMaintenanceQuotes.SingleOrDefaultAsync(x => x.MaintenanceId == row.Id && x.ManagerUserId == managerUserId && x.VendorId == vendorId && x.Amount == quoteAmount && !x.IsDeleted, ct);
            if (selectedQuote is null) throw new InvalidOperationException("Select a quote that belongs to this maintenance case.");
            if (selectedQuote.ExpiresAt is { } expiry && expiry <= timeProvider.GetUtcNow()) throw new InvalidOperationException("The selected vendor quote has expired.");
            if (!string.Equals(selectedQuote.Currency, row.Currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The selected quote currency does not match the maintenance currency.");
        }
        if (next is "ASSIGNED" or "SCHEDULED" or "IN_PROGRESS" or "COMPLETED" or "CLOSED" && amount > 0)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var agreement = await db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.OwnerUserId == row.OwnerUserId && x.Status == "ACTIVE" && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today) && (x.PropertyId == row.PropertyId || x.PropertyId == null) && !x.IsDeleted).OrderByDescending(x => x.PropertyId.HasValue).ThenByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
            var threshold = agreement?.MaintenanceApprovalLimit ?? 0m;
            if (threshold <= 0m || amount > threshold)
            {
                if (request.OwnerApprovalId is not { } approvalId) throw new InvalidOperationException("Owner approval is required before assignment or spend.");
                var approved = await db.MilestoneP0Approvals.AnyAsync(x => x.Id == approvalId && x.ManagerUserId == managerUserId && x.OwnerUserId == row.OwnerUserId && x.PropertyId == row.PropertyId && x.Status == "APPROVED" && x.Amount >= amount && !x.IsDeleted, ct);
                if (!approved) throw new InvalidOperationException("The linked owner approval is missing, rejected, or below the requested amount.");
            }
        }

        row.Status = next;
        row.VendorId = request.VendorId ?? row.VendorId;
        row.SelectedQuoteAmount = request.ApprovedAmount ?? row.SelectedQuoteAmount;
        row.SelectedQuoteId = selectedQuote?.Id ?? row.SelectedQuoteId;
        row.ExpenseAmount = request.ExpenseAmount ?? row.ExpenseAmount;
        row.OwnerCharge = request.OwnerCharge ?? row.OwnerCharge;
        row.ScheduledAt = request.ScheduledAt ?? row.ScheduledAt;
        row.OwnerApprovalId = request.OwnerApprovalId ?? row.OwnerApprovalId;
        row.CostBreakdownJson = JsonSerializer.Serialize(new { expense = row.ExpenseAmount, ownerCharge = row.OwnerCharge, managerFee = row.ManagerFee, quote = row.SelectedQuoteAmount });
        row.CompletedAt = next is "COMPLETED" or "CLOSED" ? timeProvider.GetUtcNow() : row.CompletedAt;
        if (next is "COMPLETED" or "CLOSED" && row.ExpenseAmount > 0 && !row.FinanciallyPosted)
        {
            var journal = await p0Store.PostJournalAsync(new P0Actor(managerUserId, false, true), new P0PostJournalRequest("MAINTENANCE_EXPENSE", row.Id, $"maintenance-expense:{row.Id:N}", row.Currency, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), $"Maintenance {row.Number}", [new P0JournalLineRequest($"OWNER_EXPENSE:{row.OwnerUserId}:ALL:{row.Currency}", row.ExpenseAmount, 0, row.OwnerUserId, row.PropertyId, row.Title), new P0JournalLineRequest($"OWNER_FUNDS:{row.OwnerUserId}:{row.Currency}", 0, row.ExpenseAmount, row.OwnerUserId, row.PropertyId, "Owner funds charged")], ApprovalId: row.OwnerApprovalId), ct);
            row.FinanciallyPosted = true;
            row.FinancialJournalId = journal.Id;
        }
        row.RowVersion++;
        db.MilestonePmMaintenanceEvents.Add(new MilestonePmMaintenanceEvent { ManagerUserId = managerUserId, MaintenanceId = row.Id, ActorUserId = managerUserId, EventType = "STATUS_CHANGED", FromStatus = previous, ToStatus = next, Details = request.Details?.Trim() ?? string.Empty }); AddAudit(managerUserId, "MaintenanceStatusChanged", "PmMaintenance", row.Id);
        await db.SaveChangesAsync(ct);
        return ToDto(row);
    }

    public async Task<PmMaintenanceQuoteDto> AddMaintenanceQuoteAsync(Guid managerUserId, Guid maintenanceId, AddPmMaintenanceQuoteRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmMaintenanceCases.SingleOrDefaultAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Maintenance case not found.");
        if (!await db.MilestoneManagerVendors.AnyAsync(x => x.Id == request.VendorId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Vendor is outside manager portfolio."); if (request.Amount < 0) throw new InvalidOperationException("Quote amount cannot be negative.");
        var quote = new MilestonePmMaintenanceQuote { ManagerUserId = managerUserId, MaintenanceId = row.Id, VendorId = request.VendorId, Amount = request.Amount, Currency = request.Currency.Trim().ToUpperInvariant(), Scope = request.Scope.Trim(), ExpiresAt = request.ExpiresAt?.ToUniversalTime() }; db.MilestonePmMaintenanceQuotes.Add(quote); if (row.Status == "REQUESTED") row.Status = "QUOTING"; db.MilestonePmMaintenanceEvents.Add(new MilestonePmMaintenanceEvent { ManagerUserId = managerUserId, MaintenanceId = row.Id, ActorUserId = managerUserId, EventType = "QUOTE_RECEIVED", ToStatus = row.Status, Details = $"Quote {quote.Amount:0.00} {quote.Currency}" }); await db.SaveChangesAsync(ct); return ToDto(quote);
    }

    public async Task<IReadOnlyList<PmMaintenanceQuoteDto>> ListMaintenanceQuotesAsync(Guid managerUserId, Guid maintenanceId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        if (!await db.MilestonePmMaintenanceCases.AnyAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct))
            throw new InvalidOperationException("Maintenance case not found.");
        var rows = await db.MilestonePmMaintenanceQuotes.AsNoTracking()
            .Where(x => x.MaintenanceId == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted)
            .OrderBy(x => x.Amount).ThenByDescending(x => x.CreatedAt).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<PmMaintenanceEventDto>> ListMaintenanceHistoryAsync(Guid managerUserId, Guid maintenanceId, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); if (!await db.MilestonePmMaintenanceCases.AnyAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Maintenance case not found."); return (await db.MilestonePmMaintenanceEvents.AsNoTracking().Where(x => x.MaintenanceId == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct)).Select(x => new PmMaintenanceEventDto(x.Id, x.MaintenanceId, x.ActorUserId, x.EventType, x.FromStatus, x.ToStatus, x.Details, x.CreatedAt)).ToList(); }

    public async Task<PmCleaningDto> CreateCleaningAsync(Guid managerUserId, CreatePmCleaningRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); ValidateJson(request.ChecklistJson, JsonValueKind.Array); if (request.BookingId is { } bookingId && !await BookingBelongsToManagedPropertyAsync(managerUserId, request.PropertyId, bookingId, ct)) throw new InvalidOperationException("Booking is not linked to this managed property."); var row = new MilestonePmCleaningReadiness { ManagerUserId = managerUserId, PropertyId = request.PropertyId, BookingId = request.BookingId, DueAt = request.DueAt.ToUniversalTime(), AssignedUserId = request.AssignedUserId, VendorId = request.VendorId, ChecklistJson = request.ChecklistJson, TemplateName = "DEFAULT", TemplateVersion = 1 }; db.MilestonePmCleaningReadiness.Add(row); AddAudit(managerUserId, "CleaningCreated", "PmCleaning", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<PmCleaningDto?> UpdateCleaningAsync(Guid managerUserId, Guid id, UpdatePmCleaningRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmCleaningReadiness.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Readiness changed; reload before updating."); ValidateJson(request.ChecklistJson, JsonValueKind.Array); ValidateJson(request.PhotosJson, JsonValueKind.Array); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("NOT_READY" or "IN_PROGRESS" or "READY" or "BLOCKED" or "CANCELLED")) throw new InvalidOperationException("Cleaning status is invalid."); if (status == "READY" && HasIncompleteRequiredChecklist(request.ChecklistJson)) throw new InvalidOperationException("Required checklist items must be complete before readiness can be marked."); row.Status = status; row.ChecklistJson = request.ChecklistJson; row.PhotosJson = request.PhotosJson; row.Issues = request.Issues.Trim(); row.CompletedAt = row.Status == "READY" ? timeProvider.GetUtcNow() : row.CompletedAt; row.RowVersion++; AddAudit(managerUserId, "CleaningUpdated", "PmCleaning", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmCleaningDto>> ListCleaningAsync(Guid managerUserId, Guid? propertyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmCleaningReadiness.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); if (from is { } f) q = q.Where(x => x.DueAt >= f); if (to is { } t) q = q.Where(x => x.DueAt <= t); return (await q.OrderBy(x => x.DueAt).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<PmAssetDto> CreateAssetAsync(Guid managerUserId, CreatePmAssetRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); if (request.Quantity < 0 || string.IsNullOrWhiteSpace(request.AssetTag) || string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("Asset tag, name and non-negative quantity are required."); if (request.PurchaseCost is < 0) throw new InvalidOperationException("Purchase cost cannot be negative."); if (await db.MilestonePmAssets.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.AssetTag == request.AssetTag.Trim() && !x.IsDeleted, ct)) throw new InvalidOperationException("Asset tag already exists for this property."); ValidateJson(request.MetadataJson, JsonValueKind.Object); ValidateJson(request.PhotosJson, JsonValueKind.Array); var row = new MilestonePmAsset { ManagerUserId = managerUserId, PropertyId = request.PropertyId, AssetTag = request.AssetTag.Trim(), Name = request.Name.Trim(), Description = request.Description.Trim(), SerialReference = request.SerialReference.Trim(), PurchaseDate = request.PurchaseDate, PurchaseCost = request.PurchaseCost, WarrantyExpiry = request.WarrantyExpiry, Condition = NormalizeAssetCondition(request.Condition), Category = request.Category.Trim().ToUpperInvariant(), Quantity = request.Quantity, Location = request.Location.Trim(), MetadataJson = request.MetadataJson, PhotosJson = request.PhotosJson }; db.MilestonePmAssets.Add(row); AddAudit(managerUserId, "AssetCreated", "PmAsset", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<PmAssetDto?> UpdateAssetAsync(Guid managerUserId, Guid id, UpdatePmAssetRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmAssets.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Asset changed; reload before updating."); if (request.Quantity < 0) throw new InvalidOperationException("Asset quantity cannot be negative."); if (request.PurchaseCost is < 0) throw new InvalidOperationException("Purchase cost cannot be negative."); ValidateJson(request.MetadataJson, JsonValueKind.Object); ValidateJson(request.PhotosJson, JsonValueKind.Array); row.Status = NormalizeAssetStatus(request.Status); row.Quantity = request.Quantity; row.Location = request.Location.Trim(); row.MetadataJson = request.MetadataJson; row.PhotosJson = request.PhotosJson; if (request.Description is not null) row.Description = request.Description.Trim(); if (request.SerialReference is not null) row.SerialReference = request.SerialReference.Trim(); if (request.PurchaseDate is not null) row.PurchaseDate = request.PurchaseDate; if (request.PurchaseCost is not null) row.PurchaseCost = request.PurchaseCost; if (request.WarrantyExpiry is not null) row.WarrantyExpiry = request.WarrantyExpiry; if (request.Condition is not null) row.Condition = NormalizeAssetCondition(request.Condition); row.RetiredAt = row.Status == "RETIRED" ? timeProvider.GetUtcNow() : row.RetiredAt; row.RowVersion++; AddAudit(managerUserId, "AssetUpdated", "PmAsset", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmAssetDto>> ListAssetsAsync(Guid managerUserId, Guid? propertyId, string? status, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmAssets.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.ToUpper()); return (await q.OrderBy(x => x.AssetTag).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<PmIncidentDto> CreateIncidentAsync(Guid managerUserId, CreatePmIncidentRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); if (string.IsNullOrWhiteSpace(request.Description)) throw new InvalidOperationException("Incident description is required."); var severity = request.Severity.Trim().ToUpperInvariant(); if (severity is not ("LOW" or "MEDIUM" or "HIGH" or "CRITICAL")) throw new InvalidOperationException("Incident severity is invalid."); if (request.FinancialImpact < 0) throw new InvalidOperationException("Financial impact cannot be negative."); ValidateJson(request.InvolvedPartiesJson, JsonValueKind.Array); ValidateJson(request.EvidenceJson, JsonValueKind.Array); var row = new MilestonePmIncident { ManagerUserId = managerUserId, PropertyId = request.PropertyId, BookingId = request.BookingId, IncidentType = request.IncidentType.Trim().ToUpperInvariant(), Severity = severity, OccurredAt = request.OccurredAt.ToUniversalTime(), Description = request.Description.Trim(), InvolvedPartiesJson = request.InvolvedPartiesJson, EvidenceJson = request.EvidenceJson, ActionTaken = request.ActionTaken.Trim(), FollowUp = request.FollowUp.Trim(), FinancialImpact = request.FinancialImpact, InsuranceReference = request.InsuranceReference?.Trim() }; db.MilestonePmIncidents.Add(row); AddAudit(managerUserId, "IncidentCreated", "PmIncident", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<PmIncidentDto?> UpdateIncidentAsync(Guid managerUserId, Guid id, UpdatePmIncidentRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmIncidents.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Incident changed; reload before updating."); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("OPEN" or "RESOLVED" or "CLOSED")) throw new InvalidOperationException("Incident status is invalid."); row.Status = status; row.ActionTaken = request.ActionTaken.Trim(); row.FollowUp = request.FollowUp.Trim(); row.InsuranceReference = request.InsuranceReference?.Trim(); row.ResolvedAt = status is "RESOLVED" or "CLOSED" ? timeProvider.GetUtcNow() : row.ResolvedAt; row.RowVersion++; AddAudit(managerUserId, "IncidentUpdated", "PmIncident", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmIncidentDto>> ListIncidentsAsync(Guid managerUserId, Guid? propertyId, string? status, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmIncidents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.ToUpper()); return (await q.OrderByDescending(x => x.OccurredAt).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<PmInspectionDto> CreateInspectionAsync(Guid managerUserId, CreatePmInspectionRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); ValidateJson(request.ChecklistJson, JsonValueKind.Array); var row = new MilestonePmInspectionRecord { ManagerUserId = managerUserId, PropertyId = request.PropertyId, AssignedUserId = request.AssignedUserId, InspectionType = request.InspectionType.Trim().ToUpperInvariant(), ScheduledAt = request.ScheduledAt.ToUniversalTime(), ChecklistJson = request.ChecklistJson }; db.MilestonePmInspectionRecords.Add(row); AddAudit(managerUserId, "InspectionCreated", "PmInspection", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<PmInspectionDto?> UpdateInspectionAsync(Guid managerUserId, Guid id, UpdatePmInspectionRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmInspectionRecords.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Inspection changed; reload before updating."); ValidateJson(request.EvidenceJson, JsonValueKind.Array); ValidateJson(request.FindingsJson, JsonValueKind.Array); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("SCHEDULED" or "IN_PROGRESS" or "SIGNED_OFF" or "CANCELLED")) throw new InvalidOperationException("Inspection status is invalid."); row.Status = status; row.EvidenceJson = request.EvidenceJson; row.FindingsJson = request.FindingsJson; row.SignedOffAt = status == "SIGNED_OFF" ? timeProvider.GetUtcNow() : row.SignedOffAt; row.RowVersion++; AddAudit(managerUserId, "InspectionUpdated", "PmInspection", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmInspectionDto>> ListInspectionsAsync(Guid managerUserId, Guid? propertyId, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmInspectionRecords.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); return (await q.OrderBy(x => x.ScheduledAt).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<WorkOrderDto?> CreateInspectionWorkOrderAsync(Guid managerUserId, Guid inspectionId, CreatePmInspectionWorkOrderRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        if (string.IsNullOrWhiteSpace(request.Scope)) throw new InvalidOperationException("Corrective work scope is required.");
        if (request.QuoteAmount is < 0) throw new InvalidOperationException("Quote amount cannot be negative.");
        var inspection = await db.MilestonePmInspectionRecords.SingleOrDefaultAsync(x => x.Id == inspectionId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct);
        if (inspection is null) return null;
        if (inspection.CorrectiveWorkOrderId is { } existingId)
        {
            var existing = await db.MilestoneWorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == existingId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct);
            if (existing is not null) return ToWorkOrderDto(existing);
        }
        var ownerId = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == managerUserId && x.Id == inspection.PropertyId && !x.IsDeleted).Select(x => x.OwnerUserId).SingleOrDefaultAsync(ct);
        if (ownerId == Guid.Empty) throw new InvalidOperationException("Inspection property is outside the manager portfolio.");
        if (request.VendorId is { } vendor && !await db.MilestoneManagerVendors.AnyAsync(x => x.Id == vendor && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Vendor is outside the manager portfolio.");
        var workOrder = new MilestoneWorkOrder { ManagerUserId = managerUserId, PropertyId = inspection.PropertyId, OwnerUserId = ownerId, VendorId = request.VendorId, WorkOrderNumber = $"WO-{timeProvider.GetUtcNow():yyyyMMdd}-{Guid.NewGuid():N}"[..20], Scope = request.Scope.Trim(), Status = "REQUEST", QuoteAmount = request.QuoteAmount, SlaDueAt = request.SlaDueAt };
        db.MilestoneWorkOrders.Add(workOrder); inspection.CorrectiveWorkOrderId = workOrder.Id; inspection.RowVersion++; AddAudit(managerUserId, "InspectionCorrectiveWorkOrderCreated", "PmInspection", inspection.Id); await db.SaveChangesAsync(ct); return ToWorkOrderDto(workOrder);
    }

    private void AddAudit(Guid actorUserId, string action, string subjectType, Guid subjectId) => db.MilestoneAuditEvents.Add(new MilestoneAuditEvent { ManagerUserId = actorUserId, ActorUserId = actorUserId, ActorRole = "PropertyManager", Action = action, SubjectType = subjectType, SubjectId = subjectId, Reason = action, MetadataJson = "{\"source\":\"professional-operations\"}" });
    private static void ValidateJson(string value, JsonValueKind kind) { try { using var doc = JsonDocument.Parse(value); if (doc.RootElement.ValueKind != kind || value.Length > 50000) throw new InvalidOperationException("Structured data is invalid."); } catch (JsonException) { throw new InvalidOperationException("Structured data must be valid JSON."); } }
    private static PmOwnerBlockDto ToDto(MilestonePmOwnerBlock x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.StartsAt, x.EndsAt, x.TimeZone, x.Reason, x.Status, x.BookingId, x.RowVersion, x.Category, x.Notes);
    private static PmReservationNoteDto ToDto(MilestonePmReservationNote x) => new(x.Id, x.BookingId, x.AuthorUserId, x.Body, x.Visibility, x.CreatedAt);
    private static PmMaintenanceCaseDto ToDto(MilestonePmMaintenanceCase x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.VendorId, x.Number, x.Title, x.Description, x.Status, x.Priority, x.SelectedQuoteAmount, x.ExpenseAmount, x.OwnerCharge, x.ManagerFee, x.ScheduledAt, x.Currency, x.RowVersion, x.SelectedQuoteId, x.FinanciallyPosted, x.FinancialJournalId);
    private static WorkOrderDto ToWorkOrderDto(MilestoneWorkOrder x) => new(x.Id, x.PropertyId, x.OwnerUserId, x.VendorId, x.WorkOrderNumber, x.Scope, x.Status, x.QuoteAmount, x.ApprovedAmount, x.LaborAmount, x.PartsAmount, x.SlaDueAt, x.ScheduledAt);
    private static PmMaintenanceQuoteDto ToDto(MilestonePmMaintenanceQuote x) => new(x.Id, x.MaintenanceId, x.VendorId, x.Amount, x.Currency, x.Scope, x.Status, x.ExpiresAt);
    private static PmCleaningDto ToDto(MilestonePmCleaningReadiness x) => new(x.Id, x.PropertyId, x.BookingId, x.AssignedUserId, x.VendorId, x.DueAt, x.Status, x.ChecklistJson, x.PhotosJson, x.Issues, x.CompletedAt, x.RowVersion, x.TemplateName, x.TemplateVersion);
    private static PmAssetDto ToDto(MilestonePmAsset x) => new(x.Id, x.PropertyId, x.AssetTag, x.Name, x.Category, x.Status, x.Quantity, x.Location, x.MetadataJson, x.PhotosJson, x.RetiredAt, x.RowVersion, x.Description, x.SerialReference, x.PurchaseDate, x.PurchaseCost, x.WarrantyExpiry, x.Condition);
    private static string NormalizeAssetCondition(string? condition) => condition?.Trim().ToUpperInvariant() switch { "GOOD" or "FAIR" or "POOR" or "DAMAGED" => condition.Trim().ToUpperInvariant(), _ => throw new InvalidOperationException("Asset condition must be GOOD, FAIR, POOR or DAMAGED.") };
    private static string NormalizeAssetStatus(string? status) => status?.Trim().ToUpperInvariant() switch { "ACTIVE" or "RETIRED" or "LOST" or "DAMAGED" => status.Trim().ToUpperInvariant(), _ => throw new InvalidOperationException("Asset status must be ACTIVE, RETIRED, LOST or DAMAGED.") };
    private static string NormalizeBlockCategory(string? category) => category?.Trim().ToUpperInvariant() switch { "OWNER_STAY" or "PERSONAL" or "MAINTENANCE" or "OTHER" => category.Trim().ToUpperInvariant(), _ => throw new InvalidOperationException("Block category must be OWNER_STAY, PERSONAL, MAINTENANCE or OTHER.") };
    private static PmIncidentDto ToDto(MilestonePmIncident x) => new(x.Id, x.PropertyId, x.BookingId, x.IncidentType, x.Severity, x.OccurredAt, x.Description, x.InvolvedPartiesJson, x.EvidenceJson, x.ActionTaken, x.FollowUp, x.FinancialImpact, x.InsuranceReference, x.Status, x.ResolvedAt, x.RowVersion);
    private static PmInspectionDto ToDto(MilestonePmInspectionRecord x) => new(x.Id, x.PropertyId, x.AssignedUserId, x.InspectionType, x.ScheduledAt, x.ChecklistJson, x.EvidenceJson, x.FindingsJson, x.Status, x.SignedOffAt, x.RowVersion, x.CorrectiveWorkOrderId);

    private async Task<PmReservationDto> ToReservationDtoAsync(MilestoneBooking booking, IReadOnlyList<MilestoneManagerProperty> properties, CancellationToken ct)
    {
        var managed = properties.FirstOrDefault(p => p.Id == booking.PropertyId || p.RentalListingId == booking.PropertyId);
        var notes = await db.MilestonePmReservationNotes.AsNoTracking().Where(x => x.BookingId == booking.Id && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        return new PmReservationDto(booking.Id, managed?.Id ?? booking.PropertyId, booking.GuestUserId, booking.HostUserId, new DateTimeOffset(booking.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), new DateTimeOffset(booking.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), booking.Status.ToString(), booking.PaymentStatus.ToString(), booking.TotalAmount, booking.Currency, notes.Select(ToDto).ToList(), booking.GuestName, booking.GuestEmail, booking.PropertyTitle, booking.UpdatedAt);
    }

    private async Task<bool> BookingBelongsToManagedPropertyAsync(Guid managerUserId, Guid managedPropertyId, Guid bookingId, CancellationToken ct)
    {
        var listingId = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.Id == managedPropertyId && x.ManagerUserId == managerUserId && !x.IsDeleted).Select(x => x.RentalListingId).SingleOrDefaultAsync(ct);
        return await db.MilestoneBookings.AnyAsync(x => x.Id == bookingId && !x.IsDeleted && (x.PropertyId == managedPropertyId || (listingId.HasValue && x.PropertyId == listingId.Value)), ct);
    }

    private static bool HasIncompleteRequiredChecklist(string json)
    {
        using var document = JsonDocument.Parse(json);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var required = item.TryGetProperty("required", out var requiredValue) && requiredValue.ValueKind == JsonValueKind.True;
            var complete = item.TryGetProperty("completed", out var completeValue) && completeValue.ValueKind == JsonValueKind.True;
            if (required && !complete) return true;
        }
        return false;
    }

    private static bool TryParseBookingStatus(string value, out NestyStay.Domain.BookingStatus status)
    {
        var normalized = value.Trim().Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        foreach (var candidate in Enum.GetValues<NestyStay.Domain.BookingStatus>())
        {
            if (string.Equals(candidate.ToString(), normalized, StringComparison.OrdinalIgnoreCase)) { status = candidate; return true; }
        }
        status = default; return false;
    }
}
