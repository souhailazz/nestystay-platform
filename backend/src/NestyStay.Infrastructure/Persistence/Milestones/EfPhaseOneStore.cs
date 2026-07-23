using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class EfPhaseOneStore(
    NestyStayDbContext db,
    IEkycProvider ekycProvider,
    IPaymentGateway paymentGateway,
    INotificationGateway notificationGateway,
    TimeProvider timeProvider,
    IAccessTokenService? accessTokenService = null) : IPhaseOneStore
{
    private const int PasswordHashIterations = 120_000;
    private const int TotpStepSeconds = 30;
    private readonly IAccessTokenService _accessTokenService = accessTokenService ?? DevelopmentAccessTokenService.Instance;

    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        ValidateRegistration(request);
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.MilestoneUsers.AnyAsync(user => user.NormalizedEmail == email, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new MilestoneUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.DisplayName.Trim(),
            Phone = request.Phone?.Trim(),
            TwoFactorSecret = GenerateSecret(),
            RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>([request.Role])
        };

        db.MilestoneUsers.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException("Email is already registered.", exception);
        }

        return new RegisterUserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            true);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(
            item => item.NormalizedEmail == email,
            cancellationToken);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var expiresAt = timeProvider.GetUtcNow().AddMinutes(10);
        var challenge = new MilestoneTwoFactorChallenge
        {
            Id = Guid.NewGuid(),
            ChallengeId = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = expiresAt
        };

        db.MilestoneTwoFactorChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            user.Id,
            user.Email,
            true,
            challenge.ChallengeId,
            expiresAt);
    }

    public async Task<DevelopmentAuthCodeResponse?> GetDevelopmentTwoFactorCodeAsync(string challengeId, CancellationToken cancellationToken)
    {
        var challenge = await db.MilestoneTwoFactorChallenges.SingleOrDefaultAsync(
            item => item.ChallengeId == challengeId,
            cancellationToken);
        if (challenge is null || challenge.ExpiresAt < timeProvider.GetUtcNow())
        {
            return null;
        }

        var user = await db.MilestoneUsers.SingleAsync(item => item.Id == challenge.UserId, cancellationToken);
        return new DevelopmentAuthCodeResponse(
            challenge.ChallengeId,
            GenerateTotp(user.TwoFactorSecret, timeProvider.GetUtcNow()),
            challenge.ExpiresAt);
    }

    public async Task<GoogleSignInResponse> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeGoogleEmail(request.Email);
        var displayName = NormalizeGoogleDisplayName(request.DisplayName, email);
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(
            item => item.NormalizedEmail == email,
            cancellationToken);

        if (user is null)
        {
            user = new MilestoneUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email,
                PasswordHash = HashPassword(CreateExternalPasswordSeed(request.GoogleSubject, email)),
                DisplayName = displayName,
                Phone = null,
                TwoFactorSecret = GenerateSecret(),
                RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>([UserRole.Guest])
            };
            db.MilestoneUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        var tokenExpiresAt = timeProvider.GetUtcNow().AddHours(8);
        return new GoogleSignInResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            _accessTokenService.Issue(user.Id, MilestoneJson.DeserializeList<UserRole>(user.RolesJson), tokenExpiresAt),
            tokenExpiresAt,
            MilestoneJson.DeserializeList<UserRole>(user.RolesJson),
            "Google");
    }

    public async Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var challenge = await db.MilestoneTwoFactorChallenges.SingleOrDefaultAsync(
            item => item.ChallengeId == request.ChallengeId,
            cancellationToken);
        if (challenge is null || challenge.ExpiresAt < timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Invalid or expired 2FA challenge.");
        }

        var user = await db.MilestoneUsers.SingleAsync(item => item.Id == challenge.UserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Code) ||
            !VerifyTotp(user.TwoFactorSecret, request.Code, timeProvider.GetUtcNow()))
        {
            throw new InvalidOperationException("Invalid 2FA code.");
        }

        db.MilestoneTwoFactorChallenges.Remove(challenge);
        await db.SaveChangesAsync(cancellationToken);

        var tokenExpiresAt = timeProvider.GetUtcNow().AddHours(8);
        var roles = MilestoneJson.DeserializeList<UserRole>(user.RolesJson);
        return new VerifyTwoFactorResponse(
            user.Id,
            _accessTokenService.Issue(user.Id, roles, tokenExpiresAt),
            tokenExpiresAt,
            roles);
    }

    public IReadOnlyList<PropertyListingDto> GetProperties()
    {
        EnsurePhaseOneSeeded();
        return db.MilestoneProperties
            .AsNoTracking()
            .OrderBy(property => property.Title)
            .ToList()
            .Select(ToListingDto)
            .ToList();
    }

    public PropertyListingDto? GetProperty(Guid id)
    {
        EnsurePhaseOneSeeded();
        return db.MilestoneProperties
            .AsNoTracking()
            .SingleOrDefault(property => property.Id == id) is { } property
            ? ToListingDto(property)
            : null;
    }

    public async Task<PropertyListingDto> CreatePropertyAsync(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        ValidateProperty(request);

        var property = new MilestoneProperty
        {
            Id = Guid.NewGuid(),
            HostUserId = request.HostUserId,
            HostName = request.HostName.Trim(),
            HostEmail = request.HostEmail.Trim().ToLowerInvariant(),
            Title = request.Title.Trim(),
            Location = request.Location.Trim(),
            Country = string.IsNullOrWhiteSpace(request.Country) ? "Jamaica" : request.Country.Trim(),
            NightlyRate = decimal.Round(request.NightlyRate, 2),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            BadgeLevel = request.BadgeLevel,
            GuestVerificationEnabled = request.GuestVerificationEnabled,
            InsuraGuestEnabled = request.InsuraGuestEnabled,
            CancellationPolicy = request.CancellationPolicy.Trim(),
            HighlightsJson = MilestoneJson.Serialize(
                request.Highlights is null || request.Highlights.Count == 0
                    ? ["Host-created listing"]
                    : request.Highlights.Select(item => item.Trim()).Where(item => item.Length > 0).ToList())
        };

        db.MilestoneProperties.Add(property);
        await db.SaveChangesAsync(cancellationToken);

        return ToListingDto(property);
    }

    public async Task<BookingQuoteDto> QuoteBookingAsync(BookingQuoteRequest request, CancellationToken cancellationToken)
    {
        await EnsurePhaseOneSeededAsync(cancellationToken);
        var property = await FindPropertyAsync(request.PropertyId, cancellationToken);
        var quote = BuildQuote(property, request.CheckIn, request.CheckOut, true, null);
        var now = timeProvider.GetUtcNow();

        await ExpirePendingHoldsAsync(now, cancellationToken);
        if (await FindBlockingBookingAsync(property.Id, request.CheckIn, request.CheckOut, now, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Requested dates are already held or approved for this property.");
        }

        return quote;
    }

    public IReadOnlyList<BookingDto> GetBookings(Guid? guestUserId = null)
    {
        ExpirePendingHolds(timeProvider.GetUtcNow());
        return db.MilestoneBookings
            .AsNoTracking()
            .Where(booking => guestUserId == null || booking.GuestUserId == guestUserId)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToList()
            .Select(ToDto)
            .ToList();
    }

    public BookingDto? GetBooking(Guid id)
    {
        ExpirePendingHolds(timeProvider.GetUtcNow());
        return db.MilestoneBookings
            .AsNoTracking()
            .SingleOrDefault(item => item.Id == id) is { } booking
            ? ToDto(booking)
            : null;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        await EnsurePhaseOneSeededAsync(cancellationToken);
        var property = await FindPropertyAsync(request.PropertyId, cancellationToken);
        var guest = await db.MilestoneUsers.SingleOrDefaultAsync(user => user.Id == request.GuestUserId, cancellationToken)
            ?? throw new InvalidOperationException("Guest user must register before booking.");
        var now = timeProvider.GetUtcNow();
        var quote = BuildQuote(property, request.CheckIn, request.CheckOut, true, null);

        await ExpirePendingHoldsAsync(now, cancellationToken);
        if (await FindBlockingBookingAsync(property.Id, request.CheckIn, request.CheckOut, now, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Requested dates are already held or approved for this property.");
        }

        var requiresVerification = property.GuestVerificationEnabled;
        var booking = new MilestoneBooking
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            HostUserId = property.HostUserId,
            HostName = property.HostName,
            HostEmail = property.HostEmail,
            GuestUserId = guest.Id,
            GuestEmail = guest.Email,
            GuestName = guest.DisplayName,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Status = requiresVerification ? BookingStatus.PendingVerification : BookingStatus.Approved,
            VerificationStatus = requiresVerification ? VerificationStatus.Pending : VerificationStatus.Passed,
            PaymentStatus = PaymentStatus.Pending,
            RequiresGuestVerification = requiresVerification,
            HoldExpiresAt = requiresVerification ? now.AddMinutes(NestyStayBusinessRules.DefaultBookingHoldMinutes) : null,
            Nights = quote.Nights,
            NightlyRate = property.NightlyRate,
            StaySubtotal = quote.StaySubtotal,
            GuestPlatformFee = quote.GuestPlatformFee,
            TotalAmount = quote.TotalAmount,
            Currency = property.Currency,
            PropertyTitle = property.Title,
            PriceBreakdownJson = MilestoneJson.Serialize(quote.PriceBreakdown),
            NotificationsJson = MilestoneJson.Serialize(Array.Empty<BookingNotificationDto>()),
            TimelineJson = MilestoneJson.Serialize<IReadOnlyList<string>>(requiresVerification
                ? ["Booking created", "Dates held", "Alibaba Cloud eKYC started"]
                : ["Booking created", "No guest eKYC required", "Booking approved"])
        };

        db.MilestoneBookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        if (booking.RequiresGuestVerification)
        {
            await StartEkycAsync(booking, request, cancellationToken);
        }
        else
        {
            await QueueNotificationsAsync(booking, BuildApprovalNotifications(booking), cancellationToken);
            await AuthorizePaymentAfterApprovalAsync(booking, cancellationToken);
        }

        return ToDto(booking);
    }

    public async Task<BookingDto?> ResolveVerificationAsync(Guid bookingId, ResolveVerificationRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        if (!booking.RequiresGuestVerification)
        {
            throw new InvalidOperationException("This booking does not require guest verification.");
        }

        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            throw new InvalidOperationException("Verification provider reference is required.");
        }

        if (!string.Equals(booking.EkycTransactionId, request.ProviderReference, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Verification reference does not match this booking.");
        }

        if (booking.Status != BookingStatus.PendingVerification)
        {
            if ((request.Passed && booking.Status == BookingStatus.Approved && booking.VerificationStatus == VerificationStatus.Passed) ||
                (!request.Passed && booking.Status == BookingStatus.Rejected && booking.VerificationStatus is VerificationStatus.Failed or VerificationStatus.Expired))
            {
                return ToDto(booking);
            }

            throw new InvalidOperationException("Verification can only be resolved for PENDING bookings.");
        }

        IReadOnlyList<PendingNotification> notifications;
        if (!request.Passed)
        {
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Failed;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, "Alibaba Cloud eKYC failed", "Booking rejected", "Dates released");
            notifications = BuildRejectionNotifications(booking);
        }
        else
        {
            booking.Status = BookingStatus.Approved;
            booking.VerificationStatus = VerificationStatus.Passed;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, "Alibaba Cloud eKYC approved", "Booking approved");
            notifications = BuildApprovalNotifications(booking);
        }

        await QueueNotificationsAsync(booking, notifications, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (request.Passed)
        {
            await AuthorizePaymentAfterApprovalAsync(booking, cancellationToken);
        }

        return ToDto(booking);
    }

    public async Task<BookingDto?> CapturePaymentAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        if (booking.Status != BookingStatus.Approved)
        {
            throw new InvalidOperationException("Stripe payment can only be captured after verification passes and booking is APPROVED.");
        }

        if (booking.PaymentStatus == PaymentStatus.Captured)
        {
            return ToDto(booking);
        }

        await AuthorizePaymentAfterApprovalAsync(booking, cancellationToken);
        if (booking.PaymentAuthorizationReference is null)
        {
            throw new InvalidOperationException("Stripe payment must have an authorization reference before it can be captured.");
        }

        var capture = await paymentGateway.CaptureAsync(
            new PaymentCaptureRequest(
                booking.PaymentAuthorizationReference,
                booking.TotalAmount,
                booking.Currency),
            cancellationToken);

        booking.PaymentProvider = capture.ProviderName;
        booking.PaymentCaptureReference = capture.CaptureReference;
        booking.PaymentStatus = capture.Status;
        AddTimeline(booking, "Stripe payment captured after approval");
        await QueueNotificationsAsync(booking, BuildPaymentCapturedNotifications(booking), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(booking);
    }

    private async Task StartEkycAsync(MilestoneBooking booking, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ekycProvider.StartCheckAsync(
                new EkycStartRequest(
                    booking.GuestUserId.ToString("N"),
                    UserRole.Guest,
                    booking.Id.ToString("N"),
                    request.EkycMetaInfo,
                    string.IsNullOrWhiteSpace(request.DocumentType) ? "01000000" : request.DocumentType.Trim(),
                    request.EkycCallbackUrl),
                cancellationToken);

            booking.EkycProvider = result.ProviderName;
            booking.EkycTransactionId = result.TransactionId;
            booking.EkycTransactionUrl = result.TransactionUrl;
            booking.VerificationStatus = result.Status;
            AddTimeline(booking, $"Alibaba Cloud eKYC transaction created: {result.TransactionId}");
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Failed;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, "Alibaba Cloud eKYC could not be started", "Dates released");
            await db.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException("Alibaba Cloud eKYC could not be started for this booking.", exception);
        }
    }

    private async Task AuthorizePaymentAfterApprovalAsync(MilestoneBooking booking, CancellationToken cancellationToken)
    {
        if (booking.Status != BookingStatus.Approved ||
            booking.PaymentStatus is PaymentStatus.Authorized or PaymentStatus.Captured ||
            booking.PaymentAuthorizationReference is not null)
        {
            return;
        }

        var authorization = await paymentGateway.AuthorizeAsync(
            new PaymentAuthorizationRequest(
                booking.Id,
                booking.TotalAmount,
                booking.Currency,
                $"NestyStay booking {booking.Id:N}"),
            cancellationToken);

        booking.PaymentProvider = authorization.ProviderName;
        booking.PaymentAuthorizationReference = authorization.AuthorizationReference;
        booking.PaymentClientSecret = authorization.ClientSecret;
        booking.PaymentStatus = authorization.Status;
        AddTimeline(booking, "Stripe manual-capture payment authorized after approval");
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueNotificationsAsync(
        MilestoneBooking booking,
        IReadOnlyList<PendingNotification> notifications,
        CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        var queuedAt = timeProvider.GetUtcNow();
        var existing = MilestoneJson.DeserializeList<BookingNotificationDto>(booking.NotificationsJson);
        foreach (var notification in notifications)
        {
            existing.Add(new BookingNotificationDto(
                notification.RecipientType,
                notification.Message.Recipient,
                notification.Message.Subject,
                queuedAt));
            AddTimeline(booking, $"Notification queued for {notification.RecipientType}");
        }

        booking.NotificationsJson = MilestoneJson.Serialize(existing);

        foreach (var notification in notifications)
        {
            await notificationGateway.QueueAsync(notification.Message, cancellationToken);
        }
    }

    private BookingQuoteDto BuildQuote(
        MilestoneProperty property,
        DateOnly checkIn,
        DateOnly checkOut,
        bool datesAvailable,
        DateTimeOffset? holdExpiresAt)
    {
        if (checkOut <= checkIn)
        {
            throw new InvalidOperationException("Check-out must be after check-in.");
        }

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var staySubtotal = decimal.Round(property.NightlyRate * nights, 2);
        var guestFeePercent = NestyStayBusinessRules.ResolveStandardGuestFeePercent(staySubtotal, nights);
        var guestPlatformFee = decimal.Round(staySubtotal * guestFeePercent / 100m, 2);
        var total = decimal.Round(staySubtotal + guestPlatformFee, 2);
        var lines = new List<BookingPriceLineDto>
        {
            new("stay", $"{property.NightlyRate:0.00} x {nights} night stay", staySubtotal, property.Currency, true),
            new("guest-platform-fee", $"{guestFeePercent:0}% NestyStay guest platform fee", guestPlatformFee, property.Currency, false)
        };

        if (property.GuestVerificationEnabled)
        {
            lines.Add(new("guest-verification", "Alibaba Cloud eKYC verification required before approval", 0m, property.Currency, false));
        }

        return new BookingQuoteDto(
            ToSummaryDto(property),
            checkIn,
            checkOut,
            nights,
            property.NightlyRate,
            staySubtotal,
            guestPlatformFee,
            total,
            property.Currency,
            property.GuestVerificationEnabled,
            datesAvailable,
            holdExpiresAt,
            lines);
    }

    private async Task<MilestoneProperty> FindPropertyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.MilestoneProperties.SingleOrDefaultAsync(property => property.Id == propertyId, cancellationToken)
        ?? throw new InvalidOperationException("Property not found.");

    private Task<MilestoneBooking?> FindBlockingBookingAsync(
        Guid propertyId,
        DateOnly checkIn,
        DateOnly checkOut,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        db.MilestoneBookings.FirstOrDefaultAsync(booking =>
            booking.PropertyId == propertyId &&
            (booking.Status == BookingStatus.Approved ||
             booking.Status == BookingStatus.PaymentCaptured ||
             booking.Status == BookingStatus.Confirmed ||
             (booking.Status == BookingStatus.PendingVerification && booking.HoldExpiresAt > now)) &&
            booking.CheckIn < checkOut &&
            checkIn < booking.CheckOut,
            cancellationToken);

    private async Task ExpirePendingHoldsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expired = await db.MilestoneBookings
            .Where(booking =>
                booking.Status == BookingStatus.PendingVerification &&
                booking.HoldExpiresAt != null &&
                booking.HoldExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var booking in expired)
        {
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Expired;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, "Pending verification hold expired", "Dates released");
        }

        if (expired.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private void ExpirePendingHolds(DateTimeOffset now)
    {
        var expired = db.MilestoneBookings
            .Where(booking =>
                booking.Status == BookingStatus.PendingVerification &&
                booking.HoldExpiresAt != null &&
                booking.HoldExpiresAt <= now)
            .ToList();

        foreach (var booking in expired)
        {
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Expired;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, "Pending verification hold expired", "Dates released");
        }

        if (expired.Count > 0)
        {
            db.SaveChanges();
        }
    }

    private void EnsurePhaseOneSeeded()
    {
        if (db.MilestoneProperties.Any())
        {
            return;
        }

        db.MilestoneProperties.AddRange(DefaultProperties());
        db.SaveChanges();
    }

    private async Task EnsurePhaseOneSeededAsync(CancellationToken cancellationToken)
    {
        if (await db.MilestoneProperties.AnyAsync(cancellationToken))
        {
            return;
        }

        db.MilestoneProperties.AddRange(DefaultProperties());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<PendingNotification> BuildApprovalNotifications(MilestoneBooking booking) =>
    [
        new("guest", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay booking approved",
            $"Your booking for {booking.PropertyTitle} is APPROVED after Alibaba Cloud eKYC.")),
        new("host", new NotificationMessage(
            booking.HostEmail,
            "NestyStay booking approved",
            $"{booking.GuestName}'s booking for {booking.PropertyTitle} is APPROVED."))
    ];

    private static IReadOnlyList<PendingNotification> BuildRejectionNotifications(MilestoneBooking booking) =>
    [
        new("guest", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay booking rejected",
            $"Your booking for {booking.PropertyTitle} was REJECTED because identity verification failed.")),
        new("host", new NotificationMessage(
            booking.HostEmail,
            "NestyStay booking dates released",
            $"{booking.GuestName}'s booking for {booking.PropertyTitle} was rejected and the dates were released."))
    ];

    private static IReadOnlyList<PendingNotification> BuildPaymentCapturedNotifications(MilestoneBooking booking) =>
    [
        new("guest", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay payment processed",
            $"Stripe payment for {booking.PropertyTitle} has been captured.")),
        new("host", new NotificationMessage(
            booking.HostEmail,
            "NestyStay payment processed",
            $"Stripe payment for {booking.GuestName}'s booking has been captured."))
    ];

    private static PropertyListingDto ToListingDto(MilestoneProperty property) =>
        new(
            property.Id,
            property.HostUserId,
            property.HostName,
            property.Title,
            property.Location,
            property.Country,
            property.NightlyRate,
            property.Currency,
            property.BadgeLevel,
            property.GuestVerificationEnabled,
            property.InsuraGuestEnabled,
            property.CancellationPolicy,
            MilestoneJson.DeserializeList<string>(property.HighlightsJson));

    private static BookingPropertySummaryDto ToSummaryDto(MilestoneProperty property) =>
        new(
            property.Id,
            property.Title,
            property.Location,
            property.Country,
            property.HostName,
            property.BadgeLevel,
            property.GuestVerificationEnabled,
            property.InsuraGuestEnabled,
            property.CancellationPolicy);

    private BookingDto ToDto(MilestoneBooking booking) =>
        new(
            booking.Id,
            booking.PropertyId,
            booking.HostUserId,
            booking.GuestUserId,
            booking.CheckIn,
            booking.CheckOut,
            ToMilestoneStatus(booking.Status),
            ToApiStatus(booking.VerificationStatus),
            ToApiStatus(booking.PaymentStatus),
            booking.RequiresGuestVerification,
            booking.Status == BookingStatus.PendingVerification && booking.HoldExpiresAt > timeProvider.GetUtcNow(),
            booking.HoldExpiresAt,
            booking.Nights,
            booking.NightlyRate,
            booking.StaySubtotal,
            booking.GuestPlatformFee,
            booking.TotalAmount,
            booking.Currency,
            booking.PropertyTitle,
            booking.HostName,
            booking.EkycProvider,
            booking.EkycTransactionId,
            booking.EkycTransactionUrl,
            booking.PaymentProvider,
            booking.PaymentAuthorizationReference,
            booking.PaymentClientSecret,
            booking.PaymentCaptureReference,
            MilestoneJson.DeserializeList<BookingPriceLineDto>(booking.PriceBreakdownJson),
            MilestoneJson.DeserializeList<BookingNotificationDto>(booking.NotificationsJson),
            MilestoneJson.DeserializeList<string>(booking.TimelineJson));

    private static void ValidateRegistration(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("Email, password, and display name are required.");
        }

        try
        {
            var address = new MailAddress(request.Email.Trim());
            if (!address.Address.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A valid email address is required.");
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("A valid email address is required.");
        }

        if (request.Password.Length < 8 ||
            !request.Password.Any(char.IsUpper) ||
            !request.Password.Any(char.IsLower) ||
            !request.Password.Any(char.IsDigit))
        {
            throw new InvalidOperationException("Password must be at least 8 characters and include uppercase, lowercase, and a number.");
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Password confirmation must match.");
        }

        if (!request.AcceptedTerms || !request.AcceptedPrivacy)
        {
            throw new InvalidOperationException("Terms of service and privacy policy acceptance are required.");
        }

        if (request.Role is not (UserRole.Guest or UserRole.Host))
        {
            throw new InvalidOperationException("Only traveler and host self-service registration is available.");
        }
    }

    private static string NormalizeGoogleEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Google email is required.");
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return address.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Google returned an invalid email address.");
        }
    }

    private static string NormalizeGoogleDisplayName(string displayName, string email)
    {
        var normalized = displayName.Trim();
        return normalized.Length == 0 ? email.Split('@')[0] : normalized;
    }

    private static string CreateExternalPasswordSeed(string? subject, string email) =>
        $"GOOGLE::{subject?.Trim() ?? email}::{Guid.NewGuid():N}";

    private static void ValidateProperty(CreatePropertyRequest request)
    {
        if (request.HostUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.HostName) ||
            string.IsNullOrWhiteSpace(request.HostEmail) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Location) ||
            string.IsNullOrWhiteSpace(request.Currency) ||
            string.IsNullOrWhiteSpace(request.CancellationPolicy))
        {
            throw new InvalidOperationException("Host, title, location, currency, and cancellation policy are required.");
        }

        try
        {
            _ = new MailAddress(request.HostEmail.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("A valid host email address is required.");
        }

        if (request.NightlyRate <= 0)
        {
            throw new InvalidOperationException("Nightly rate must be greater than zero.");
        }

        if (request.Currency.Trim().Length != 3)
        {
            throw new InvalidOperationException("Currency must be a three-letter code.");
        }

        if (request.GuestVerificationEnabled && request.BadgeLevel == BadgeLevel.Free)
        {
            throw new InvalidOperationException("Guest verification upsell requires a Verified, Trusted, or Wellness host badge.");
        }
    }

    private static string ToMilestoneStatus(BookingStatus status) =>
        status switch
        {
            BookingStatus.Approved or BookingStatus.PaymentCaptured or BookingStatus.Confirmed => "APPROVED",
            BookingStatus.Rejected or BookingStatus.Cancelled => "REJECTED",
            _ => "PENDING"
        };

    private static string ToApiStatus<TStatus>(TStatus status) where TStatus : struct, Enum =>
        status.ToString().ToUpperInvariant();

    private static void AddTimeline(MilestoneBooking booking, params string[] entries)
    {
        var timeline = MilestoneJson.DeserializeList<string>(booking.TimelineJson);
        timeline.AddRange(entries);
        booking.TimelineJson = MilestoneJson.Serialize(timeline);
    }

    private static IReadOnlyList<MilestoneProperty> DefaultProperties() =>
    [
        new()
        {
            Id = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            HostUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            HostName = "Island Villa Hosting",
            HostEmail = "host-villa@nestystay.local",
            Title = "Ocho Rios Verified Villa",
            Location = "Ocho Rios, St. Ann",
            Country = "Jamaica",
            NightlyRate = 185m,
            Currency = "USD",
            BadgeLevel = BadgeLevel.Verified,
            GuestVerificationEnabled = true,
            InsuraGuestEnabled = true,
            CancellationPolicy = "Moderate",
            HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Alibaba eKYC", "QR gate access", "InsuraGuest available", "Emergency 119 displayed"])
        },
        new()
        {
            Id = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            HostUserId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            HostName = "Kingston Corporate Homes",
            HostEmail = "host-kingston@nestystay.local",
            Title = "Kingston Business Stay",
            Location = "New Kingston, St. Andrew",
            Country = "Jamaica",
            NightlyRate = 140m,
            Currency = "USD",
            BadgeLevel = BadgeLevel.Trusted,
            GuestVerificationEnabled = true,
            InsuraGuestEnabled = true,
            CancellationPolicy = "Flexible",
            HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Trusted host", "Local business directory", "Split payments", "Messaging code"])
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-4333-8333-333333333333"),
            HostUserId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            HostName = "Montego Bay Apartments",
            HostEmail = "host-mobay@nestystay.local",
            Title = "Montego Bay Standard Apartment",
            Location = "Montego Bay, St. James",
            Country = "Jamaica",
            NightlyRate = 110m,
            Currency = "USD",
            BadgeLevel = BadgeLevel.Free,
            GuestVerificationEnabled = false,
            InsuraGuestEnabled = false,
            CancellationPolicy = "Strict",
            HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Free listing", "Calendar", "Messaging", "Host keeps 97% payout"])
        }
    ];

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordHashIterations,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2-SHA256${PasswordHashIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256")
        {
            return false;
        }

        var iterations = int.Parse(parts[1], CultureInfo.InvariantCulture);
        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] GenerateSecret() => RandomNumberGenerator.GetBytes(20);

    private static bool VerifyTotp(byte[] secret, string code, DateTimeOffset now)
    {
        var normalizedCode = code.Trim();
        return GenerateTotp(secret, now.AddSeconds(-TotpStepSeconds)) == normalizedCode ||
               GenerateTotp(secret, now) == normalizedCode ||
               GenerateTotp(secret, now.AddSeconds(TotpStepSeconds)) == normalizedCode;
    }

    private static string GenerateTotp(byte[] secret, DateTimeOffset timestamp)
    {
        var counter = timestamp.ToUnixTimeSeconds() / TotpStepSeconds;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0f;
        var binaryCode =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);

        return (binaryCode % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private sealed record PendingNotification(string RecipientType, NotificationMessage Message);
}
