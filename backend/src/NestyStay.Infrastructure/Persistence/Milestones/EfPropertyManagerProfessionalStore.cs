using System.Data;
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
    private static readonly SemaphoreSlim NonRelationalRebookGate = new(1, 1);
    private static readonly string[] BlockingCalendarTypes = ["BOOKING", "RESERVATION", "OWNER_BLOCK", "OUT_OF_SERVICE", "IMPORTED_UNAVAILABLE"];
    private static readonly string[] MaintenanceStates = ["REQUESTED", "TRIAGED", "QUOTING", "OWNER_APPROVAL", "ASSIGNED", "SCHEDULED", "IN_PROGRESS", "COMPLETED", "CLOSED", "CANCELLED"];
    private static readonly IReadOnlyDictionary<string, string[]> MaintenanceTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["REQUESTED"] = ["TRIAGED", "CANCELLED"], ["TRIAGED"] = ["QUOTING", "OWNER_APPROVAL", "CANCELLED"], ["QUOTING"] = ["OWNER_APPROVAL", "ASSIGNED", "CANCELLED"],
        ["OWNER_APPROVAL"] = ["ASSIGNED", "CANCELLED"], ["ASSIGNED"] = ["SCHEDULED", "CANCELLED"], ["SCHEDULED"] = ["IN_PROGRESS", "CANCELLED"], ["IN_PROGRESS"] = ["COMPLETED"], ["COMPLETED"] = ["CLOSED", "IN_PROGRESS"], ["CLOSED"] = ["IN_PROGRESS"], ["CANCELLED"] = []
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
        var starts = request.StartsAt.ToUniversalTime(); var ends = request.EndsAt.ToUniversalTime();
        if (ends <= starts) throw new InvalidOperationException("Block end must be after start.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A block reason is required.");
        var rentalListingId = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.Id == request.PropertyId && x.ManagerUserId == managerUserId && !x.IsDeleted).Select(x => x.RentalListingId).SingleOrDefaultAsync(ct);
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct) : null;
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:availability:{rentalListingId ?? request.PropertyId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var overlap = await db.MilestonePmOwnerBlocks.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status == "ACTIVE" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        var calendarConflict = await db.MilestoneManagerCalendarEvents.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.StartsAt < ends && x.EndsAt > starts && !x.IsDeleted, ct);
        var maintenanceConflict = await db.MilestonePmMaintenanceCases.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.ScheduledAt >= starts && x.ScheduledAt <= ends && !x.IsDeleted, ct);
        var cleaningConflict = await db.MilestonePmCleaningReadiness.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.DueAt >= starts && x.DueAt <= ends && !x.IsDeleted, ct);
        var inspectionConflict = await db.MilestonePmInspectionRecords.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status != "CANCELLED" && x.ScheduledAt >= starts && x.ScheduledAt <= ends && !x.IsDeleted, ct);
        if (overlap || calendarConflict || maintenanceConflict || cleaningConflict || inspectionConflict)
            throw new InvalidOperationException("The requested owner block conflicts with an existing calendar event.");
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
        if (request.ExpectedUpdatedTicks is { } expected && booking.UpdatedAt.UtcTicks != expected) throw new DbUpdateConcurrencyException("Reservation changed; reload before updating.");
        if (request.ExpectedUpdatedAt is { } expectedAt && booking.UpdatedAt.ToUnixTimeMilliseconds() != expectedAt.ToUnixTimeMilliseconds()) throw new DbUpdateConcurrencyException("Reservation changed; reload before updating.");
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
        var checkIn = request.CheckIn ?? booking.CheckIn;
        var checkOut = request.CheckOut ?? booking.CheckOut;
        if (checkOut <= checkIn) throw new InvalidOperationException("Check-out must be after check-in.");
        if (checkIn != booking.CheckIn || checkOut != booking.CheckOut)
            throw new InvalidOperationException("Reservation dates must be changed through the preview and confirm amendment workflow.");
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
        var calendarConflict = await db.MilestoneManagerCalendarEvents.AsNoTracking().AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == managed.Id && BlockingCalendarTypes.Contains(x.EventType) && x.Status != "CANCELLED" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        var priceChanged = proposedTotal != booking.TotalAmount;
        var paid = booking.PaymentStatus is NestyStay.Domain.PaymentStatus.Authorized or NestyStay.Domain.PaymentStatus.Captured;
        var reason = conflict ? "The proposed dates overlap another reservation." : blockConflict ? "The proposed dates overlap an active owner block." : calendarConflict ? "The proposed dates overlap an out-of-service or imported-unavailable calendar event." : paid && priceChanged ? "Paid reservations with a price change must be cancelled and rebooked." : null;
        return new PmReservationDateChangePreviewDto(booking.Id, booking.CheckIn, booking.CheckOut, request.CheckIn, request.CheckOut, booking.Nights, proposedNights, booking.TotalAmount, proposedTotal, booking.Currency, reason is null, reason, paid && priceChanged);
    }

    public async Task<PmReservationDto?> AmendReservationAsync(Guid managerUserId, Guid bookingId, AmendPmReservationRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("An amendment reason and idempotency key are required.");
        if (request.CheckOut <= request.CheckIn) throw new InvalidOperationException("Check-out must be after check-in.");

        await using var transaction = db.Database.IsNpgsql()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)
            : null;
        if (db.Database.IsNpgsql())
        {
            var reservationLock = $"nesty-pm:reservation:{bookingId}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({reservationLock}, 0))", ct);
        }

        var properties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var propertyIds = properties.Select(x => x.Id).ToArray();
        var listingIds = properties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(x => x.Id == bookingId && !x.IsDeleted && (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)), ct);
        if (booking is null) return null;
        var managed = properties.First(x => x.Id == booking.PropertyId || x.RentalListingId == booking.PropertyId);
        var payload = JsonSerializer.Serialize(new { checkIn = request.CheckIn, checkOut = request.CheckOut, reason = request.Reason.Trim() });
        var idempotencyKey = request.IdempotencyKey.Trim();
        var prior = await db.MilestonePmReservationEvents.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == idempotencyKey && !x.IsDeleted, ct);
        if (prior is not null)
        {
            if (prior.BookingId != bookingId || prior.EventType != "AMENDED" || !JsonPayloadEquals(prior.PayloadJson, payload))
                throw new InvalidOperationException("The idempotency key was already used for a different reservation operation.");
            return await ToReservationDtoAsync(booking, properties, ct);
        }
        if (request.ExpectedUpdatedTicks is { } expected && booking.UpdatedAt.UtcTicks != expected)
            throw new DbUpdateConcurrencyException("Reservation changed; reload the preview before confirming.");
        if (request.ExpectedUpdatedAt is { } expectedAt && booking.UpdatedAt.ToUnixTimeMilliseconds() != expectedAt.ToUnixTimeMilliseconds())
            throw new DbUpdateConcurrencyException("Reservation changed; reload the preview before confirming.");
        if (booking.Status is NestyStay.Domain.BookingStatus.Cancelled or NestyStay.Domain.BookingStatus.Rejected)
            throw new InvalidOperationException("Cancelled or rejected reservations cannot be amended. Use rebooking for a cancelled reservation.");

        if (db.Database.IsNpgsql())
        {
            var availabilityLock = $"nesty-pm:availability:{booking.PropertyId}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({availabilityLock}, 0))", ct);
        }
        var conflict = await db.MilestoneBookings.AnyAsync(x => x.Id != booking.Id && x.PropertyId == booking.PropertyId && !x.IsDeleted && x.CheckIn < request.CheckOut && request.CheckIn < x.CheckOut &&
            (x.Status == NestyStay.Domain.BookingStatus.PendingVerification || x.Status == NestyStay.Domain.BookingStatus.Approved || x.Status == NestyStay.Domain.BookingStatus.PaymentCaptured || x.Status == NestyStay.Domain.BookingStatus.Confirmed), ct);
        var starts = new DateTimeOffset(request.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var ends = new DateTimeOffset(request.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var blockConflict = await db.MilestonePmOwnerBlocks.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == managed.Id && x.Status == "ACTIVE" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        var calendarConflict = await db.MilestoneManagerCalendarEvents.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == managed.Id && BlockingCalendarTypes.Contains(x.EventType) && x.Status != "CANCELLED" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        if (conflict || blockConflict || calendarConflict) throw new InvalidOperationException("The revised reservation conflicts with another reservation, owner block, or unavailable calendar event.");

        var proposedNights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
        var perNightFee = booking.GuestPlatformFee / Math.Max(1, booking.Nights);
        var proposedSubtotal = decimal.Round(booking.NightlyRate * proposedNights, 2, MidpointRounding.AwayFromZero);
        var proposedGuestFee = decimal.Round(perNightFee * proposedNights, 2, MidpointRounding.AwayFromZero);
        var proposedTotal = proposedSubtotal + proposedGuestFee;
        if (booking.PaymentStatus is (NestyStay.Domain.PaymentStatus.Authorized or NestyStay.Domain.PaymentStatus.Captured) && proposedTotal != booking.TotalAmount)
            throw new InvalidOperationException("Paid reservations with a price change must be cancelled and rebooked.");

        var previousDates = $"{booking.CheckIn:yyyy-MM-dd} to {booking.CheckOut:yyyy-MM-dd}";
        booking.CheckIn = request.CheckIn;
        booking.CheckOut = request.CheckOut;
        booking.Nights = proposedNights;
        booking.StaySubtotal = proposedSubtotal;
        booking.GuestPlatformFee = proposedGuestFee;
        booking.TotalAmount = proposedTotal;
        booking.UpdatedAt = timeProvider.GetUtcNow();
        db.MilestonePmReservationEvents.Add(new MilestonePmReservationEvent
        {
            ManagerUserId = managerUserId,
            BookingId = booking.Id,
            ActorUserId = managerUserId,
            EventType = "AMENDED",
            FromStatus = booking.Status.ToString(),
            ToStatus = booking.Status.ToString(),
            Reason = $"{request.Reason.Trim()} · {previousDates} → {request.CheckIn:yyyy-MM-dd} to {request.CheckOut:yyyy-MM-dd}",
            IdempotencyKey = idempotencyKey,
            PayloadJson = payload
        });
        AddAudit(managerUserId, "ReservationAmended", "Booking", booking.Id);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return await ToReservationDtoAsync(booking, properties, ct);
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
        var payload = JsonSerializer.Serialize(new { reason = request.Reason.Trim() });
        var prior = await db.MilestonePmReservationEvents.AsNoTracking().FirstOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == idempotencyKey && !x.IsDeleted, ct);
        if (prior is not null)
        {
            if (prior.BookingId != bookingId || prior.EventType != "CANCELLED" || !JsonPayloadEquals(prior.PayloadJson, payload)) throw new InvalidOperationException("Idempotency key was already used for a different reservation operation.");
            var priorBooking = await db.MilestoneBookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == prior.BookingId && !x.IsDeleted, ct);
            return priorBooking is null ? null : await ToReservationDtoAsync(priorBooking, managedProperties, ct);
        }
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(x => x.Id == bookingId && (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)) && !x.IsDeleted, ct);
        if (booking is null) return null;
        if (request.ExpectedUpdatedTicks is { } expected && booking.UpdatedAt.UtcTicks != expected) throw new DbUpdateConcurrencyException("Reservation changed; reload before cancelling.");
        if (request.ExpectedUpdatedAt is { } expectedAt && booking.UpdatedAt.ToUnixTimeMilliseconds() != expectedAt.ToUnixTimeMilliseconds()) throw new DbUpdateConcurrencyException("Reservation changed; reload before cancelling.");
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
        db.MilestonePmReservationEvents.Add(new MilestonePmReservationEvent { ManagerUserId = managerUserId, BookingId = booking.Id, ActorUserId = managerUserId, EventType = "CANCELLED", FromStatus = previousStatus.ToString(), ToStatus = booking.Status.ToString(), Reason = request.Reason.Trim(), IdempotencyKey = idempotencyKey, PayloadJson = payload });
        AddAudit(managerUserId, "ReservationCancelled", "Booking", booking.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct);
        return await ToReservationDtoAsync(booking, managedProperties, ct);
    }

    public async Task<PmReservationRebookDto?> RebookReservationAsync(Guid managerUserId, Guid bookingId, RebookPmReservationRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("A rebooking reason and idempotency key are required.");
        if (request.CheckOut <= request.CheckIn) throw new InvalidOperationException("Check-out must be after check-in.");

        if (!db.Database.IsNpgsql())
        {
            await NonRelationalRebookGate.WaitAsync(ct);
            try { return await RebookReservationCoreAsync(managerUserId, bookingId, request, ct); }
            finally { NonRelationalRebookGate.Release(); }
        }

        await db.Database.OpenConnectionAsync(ct);
        var lockKey = $"nesty-pm:rebook:{managerUserId}:{request.IdempotencyKey.Trim()}";
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_lock(hashtextextended({lockKey}, 0))", ct);
            return await RebookReservationCoreAsync(managerUserId, bookingId, request, ct);
        }
        finally
        {
            try { await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_unlock(hashtextextended({lockKey}, 0))", ct); }
            finally { await db.Database.CloseConnectionAsync(); }
        }
    }

    private async Task<PmReservationRebookDto?> RebookReservationCoreAsync(Guid managerUserId, Guid bookingId, RebookPmReservationRequest request, CancellationToken ct)
    {
        var properties = await db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).ToListAsync(ct);
        var propertyIds = properties.Select(x => x.Id).ToArray();
        var listingIds = properties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        var original = await db.MilestoneBookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == bookingId && !x.IsDeleted && (propertyIds.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)), ct);
        if (original is null) return null;
        var payload = JsonSerializer.Serialize(new { checkIn = request.CheckIn, checkOut = request.CheckOut, reason = request.Reason.Trim() });
        var idempotencyKey = request.IdempotencyKey.Trim();
        var prior = await db.MilestonePmReservationEvents.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == idempotencyKey && !x.IsDeleted, ct);
        if (prior is not null)
        {
            if (prior.BookingId != bookingId || prior.EventType != "REBOOKED" || !JsonPayloadEquals(prior.PayloadJson, payload) || prior.RelatedBookingId is null)
                throw new InvalidOperationException("The idempotency key was already used for a different reservation operation.");
            var priorReplacement = await db.MilestoneBookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == prior.RelatedBookingId && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("The persisted replacement reservation could not be found.");
            return new PmReservationRebookDto(bookingId, await ToReservationDtoAsync(priorReplacement, properties, ct), true);
        }
        if (original.Status != NestyStay.Domain.BookingStatus.Cancelled)
            throw new InvalidOperationException("Only a cancelled reservation can be rebooked from this workflow.");

        var managed = properties.First(x => x.Id == original.PropertyId || x.RentalListingId == original.PropertyId);
        var listingId = managed.RentalListingId ?? original.PropertyId;
        var starts = new DateTimeOffset(request.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var ends = new DateTimeOffset(request.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var calendarConflict = await db.MilestoneManagerCalendarEvents.AsNoTracking().AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == managed.Id && BlockingCalendarTypes.Contains(x.EventType) && x.Status != "CANCELLED" && !x.IsDeleted && x.StartsAt < ends && x.EndsAt > starts, ct);
        if (calendarConflict) throw new InvalidOperationException("The replacement dates overlap an out-of-service or imported-unavailable calendar event.");
        var replacement = await phaseOneStore.CreateBookingAsync(new CreateBookingRequest(listingId, original.GuestUserId, request.CheckIn, request.CheckOut), ct);
        db.MilestonePmReservationEvents.Add(new MilestonePmReservationEvent
        {
            ManagerUserId = managerUserId,
            BookingId = original.Id,
            ActorUserId = managerUserId,
            EventType = "REBOOKED",
            FromStatus = original.Status.ToString(),
            ToStatus = original.Status.ToString(),
            Reason = request.Reason.Trim(),
            IdempotencyKey = idempotencyKey,
            RelatedBookingId = replacement.Id,
            PayloadJson = payload
        });
        db.MilestonePmReservationEvents.Add(new MilestonePmReservationEvent
        {
            ManagerUserId = managerUserId,
            BookingId = replacement.Id,
            ActorUserId = managerUserId,
            EventType = "REBOOK_CREATED",
            FromStatus = string.Empty,
            ToStatus = replacement.Status,
            Reason = $"Replacement for cancelled reservation {original.Id}",
            RelatedBookingId = original.Id,
            PayloadJson = payload
        });
        AddAudit(managerUserId, "ReservationRebooked", "Booking", original.Id);
        await db.SaveChangesAsync(ct);
        var persisted = await db.MilestoneBookings.AsNoTracking().SingleAsync(x => x.Id == replacement.Id && !x.IsDeleted, ct);
        return new PmReservationRebookDto(original.Id, await ToReservationDtoAsync(persisted, properties, ct), false);
    }

    public async Task<IReadOnlyList<PmReservationEventDto>> ListReservationHistoryAsync(Guid managerUserId, Guid bookingId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var managedPropertyIds = db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted).Select(x => x.Id);
        var managedListingIds = db.MilestoneManagerProperties.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.RentalListingId.HasValue && !x.IsDeleted).Select(x => x.RentalListingId!.Value);
        if (!await db.MilestoneBookings.AnyAsync(x => x.Id == bookingId && !x.IsDeleted && (managedPropertyIds.Contains(x.PropertyId) || managedListingIds.Contains(x.PropertyId)), ct)) throw new InvalidOperationException("Reservation is outside the manager portfolio.");
        return await db.MilestonePmReservationEvents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.BookingId == bookingId && !x.IsDeleted).OrderBy(x => x.CreatedAt).Select(x => new PmReservationEventDto(x.Id, x.BookingId, x.ActorUserId, x.EventType, x.FromStatus, x.ToStatus, x.Reason, x.CreatedAt, x.RelatedBookingId, x.PayloadJson)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PmCalendarItemDto>> ListMasterCalendarAsync(Guid managerUserId, DateTimeOffset from, DateTimeOffset to, Guid? propertyId, Guid? ownerUserId, string? eventType, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        from = from.ToUniversalTime();
        to = to.ToUniversalTime();
        if (to <= from) throw new InvalidOperationException("Calendar end must be after start.");
        if ((to - from).TotalDays > 370) throw new InvalidOperationException("Calendar ranges cannot exceed 370 days.");

        var portfolioProperties = await db.MilestoneManagerProperties.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted)
            .ToListAsync(ct);
        if (propertyId is { } requestedProperty && portfolioProperties.All(x => x.Id != requestedProperty))
            throw new InvalidOperationException("Property is outside the manager portfolio.");
        if (ownerUserId is { } requestedOwner && portfolioProperties.All(x => x.OwnerUserId != requestedOwner))
            throw new InvalidOperationException("Owner is outside the manager portfolio.");

        var selectedProperties = portfolioProperties
            .Where(x => (!propertyId.HasValue || x.Id == propertyId) && (!ownerUserId.HasValue || x.OwnerUserId == ownerUserId))
            .ToList();
        var pids = selectedProperties.Select(x => x.Id).ToArray();
        var listingIds = selectedProperties.Where(x => x.RentalListingId.HasValue).Select(x => x.RentalListingId!.Value).ToArray();
        var normalizedType = string.IsNullOrWhiteSpace(eventType) || string.Equals(eventType, "ALL", StringComparison.OrdinalIgnoreCase)
            ? null
            : eventType.Trim().ToUpperInvariant();
        var result = new List<PmCalendarItemDto>();

        var bookings = await db.MilestoneBookings.AsNoTracking()
            .Where(x => (pids.Contains(x.PropertyId) || listingIds.Contains(x.PropertyId)) && !x.IsDeleted && x.CheckIn < DateOnly.FromDateTime(to.UtcDateTime) && x.CheckOut > DateOnly.FromDateTime(from.UtcDateTime))
            .Take(2000)
            .ToListAsync(ct);
        result.AddRange(bookings.Select(x =>
        {
            var managed = selectedProperties.First(p => p.Id == x.PropertyId || p.RentalListingId == x.PropertyId);
            return new PmCalendarItemDto("RESERVATION", x.Id, managed.Id, new DateTimeOffset(x.CheckIn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), new DateTimeOffset(x.CheckOut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), x.PropertyTitle ?? "Reservation", x.Status.ToString(), managed.OwnerUserId, RelatedPath: $"/pm/operations?tab=reservations&reservation={x.Id}");
        }));

        var blocks = await db.MilestonePmOwnerBlocks.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && pids.Contains(x.PropertyId) && !x.IsDeleted && x.StartsAt < to && x.EndsAt > from)
            .Take(2000).ToListAsync(ct);
        result.AddRange(blocks.Select(x => new PmCalendarItemDto("OWNER_BLOCK", x.Id, x.PropertyId, x.StartsAt, x.EndsAt, x.Reason, x.Status, x.OwnerUserId, RelatedPath: $"/pm/operations?tab=blocks&block={x.Id}")));

        var events = await db.MilestoneManagerCalendarEvents.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && x.PropertyId.HasValue && pids.Contains(x.PropertyId.Value) && !x.IsDeleted && x.StartsAt < to && x.EndsAt > from)
            .Take(2000).ToListAsync(ct);
        result.AddRange(events.Select(x => new PmCalendarItemDto(x.EventType.Trim().ToUpperInvariant(), x.Id, x.PropertyId!.Value, x.StartsAt, x.EndsAt, x.Title, x.Status, x.OwnerUserId ?? selectedProperties.First(p => p.Id == x.PropertyId).OwnerUserId, RelatedPath: $"/pm/calendar?event={x.Id}")));

        var maintenance = await db.MilestonePmMaintenanceCases.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && pids.Contains(x.PropertyId) && !x.IsDeleted && x.ScheduledAt.HasValue && x.ScheduledAt.Value < to && x.ScheduledAt.Value.AddHours(1) > from)
            .Take(2000).ToListAsync(ct);
        result.AddRange(maintenance.Select(x => new PmCalendarItemDto("MAINTENANCE", x.Id, x.PropertyId, x.ScheduledAt!.Value, x.ScheduledAt!.Value.AddHours(1), x.Title, x.Status, x.OwnerUserId, RelatedPath: $"/pm/operations?tab=maintenance&maintenance={x.Id}")));

        var workOrders = await db.MilestoneWorkOrders.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && pids.Contains(x.PropertyId) && !x.IsDeleted && x.ScheduledAt.HasValue && x.ScheduledAt.Value < to && x.ScheduledAt.Value.AddHours(2) > from)
            .Take(2000).ToListAsync(ct);
        result.AddRange(workOrders.Select(x => new PmCalendarItemDto("WORK_ORDER", x.Id, x.PropertyId, x.ScheduledAt!.Value, x.ScheduledAt!.Value.AddHours(2), x.Scope, x.Status, x.OwnerUserId, RelatedPath: $"/pm/work-orders?workOrder={x.Id}")));

        var cleaning = await db.MilestonePmCleaningReadiness.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && pids.Contains(x.PropertyId) && !x.IsDeleted && x.DueAt < to && x.DueAt.AddHours(2) > from)
            .Take(2000).ToListAsync(ct);
        result.AddRange(cleaning.Select(x => new PmCalendarItemDto("CLEANING", x.Id, x.PropertyId, x.DueAt, x.DueAt.AddHours(2), "Cleaning / readiness", x.Status, selectedProperties.First(p => p.Id == x.PropertyId).OwnerUserId, RelatedPath: $"/pm/operations?tab=cleaning&cleaning={x.Id}")));

        var inspections = await db.MilestonePmInspectionRecords.AsNoTracking()
            .Where(x => x.ManagerUserId == managerUserId && pids.Contains(x.PropertyId) && !x.IsDeleted && x.ScheduledAt < to && x.ScheduledAt.AddHours(1) > from)
            .Take(2000).ToListAsync(ct);
        result.AddRange(inspections.Select(x => new PmCalendarItemDto("INSPECTION", x.Id, x.PropertyId, x.ScheduledAt, x.ScheduledAt.AddHours(1), $"{x.InspectionType} inspection", x.Status, selectedProperties.First(p => p.Id == x.PropertyId).OwnerUserId, RelatedPath: $"/pm/operations?tab=inspections&inspection={x.Id}")));

        if (normalizedType is not null) result = result.Where(x => x.Type == normalizedType).ToList();
        var active = result.Where(x => !string.Equals(x.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.Status, "REJECTED", StringComparison.OrdinalIgnoreCase)).ToList();
        var enriched = result.Select(item =>
        {
            if (!active.Contains(item)) return item;
            var conflicts = active.Where(other => other != item && other.PropertyId == item.PropertyId && other.StartsAt < item.EndsAt && other.EndsAt > item.StartsAt)
                .Select(other =>
                {
                    var blocking = IsBlockingCalendarItem(item) && IsBlockingCalendarItem(other);
                    var level = blocking ? "BLOCKING" : "WARNING";
                    var explanation = blocking
                        ? $"{item.Type} overlaps blocking {other.Type.ToLowerInvariant()} '{other.Title}'."
                        : $"{item.Type} overlaps operational {other.Type.ToLowerInvariant()} '{other.Title}'; review staffing and readiness.";
                    return new PmCalendarConflictDto(other.Type, other.SourceId, other.Title, level, explanation);
                }).ToList();
            var conflictLevel = conflicts.Any(x => x.Level == "BLOCKING") ? "BLOCKING" : conflicts.Count > 0 ? "WARNING" : "NONE";
            return item with { ConflictLevel = conflictLevel, Conflicts = conflicts };
        }).OrderBy(x => x.StartsAt).ThenBy(x => x.Type).ToList();
        return enriched;
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
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:maintenance:{id}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var row = await db.MilestonePmMaintenanceCases.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct);
        if (row is null) return null;
        var next = request.Status.Trim().ToUpperInvariant();
        if (!MaintenanceStates.Contains(next)) throw new InvalidOperationException("Unsupported maintenance state.");
        if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Maintenance changed; reload before updating.");
        var previous = row.Status;
        if (next != previous && (!MaintenanceTransitions.TryGetValue(previous, out var allowed) || !allowed.Contains(next, StringComparer.OrdinalIgnoreCase))) throw new InvalidOperationException($"Maintenance cannot transition from {previous} to {next}.");
        if ((next == "CANCELLED" || (next == "IN_PROGRESS" && (previous is "COMPLETED" or "CLOSED"))) && string.IsNullOrWhiteSpace(request.Details)) throw new InvalidOperationException("A reason is required to cancel or reopen maintenance.");

        var amount = request.ExpenseAmount ?? request.ApprovedAmount ?? row.ExpenseAmount;
        if (amount < 0) throw new InvalidOperationException("Maintenance cost cannot be negative.");
        var ownerCharge = request.OwnerCharge ?? row.OwnerCharge;
        if (ownerCharge < 0 || ownerCharge > amount) throw new InvalidOperationException("Owner charge must be between zero and the total expense.");
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
        if ((next is "ASSIGNED" or "SCHEDULED" or "IN_PROGRESS" or "COMPLETED" or "CLOSED") && amount > 0)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var agreement = await db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.OwnerUserId == row.OwnerUserId && x.Status == "ACTIVE" && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today) && (x.PropertyId == row.PropertyId || x.PropertyId == null) && !x.IsDeleted).OrderByDescending(x => x.PropertyId.HasValue).ThenByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
            var threshold = agreement?.MaintenanceApprovalLimit ?? 0m;
            if (threshold <= 0m || amount > threshold)
            {
                if (request.OwnerApprovalId is not { } approvalId) throw new InvalidOperationException("Owner approval is required before assignment or spend.");
                var approved = await db.MilestoneP0Approvals.AnyAsync(x => x.Id == approvalId && x.ManagerUserId == managerUserId && x.OwnerUserId == row.OwnerUserId && x.PropertyId == row.PropertyId && x.Status == "APPROVED" && x.Amount >= amount && (!x.ExpiresAt.HasValue || x.ExpiresAt > timeProvider.GetUtcNow()) && (x.SourceType == null || (x.SourceType == "MAINTENANCE" && x.SourceId == row.Id)) && !x.IsDeleted, ct);
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
        if ((next is "COMPLETED" or "CLOSED") && row.ExpenseAmount > 0 && !row.FinanciallyPosted)
        {
            var journal = await p0Store.PostJournalAsync(new P0Actor(managerUserId, false, true), new P0PostJournalRequest("MAINTENANCE_EXPENSE", row.Id, $"maintenance-expense:{row.Id:N}", row.Currency, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), $"Maintenance {row.Number}", BuildMaintenanceJournalLines(row), ApprovalId: row.OwnerApprovalId), ct);
            row.FinanciallyPosted = true;
            row.FinancialJournalId = journal.Id;
            row.FinancialStatus = "POSTED";
        }
        row.RowVersion++;
        db.MilestonePmMaintenanceEvents.Add(new MilestonePmMaintenanceEvent { ManagerUserId = managerUserId, MaintenanceId = row.Id, ActorUserId = managerUserId, EventType = "STATUS_CHANGED", FromStatus = previous, ToStatus = next, Details = request.Details?.Trim() ?? string.Empty }); AddAudit(managerUserId, "MaintenanceStatusChanged", "PmMaintenance", row.Id);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
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

    public async Task<PmCostLineDto> AddMaintenanceCostLineAsync(Guid managerUserId, Guid maintenanceId, AddPmCostLineRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        var row = await db.MilestonePmMaintenanceCases.SingleOrDefaultAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Maintenance case not found.");
        if (row.FinanciallyPosted) throw new InvalidOperationException("Posted maintenance cost lines are immutable; use a financial correction.");
        var normalized = ValidateCostLine(request, row.Currency);
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"maintenance-cost:{maintenanceId:N}:{Guid.NewGuid():N}" : request.IdempotencyKey.Trim();
        var duplicate = await db.MilestonePmCostLines.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == key && !x.IsDeleted, ct);
        if (duplicate is not null)
        {
            if (duplicate.MaintenanceId != maintenanceId || duplicate.WorkOrderId.HasValue || duplicate.LineType != normalized.LineType || duplicate.Responsibility != normalized.Responsibility || duplicate.Description != normalized.Description || duplicate.Amount != request.Amount || duplicate.Currency != normalized.Currency || duplicate.ReceiptAttachmentId != request.ReceiptAttachmentId) throw new InvalidOperationException("The cost-line idempotency key was already used for a different request.");
            return await ToCostLineDtoAsync(duplicate, ct);
        }
        await EnsureReceiptAsync(managerUserId, maintenanceId, request.ReceiptAttachmentId, ct);
        var line = new MilestonePmCostLine { ManagerUserId = managerUserId, MaintenanceId = maintenanceId, LineType = normalized.LineType, Responsibility = normalized.Responsibility, Description = normalized.Description, Amount = request.Amount, Currency = normalized.Currency, ReceiptAttachmentId = request.ReceiptAttachmentId, IdempotencyKey = key };
        db.MilestonePmCostLines.Add(line);
        var lines = await db.MilestonePmCostLines.Where(x => x.MaintenanceId == maintenanceId && !x.IsDeleted).ToListAsync(ct); lines.Add(line); ApplyMaintenanceCostTotals(row, lines);
        db.MilestonePmMaintenanceEvents.Add(new MilestonePmMaintenanceEvent { ManagerUserId = managerUserId, MaintenanceId = maintenanceId, ActorUserId = managerUserId, EventType = "COST_LINE_ADDED", FromStatus = row.Status, ToStatus = row.Status, Details = $"{line.LineType}: {line.Description} ({line.Currency} {line.Amount:0.00})" });
        row.RowVersion++; AddAudit(managerUserId, "MaintenanceCostLineAdded", "PmMaintenance", maintenanceId); await db.SaveChangesAsync(ct); return await ToCostLineDtoAsync(line, ct);
    }

    public async Task<IReadOnlyList<PmCostLineDto>> ListMaintenanceCostLinesAsync(Guid managerUserId, Guid maintenanceId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (!await db.MilestonePmMaintenanceCases.AnyAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Maintenance case not found.");
        var rows = await db.MilestonePmCostLines.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.MaintenanceId == maintenanceId && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct); var result = new List<PmCostLineDto>(); foreach (var line in rows) result.Add(await ToCostLineDtoAsync(line, ct)); return result;
    }

    public async Task<PmMaintenanceFinancialCorrectionDto?> CorrectMaintenanceFinancialAsync(Guid managerUserId, Guid maintenanceId, CorrectPmMaintenanceFinancialRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("A correction reason and idempotency key are required."); if (request.ExpenseAmount <= 0 || request.OwnerCharge < 0 || request.OwnerCharge > request.ExpenseAmount || request.ManagerFee < 0) throw new InvalidOperationException("Corrected amounts are invalid.");
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null; if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:maintenance-finance:{maintenanceId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var row = await db.MilestonePmMaintenanceCases.SingleOrDefaultAsync(x => x.Id == maintenanceId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null;
        var key = request.IdempotencyKey.Trim(); var payload = JsonSerializer.Serialize(new { request.ExpenseAmount, request.OwnerCharge, request.ManagerFee, Reason = request.Reason.Trim(), request.OwnerApprovalId });
        var replay = await db.MilestonePmMaintenanceEvents.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == key && !x.IsDeleted, ct);
        if (replay is not null)
        {
            if (replay.MaintenanceId != maintenanceId || !JsonPayloadEquals(replay.PayloadJson, payload)) throw new InvalidOperationException("The correction idempotency key was already used for a different request.");
            if (!row.FinancialReversalJournalId.HasValue || !row.ReplacementFinancialJournalId.HasValue) throw new InvalidOperationException("The prior correction is incomplete and requires administrator recovery.");
            return new PmMaintenanceFinancialCorrectionDto(ToDto(row), row.FinancialReversalJournalId.Value, row.ReplacementFinancialJournalId.Value, true);
        }
        if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Maintenance changed; reload before correcting its financials."); if (!row.FinanciallyPosted || row.FinancialJournalId is null) throw new InvalidOperationException("Maintenance has no posted financial journal to correct."); if (row.Status is not ("COMPLETED" or "CLOSED" or "IN_PROGRESS")) throw new InvalidOperationException("Only completed, closed or reopened maintenance financials can be corrected.");
        var approvalId = request.OwnerApprovalId ?? row.OwnerApprovalId; await EnsureMaintenanceApprovalAsync(row, request.ExpenseAmount, approvalId, ct);
        var currentJournal = row.ReplacementFinancialJournalId ?? row.FinancialJournalId.Value;
        var reversal = await p0Store.ReverseJournalAsync(new P0Actor(managerUserId, false, true), currentJournal, new P0ReverseJournalRequest(request.Reason.Trim(), $"{key}:reverse"), ct) ?? throw new InvalidOperationException("Posted maintenance journal was not found.");
        row.ExpenseAmount = request.ExpenseAmount; row.OwnerCharge = request.OwnerCharge; row.ManagerFee = request.ManagerFee; row.OwnerApprovalId = approvalId; row.CostBreakdownJson = JsonSerializer.Serialize(new { expense = row.ExpenseAmount, ownerCharge = row.OwnerCharge, managerExpense = row.ExpenseAmount - row.OwnerCharge, managerFee = row.ManagerFee, correctionReason = request.Reason.Trim() });
        var replacement = await p0Store.PostJournalAsync(new P0Actor(managerUserId, false, true), new P0PostJournalRequest("MAINTENANCE_EXPENSE", row.Id, $"{key}:replacement", row.Currency, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), $"Corrected maintenance {row.Number}: {request.Reason.Trim()}", BuildMaintenanceJournalLines(row), ApprovalId: approvalId), ct);
        row.FinancialReversalJournalId = reversal.Id; row.ReplacementFinancialJournalId = replacement.Id; row.FinancialStatus = "ADJUSTED"; row.CorrectionCount++; row.RowVersion++;
        db.MilestonePmMaintenanceEvents.Add(new MilestonePmMaintenanceEvent { ManagerUserId = managerUserId, MaintenanceId = maintenanceId, ActorUserId = managerUserId, EventType = "FINANCIAL_CORRECTED", FromStatus = row.Status, ToStatus = row.Status, Details = request.Reason.Trim(), IdempotencyKey = key, PayloadJson = payload }); AddAudit(managerUserId, "MaintenanceFinancialCorrected", "PmMaintenance", maintenanceId); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return new PmMaintenanceFinancialCorrectionDto(ToDto(row), reversal.Id, replacement.Id, false);
    }

    public async Task<IReadOnlyList<PmProfessionalWorkOrderDto>> ListWorkOrdersAsync(Guid managerUserId, Guid? propertyId, string? status, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var query = db.MilestoneWorkOrders.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } property) { await EnsurePropertyAsync(managerUserId, property, null, ct); query = query.Where(x => x.PropertyId == property); } if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant()); return (await query.OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync(ct)).Select(ToProfessionalWorkOrderDto).ToList();
    }

    public async Task<PmWorkOrderQuoteDto> AddWorkOrderQuoteAsync(Guid managerUserId, Guid workOrderId, AddPmWorkOrderQuoteRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var workOrder = await db.MilestoneWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Work order not found."); if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Scope)) throw new InvalidOperationException("A positive quote amount and scope are required."); if (!await db.MilestoneManagerVendors.AnyAsync(x => x.Id == request.VendorId && x.ManagerUserId == managerUserId && x.IsActive && !x.IsSuspended && !x.IsDeleted, ct)) throw new InvalidOperationException("Vendor is not active in this portfolio."); var currency = request.Currency.Trim().ToUpperInvariant(); if (currency.Length != 3) throw new InvalidOperationException("Currency must be a three-letter code."); if (request.EvidenceAttachmentId.HasValue) await EnsureReceiptAsync(managerUserId, workOrder.SourceInspectionId ?? workOrder.Id, request.EvidenceAttachmentId, ct, allowAnyMaintenance: true);
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"work-order-quote:{workOrderId:N}:{Guid.NewGuid():N}" : request.IdempotencyKey.Trim(); var duplicate = await db.MilestonePmWorkOrderQuotes.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == key && !x.IsDeleted, ct); if (duplicate is not null) { if (duplicate.WorkOrderId != workOrderId || duplicate.VendorId != request.VendorId || duplicate.Amount != request.Amount || duplicate.Currency != currency || duplicate.Scope != request.Scope.Trim()) throw new InvalidOperationException("The quote idempotency key was already used for a different request."); return ToDto(duplicate); }
        var quote = new MilestonePmWorkOrderQuote { ManagerUserId = managerUserId, WorkOrderId = workOrderId, VendorId = request.VendorId, Amount = request.Amount, Currency = currency, Scope = request.Scope.Trim(), ExpiresAt = request.ExpiresAt?.ToUniversalTime(), EvidenceAttachmentId = request.EvidenceAttachmentId, IdempotencyKey = key }; db.MilestonePmWorkOrderQuotes.Add(quote); if (workOrder.Status == "REQUEST") workOrder.Status = "QUOTING"; workOrder.RowVersion++; db.MilestonePmWorkOrderEvents.Add(new MilestonePmWorkOrderEvent { ManagerUserId = managerUserId, WorkOrderId = workOrderId, ActorUserId = managerUserId, EventType = "QUOTE_RECEIVED", ToStatus = workOrder.Status, Details = $"{currency} {request.Amount:0.00}", IdempotencyKey = key }); AddAudit(managerUserId, "WorkOrderQuoteReceived", "WorkOrder", workOrderId); await db.SaveChangesAsync(ct); return ToDto(quote);
    }

    public async Task<IReadOnlyList<PmWorkOrderQuoteDto>> ListWorkOrderQuotesAsync(Guid managerUserId, Guid workOrderId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (!await db.MilestoneWorkOrders.AnyAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Work order not found."); return (await db.MilestonePmWorkOrderQuotes.AsNoTracking().Where(x => x.WorkOrderId == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted).OrderBy(x => x.Amount).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<PmCostLineDto> AddWorkOrderCostLineAsync(Guid managerUserId, Guid workOrderId, AddPmCostLineRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var workOrder = await db.MilestoneWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Work order not found."); if (workOrder.PostingStatus != "UNPOSTED") throw new InvalidOperationException("Posted work-order costs are immutable."); var normalized = ValidateCostLine(request, workOrder.Currency); var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"work-order-cost:{workOrderId:N}:{Guid.NewGuid():N}" : request.IdempotencyKey.Trim(); var duplicate = await db.MilestonePmCostLines.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == key && !x.IsDeleted, ct); if (duplicate is not null) { if (duplicate.WorkOrderId != workOrderId || duplicate.MaintenanceId.HasValue || duplicate.LineType != normalized.LineType || duplicate.Responsibility != normalized.Responsibility || duplicate.Description != normalized.Description || duplicate.Amount != request.Amount || duplicate.Currency != normalized.Currency || duplicate.ReceiptAttachmentId != request.ReceiptAttachmentId) throw new InvalidOperationException("The cost-line idempotency key was already used for a different request."); return await ToCostLineDtoAsync(duplicate, ct); } if (request.ReceiptAttachmentId.HasValue && !await db.MilestoneManagerMaintenanceAttachments.AnyAsync(x => x.Id == request.ReceiptAttachmentId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Receipt is outside the manager portfolio."); var line = new MilestonePmCostLine { ManagerUserId = managerUserId, WorkOrderId = workOrderId, LineType = normalized.LineType, Responsibility = normalized.Responsibility, Description = normalized.Description, Amount = request.Amount, Currency = normalized.Currency, ReceiptAttachmentId = request.ReceiptAttachmentId, IdempotencyKey = key }; db.MilestonePmCostLines.Add(line); var lines = await db.MilestonePmCostLines.Where(x => x.WorkOrderId == workOrderId && !x.IsDeleted).ToListAsync(ct); lines.Add(line); ApplyWorkOrderCostTotals(workOrder, lines); workOrder.RowVersion++; db.MilestonePmWorkOrderEvents.Add(new MilestonePmWorkOrderEvent { ManagerUserId = managerUserId, WorkOrderId = workOrderId, ActorUserId = managerUserId, EventType = "COST_LINE_ADDED", FromStatus = workOrder.Status, ToStatus = workOrder.Status, Details = $"{line.LineType}: {line.Description}", IdempotencyKey = key }); AddAudit(managerUserId, "WorkOrderCostLineAdded", "WorkOrder", workOrderId); await db.SaveChangesAsync(ct); return await ToCostLineDtoAsync(line, ct);
    }

    public async Task<IReadOnlyList<PmCostLineDto>> ListWorkOrderCostLinesAsync(Guid managerUserId, Guid workOrderId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (!await db.MilestoneWorkOrders.AnyAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Work order not found."); var rows = await db.MilestonePmCostLines.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.WorkOrderId == workOrderId && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct); var result = new List<PmCostLineDto>(); foreach (var line in rows) result.Add(await ToCostLineDtoAsync(line, ct)); return result;
    }

    public async Task<PmProfessionalWorkOrderDto?> UpdateWorkOrderAsync(Guid managerUserId, Guid workOrderId, UpdatePmProfessionalWorkOrderRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("A work-order reason and idempotency key are required."); await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null; if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:work-order:{workOrderId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); } var row = await db.MilestoneWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; var key = request.IdempotencyKey.Trim(); var replay = await db.MilestonePmWorkOrderEvents.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == key && !x.IsDeleted, ct); if (replay is not null) { if (replay.WorkOrderId != workOrderId || replay.ToStatus != request.Status.Trim().ToUpperInvariant() || replay.Details != request.Reason.Trim()) throw new InvalidOperationException("The work-order idempotency key was already used for a different request."); return ToProfessionalWorkOrderDto(row); } if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Work order changed; reload before updating."); var next = request.Status.Trim().ToUpperInvariant(); var allowed = row.Status switch { "REQUEST" => new[] { "QUOTING", "CANCELLED" }, "QUOTING" => new[] { "OWNER_APPROVAL", "APPROVED", "ASSIGNED", "CANCELLED" }, "OWNER_APPROVAL" => new[] { "APPROVED", "CANCELLED" }, "APPROVED" => new[] { "ASSIGNED", "CANCELLED" }, "ASSIGNED" => new[] { "SCHEDULED", "CANCELLED" }, "SCHEDULED" => new[] { "IN_PROGRESS", "CANCELLED" }, "IN_PROGRESS" => new[] { "COMPLETED" }, "COMPLETED" => new[] { "CLOSED", "IN_PROGRESS" }, "CLOSED" => new[] { "IN_PROGRESS" }, _ => Array.Empty<string>() }; if (next != row.Status && !allowed.Contains(next)) throw new InvalidOperationException($"Work order cannot transition from {row.Status} to {next}.");
        MilestonePmWorkOrderQuote? quote = null; if (request.SelectedQuoteId.HasValue) { quote = await db.MilestonePmWorkOrderQuotes.SingleOrDefaultAsync(x => x.Id == request.SelectedQuoteId && x.WorkOrderId == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Selected quote does not belong to this work order."); if (quote.ExpiresAt.HasValue && quote.ExpiresAt <= timeProvider.GetUtcNow()) throw new InvalidOperationException("Selected quote has expired."); row.SelectedQuoteId = quote.Id; row.VendorId = quote.VendorId; row.QuoteAmount = quote.Amount; row.Currency = quote.Currency; }
        var approvedAmount = request.ApprovedAmount ?? row.ApprovedAmount ?? row.QuoteAmount ?? row.FinalAmount; if (approvedAmount < 0) throw new InvalidOperationException("Approved work-order amount cannot be negative."); if ((next is "APPROVED" or "ASSIGNED" or "SCHEDULED" or "IN_PROGRESS" or "COMPLETED" or "CLOSED") && approvedAmount > 0) await EnsureWorkOrderApprovalAsync(row, approvedAmount, request.OwnerApprovalId ?? row.OwnerApprovalId, ct); row.OwnerApprovalId = request.OwnerApprovalId ?? row.OwnerApprovalId; row.ApprovedAmount = approvedAmount; row.VendorId = request.VendorId ?? row.VendorId; row.ScheduledAt = request.ScheduledAt ?? row.ScheduledAt;
        if ((next is "COMPLETED" or "CLOSED") && row.FinalAmount > 0 && row.PostingStatus == "UNPOSTED") { var journal = await p0Store.PostJournalAsync(new P0Actor(managerUserId, false, true), new P0PostJournalRequest("MAINTENANCE_EXPENSE", row.Id, $"work-order-expense:{row.Id:N}", row.Currency, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), $"Work order {row.WorkOrderNumber}", BuildWorkOrderJournalLines(row), ApprovalId: row.OwnerApprovalId), ct); row.FinancialJournalId = journal.Id; row.PostingStatus = "POSTED"; }
        var previous = row.Status; row.Status = next; row.RowVersion++; db.MilestonePmWorkOrderEvents.Add(new MilestonePmWorkOrderEvent { ManagerUserId = managerUserId, WorkOrderId = workOrderId, ActorUserId = managerUserId, EventType = "STATUS_CHANGED", FromStatus = previous, ToStatus = next, Details = request.Reason.Trim(), IdempotencyKey = key }); AddAudit(managerUserId, "WorkOrderUpdated", "WorkOrder", workOrderId); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return ToProfessionalWorkOrderDto(row);
    }

    public async Task<PmWorkOrderFinancialCorrectionDto?> CorrectWorkOrderFinancialAsync(Guid managerUserId, Guid workOrderId, CorrectPmWorkOrderFinancialRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("A correction reason and idempotency key are required."); var amounts = new[] { request.LaborAmount, request.MaterialAmount, request.TaxAmount, request.OtherAmount, request.OwnerResponsibility, request.ManagerResponsibility, request.VendorResponsibility }; if (amounts.Any(x => x < 0 || x != decimal.Round(x, 2))) throw new InvalidOperationException("Corrected work-order amounts must be non-negative with at most two decimals."); var totalCost = request.LaborAmount + request.MaterialAmount + request.TaxAmount + request.OtherAmount; if (request.OwnerResponsibility + request.ManagerResponsibility + request.VendorResponsibility != totalCost) throw new InvalidOperationException("Responsibility totals must equal the corrected cost total."); var postedTotal = request.OwnerResponsibility + request.ManagerResponsibility; if (postedTotal <= 0) throw new InvalidOperationException("A corrected posting requires a positive owner or PM responsibility.");
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null; if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:work-order-finance:{workOrderId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); } var row = await db.MilestoneWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; var key = request.IdempotencyKey.Trim(); var payload = JsonSerializer.Serialize(new { request.LaborAmount, request.MaterialAmount, request.TaxAmount, request.OtherAmount, request.OwnerResponsibility, request.ManagerResponsibility, request.VendorResponsibility, Reason = request.Reason.Trim(), request.OwnerApprovalId }); var replay = await db.MilestonePmWorkOrderEvents.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == managerUserId && x.IdempotencyKey == key && !x.IsDeleted, ct); if (replay is not null) { if (replay.WorkOrderId != workOrderId || !JsonPayloadEquals(replay.PayloadJson, payload)) throw new InvalidOperationException("The correction idempotency key was already used for a different request."); if (!row.FinancialReversalJournalId.HasValue || !row.ReplacementFinancialJournalId.HasValue) throw new InvalidOperationException("The prior work-order correction requires administrator recovery."); return new PmWorkOrderFinancialCorrectionDto(ToProfessionalWorkOrderDto(row), row.FinancialReversalJournalId.Value, row.ReplacementFinancialJournalId.Value, true); } if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Work order changed; reload before correcting its financials."); if (row.PostingStatus is not ("POSTED" or "ADJUSTED") || !row.FinancialJournalId.HasValue) throw new InvalidOperationException("Work order has no posted financial journal to correct."); var approvalId = request.OwnerApprovalId ?? row.OwnerApprovalId; await EnsureWorkOrderApprovalAsync(row, postedTotal, approvalId, ct); var currentJournal = row.ReplacementFinancialJournalId ?? row.FinancialJournalId.Value; var reversal = await p0Store.ReverseJournalAsync(new P0Actor(managerUserId, false, true), currentJournal, new P0ReverseJournalRequest(request.Reason.Trim(), $"{key}:reverse"), ct) ?? throw new InvalidOperationException("Posted work-order journal was not found."); row.LaborAmount = request.LaborAmount; row.PartsAmount = request.MaterialAmount; row.TaxAmount = request.TaxAmount; row.OtherAmount = request.OtherAmount; row.OwnerResponsibility = request.OwnerResponsibility; row.ManagerResponsibility = request.ManagerResponsibility; row.VendorResponsibility = request.VendorResponsibility; row.FinalAmount = postedTotal; row.OwnerApprovalId = approvalId; var replacement = await p0Store.PostJournalAsync(new P0Actor(managerUserId, false, true), new P0PostJournalRequest("MAINTENANCE_EXPENSE", row.Id, $"{key}:replacement", row.Currency, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), $"Corrected work order {row.WorkOrderNumber}: {request.Reason.Trim()}", BuildWorkOrderJournalLines(row), ApprovalId: approvalId), ct); row.FinancialReversalJournalId = reversal.Id; row.ReplacementFinancialJournalId = replacement.Id; row.PostingStatus = "ADJUSTED"; row.CorrectionCount++; row.RowVersion++; db.MilestonePmWorkOrderEvents.Add(new MilestonePmWorkOrderEvent { ManagerUserId = managerUserId, WorkOrderId = workOrderId, ActorUserId = managerUserId, EventType = "FINANCIAL_CORRECTED", FromStatus = row.Status, ToStatus = row.Status, Details = request.Reason.Trim(), IdempotencyKey = key, PayloadJson = payload }); AddAudit(managerUserId, "WorkOrderFinancialCorrected", "WorkOrder", row.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return new PmWorkOrderFinancialCorrectionDto(ToProfessionalWorkOrderDto(row), reversal.Id, replacement.Id, false);
    }

    public async Task<PmCleaningDto> CreateCleaningAsync(Guid managerUserId, CreatePmCleaningRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct);
        MilestonePmChecklistTemplate? template = null;
        if (request.TemplateId.HasValue)
            template = await db.MilestonePmChecklistTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.TemplateId && x.ManagerUserId == managerUserId && x.WorkflowType == "CLEANING" && x.Status == "ACTIVE" && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Cleaning template is not active in this portfolio.");
        else
        {
            var assignedTemplateId = await db.MilestonePmPropertyChecklistAssignments.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.WorkflowType == "CLEANING" && x.EndedAt == null && !x.IsDeleted).OrderByDescending(x => x.EffectiveAt).Select(x => x.TemplateId).FirstOrDefaultAsync(ct);
            if (assignedTemplateId != Guid.Empty) template = await db.MilestonePmChecklistTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assignedTemplateId && !x.IsDeleted, ct);
        }
        var checklist = template?.ItemsJson ?? request.ChecklistJson;
        ValidateJson(checklist, JsonValueKind.Array);
        if (request.BookingId is { } bookingId && !await BookingBelongsToManagedPropertyAsync(managerUserId, request.PropertyId, bookingId, ct)) throw new InvalidOperationException("Booking is not linked to this managed property.");
        var row = new MilestonePmCleaningReadiness { ManagerUserId = managerUserId, PropertyId = request.PropertyId, BookingId = request.BookingId, DueAt = request.DueAt.ToUniversalTime(), AssignedUserId = request.AssignedUserId, VendorId = request.VendorId, ChecklistJson = checklist, TemplateName = template?.Name ?? "DEFAULT", TemplateVersion = template?.Version ?? 1 };
        db.MilestonePmCleaningReadiness.Add(row); AddAudit(managerUserId, "CleaningCreated", "PmCleaning", row.Id); await db.SaveChangesAsync(ct); return ToDto(row);
    }
    public async Task<PmCleaningDto?> UpdateCleaningAsync(Guid managerUserId, Guid id, UpdatePmCleaningRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmCleaningReadiness.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Readiness changed; reload before updating."); ValidateJson(request.ChecklistJson, JsonValueKind.Array); ValidateJson(request.PhotosJson, JsonValueKind.Array); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("NOT_READY" or "IN_PROGRESS" or "READY" or "BLOCKED" or "CANCELLED")) throw new InvalidOperationException("Cleaning status is invalid."); if (status == "READY" && HasIncompleteRequiredChecklist(request.ChecklistJson)) throw new InvalidOperationException("Required checklist items must be complete before readiness can be marked."); if (status == "READY" && await db.MilestonePmCorrectiveActions.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == row.PropertyId && x.BlocksReadiness && x.Status != "RESOLVED" && x.Status != "CANCELLED" && !x.IsDeleted, ct)) throw new InvalidOperationException("Readiness is blocked by unresolved mandatory corrective actions."); row.Status = status; row.ChecklistJson = request.ChecklistJson; row.PhotosJson = request.PhotosJson; row.Issues = request.Issues.Trim(); row.CompletedAt = row.Status == "READY" ? timeProvider.GetUtcNow() : row.CompletedAt; row.RowVersion++; AddAudit(managerUserId, "CleaningUpdated", "PmCleaning", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmCleaningDto>> ListCleaningAsync(Guid managerUserId, Guid? propertyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmCleaningReadiness.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); if (from is { } f) q = q.Where(x => x.DueAt >= f); if (to is { } t) q = q.Where(x => x.DueAt <= t); return (await q.OrderBy(x => x.DueAt).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<PmAssetDto> CreateAssetAsync(Guid managerUserId, CreatePmAssetRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); if (request.Quantity < 0 || string.IsNullOrWhiteSpace(request.AssetTag) || string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("Asset tag, name and non-negative quantity are required."); if (request.PurchaseCost is < 0) throw new InvalidOperationException("Purchase cost cannot be negative."); if (await db.MilestonePmAssets.AnyAsync(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.AssetTag == request.AssetTag.Trim() && !x.IsDeleted, ct)) throw new InvalidOperationException("Asset tag already exists for this property."); ValidateJson(request.MetadataJson, JsonValueKind.Object); ValidateJson(request.PhotosJson, JsonValueKind.Array); var row = new MilestonePmAsset { ManagerUserId = managerUserId, PropertyId = request.PropertyId, AssetTag = request.AssetTag.Trim(), Name = request.Name.Trim(), Description = request.Description.Trim(), SerialReference = request.SerialReference.Trim(), PurchaseDate = request.PurchaseDate, PurchaseCost = request.PurchaseCost, WarrantyExpiry = request.WarrantyExpiry, Condition = NormalizeAssetCondition(request.Condition), Category = request.Category.Trim().ToUpperInvariant(), Quantity = request.Quantity, Location = request.Location.Trim(), MetadataJson = request.MetadataJson, PhotosJson = request.PhotosJson }; db.MilestonePmAssets.Add(row); AddAudit(managerUserId, "AssetCreated", "PmAsset", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<PmAssetDto?> UpdateAssetAsync(Guid managerUserId, Guid id, UpdatePmAssetRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmAssets.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Asset changed; reload before updating."); if (request.Quantity < 0) throw new InvalidOperationException("Asset quantity cannot be negative."); if (request.PurchaseCost is < 0) throw new InvalidOperationException("Purchase cost cannot be negative."); ValidateJson(request.MetadataJson, JsonValueKind.Object); ValidateJson(request.PhotosJson, JsonValueKind.Array); row.Status = NormalizeAssetStatus(request.Status); row.Quantity = request.Quantity; row.Location = request.Location.Trim(); row.MetadataJson = request.MetadataJson; row.PhotosJson = request.PhotosJson; if (request.Description is not null) row.Description = request.Description.Trim(); if (request.SerialReference is not null) row.SerialReference = request.SerialReference.Trim(); if (request.PurchaseDate is not null) row.PurchaseDate = request.PurchaseDate; if (request.PurchaseCost is not null) row.PurchaseCost = request.PurchaseCost; if (request.WarrantyExpiry is not null) row.WarrantyExpiry = request.WarrantyExpiry; if (request.Condition is not null) row.Condition = NormalizeAssetCondition(request.Condition); row.RetiredAt = row.Status == "RETIRED" ? timeProvider.GetUtcNow() : row.RetiredAt; row.RowVersion++; AddAudit(managerUserId, "AssetUpdated", "PmAsset", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmAssetDto>> ListAssetsAsync(Guid managerUserId, Guid? propertyId, string? status, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmAssets.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.ToUpper()); return (await q.OrderBy(x => x.AssetTag).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<PmIncidentDto> CreateIncidentAsync(Guid managerUserId, CreatePmIncidentRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); if (string.IsNullOrWhiteSpace(request.Description)) throw new InvalidOperationException("Incident description is required."); var severity = request.Severity.Trim().ToUpperInvariant(); if (severity is not ("LOW" or "MEDIUM" or "HIGH" or "CRITICAL")) throw new InvalidOperationException("Incident severity is invalid."); if (request.FinancialImpact < 0) throw new InvalidOperationException("Financial impact cannot be negative."); ValidateJson(request.InvolvedPartiesJson, JsonValueKind.Array); ValidateJson(request.EvidenceJson, JsonValueKind.Array); var row = new MilestonePmIncident { ManagerUserId = managerUserId, PropertyId = request.PropertyId, BookingId = request.BookingId, IncidentType = request.IncidentType.Trim().ToUpperInvariant(), Severity = severity, OccurredAt = request.OccurredAt.ToUniversalTime(), Description = request.Description.Trim(), InvolvedPartiesJson = request.InvolvedPartiesJson, EvidenceJson = request.EvidenceJson, ActionTaken = request.ActionTaken.Trim(), FollowUp = request.FollowUp.Trim(), FinancialImpact = request.FinancialImpact, InsuranceReference = request.InsuranceReference?.Trim() }; db.MilestonePmIncidents.Add(row); AddAudit(managerUserId, "IncidentCreated", "PmIncident", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<PmIncidentDto?> UpdateIncidentAsync(Guid managerUserId, Guid id, UpdatePmIncidentRequest request, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var row = await db.MilestonePmIncidents.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Incident changed; reload before updating."); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("OPEN" or "RESOLVED" or "CLOSED")) throw new InvalidOperationException("Incident status is invalid."); row.Status = status; row.ActionTaken = request.ActionTaken.Trim(); row.FollowUp = request.FollowUp.Trim(); row.InsuranceReference = request.InsuranceReference?.Trim(); row.ResolvedAt = status is "RESOLVED" or "CLOSED" ? timeProvider.GetUtcNow() : row.ResolvedAt; row.RowVersion++; AddAudit(managerUserId, "IncidentUpdated", "PmIncident", row.Id); await db.SaveChangesAsync(ct); return ToDto(row); }
    public async Task<IReadOnlyList<PmIncidentDto>> ListIncidentsAsync(Guid managerUserId, Guid? propertyId, string? status, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmIncidents.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.ToUpper()); return (await q.OrderByDescending(x => x.OccurredAt).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<PmInspectionDto> CreateInspectionAsync(Guid managerUserId, CreatePmInspectionRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, request.PropertyId, null, ct); MilestonePmChecklistTemplate? template = null; if (request.TemplateId.HasValue) template = await db.MilestonePmChecklistTemplates.SingleOrDefaultAsync(x => x.Id == request.TemplateId && x.ManagerUserId == managerUserId && x.WorkflowType == "INSPECTION" && x.Status == "ACTIVE" && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Inspection template is not active in this portfolio."); else { var assignedTemplateId = await db.MilestonePmPropertyChecklistAssignments.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.WorkflowType == "INSPECTION" && x.EndedAt == null && !x.IsDeleted).OrderByDescending(x => x.EffectiveAt).Select(x => x.TemplateId).FirstOrDefaultAsync(ct); if (assignedTemplateId != Guid.Empty) template = await db.MilestonePmChecklistTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assignedTemplateId && !x.IsDeleted, ct); } var checklist = template?.ItemsJson ?? request.ChecklistJson; ValidateJson(checklist, JsonValueKind.Array); MilestonePmCorrectiveAction? action = null; if (request.ReinspectionOfActionId.HasValue) { action = await db.MilestonePmCorrectiveActions.SingleOrDefaultAsync(x => x.Id == request.ReinspectionOfActionId && x.ManagerUserId == managerUserId && x.PropertyId == request.PropertyId && x.Status == "RETEST_REQUIRED" && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Corrective action is not ready for reinspection."); }
        var row = new MilestonePmInspectionRecord { ManagerUserId = managerUserId, PropertyId = request.PropertyId, AssignedUserId = request.AssignedUserId, InspectionType = request.InspectionType.Trim().ToUpperInvariant(), ScheduledAt = request.ScheduledAt.ToUniversalTime(), ChecklistJson = checklist, TemplateId = template?.Id, TemplateName = template?.Name ?? "CUSTOM", TemplateVersion = template?.Version ?? 1, ReinspectionOfActionId = action?.Id }; db.MilestonePmInspectionRecords.Add(row); if (action is not null) { action.RetestInspectionId = row.Id; action.Status = "REINSPECTION_SCHEDULED"; action.RowVersion++; } AddAudit(managerUserId, action is null ? "InspectionCreated" : "ReinspectionCreated", "PmInspection", row.Id); await db.SaveChangesAsync(ct); return ToDto(row);
    }

    public async Task<PmInspectionDto?> UpdateInspectionAsync(Guid managerUserId, Guid id, UpdatePmInspectionRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null; if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:inspection-update:{id}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); } var row = await db.MilestonePmInspectionRecords.SingleOrDefaultAsync(x => x.Id == id && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Inspection changed; reload before updating."); ValidateJson(request.EvidenceJson, JsonValueKind.Array); ValidateJson(request.FindingsJson, JsonValueKind.Array); var checklist = request.ChecklistJson ?? row.ChecklistJson; ValidateJson(checklist, JsonValueKind.Array); var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("SCHEDULED" or "IN_PROGRESS" or "FAILED" or "SIGNED_OFF" or "CANCELLED")) throw new InvalidOperationException("Inspection status is invalid."); if (status == "SIGNED_OFF" && HasIncompleteRequiredChecklist(checklist)) throw new InvalidOperationException("Required inspection checklist items must be complete before sign-off."); var findings = ParseCorrectiveFindings(request.FindingsJson); if ((status is "FAILED" or "SIGNED_OFF") && findings.Count > 0) { foreach (var finding in findings) { if (!await db.MilestonePmCorrectiveActions.AnyAsync(x => x.InspectionId == row.Id && x.ChecklistItemId == finding.ItemId && !x.IsDeleted, ct)) db.MilestonePmCorrectiveActions.Add(new MilestonePmCorrectiveAction { ManagerUserId = managerUserId, InspectionId = row.Id, PropertyId = row.PropertyId, ChecklistItemId = finding.ItemId, Description = finding.Description, Severity = finding.Severity, BlocksReadiness = finding.BlocksReadiness }); } status = "FAILED"; }
        if (status == "SIGNED_OFF" && await db.MilestonePmCorrectiveActions.AnyAsync(x => x.InspectionId == row.Id && x.Status != "RESOLVED" && x.Status != "CANCELLED" && !x.IsDeleted, ct)) throw new InvalidOperationException("Inspection cannot be signed off while corrective actions remain unresolved."); row.Status = status; row.ChecklistJson = checklist; row.EvidenceJson = request.EvidenceJson; row.FindingsJson = request.FindingsJson; row.SignedOffAt = status == "SIGNED_OFF" ? timeProvider.GetUtcNow() : null; row.RowVersion++;
        if (status == "SIGNED_OFF" && row.ReinspectionOfActionId.HasValue) { var action = await db.MilestonePmCorrectiveActions.SingleOrDefaultAsync(x => x.Id == row.ReinspectionOfActionId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (action is not null) { action.Status = "RESOLVED"; action.ResolutionNotes = "Passed linked reinspection"; action.ResolvedAt = timeProvider.GetUtcNow(); action.RowVersion++; } }
        AddAudit(managerUserId, status == "FAILED" ? "InspectionFailed" : "InspectionUpdated", "PmInspection", row.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return ToDto(row);
    }
    public async Task<IReadOnlyList<PmInspectionDto>> ListInspectionsAsync(Guid managerUserId, Guid? propertyId, CancellationToken ct) { await EnsureManagerAsync(managerUserId, ct); var q = db.MilestonePmInspectionRecords.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (propertyId is { } p) q = q.Where(x => x.PropertyId == p); return (await q.OrderBy(x => x.ScheduledAt).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<WorkOrderDto?> CreateInspectionWorkOrderAsync(Guid managerUserId, Guid inspectionId, CreatePmInspectionWorkOrderRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct);
        if (string.IsNullOrWhiteSpace(request.Scope)) throw new InvalidOperationException("Corrective work scope is required.");
        if (request.QuoteAmount is < 0) throw new InvalidOperationException("Quote amount cannot be negative.");
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:inspection:{inspectionId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); }
        var inspection = await db.MilestonePmInspectionRecords.SingleOrDefaultAsync(x => x.Id == inspectionId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct);
        if (inspection is null) return null;
        if (inspection.Status is not ("FAILED" or "SIGNED_OFF")) throw new InvalidOperationException("Inspection findings must be recorded before creating corrective work.");
        if (inspection.CorrectiveWorkOrderId is { } existingId)
        {
            var existing = await db.MilestoneWorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == existingId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct);
            if (existing is not null) return ToWorkOrderDto(existing);
        }
        var ownerId = await db.MilestoneManagerProperties.Where(x => x.ManagerUserId == managerUserId && x.Id == inspection.PropertyId && !x.IsDeleted).Select(x => x.OwnerUserId).SingleOrDefaultAsync(ct);
        if (ownerId == Guid.Empty) throw new InvalidOperationException("Inspection property is outside the manager portfolio.");
        if (request.VendorId is { } vendor && !await db.MilestoneManagerVendors.AnyAsync(x => x.Id == vendor && x.ManagerUserId == managerUserId && !x.IsDeleted, ct)) throw new InvalidOperationException("Vendor is outside the manager portfolio.");
        var workOrder = new MilestoneWorkOrder { ManagerUserId = managerUserId, PropertyId = inspection.PropertyId, OwnerUserId = ownerId, VendorId = request.VendorId, WorkOrderNumber = $"WO-{timeProvider.GetUtcNow():yyyyMMdd}-{Guid.NewGuid():N}"[..20], Scope = request.Scope.Trim(), Status = "REQUEST", QuoteAmount = request.QuoteAmount, SlaDueAt = request.SlaDueAt, SourceInspectionId = inspection.Id };
        db.MilestoneWorkOrders.Add(workOrder); inspection.CorrectiveWorkOrderId = workOrder.Id; inspection.RowVersion++; var actions = await db.MilestonePmCorrectiveActions.Where(x => x.InspectionId == inspection.Id && x.Status == "OPEN" && !x.IsDeleted).ToListAsync(ct); foreach (var action in actions) { action.WorkOrderId = workOrder.Id; action.Status = "IN_PROGRESS"; action.RowVersion++; } db.MilestonePmWorkOrderEvents.Add(new MilestonePmWorkOrderEvent { ManagerUserId = managerUserId, WorkOrderId = workOrder.Id, ActorUserId = managerUserId, EventType = "CREATED_FROM_INSPECTION", ToStatus = workOrder.Status, Details = request.Scope.Trim() }); AddAudit(managerUserId, "InspectionCorrectiveWorkOrderCreated", "PmInspection", inspection.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return ToWorkOrderDto(workOrder);
    }

    public async Task<PmChecklistTemplateDto> CreateChecklistTemplateAsync(Guid managerUserId, CreatePmChecklistTemplateRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("Template name is required."); var workflow = request.WorkflowType.Trim().ToUpperInvariant(); if (workflow is not ("INSPECTION" or "CLEANING")) throw new InvalidOperationException("Template workflow must be INSPECTION or CLEANING."); ValidateJson(request.ItemsJson, JsonValueKind.Array); if (HasNoChecklistItems(request.ItemsJson)) throw new InvalidOperationException("Template must contain at least one checklist item."); MilestonePmChecklistTemplate? previous = null; if (request.SupersedesTemplateId.HasValue) previous = await db.MilestonePmChecklistTemplates.SingleOrDefaultAsync(x => x.Id == request.SupersedesTemplateId && x.ManagerUserId == managerUserId && x.WorkflowType == workflow && x.Status == "ACTIVE" && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Superseded template is not active in this portfolio."); var template = new MilestonePmChecklistTemplate { ManagerUserId = managerUserId, Name = request.Name.Trim(), WorkflowType = workflow, Version = previous?.Version + 1 ?? 1, ItemsJson = request.ItemsJson, SupersedesTemplateId = previous?.Id }; if (previous is not null) previous.Status = "SUPERSEDED"; db.MilestonePmChecklistTemplates.Add(template); AddAudit(managerUserId, "ChecklistTemplateCreated", "PmChecklistTemplate", template.Id); await db.SaveChangesAsync(ct); return ToDto(template);
    }

    public async Task<IReadOnlyList<PmChecklistTemplateDto>> ListChecklistTemplatesAsync(Guid managerUserId, string? workflowType, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var query = db.MilestonePmChecklistTemplates.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (!string.IsNullOrWhiteSpace(workflowType)) query = query.Where(x => x.WorkflowType == workflowType.Trim().ToUpperInvariant()); return (await query.OrderBy(x => x.Name).ThenByDescending(x => x.Version).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<PmPropertyChecklistAssignmentDto> AssignChecklistTemplateAsync(Guid managerUserId, Guid propertyId, AssignPmChecklistTemplateRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); await EnsurePropertyAsync(managerUserId, propertyId, null, ct); var template = await db.MilestonePmChecklistTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.TemplateId && x.ManagerUserId == managerUserId && x.Status == "ACTIVE" && !x.IsDeleted, ct) ?? throw new InvalidOperationException("Template is not active in this portfolio."); var now = timeProvider.GetUtcNow(); var existing = await db.MilestonePmPropertyChecklistAssignments.Where(x => x.ManagerUserId == managerUserId && x.PropertyId == propertyId && x.WorkflowType == template.WorkflowType && x.EndedAt == null && !x.IsDeleted).ToListAsync(ct); foreach (var row in existing) row.EndedAt = now; var assignment = new MilestonePmPropertyChecklistAssignment { ManagerUserId = managerUserId, PropertyId = propertyId, TemplateId = template.Id, WorkflowType = template.WorkflowType, EffectiveAt = now }; db.MilestonePmPropertyChecklistAssignments.Add(assignment); AddAudit(managerUserId, "ChecklistTemplateAssigned", "PmProperty", propertyId); await db.SaveChangesAsync(ct); return ToDto(assignment);
    }

    public async Task<IReadOnlyList<PmCorrectiveActionDto>> ListCorrectiveActionsAsync(Guid managerUserId, Guid? inspectionId, Guid? propertyId, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); var query = db.MilestonePmCorrectiveActions.AsNoTracking().Where(x => x.ManagerUserId == managerUserId && !x.IsDeleted); if (inspectionId.HasValue) query = query.Where(x => x.InspectionId == inspectionId); if (propertyId.HasValue) { await EnsurePropertyAsync(managerUserId, propertyId.Value, null, ct); query = query.Where(x => x.PropertyId == propertyId); } return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<PmCorrectiveActionDto?> UpdateCorrectiveActionAsync(Guid managerUserId, Guid actionId, UpdatePmCorrectiveActionRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("Corrective-action idempotency key is required."); var row = await db.MilestonePmCorrectiveActions.SingleOrDefaultAsync(x => x.Id == actionId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (row is null) return null; var status = request.Status.Trim().ToUpperInvariant(); if (status is not ("OPEN" or "IN_PROGRESS" or "RETEST_REQUIRED" or "CANCELLED")) throw new InvalidOperationException("Corrective action can only be advanced to work, retest or cancellation; resolution requires a passed reinspection."); if (row.LastIdempotencyKey == request.IdempotencyKey.Trim()) return ToDto(row); if (row.RowVersion != request.RowVersion) throw new DbUpdateConcurrencyException("Corrective action changed; reload before updating."); if (status == "RETEST_REQUIRED" && row.WorkOrderId.HasValue) { var work = await db.MilestoneWorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == row.WorkOrderId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (work?.Status is not ("COMPLETED" or "CLOSED")) throw new InvalidOperationException("Corrective work must be completed before requesting reinspection."); } if (status == "CANCELLED" && string.IsNullOrWhiteSpace(request.ResolutionNotes)) throw new InvalidOperationException("A cancellation reason is required."); row.Status = status; row.ResolutionNotes = request.ResolutionNotes.Trim(); row.LastIdempotencyKey = request.IdempotencyKey.Trim(); row.RowVersion++; AddAudit(managerUserId, "CorrectiveActionUpdated", "PmCorrectiveAction", row.Id); await db.SaveChangesAsync(ct); return ToDto(row);
    }

    public async Task<PmInspectionDto?> CreateReinspectionAsync(Guid managerUserId, Guid actionId, CreatePmReinspectionRequest request, CancellationToken ct)
    {
        await EnsureManagerAsync(managerUserId, ct); if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("Reinspection idempotency key is required."); await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null; if (db.Database.IsNpgsql()) { var lockKey = $"nesty-pm:corrective:{actionId}"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct); } var action = await db.MilestonePmCorrectiveActions.SingleOrDefaultAsync(x => x.Id == actionId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); if (action is null) return null; if (action.RetestInspectionId.HasValue) { var existing = await db.MilestonePmInspectionRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == action.RetestInspectionId && !x.IsDeleted, ct); if (existing is not null) return ToDto(existing); } if (action.Status != "RETEST_REQUIRED") throw new InvalidOperationException("Corrective action must be ready for retest before scheduling reinspection."); var original = await db.MilestonePmInspectionRecords.AsNoTracking().SingleAsync(x => x.Id == action.InspectionId && x.ManagerUserId == managerUserId && !x.IsDeleted, ct); var inspection = new MilestonePmInspectionRecord { ManagerUserId = managerUserId, PropertyId = action.PropertyId, AssignedUserId = request.AssignedUserId, InspectionType = "REINSPECTION", ScheduledAt = request.ScheduledAt.ToUniversalTime(), ChecklistJson = original.ChecklistJson, TemplateId = original.TemplateId, TemplateName = original.TemplateName, TemplateVersion = original.TemplateVersion, ReinspectionOfActionId = action.Id }; db.MilestonePmInspectionRecords.Add(inspection); action.RetestInspectionId = inspection.Id; action.Status = "REINSPECTION_SCHEDULED"; action.LastIdempotencyKey = request.IdempotencyKey.Trim(); action.RowVersion++; AddAudit(managerUserId, "ReinspectionCreated", "PmInspection", inspection.Id); await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return ToDto(inspection);
    }

    private static (string LineType, string Responsibility, string Description, string Currency) ValidateCostLine(AddPmCostLineRequest request, string expectedCurrency)
    {
        var lineType = request.LineType.Trim().ToUpperInvariant(); var responsibility = request.Responsibility.Trim().ToUpperInvariant(); var currency = request.Currency.Trim().ToUpperInvariant();
        if (lineType is not ("LABOR" or "MATERIAL" or "TAX" or "FEE" or "OTHER")) throw new InvalidOperationException("Cost line type must be LABOR, MATERIAL, TAX, FEE or OTHER.");
        if (responsibility is not ("OWNER" or "PM" or "VENDOR")) throw new InvalidOperationException("Cost responsibility must be OWNER, PM or VENDOR.");
        if (request.Amount <= 0 || request.Amount != decimal.Round(request.Amount, 2)) throw new InvalidOperationException("Cost amount must be positive with at most two decimal places.");
        if (currency != expectedCurrency.Trim().ToUpperInvariant()) throw new InvalidOperationException("Cost-line currency does not match the record currency.");
        if (string.IsNullOrWhiteSpace(request.Description)) throw new InvalidOperationException("Cost-line description is required.");
        return (lineType, responsibility, request.Description.Trim(), currency);
    }

    private async Task EnsureReceiptAsync(Guid managerUserId, Guid maintenanceId, Guid? attachmentId, CancellationToken ct, bool allowAnyMaintenance = false)
    {
        if (!attachmentId.HasValue) return; var valid = await db.MilestoneManagerMaintenanceAttachments.AnyAsync(x => x.Id == attachmentId && x.ManagerUserId == managerUserId && (allowAnyMaintenance || x.MaintenanceId == maintenanceId) && (x.Status == "UPLOADED" || x.Status == "VERIFIED" || x.Status == "ACTIVE") && !x.IsDeleted, ct); if (!valid) throw new InvalidOperationException("Receipt attachment is missing or outside this record.");
    }

    private async Task<PmCostLineDto> ToCostLineDtoAsync(MilestonePmCostLine line, CancellationToken ct)
    {
        var fileName = line.ReceiptAttachmentId.HasValue ? await db.MilestoneManagerMaintenanceAttachments.AsNoTracking().Where(x => x.Id == line.ReceiptAttachmentId && !x.IsDeleted).Select(x => x.FileName).SingleOrDefaultAsync(ct) : null; return new PmCostLineDto(line.Id, line.MaintenanceId, line.WorkOrderId, line.LineType, line.Responsibility, line.Description, line.Amount, line.Currency, line.ReceiptAttachmentId, fileName, line.IdempotencyKey, line.CreatedAt);
    }

    private static void ApplyMaintenanceCostTotals(MilestonePmMaintenanceCase row, IReadOnlyList<MilestonePmCostLine> lines)
    {
        row.ExpenseAmount = lines.Where(x => x.Responsibility is "OWNER" or "PM").Sum(x => x.Amount); row.OwnerCharge = lines.Where(x => x.Responsibility == "OWNER").Sum(x => x.Amount); row.ManagerFee = lines.Where(x => x.Responsibility == "OWNER" && x.LineType == "FEE").Sum(x => x.Amount); row.CostBreakdownJson = JsonSerializer.Serialize(lines.Select(x => new { x.LineType, x.Responsibility, x.Description, x.Amount, x.ReceiptAttachmentId }));
    }

    private static void ApplyWorkOrderCostTotals(MilestoneWorkOrder row, IReadOnlyList<MilestonePmCostLine> lines)
    {
        row.LaborAmount = lines.Where(x => x.LineType == "LABOR").Sum(x => x.Amount); row.PartsAmount = lines.Where(x => x.LineType == "MATERIAL").Sum(x => x.Amount); row.TaxAmount = lines.Where(x => x.LineType == "TAX").Sum(x => x.Amount); row.OtherAmount = lines.Where(x => x.LineType is "FEE" or "OTHER").Sum(x => x.Amount); row.FinalAmount = lines.Where(x => x.Responsibility is "OWNER" or "PM").Sum(x => x.Amount); row.OwnerResponsibility = lines.Where(x => x.Responsibility == "OWNER").Sum(x => x.Amount); row.ManagerResponsibility = lines.Where(x => x.Responsibility == "PM").Sum(x => x.Amount); row.VendorResponsibility = lines.Where(x => x.Responsibility == "VENDOR").Sum(x => x.Amount);
    }

    private static IReadOnlyList<P0JournalLineRequest> BuildMaintenanceJournalLines(MilestonePmMaintenanceCase row)
    {
        var lines = new List<P0JournalLineRequest>(); if (row.OwnerCharge > 0) lines.Add(new P0JournalLineRequest($"OWNER_EXPENSE:{row.OwnerUserId}:{row.PropertyId}:{row.Currency}", row.OwnerCharge, 0, row.OwnerUserId, row.PropertyId, row.Title)); var managerShare = row.ExpenseAmount - row.OwnerCharge; if (managerShare > 0) lines.Add(new P0JournalLineRequest($"PM_OPERATING_EXPENSE:{row.Currency}", managerShare, 0, null, null, $"PM responsibility for {row.Number}")); lines.Add(new P0JournalLineRequest($"THIRD_PARTY_PAYABLE:{row.OwnerUserId}:{row.PropertyId}:{row.Currency}", 0, row.ExpenseAmount, row.OwnerUserId, row.PropertyId, "Vendor payable; payment tracked separately")); return lines;
    }

    private static IReadOnlyList<P0JournalLineRequest> BuildWorkOrderJournalLines(MilestoneWorkOrder row)
    {
        var lines = new List<P0JournalLineRequest>(); if (row.OwnerResponsibility > 0) lines.Add(new P0JournalLineRequest($"OWNER_EXPENSE:{row.OwnerUserId}:{row.PropertyId}:{row.Currency}", row.OwnerResponsibility, 0, row.OwnerUserId, row.PropertyId, row.Scope)); if (row.ManagerResponsibility > 0) lines.Add(new P0JournalLineRequest($"PM_OPERATING_EXPENSE:{row.Currency}", row.ManagerResponsibility, 0, null, null, $"PM responsibility for {row.WorkOrderNumber}")); lines.Add(new P0JournalLineRequest($"THIRD_PARTY_PAYABLE:{row.OwnerUserId}:{row.PropertyId}:{row.Currency}", 0, row.FinalAmount, row.OwnerUserId, row.PropertyId, "Vendor payable; payment tracked separately")); return lines;
    }

    private static bool HasNoChecklistItems(string json) { using var document = JsonDocument.Parse(json); return document.RootElement.GetArrayLength() == 0; }

    private static IReadOnlyList<(string ItemId, string Description, string Severity, bool BlocksReadiness)> ParseCorrectiveFindings(string json)
    {
        using var document = JsonDocument.Parse(json); var result = new List<(string, string, string, bool)>(); var index = 0; foreach (var item in document.RootElement.EnumerateArray()) { index++; if (item.ValueKind != JsonValueKind.Object) continue; var severity = item.TryGetProperty("severity", out var severityNode) ? severityNode.GetString()?.Trim().ToUpperInvariant() ?? "MEDIUM" : "MEDIUM"; var explicitlyRequired = item.TryGetProperty("correctiveRequired", out var requiredNode) && requiredNode.ValueKind == JsonValueKind.True; var resolved = item.TryGetProperty("resolved", out var resolvedNode) && resolvedNode.ValueKind == JsonValueKind.True; if (resolved || (!explicitlyRequired && severity is not ("HIGH" or "CRITICAL"))) continue; var itemId = item.TryGetProperty("checklistItemId", out var idNode) ? idNode.GetString() : item.TryGetProperty("id", out idNode) ? idNode.GetString() : null; var description = item.TryGetProperty("finding", out var findingNode) ? findingNode.GetString() : item.TryGetProperty("description", out findingNode) ? findingNode.GetString() : null; result.Add((string.IsNullOrWhiteSpace(itemId) ? $"finding-{index}" : itemId.Trim(), string.IsNullOrWhiteSpace(description) ? $"Correct finding {index}" : description.Trim(), severity is "LOW" or "MEDIUM" or "HIGH" or "CRITICAL" ? severity : "MEDIUM", explicitlyRequired || severity is "HIGH" or "CRITICAL")); } return result;
    }

    private async Task EnsureMaintenanceApprovalAsync(MilestonePmMaintenanceCase row, decimal amount, Guid? approvalId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime); var agreement = await db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == row.ManagerUserId && x.OwnerUserId == row.OwnerUserId && x.Status == "ACTIVE" && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today) && (x.PropertyId == row.PropertyId || x.PropertyId == null) && !x.IsDeleted).OrderByDescending(x => x.PropertyId.HasValue).ThenByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct); var threshold = agreement?.MaintenanceApprovalLimit ?? 0m; if (amount <= threshold && threshold > 0) return; if (!approvalId.HasValue || !await db.MilestoneP0Approvals.AnyAsync(x => x.Id == approvalId && x.ManagerUserId == row.ManagerUserId && x.OwnerUserId == row.OwnerUserId && x.PropertyId == row.PropertyId && x.Status == "APPROVED" && x.Amount >= amount && x.Currency == row.Currency && (!x.ExpiresAt.HasValue || x.ExpiresAt > timeProvider.GetUtcNow()) && (x.SourceType == null || (x.SourceType == "MAINTENANCE" && x.SourceId == row.Id)) && !x.IsDeleted, ct)) throw new InvalidOperationException("A current owner approval covering the maintenance amount is required.");
    }

    private async Task EnsureWorkOrderApprovalAsync(MilestoneWorkOrder row, decimal amount, Guid? approvalId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime); var agreement = await db.MilestoneP0ManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == row.ManagerUserId && x.OwnerUserId == row.OwnerUserId && x.Status == "ACTIVE" && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today) && (x.PropertyId == row.PropertyId || x.PropertyId == null) && !x.IsDeleted).OrderByDescending(x => x.PropertyId.HasValue).ThenByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct); var threshold = agreement?.MaintenanceApprovalLimit ?? 0m; if (amount <= threshold && threshold > 0) return; if (!approvalId.HasValue || !await db.MilestoneP0Approvals.AnyAsync(x => x.Id == approvalId && x.ManagerUserId == row.ManagerUserId && x.OwnerUserId == row.OwnerUserId && x.PropertyId == row.PropertyId && x.Status == "APPROVED" && x.Amount >= amount && x.Currency == row.Currency && (!x.ExpiresAt.HasValue || x.ExpiresAt > timeProvider.GetUtcNow()) && (x.SourceType == null || (x.SourceType == "WORK_ORDER" && x.SourceId == row.Id)) && !x.IsDeleted, ct)) throw new InvalidOperationException("A current owner approval covering the work order is required.");
    }

    private void AddAudit(Guid actorUserId, string action, string subjectType, Guid subjectId) => db.MilestoneAuditEvents.Add(new MilestoneAuditEvent { ManagerUserId = actorUserId, ActorUserId = actorUserId, ActorRole = "PropertyManager", Action = action, SubjectType = subjectType, SubjectId = subjectId, Reason = action, MetadataJson = "{\"source\":\"professional-operations\"}" });
    private static void ValidateJson(string value, JsonValueKind kind) { try { using var doc = JsonDocument.Parse(value); if (doc.RootElement.ValueKind != kind || value.Length > 50000) throw new InvalidOperationException("Structured data is invalid."); } catch (JsonException) { throw new InvalidOperationException("Structured data must be valid JSON."); } }
    private static bool JsonPayloadEquals(string persisted, string requested)
    {
        try
        {
            using var left = JsonDocument.Parse(persisted); using var right = JsonDocument.Parse(requested);
            return JsonElement.DeepEquals(left.RootElement, right.RootElement);
        }
        catch (JsonException) { return string.Equals(persisted, requested, StringComparison.Ordinal); }
    }
    private static PmOwnerBlockDto ToDto(MilestonePmOwnerBlock x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.StartsAt, x.EndsAt, x.TimeZone, x.Reason, x.Status, x.BookingId, x.RowVersion, x.Category, x.Notes);
    private static PmReservationNoteDto ToDto(MilestonePmReservationNote x) => new(x.Id, x.BookingId, x.AuthorUserId, x.Body, x.Visibility, x.CreatedAt);
    private static PmMaintenanceCaseDto ToDto(MilestonePmMaintenanceCase x) => new(x.Id, x.OwnerUserId, x.PropertyId, x.VendorId, x.Number, x.Title, x.Description, x.Status, x.Priority, x.SelectedQuoteAmount, x.ExpenseAmount, x.OwnerCharge, x.ManagerFee, x.ScheduledAt, x.Currency, x.RowVersion, x.SelectedQuoteId, x.FinanciallyPosted, x.FinancialJournalId, x.FinancialReversalJournalId, x.ReplacementFinancialJournalId, x.FinancialStatus, x.CorrectionCount);
    private static WorkOrderDto ToWorkOrderDto(MilestoneWorkOrder x) => new(x.Id, x.PropertyId, x.OwnerUserId, x.VendorId, x.WorkOrderNumber, x.Scope, x.Status, x.QuoteAmount, x.ApprovedAmount, x.LaborAmount, x.PartsAmount, x.SlaDueAt, x.ScheduledAt);
    private static PmProfessionalWorkOrderDto ToProfessionalWorkOrderDto(MilestoneWorkOrder x) => new(x.Id, x.PropertyId, x.OwnerUserId, x.VendorId, x.WorkOrderNumber, x.Scope, x.Status, x.QuoteAmount, x.ApprovedAmount, x.LaborAmount, x.PartsAmount, x.OtherAmount, x.TaxAmount, x.FinalAmount, x.OwnerResponsibility, x.ManagerResponsibility, x.VendorResponsibility, x.Currency, x.SlaDueAt, x.ScheduledAt, x.OwnerApprovalId, x.SelectedQuoteId, x.PostingStatus, x.FinancialJournalId, x.FinancialReversalJournalId, x.ReplacementFinancialJournalId, x.CorrectionCount, x.RowVersion, x.SourceInspectionId);
    private static PmWorkOrderQuoteDto ToDto(MilestonePmWorkOrderQuote x) => new(x.Id, x.WorkOrderId, x.VendorId, x.Amount, x.Currency, x.Scope, x.Status, x.ExpiresAt, x.EvidenceAttachmentId, x.CreatedAt);
    private static PmMaintenanceQuoteDto ToDto(MilestonePmMaintenanceQuote x) => new(x.Id, x.MaintenanceId, x.VendorId, x.Amount, x.Currency, x.Scope, x.Status, x.ExpiresAt);
    private static PmCleaningDto ToDto(MilestonePmCleaningReadiness x) => new(x.Id, x.PropertyId, x.BookingId, x.AssignedUserId, x.VendorId, x.DueAt, x.Status, x.ChecklistJson, x.PhotosJson, x.Issues, x.CompletedAt, x.RowVersion, x.TemplateName, x.TemplateVersion);
    private static PmAssetDto ToDto(MilestonePmAsset x) => new(x.Id, x.PropertyId, x.AssetTag, x.Name, x.Category, x.Status, x.Quantity, x.Location, x.MetadataJson, x.PhotosJson, x.RetiredAt, x.RowVersion, x.Description, x.SerialReference, x.PurchaseDate, x.PurchaseCost, x.WarrantyExpiry, x.Condition);
    private static string NormalizeAssetCondition(string? condition) => condition?.Trim().ToUpperInvariant() switch { "GOOD" or "FAIR" or "POOR" or "DAMAGED" => condition.Trim().ToUpperInvariant(), _ => throw new InvalidOperationException("Asset condition must be GOOD, FAIR, POOR or DAMAGED.") };
    private static string NormalizeAssetStatus(string? status) => status?.Trim().ToUpperInvariant() switch { "ACTIVE" or "RETIRED" or "LOST" or "DAMAGED" => status.Trim().ToUpperInvariant(), _ => throw new InvalidOperationException("Asset status must be ACTIVE, RETIRED, LOST or DAMAGED.") };
    private static string NormalizeBlockCategory(string? category) => category?.Trim().ToUpperInvariant() switch { "OWNER_STAY" or "PERSONAL" or "MAINTENANCE" or "OTHER" => category.Trim().ToUpperInvariant(), _ => throw new InvalidOperationException("Block category must be OWNER_STAY, PERSONAL, MAINTENANCE or OTHER.") };
    private static bool IsBlockingCalendarItem(PmCalendarItemDto item) => BlockingCalendarTypes.Contains(item.Type) && !string.Equals(item.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) && !string.Equals(item.Status, "REJECTED", StringComparison.OrdinalIgnoreCase);
    private static PmIncidentDto ToDto(MilestonePmIncident x) => new(x.Id, x.PropertyId, x.BookingId, x.IncidentType, x.Severity, x.OccurredAt, x.Description, x.InvolvedPartiesJson, x.EvidenceJson, x.ActionTaken, x.FollowUp, x.FinancialImpact, x.InsuranceReference, x.Status, x.ResolvedAt, x.RowVersion);
    private static PmInspectionDto ToDto(MilestonePmInspectionRecord x) => new(x.Id, x.PropertyId, x.AssignedUserId, x.InspectionType, x.ScheduledAt, x.ChecklistJson, x.EvidenceJson, x.FindingsJson, x.Status, x.SignedOffAt, x.RowVersion, x.CorrectiveWorkOrderId, x.TemplateId, x.TemplateName, x.TemplateVersion, x.ReinspectionOfActionId);
    private static PmChecklistTemplateDto ToDto(MilestonePmChecklistTemplate x) => new(x.Id, x.Name, x.WorkflowType, x.Version, x.ItemsJson, x.Status, x.SupersedesTemplateId, x.CreatedAt);
    private static PmPropertyChecklistAssignmentDto ToDto(MilestonePmPropertyChecklistAssignment x) => new(x.Id, x.PropertyId, x.TemplateId, x.WorkflowType, x.EffectiveAt);
    private static PmCorrectiveActionDto ToDto(MilestonePmCorrectiveAction x) => new(x.Id, x.InspectionId, x.PropertyId, x.ChecklistItemId, x.Description, x.Severity, x.BlocksReadiness, x.Status, x.WorkOrderId, x.RetestInspectionId, x.ResolutionNotes, x.RowVersion, x.CreatedAt, x.ResolvedAt);

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
