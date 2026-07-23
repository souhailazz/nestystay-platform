using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
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
    IAccessTokenService? accessTokenService = null,
    IGoogleIdentityValidator? googleIdentityValidator = null,
    IEmailSender? emailSender = null,
    IDevelopmentAuthSecretStore? developmentAuthSecrets = null) : IPhaseOneStore
{
    private const int PasswordHashIterations = 120_000;
    private const int TotpStepSeconds = 30;
    private const int MaximumLoginAttempts = 5;
    private const int MaximumChallengeAttempts = 5;
    private const string PaymentOperationAuthorize = "Authorize";
    private const string PaymentOperationCapture = "Capture";
    private const string PaymentOperationRefund = "Refund";
    private const string PasswordResetStatusPending = "Pending";
    private const string PasswordResetStatusCompleted = "Completed";
    private const string PasswordResetStatusExpired = "Expired";
    private const string PasswordResetStatusFailed = "Failed";
    private const string PasswordResetStatusInvalidated = "Invalidated";
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan TwoFactorEnrollmentLifetime = TimeSpan.FromMinutes(10);
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

        var now = timeProvider.GetUtcNow();
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaximumLoginAttempts)
                {
                    user.LockoutEndsAt = now.Add(LoginLockoutDuration);
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            throw new InvalidOperationException("Invalid email or password.");
        }

        if (user.LockoutEndsAt is not null && user.LockoutEndsAt > now)
        {
            throw new InvalidOperationException("Account is temporarily locked. Try again later.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        var previousChallenges = db.MilestoneTwoFactorChallenges.Where(item => item.UserId == user.Id);
        db.MilestoneTwoFactorChallenges.RemoveRange(previousChallenges);

        var expiresAt = now.AddMinutes(10);
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

    public async Task<BeginTwoFactorEnrollmentResponse> BeginTwoFactorEnrollmentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var now = timeProvider.GetUtcNow();
        var secret = GenerateSecret();
        var manualKey = ToBase32(secret);
        user.PendingTwoFactorEnrollmentId = Guid.NewGuid().ToString("N");
        user.PendingTwoFactorSecret = secret;
        user.PendingTwoFactorExpiresAt = now.Add(TwoFactorEnrollmentLifetime);
        await db.SaveChangesAsync(cancellationToken);

        return new BeginTwoFactorEnrollmentResponse(
            user.PendingTwoFactorEnrollmentId,
            manualKey,
            BuildOtpAuthUri(user.Email, manualKey),
            user.PendingTwoFactorExpiresAt.Value);
    }

    public async Task<ConfirmTwoFactorEnrollmentResponse> ConfirmTwoFactorEnrollmentAsync(
        Guid userId,
        ConfirmTwoFactorEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        if (user.PendingTwoFactorSecret is null ||
            user.PendingTwoFactorExpiresAt is null ||
            user.PendingTwoFactorEnrollmentId is null ||
            !user.PendingTwoFactorEnrollmentId.Equals(request.EnrollmentId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Authenticator enrollment was not found.");
        }

        var now = timeProvider.GetUtcNow();
        if (user.PendingTwoFactorExpiresAt <= now)
        {
            ClearPendingTwoFactorEnrollment(user);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Authenticator enrollment has expired.");
        }

        if (!TryVerifyTotp(user.PendingTwoFactorSecret, request.Code, now, out var acceptedCounter))
        {
            throw new InvalidOperationException("Authenticator code is invalid.");
        }

        user.TwoFactorSecret = user.PendingTwoFactorSecret;
        user.LastAcceptedTotpCounter = acceptedCounter;
        ClearPendingTwoFactorEnrollment(user);

        var existing = await db.MilestoneRecoveryCodes.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken);
        db.MilestoneRecoveryCodes.RemoveRange(existing);
        var codes = Enumerable.Range(0, 8).Select(_ => GenerateRecoveryCode()).ToList();
        foreach (var code in codes)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            db.MilestoneRecoveryCodes.Add(new MilestoneRecoveryCode
            {
                UserId = user.Id,
                CodeHash = HashBoundSecret("RecoveryCode", user.Id, user.Id.ToString("N"), code, salt),
                SecretSalt = Convert.ToBase64String(salt),
                CreatedByUserId = user.Id
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ConfirmTwoFactorEnrollmentResponse(true, codes);
    }

    public async Task<GoogleSignInResponse> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        if (googleIdentityValidator is null || !googleIdentityValidator.IsConfigured)
        {
            throw new InvalidOperationException("Google sign-in is unavailable until server-side OAuth validation is configured.");
        }

        var identity = await googleIdentityValidator.ValidateAsync(request.Credential, cancellationToken);
        if (!identity.EmailVerified)
        {
            throw new InvalidOperationException("Google account email must be verified.");
        }

        var email = NormalizeGoogleEmail(identity.Email);
        var displayName = NormalizeGoogleDisplayName(identity.DisplayName, email);
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(
            item => item.NormalizedEmail == email,
            cancellationToken);

        if (user is null)
        {
            var role = ResolveSocialRegistrationRole(request.Role);
            user = new MilestoneUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email,
                PasswordHash = HashPassword(CreateExternalPasswordSeed(identity.Subject, email)),
                DisplayName = displayName,
                Phone = null,
                TwoFactorSecret = GenerateSecret(),
                RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>([role])
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
        var now = timeProvider.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(request.Code) &&
            await TryConsumeRecoveryCodeAsync(user.Id, request.Code, now, cancellationToken))
        {
            db.MilestoneTwoFactorChallenges.Remove(challenge);
            await db.SaveChangesAsync(cancellationToken);

            var recoveryTokenExpiresAt = now.AddHours(8);
            var recoveryRoles = MilestoneJson.DeserializeList<UserRole>(user.RolesJson);
            return new VerifyTwoFactorResponse(
                user.Id,
                _accessTokenService.Issue(user.Id, recoveryRoles, recoveryTokenExpiresAt),
                recoveryTokenExpiresAt,
                recoveryRoles);
        }

        if (string.IsNullOrWhiteSpace(request.Code) ||
            !TryVerifyTotp(user.TwoFactorSecret, request.Code, now, out var acceptedCounter) ||
            user.LastAcceptedTotpCounter is not null && acceptedCounter <= user.LastAcceptedTotpCounter.Value)
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= MaximumChallengeAttempts)
            {
                db.MilestoneTwoFactorChallenges.Remove(challenge);
            }

            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Invalid 2FA code.");
        }

        db.MilestoneTwoFactorChallenges.Remove(challenge);
        user.LastAcceptedTotpCounter = acceptedCounter;
        await db.SaveChangesAsync(cancellationToken);

        var tokenExpiresAt = now.AddHours(8);
        var roles = MilestoneJson.DeserializeList<UserRole>(user.RolesJson);
        return new VerifyTwoFactorResponse(
            user.Id,
            _accessTokenService.Issue(user.Id, roles, tokenExpiresAt),
            tokenExpiresAt,
            roles);
    }

    public async Task<PasswordResetRequestResponse> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizePasswordResetEmail(request.Email);
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(PasswordResetLifetime);
        var requestId = Guid.NewGuid();
        var requestIpHash = HashOpaque(string.IsNullOrWhiteSpace(request.RequestIp) ? "unknown" : request.RequestIp.Trim());
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(
            item => item.NormalizedEmail == email,
            cancellationToken);
        string? token = null;

        if (user is not null)
        {
            var pendingResets = await db.MilestoneAuthFlows
                .Where(item =>
                    item.UserId == user.Id &&
                    item.FlowType == "PasswordReset" &&
                    item.Status == PasswordResetStatusPending &&
                    !item.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var pending in pendingResets)
            {
                pending.Status = PasswordResetStatusInvalidated;
                pending.InvalidatedAt = now;
                pending.UpdatedAt = now;
            }
        }

        token = GenerateSecureToken();
        var salt = RandomNumberGenerator.GetBytes(16);
        var flow = new MilestoneAuthFlow
        {
            Id = requestId,
            UserId = user?.Id,
            FlowType = "PasswordReset",
            Destination = email,
            NormalizedDestination = email,
            DestinationHash = HashOpaque(email),
            CodeHash = HashBoundSecret("PasswordResetCode", user?.Id, email, GenerateTotp(salt, now), salt),
            TokenHash = HashBoundSecret("PasswordReset", user?.Id, email, token, salt),
            SecretSalt = Convert.ToBase64String(salt),
            Status = PasswordResetStatusPending,
            DeliveryChannel = "Email",
            RequestIpHash = requestIpHash,
            ExpiresAt = expiresAt,
            LastSentAt = now
        };
        db.MilestoneAuthFlows.Add(flow);
        await db.SaveChangesAsync(cancellationToken);

        if (user is not null)
        {
            developmentAuthSecrets?.Store(new DevelopmentAuthSecret(
                flow.Id,
                email,
                "Email",
                string.Empty,
                token,
                expiresAt,
                now));
            if (emailSender is not null)
            {
                await emailSender.SendAsync(
                    new EmailMessage(
                        email,
                        "NestyStay password reset",
                        $"Use this NestyStay password reset token: {token}. It expires at {expiresAt:O}.",
                        flow.Id),
                    cancellationToken);
            }
        }

        return new PasswordResetRequestResponse(
            requestId.ToString("N"),
            "If an account exists for that email, password reset instructions have been sent.",
            expiresAt);
    }

    public Task<DevelopmentPasswordResetTokenResponse?> GetDevelopmentPasswordResetTokenAsync(string requestId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestId, out var correlationId))
        {
            return Task.FromResult<DevelopmentPasswordResetTokenResponse?>(null);
        }

        var secret = developmentAuthSecrets?.Get(correlationId);
        if (secret is null || secret.ExpiresAt < timeProvider.GetUtcNow())
        {
            return Task.FromResult<DevelopmentPasswordResetTokenResponse?>(null);
        }

        return Task.FromResult<DevelopmentPasswordResetTokenResponse?>(new DevelopmentPasswordResetTokenResponse(
            requestId,
            secret.Token,
            secret.ExpiresAt));
    }

    public async Task<CompletePasswordResetResponse> CompletePasswordResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        ValidatePasswordPolicy(request.NewPassword);
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Password confirmation must match.");
        }

        if (!Guid.TryParse(request.RequestId, out var requestId))
        {
            throw new InvalidOperationException("Password reset token is invalid.");
        }

        var reset = await db.MilestoneAuthFlows.SingleOrDefaultAsync(
            item => item.Id == requestId && item.FlowType == "PasswordReset" && !item.IsDeleted,
            cancellationToken);
        if (reset is null || reset.Status == PasswordResetStatusFailed || reset.Status == PasswordResetStatusInvalidated)
        {
            throw new InvalidOperationException("Password reset token is invalid.");
        }

        if (reset.Status == PasswordResetStatusCompleted)
        {
            throw new InvalidOperationException("Password reset token was already used.");
        }

        var now = timeProvider.GetUtcNow();
        if (reset.ExpiresAt < now)
        {
            reset.Status = PasswordResetStatusExpired;
            reset.UpdatedAt = now;
            developmentAuthSecrets?.Remove(reset.Id);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Password reset token has expired.");
        }

        var user = reset.UserId is null
            ? null
            : await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == reset.UserId, cancellationToken);
        if (user is null)
        {
            reset.Status = PasswordResetStatusFailed;
            reset.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Password reset token is invalid.");
        }

        var salt = Convert.FromBase64String(reset.SecretSalt);
        var actualHash = HashBoundSecret("PasswordReset", user.Id, reset.NormalizedDestination, request.Token.Trim(), salt);
        if (!FixedTimeEquals(actualHash, reset.TokenHash))
        {
            reset.Status = PasswordResetStatusFailed;
            reset.UpdatedAt = now;
            developmentAuthSecrets?.Remove(reset.Id);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Password reset token is invalid.");
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        user.SessionInvalidatedAt = now;
        reset.Status = PasswordResetStatusCompleted;
        reset.CompletedAt = now;
        reset.UpdatedAt = now;
        db.MilestoneTwoFactorChallenges.RemoveRange(db.MilestoneTwoFactorChallenges.Where(item => item.UserId == user.Id));
        var pendingUserResets = await db.MilestoneAuthFlows
            .Where(item =>
                item.UserId == user.Id &&
                item.FlowType == "PasswordReset" &&
                item.Status == PasswordResetStatusPending &&
                item.Id != reset.Id &&
                !item.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var pending in pendingUserResets)
        {
            pending.Status = PasswordResetStatusInvalidated;
            pending.InvalidatedAt = now;
            pending.UpdatedAt = now;
        }

        developmentAuthSecrets?.Remove(reset.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new CompletePasswordResetResponse(PasswordResetStatusCompleted, true);
    }

    public async Task<bool> IsSessionActiveAsync(Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user?.SessionInvalidatedAt is null || issuedAt > user.SessionInvalidatedAt.Value;
    }

    public IReadOnlyList<PropertyListingDto> GetProperties()
    {
        EnsurePhaseOneSeeded();
        return db.MilestoneProperties
            .AsNoTracking()
            .Where(property => !property.IsDeleted && !property.IsArchived)
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
            .SingleOrDefault(property => property.Id == id && !property.IsDeleted && !property.IsArchived) is { } property
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

    public async Task<PropertyListingDto> UpdatePropertyAsync(Guid hostUserId, Guid propertyId, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        ValidateProperty(request);

        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        ApplyPropertyChanges(property, request);
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;

        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    public async Task<PropertyListingDto> ArchivePropertyAsync(Guid hostUserId, Guid propertyId, bool isArchived, CancellationToken cancellationToken)
    {
        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        property.IsArchived = isArchived;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;

        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    public async Task DeletePropertyAsync(Guid hostUserId, Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        var hasActiveBookings = await db.MilestoneBookings.AnyAsync(
            booking =>
                booking.PropertyId == propertyId &&
                !booking.IsDeleted &&
                (booking.Status == BookingStatus.PendingVerification ||
                 booking.Status == BookingStatus.Approved ||
                 booking.Status == BookingStatus.PaymentCaptured ||
                 booking.Status == BookingStatus.Confirmed),
            cancellationToken);
        if (hasActiveBookings)
        {
            throw new InvalidOperationException("Properties with active bookings cannot be deleted.");
        }

        property.IsDeleted = true;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;
        await db.SaveChangesAsync(cancellationToken);
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

    public async Task<BookingDocumentDto?> GetBookingInvoiceAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await ExpirePendingHoldsAsync(now, cancellationToken);
        var booking = await db.MilestoneBookings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        return booking is null ? null : BookingDocumentRenderer.RenderInvoice(ToDto(booking), now);
    }

    public async Task<BookingDocumentDto?> GetBookingReceiptAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await ExpirePendingHoldsAsync(now, cancellationToken);
        var booking = await db.MilestoneBookings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        return booking is null ? null : BookingDocumentRenderer.RenderReceipt(ToDto(booking), now);
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

        var idempotencyKey = BuildPaymentIdempotencyKey(booking.Id, PaymentOperationCapture);
        var attempt = await BeginPaymentAttemptAsync(booking.Id, PaymentOperationCapture, idempotencyKey, booking.TotalAmount, booking.Currency, cancellationToken);
        PaymentCaptureResult capture;
        try
        {
            capture = await paymentGateway.CaptureAsync(
                new PaymentCaptureRequest(
                    booking.PaymentAuthorizationReference,
                    booking.TotalAmount,
                    booking.Currency,
                    idempotencyKey),
                cancellationToken);
        }
        catch (Exception exception)
        {
            await FailPaymentAttemptAsync(attempt, exception, cancellationToken);
            throw;
        }

        booking.PaymentProvider = capture.ProviderName;
        booking.PaymentCaptureReference = capture.CaptureReference;
        booking.PaymentStatus = capture.Status;
        CompletePaymentAttempt(attempt, capture.ProviderName, capture.CaptureReference, capture.Status);
        AddTimeline(booking, "Stripe payment captured after approval");
        await QueueNotificationsAsync(booking, BuildPaymentCapturedNotifications(booking), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(booking);
    }

    public async Task<BookingDto?> RefundPaymentAsync(Guid bookingId, RefundBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        if (booking.PaymentStatus == PaymentStatus.Refunded)
        {
            return ToDto(booking);
        }

        if (booking.PaymentStatus != PaymentStatus.Captured)
        {
            throw new InvalidOperationException("Refunds require a captured payment.");
        }

        if (string.IsNullOrWhiteSpace(booking.PaymentCaptureReference))
        {
            throw new InvalidOperationException("Refunds require a payment capture reference.");
        }

        var amount = BookingRefundPolicy.ResolveAmount(request.Amount, booking.TotalAmount, booking.RefundedAmount);
        var reason = BookingRefundPolicy.NormalizeReason(request.Reason);
        var idempotencyKey = BookingRefundPolicy.ResolveIdempotencyKey(booking.Id, amount, request.IdempotencyKey);
        var attempt = await BeginPaymentAttemptAsync(booking.Id, PaymentOperationRefund, idempotencyKey, amount, booking.Currency, cancellationToken);
        if (attempt.CompletedAt is not null && attempt.Status == PaymentStatus.Refunded)
        {
            return ToDto(booking);
        }

        PaymentRefundResult refund;
        try
        {
            refund = await paymentGateway.RefundAsync(
                new PaymentRefundRequest(
                    booking.PaymentCaptureReference,
                    amount,
                    booking.Currency,
                    reason,
                    idempotencyKey),
                cancellationToken);
        }
        catch (Exception exception)
        {
            await FailPaymentAttemptAsync(attempt, exception, cancellationToken);
            throw;
        }

        booking.PaymentProvider = refund.ProviderName;
        booking.PaymentRefundReference = refund.RefundReference;
        if (refund.Status == PaymentStatus.Refunded)
        {
            booking.RefundedAmount = decimal.Round(booking.RefundedAmount + refund.RefundedAmount, 2, MidpointRounding.AwayFromZero);
            booking.RefundReason = reason;
            booking.RefundedAt = refund.RefundedAt;
            if (BookingRefundPolicy.IsFullyRefunded(booking.TotalAmount, booking.RefundedAmount))
            {
                booking.PaymentStatus = PaymentStatus.Refunded;
            }
        }

        CompletePaymentAttempt(attempt, refund.ProviderName, refund.RefundReference, refund.Status);
        AddTimeline(
            booking,
            $"Stripe refund {ToApiStatus(refund.Status)} for {refund.Currency.ToUpperInvariant()} {refund.RefundedAmount:0.00}",
            $"Refund reason: {reason}");
        if (refund.Status == PaymentStatus.Refunded)
        {
            await QueueNotificationsAsync(booking, BuildPaymentRefundedNotifications(booking, refund.RefundedAmount, refund.Currency), cancellationToken);
        }

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

        var idempotencyKey = BuildPaymentIdempotencyKey(booking.Id, PaymentOperationAuthorize);
        var attempt = await BeginPaymentAttemptAsync(booking.Id, PaymentOperationAuthorize, idempotencyKey, booking.TotalAmount, booking.Currency, cancellationToken);
        PaymentAuthorizationResult authorization;
        try
        {
            authorization = await paymentGateway.AuthorizeAsync(
                new PaymentAuthorizationRequest(
                    booking.Id,
                    booking.TotalAmount,
                    booking.Currency,
                    $"NestyStay booking {booking.Id:N}",
                    idempotencyKey),
                cancellationToken);
        }
        catch (Exception exception)
        {
            await FailPaymentAttemptAsync(attempt, exception, cancellationToken);
            throw;
        }

        booking.PaymentProvider = authorization.ProviderName;
        booking.PaymentAuthorizationReference = authorization.AuthorizationReference;
        booking.PaymentClientSecret = authorization.ClientSecret;
        booking.PaymentStatus = authorization.Status;
        CompletePaymentAttempt(attempt, authorization.ProviderName, authorization.AuthorizationReference, authorization.Status);
        AddTimeline(booking, "Stripe manual-capture payment authorized after approval");
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MilestonePaymentAttempt> BeginPaymentAttemptAsync(
        Guid bookingId,
        string operation,
        string idempotencyKey,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        var attempt = await db.MilestonePaymentAttempts.SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
        if (attempt is null)
        {
            attempt = new MilestonePaymentAttempt
            {
                BookingId = bookingId,
                Operation = operation,
                IdempotencyKey = idempotencyKey,
                Amount = amount,
                Currency = currency,
                Status = PaymentStatus.Pending
            };
            db.MilestonePaymentAttempts.Add(attempt);
            await db.SaveChangesAsync(cancellationToken);
        }

        return attempt;
    }

    private void CompletePaymentAttempt(MilestonePaymentAttempt attempt, string provider, string providerReference, PaymentStatus status)
    {
        attempt.Provider = provider;
        attempt.ProviderReference = providerReference;
        attempt.Status = status;
        attempt.FailureReason = string.Empty;
        attempt.CompletedAt = timeProvider.GetUtcNow();
        attempt.UpdatedAt = attempt.CompletedAt.Value;
    }

    private async Task FailPaymentAttemptAsync(MilestonePaymentAttempt attempt, Exception exception, CancellationToken cancellationToken)
    {
        attempt.Status = PaymentStatus.Failed;
        attempt.FailureReason = exception.Message;
        attempt.CompletedAt = timeProvider.GetUtcNow();
        attempt.UpdatedAt = attempt.CompletedAt.Value;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildPaymentIdempotencyKey(Guid bookingId, string operation) =>
        $"booking:{bookingId:N}:{operation.ToLowerInvariant()}";

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
        await db.MilestoneProperties.SingleOrDefaultAsync(property => property.Id == propertyId && !property.IsDeleted && !property.IsArchived, cancellationToken)
        ?? throw new InvalidOperationException("Property not found.");

    private async Task<MilestoneProperty> FindHostPropertyAsync(Guid hostUserId, Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Property not found.");
        if (property.HostUserId != hostUserId)
        {
            throw new UnauthorizedAccessException("Property is not available to this host.");
        }

        return property;
    }

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

    private static IReadOnlyList<PendingNotification> BuildPaymentRefundedNotifications(MilestoneBooking booking, decimal amount, string currency) =>
    [
        new("guest", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay payment refunded",
            $"Refund of {currency.ToUpperInvariant()} {amount:0.00} has been issued for {booking.PropertyTitle}.")),
        new("host", new NotificationMessage(
            booking.HostEmail,
            "NestyStay payment refunded",
            $"Refund of {currency.ToUpperInvariant()} {amount:0.00} has been issued for {booking.GuestName}'s booking."))
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
            MilestoneJson.DeserializeList<string>(property.HighlightsJson),
            property.IsArchived);

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
            booking.PaymentRefundReference,
            booking.RefundedAmount,
            booking.RefundReason,
            booking.RefundedAt,
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

        ValidatePasswordPolicy(request.Password);

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

    private static string NormalizePasswordResetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("A valid email address is required.");
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return address.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("A valid email address is required.");
        }
    }

    private static void ValidatePasswordPolicy(string password)
    {
        if (password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new InvalidOperationException("Password must be at least 8 characters and include uppercase, lowercase, and a number.");
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

    private static UserRole ResolveSocialRegistrationRole(UserRole? role)
    {
        if (role is not (UserRole.Guest or UserRole.Host))
        {
            throw new InvalidOperationException("Role confirmation is required before creating a social account.");
        }

        return role.Value;
    }

    private static void ValidateProperty(CreatePropertyRequest request)
    {
        if (request.HostUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Host, title, location, currency, and cancellation policy are required.");
        }

        ValidatePropertyFields(
            request.HostName,
            request.HostEmail,
            request.Title,
            request.Location,
            request.Currency,
            request.CancellationPolicy,
            request.NightlyRate,
            request.GuestVerificationEnabled,
            request.BadgeLevel);
    }

    private static void ValidateProperty(UpdatePropertyRequest request)
    {
        ValidatePropertyFields(
            request.HostName,
            request.HostEmail,
            request.Title,
            request.Location,
            request.Currency,
            request.CancellationPolicy,
            request.NightlyRate,
            request.GuestVerificationEnabled,
            request.BadgeLevel);
    }

    private static void ValidatePropertyFields(
        string hostName,
        string hostEmail,
        string title,
        string location,
        string currency,
        string cancellationPolicy,
        decimal nightlyRate,
        bool guestVerificationEnabled,
        BadgeLevel badgeLevel)
    {
        if (string.IsNullOrWhiteSpace(hostName) ||
            string.IsNullOrWhiteSpace(hostEmail) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(location) ||
            string.IsNullOrWhiteSpace(currency) ||
            string.IsNullOrWhiteSpace(cancellationPolicy))
        {
            throw new InvalidOperationException("Host, title, location, currency, and cancellation policy are required.");
        }

        try
        {
            _ = new MailAddress(hostEmail.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("A valid host email address is required.");
        }

        if (nightlyRate <= 0)
        {
            throw new InvalidOperationException("Nightly rate must be greater than zero.");
        }

        if (currency.Trim().Length != 3)
        {
            throw new InvalidOperationException("Currency must be a three-letter code.");
        }

        if (guestVerificationEnabled && badgeLevel == BadgeLevel.Free)
        {
            throw new InvalidOperationException("Guest verification upsell requires a Verified, Trusted, or Wellness host badge.");
        }
    }

    private static void ApplyPropertyChanges(MilestoneProperty property, UpdatePropertyRequest request)
    {
        property.HostName = request.HostName.Trim();
        property.HostEmail = request.HostEmail.Trim().ToLowerInvariant();
        property.Title = request.Title.Trim();
        property.Location = request.Location.Trim();
        property.Country = string.IsNullOrWhiteSpace(request.Country) ? "Jamaica" : request.Country.Trim();
        property.NightlyRate = decimal.Round(request.NightlyRate, 2);
        property.Currency = request.Currency.Trim().ToUpperInvariant();
        property.BadgeLevel = request.BadgeLevel;
        property.GuestVerificationEnabled = request.GuestVerificationEnabled;
        property.InsuraGuestEnabled = request.InsuraGuestEnabled;
        property.CancellationPolicy = request.CancellationPolicy.Trim();
        property.HighlightsJson = MilestoneJson.Serialize(NormalizeHighlights(request.Highlights));
    }

    private static IReadOnlyList<string> NormalizeHighlights(IReadOnlyList<string>? highlights) =>
        highlights is null || highlights.Count == 0
            ? ["Host-created listing"]
            : highlights.Select(item => item.Trim()).Where(item => item.Length > 0).ToList();

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

    private static string GenerateRecoveryCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        return $"{Convert.ToHexString(bytes[..4])}-{Convert.ToHexString(bytes[4..])}";
    }

    private async Task<bool> TryConsumeRecoveryCodeAsync(
        Guid userId,
        string code,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken)
    {
        var candidates = await db.MilestoneRecoveryCodes
            .Where(item => item.UserId == userId && item.UsedAt == null && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        var normalizedCode = code.Trim();
        foreach (var candidate in candidates)
        {
            var salt = Convert.FromBase64String(candidate.SecretSalt);
            var actualHash = HashBoundSecret("RecoveryCode", userId, userId.ToString("N"), normalizedCode, salt);
            if (!FixedTimeEquals(actualHash, candidate.CodeHash))
            {
                continue;
            }

            candidate.UsedAt = usedAt;
            candidate.UpdatedAt = usedAt;
            return true;
        }

        return false;
    }

    private static string GenerateSecureToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string HashBoundSecret(string purpose, Guid? userId, string destination, string secret, byte[] salt)
    {
        var binding = $"{purpose}|{userId?.ToString("N") ?? "anonymous"}|{destination}|{secret}";
        using var hmac = new HMACSHA256(salt);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(binding)));
    }

    private static bool FixedTimeEquals(string firstBase64, string secondBase64)
    {
        var first = Convert.FromBase64String(firstBase64);
        var second = Convert.FromBase64String(secondBase64);
        return first.Length == second.Length && CryptographicOperations.FixedTimeEquals(first, second);
    }

    private static string HashOpaque(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant())));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ToBase32(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder();
        var bitBuffer = 0;
        var bitCount = 0;
        foreach (var value in bytes)
        {
            bitBuffer = (bitBuffer << 8) | value;
            bitCount += 8;
            while (bitCount >= 5)
            {
                output.Append(alphabet[(bitBuffer >> (bitCount - 5)) & 31]);
                bitCount -= 5;
            }
        }

        if (bitCount > 0)
        {
            output.Append(alphabet[(bitBuffer << (5 - bitCount)) & 31]);
        }

        return output.ToString();
    }

    private static string BuildOtpAuthUri(string email, string manualKey)
    {
        var label = Uri.EscapeDataString($"NestyStay:{email}");
        var issuer = Uri.EscapeDataString("NestyStay");
        return $"otpauth://totp/{label}?secret={manualKey}&issuer={issuer}&algorithm=SHA1&digits=6&period={TotpStepSeconds}";
    }

    private static void ClearPendingTwoFactorEnrollment(MilestoneUser user)
    {
        user.PendingTwoFactorEnrollmentId = null;
        user.PendingTwoFactorSecret = null;
        user.PendingTwoFactorExpiresAt = null;
    }

    private static bool TryVerifyTotp(byte[] secret, string code, DateTimeOffset now, out long acceptedCounter)
    {
        var normalizedCode = code.Trim();
        var currentCounter = GetTotpCounter(now);
        foreach (var counter in new[] { currentCounter - 1, currentCounter, currentCounter + 1 })
        {
            if (GenerateTotp(secret, counter) == normalizedCode)
            {
                acceptedCounter = counter;
                return true;
            }
        }

        acceptedCounter = 0;
        return false;
    }

    private static string GenerateTotp(byte[] secret, DateTimeOffset timestamp)
    {
        var counter = GetTotpCounter(timestamp);
        return GenerateTotp(secret, counter);
    }

    private static long GetTotpCounter(DateTimeOffset timestamp) =>
        timestamp.ToUnixTimeSeconds() / TotpStepSeconds;

    private static string GenerateTotp(byte[] secret, long counter)
    {
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
