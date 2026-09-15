using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Access;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
using NestyStay.Domain.Access;

namespace NestyStay.Infrastructure.Persistence;

public sealed class EfQrAccessStore(
    NestyStayDbContext db,
    IPhaseOneStore phaseOneStore,
    TimeProvider timeProvider) : IQrAccessStore
{
    private static readonly TimeSpan TokenLifetimeGrace = TimeSpan.FromMinutes(5);

    public async Task<QrIssueResult> IssueForBookingAsync(Guid bookingId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var booking = phaseOneStore.GetBooking(bookingId)
            ?? throw new InvalidOperationException("Booking was not found.");

        if (booking.GuestUserId != actorUserId && booking.HostUserId != actorUserId)
        {
            throw new UnauthorizedAccessException("Only the booking guest or host can issue a gate pass.");
        }

        if (!string.Equals(booking.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.Status, "PaymentCaptured", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A gate pass requires an approved and paid booking.");
        }

        var now = timeProvider.GetUtcNow();
        var validFrom = booking.CheckIn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var expiresAt = booking.CheckOut.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (expiresAt <= validFrom)
        {
            throw new InvalidOperationException("Booking dates cannot issue a gate pass.");
        }

        var activeExisting = await db.QrAccessCodes
            .Where(code => code.BookingId == bookingId && !code.IsRevoked && code.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var existing in activeExisting)
        {
            existing.IsRevoked = true;
            existing.RevokedAt = now;
            existing.UpdatedAt = now;
        }

        var token = CreateToken();
        var entity = new QrAccessCode
        {
            Id = Guid.NewGuid(),
            SubjectType = "Booking",
            SubjectId = bookingId,
            BookingId = bookingId,
            PropertyId = booking.PropertyId,
            GuestUserId = booking.GuestUserId,
            CodeHash = HashToken(token),
            ValidFrom = validFrom,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorUserId
        };
        db.QrAccessCodes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return ToIssueResult(entity, token);
    }

    public async Task<QrAccessDto?> GetAsync(Guid qrId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var entity = await db.QrAccessCodes.AsNoTracking().SingleOrDefaultAsync(code => code.Id == qrId && !code.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.GuestUserId != actorUserId && phaseOneStore.GetBooking(entity.BookingId)?.HostUserId != actorUserId)
        {
            throw new UnauthorizedAccessException("This gate pass is not available to the current user.");
        }

        return ToDto(entity);
    }

    public async Task<QrAccessDto?> RevokeAsync(Guid qrId, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        var entity = await db.QrAccessCodes.SingleOrDefaultAsync(code => code.Id == qrId && !code.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var booking = phaseOneStore.GetBooking(entity.BookingId);
        if (booking is null || (booking.GuestUserId != actorUserId && booking.HostUserId != actorUserId))
        {
            throw new UnauthorizedAccessException("This gate pass is not available to the current user.");
        }

        var now = timeProvider.GetUtcNow();
        entity.IsRevoked = true;
        entity.RevokedAt = now;
        entity.RevokeReason = string.IsNullOrWhiteSpace(reason) ? "Revoked by account holder." : reason.Trim()[..Math.Min(reason.Trim().Length, 256)];
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<QrAccessDto>> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var codes = await db.QrAccessCodes.AsNoTracking()
            .Where(code => !code.IsDeleted && (code.GuestUserId == actorUserId || code.CreatedByUserId == actorUserId))
            .OrderByDescending(code => code.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return codes.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<QrHistoryEventDto>> HistoryAsync(Guid qrId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var entity = await db.QrAccessCodes.AsNoTracking().SingleOrDefaultAsync(code => code.Id == qrId && !code.IsDeleted, cancellationToken);
        if (entity is null) return [];
        if (entity.GuestUserId != actorUserId && entity.CreatedByUserId != actorUserId && phaseOneStore.GetBooking(entity.BookingId)?.HostUserId != actorUserId)
            throw new UnauthorizedAccessException("This gate pass is not available to the current user.");

        var history = new List<QrHistoryEventDto>
        {
            new(Guid.NewGuid(), entity.Id, "Issued", Status(entity), entity.CreatedAt),
        };
        if (entity.IsRevoked && entity.RevokedAt is not null)
            history.Add(new(Guid.NewGuid(), entity.Id, "Revoked", "Revoked", entity.RevokedAt.Value, entity.RevokeReason));

        var scans = await db.QrScanLogs.AsNoTracking()
            .Where(scan => scan.QrAccessCodeId == qrId && !scan.IsDeleted)
            .OrderBy(scan => scan.ScannedAt)
            .ToListAsync(cancellationToken);
        history.AddRange(scans.Select(scan => new QrHistoryEventDto(scan.Id, entity.Id, "Scanned", scan.Result, scan.ScannedAt, DeviceMetadata: scan.DeviceMetadataJson)));
        return history.OrderBy(item => item.OccurredAt).ToArray();
    }

    public async Task<QrValidationResult> ValidateAsync(string token, Guid propertyId, string? deviceMetadata, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256 || propertyId == Guid.Empty)
        {
            return Invalid("Malformed QR access request.");
        }

        var hash = HashToken(token.Trim());
        var entity = await db.QrAccessCodes.SingleOrDefaultAsync(code => code.CodeHash == hash && !code.IsDeleted, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (entity is null)
        {
            return Invalid("QR access token is invalid.");
        }

        var booking = phaseOneStore.GetBooking(entity.BookingId);
        var result = "Valid";
        var message = "QR access approved for this property and booking.";
        if (entity.IsRevoked)
        {
            result = "Revoked";
            message = "QR access token has been revoked.";
        }
        else if (entity.PropertyId != propertyId)
        {
            result = "WrongProperty";
            message = "QR access token is not valid for this property.";
        }
        else if (booking is null || booking.PropertyId != entity.PropertyId || booking.GuestUserId != entity.GuestUserId ||
                 string.Equals(booking.Status, "Rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            result = "BookingInvalid";
            message = "The booking is no longer valid for access.";
        }
        else if (now < entity.ValidFrom)
        {
            result = "NotStarted";
            message = "QR access is not active yet.";
        }
        else if (now >= entity.ExpiresAt + TokenLifetimeGrace)
        {
            result = "Expired";
            message = "QR access token has expired.";
        }

        db.QrScanLogs.Add(new QrScanLog
        {
            Id = Guid.NewGuid(),
            QrAccessCodeId = entity.Id,
            PropertyId = propertyId,
            ScannedAt = now,
            Result = result,
            DeviceMetadataJson = SanitizeDeviceMetadata(deviceMetadata),
            CreatedAt = now,
            UpdatedAt = now
        });

        if (result == "Valid")
        {
            entity.ValidationCount++;
            entity.LastValidatedAt = now;
            entity.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new QrValidationResult(
            result == "Valid",
            result,
            result == "Valid" ? entity.BookingId : null,
            result == "Valid" ? entity.PropertyId : null,
            result == "Valid" ? entity.ValidFrom : null,
            result == "Valid" ? entity.ExpiresAt : null,
            entity.ValidationCount,
            message);
    }

    private static string CreateToken() => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static QrIssueResult ToIssueResult(QrAccessCode code, string token) =>
        new(code.Id, code.BookingId, code.PropertyId, code.ValidFrom, code.ExpiresAt, Status(code), token, $"/api/access/qr/validate?token={Uri.EscapeDataString(token)}&propertyId={code.PropertyId}");

    private static QrAccessDto ToDto(QrAccessCode code) =>
        new(code.Id, code.BookingId, code.PropertyId, code.ValidFrom, code.ExpiresAt, code.IsRevoked, code.ValidationCount, code.LastValidatedAt, Status(code), code.RevokeReason);

    private static string Status(QrAccessCode code)
    {
        var now = DateTimeOffset.UtcNow;
        return code.IsRevoked ? "Revoked" : now < code.ValidFrom ? "NotStarted" : now >= code.ExpiresAt + TokenLifetimeGrace ? "Expired" : "Active";
    }

    private static QrValidationResult Invalid(string message) => new(false, "Invalid", null, null, null, null, 0, message);

    private static string? SanitizeDeviceMetadata(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var value = raw.Trim();
        return value.Length <= 256 ? value : value[..256];
    }
}
