using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Application.PhaseOne;

public interface IPhaseOneStore
{
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<DevelopmentAuthCodeResponse?> GetDevelopmentTwoFactorCodeAsync(string challengeId, CancellationToken cancellationToken);
    Task<GoogleSignInResponse> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken);
    Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken);
    Task<PasswordResetRequestResponse> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken);
    Task<DevelopmentPasswordResetTokenResponse?> GetDevelopmentPasswordResetTokenAsync(string requestId, CancellationToken cancellationToken);
    Task<CompletePasswordResetResponse> CompletePasswordResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken);
    IReadOnlyList<PropertyListingDto> GetProperties();
    PropertyListingDto? GetProperty(Guid id);
    Task<PropertyListingDto> CreatePropertyAsync(CreatePropertyRequest request, CancellationToken cancellationToken);
    Task<BookingQuoteDto> QuoteBookingAsync(BookingQuoteRequest request, CancellationToken cancellationToken);
    IReadOnlyList<BookingDto> GetBookings(Guid? guestUserId = null);
    BookingDto? GetBooking(Guid id);
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken);
    Task<BookingDto?> ResolveVerificationAsync(Guid bookingId, ResolveVerificationRequest request, CancellationToken cancellationToken);
    Task<BookingDto?> CapturePaymentAsync(Guid bookingId, CancellationToken cancellationToken);
}

public sealed class PhaseOneStore(
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
    private const string PasswordResetStatusPending = "Pending";
    private const string PasswordResetStatusCompleted = "Completed";
    private const string PasswordResetStatusExpired = "Expired";
    private const string PasswordResetStatusFailed = "Failed";
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    private readonly IAccessTokenService _accessTokenService = accessTokenService ?? DevelopmentAccessTokenService.Instance;
    private readonly object _gate = new();
    private readonly List<PhaseOneUser> _users = [];
    private readonly List<PhaseOneChallenge> _challenges = [];
    private readonly List<PhaseOnePasswordReset> _passwordResets = [];
    private readonly List<PhaseOneBooking> _bookings = [];
    private readonly List<PhaseOneProperty> _properties =
    [
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            "Island Villa Hosting",
            "host-villa@nestystay.local",
            "Ocho Rios Verified Villa",
            "Ocho Rios, St. Ann",
            "Jamaica",
            185m,
            "USD",
            BadgeLevel.Verified,
            true,
            true,
            "Moderate",
            ["Alibaba eKYC", "QR gate access", "InsuraGuest available", "Emergency 119 displayed"]),
        new(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            "Kingston Corporate Homes",
            "host-kingston@nestystay.local",
            "Kingston Business Stay",
            "New Kingston, St. Andrew",
            "Jamaica",
            140m,
            "USD",
            BadgeLevel.Trusted,
            true,
            true,
            "Flexible",
            ["Trusted host", "Local business directory", "Split payments", "Messaging code"]),
        new(
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            "Montego Bay Apartments",
            "host-mobay@nestystay.local",
            "Montego Bay Standard Apartment",
            "Montego Bay, St. James",
            "Jamaica",
            110m,
            "USD",
            BadgeLevel.Free,
            false,
            false,
            "Strict",
            ["Free listing", "Calendar", "Messaging", "Host keeps 97% payout"])
    ];

    public Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        ValidateRegistration(request);
        var email = request.Email.Trim().ToLowerInvariant();

        lock (_gate)
        {
            var existing = _users.SingleOrDefault(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var user = new PhaseOneUser(
                Guid.NewGuid(),
                email,
                HashPassword(request.Password),
                request.DisplayName.Trim(),
                request.Phone?.Trim(),
                GenerateSecret(),
                [request.Role]);

            _users.Add(user);
            return Task.FromResult(new RegisterUserResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                true));
        }
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var now = timeProvider.GetUtcNow();
            var user = _users.SingleOrDefault(item => item.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
            if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                if (user is not null)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= MaximumLoginAttempts)
                    {
                        user.LockoutEndsAt = now.Add(LoginLockoutDuration);
                    }
                }

                throw new InvalidOperationException("Invalid email or password.");
            }

            if (user.LockoutEndsAt is not null && user.LockoutEndsAt > now)
            {
                throw new InvalidOperationException("Account is temporarily locked. Try again later.");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEndsAt = null;
            _challenges.RemoveAll(item => item.UserId == user.Id);

            var expiresAt = now.AddMinutes(10);
            var challenge = new PhaseOneChallenge(Guid.NewGuid().ToString("N"), user.Id, expiresAt);
            _challenges.Add(challenge);

            return Task.FromResult(new LoginResponse(
                user.Id,
                user.Email,
                true,
                challenge.Id,
                expiresAt));
        }
    }

    public Task<DevelopmentAuthCodeResponse?> GetDevelopmentTwoFactorCodeAsync(string challengeId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var challenge = _challenges.SingleOrDefault(item => item.Id == challengeId);
            if (challenge is null || challenge.ExpiresAt < timeProvider.GetUtcNow())
            {
                return Task.FromResult<DevelopmentAuthCodeResponse?>(null);
            }

            var user = _users.Single(item => item.Id == challenge.UserId);
            return Task.FromResult<DevelopmentAuthCodeResponse?>(new DevelopmentAuthCodeResponse(
                challenge.Id,
                GenerateTotp(user.TwoFactorSecret, timeProvider.GetUtcNow()),
                challenge.ExpiresAt));
        }
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

        lock (_gate)
        {
            var user = _users.SingleOrDefault(item => item.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                var role = ResolveSocialRegistrationRole(request.Role);
                user = new PhaseOneUser(
                    Guid.NewGuid(),
                    email,
                    HashPassword(CreateExternalPasswordSeed(identity.Subject, email)),
                    displayName,
                    null,
                    GenerateSecret(),
                    [role]);
                _users.Add(user);
            }

            var tokenExpiresAt = timeProvider.GetUtcNow().AddHours(8);
            return new GoogleSignInResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                _accessTokenService.Issue(user.Id, user.Roles, tokenExpiresAt),
                tokenExpiresAt,
                user.Roles,
                "Google");
        }
    }

    public Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var challenge = _challenges.SingleOrDefault(item => item.Id == request.ChallengeId);
            if (challenge is null || challenge.ExpiresAt < timeProvider.GetUtcNow())
            {
                throw new InvalidOperationException("Invalid or expired 2FA challenge.");
            }

            var user = _users.Single(item => item.Id == challenge.UserId);
            var now = timeProvider.GetUtcNow();
            if (string.IsNullOrWhiteSpace(request.Code) ||
                !TryVerifyTotp(user.TwoFactorSecret, request.Code, now, out var acceptedCounter) ||
                user.LastAcceptedTotpCounter is not null && acceptedCounter <= user.LastAcceptedTotpCounter.Value)
            {
                challenge.FailedAttempts++;
                if (challenge.FailedAttempts >= MaximumChallengeAttempts)
                {
                    _challenges.Remove(challenge);
                }

                throw new InvalidOperationException("Invalid 2FA code.");
            }

            _challenges.Remove(challenge);
            user.LastAcceptedTotpCounter = acceptedCounter;
            var tokenExpiresAt = now.AddHours(8);

            return Task.FromResult(new VerifyTwoFactorResponse(
                user.Id,
                _accessTokenService.Issue(user.Id, user.Roles, tokenExpiresAt),
                tokenExpiresAt,
                user.Roles));
        }
    }

    public async Task<PasswordResetRequestResponse> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizePasswordResetEmail(request.Email);
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(PasswordResetLifetime);
        var requestId = Guid.NewGuid();
        string? token = null;
        PhaseOneUser? user;

        lock (_gate)
        {
            user = _users.SingleOrDefault(item => item.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user is not null)
            {
                foreach (var pending in _passwordResets.Where(item => item.UserId == user.Id && item.Status == PasswordResetStatusPending))
                {
                    pending.Status = "Invalidated";
                    pending.InvalidatedAt = now;
                }

                token = GenerateSecureToken();
                var salt = RandomNumberGenerator.GetBytes(16);
                _passwordResets.Add(new PhaseOnePasswordReset(
                    requestId.ToString("N"),
                    user.Id,
                    email,
                    Convert.ToBase64String(salt),
                    HashBoundSecret("PasswordReset", user.Id, email, token, salt),
                    PasswordResetStatusPending,
                    expiresAt));
            }
        }

        if (user is not null && token is not null)
        {
            developmentAuthSecrets?.Store(new DevelopmentAuthSecret(
                requestId,
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
                        requestId),
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

    public Task<CompletePasswordResetResponse> CompletePasswordResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        ValidatePasswordPolicy(request.NewPassword);
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Password confirmation must match.");
        }

        lock (_gate)
        {
            var reset = _passwordResets.SingleOrDefault(item => item.RequestId.Equals(request.RequestId, StringComparison.OrdinalIgnoreCase));
            if (reset is null || reset.Status == PasswordResetStatusFailed)
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
                developmentAuthSecrets?.Remove(Guid.Parse(reset.RequestId));
                throw new InvalidOperationException("Password reset token has expired.");
            }

            var user = _users.SingleOrDefault(item => item.Id == reset.UserId)
                ?? throw new InvalidOperationException("Password reset token is invalid.");
            var salt = Convert.FromBase64String(reset.SecretSalt);
            var tokenHash = HashBoundSecret("PasswordReset", user.Id, reset.Email, request.Token.Trim(), salt);
            if (!FixedTimeEquals(tokenHash, reset.TokenHash))
            {
                reset.Status = PasswordResetStatusFailed;
                developmentAuthSecrets?.Remove(Guid.Parse(reset.RequestId));
                throw new InvalidOperationException("Password reset token is invalid.");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEndsAt = null;
            user.SessionInvalidatedAt = now;
            reset.Status = PasswordResetStatusCompleted;
            reset.UsedAt = now;
            _challenges.RemoveAll(item => item.UserId == user.Id);
            foreach (var pending in _passwordResets.Where(item => item.UserId == user.Id && item.Status == PasswordResetStatusPending))
            {
                pending.Status = "Invalidated";
                pending.InvalidatedAt = now;
            }

            developmentAuthSecrets?.Remove(Guid.Parse(reset.RequestId));
            return Task.FromResult(new CompletePasswordResetResponse("Completed", true));
        }
    }

    public Task<bool> IsSessionActiveAsync(Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var user = _users.SingleOrDefault(item => item.Id == userId);
            return Task.FromResult(user?.SessionInvalidatedAt is null || issuedAt > user.SessionInvalidatedAt.Value);
        }
    }

    public IReadOnlyList<PropertyListingDto> GetProperties() =>
        _properties.Select(ToListingDto).ToList();

    public PropertyListingDto? GetProperty(Guid id) =>
        _properties.SingleOrDefault(property => property.Id == id) is { } property ? ToListingDto(property) : null;

    public Task<PropertyListingDto> CreatePropertyAsync(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        ValidateProperty(request);

        lock (_gate)
        {
            var property = new PhaseOneProperty(
                Guid.NewGuid(),
                request.HostUserId,
                request.HostName.Trim(),
                request.HostEmail.Trim().ToLowerInvariant(),
                request.Title.Trim(),
                request.Location.Trim(),
                string.IsNullOrWhiteSpace(request.Country) ? "Jamaica" : request.Country.Trim(),
                decimal.Round(request.NightlyRate, 2),
                request.Currency.Trim().ToUpperInvariant(),
                request.BadgeLevel,
                request.GuestVerificationEnabled,
                request.InsuraGuestEnabled,
                request.CancellationPolicy.Trim(),
                request.Highlights is null || request.Highlights.Count == 0
                    ? ["Host-created listing"]
                    : request.Highlights.Select(item => item.Trim()).Where(item => item.Length > 0).ToList());

            _properties.Add(property);
            return Task.FromResult(ToListingDto(property));
        }
    }

    public Task<BookingQuoteDto> QuoteBookingAsync(BookingQuoteRequest request, CancellationToken cancellationToken)
    {
        var property = FindProperty(request.PropertyId);
        var now = timeProvider.GetUtcNow();

        lock (_gate)
        {
            ExpirePendingHoldsNoLock(now);
            var blockingBooking = FindBlockingBookingNoLock(property.Id, request.CheckIn, request.CheckOut, now);
            if (blockingBooking is not null)
            {
                throw new InvalidOperationException("Requested dates are already held or approved for this property.");
            }

            var quote = BuildQuote(property, request.CheckIn, request.CheckOut, true, null);
            return Task.FromResult(quote);
        }
    }

    public IReadOnlyList<BookingDto> GetBookings(Guid? guestUserId = null)
    {
        lock (_gate)
        {
            ExpirePendingHoldsNoLock(timeProvider.GetUtcNow());
            return _bookings
                .Where(booking => guestUserId is null || booking.GuestUserId == guestUserId)
                .Select(ToDto)
                .ToList();
        }
    }

    public BookingDto? GetBooking(Guid id)
    {
        lock (_gate)
        {
            ExpirePendingHoldsNoLock(timeProvider.GetUtcNow());
            return _bookings.SingleOrDefault(item => item.Id == id) is { } booking ? ToDto(booking) : null;
        }
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var property = FindProperty(request.PropertyId);
        PhaseOneUser guest;
        PhaseOneBooking booking;
        var now = timeProvider.GetUtcNow();
        var quote = BuildQuote(property, request.CheckIn, request.CheckOut, true, null);

        lock (_gate)
        {
            guest = _users.SingleOrDefault(user => user.Id == request.GuestUserId)
                ?? throw new InvalidOperationException("Guest user must register before booking.");

            ExpirePendingHoldsNoLock(now);
            if (FindBlockingBookingNoLock(property.Id, request.CheckIn, request.CheckOut, now) is not null)
            {
                throw new InvalidOperationException("Requested dates are already held or approved for this property.");
            }

            var requiresVerification = property.GuestVerificationEnabled;
            booking = new PhaseOneBooking(
                Guid.NewGuid(),
                property.Id,
                property.HostUserId,
                property.HostName,
                property.HostEmail,
                guest.Id,
                guest.Email,
                guest.DisplayName,
                request.CheckIn,
                request.CheckOut,
                requiresVerification ? BookingStatus.PendingVerification : BookingStatus.Approved,
                requiresVerification ? VerificationStatus.Pending : VerificationStatus.Passed,
                PaymentStatus.Pending,
                requiresVerification,
                requiresVerification ? now.AddMinutes(NestyStayBusinessRules.DefaultBookingHoldMinutes) : null,
                quote.Nights,
                property.NightlyRate,
                quote.StaySubtotal,
                quote.GuestPlatformFee,
                quote.TotalAmount,
                property.Currency,
                property.Title,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                quote.PriceBreakdown,
                requiresVerification
                    ? ["Booking created", "Dates held", "Alibaba Cloud eKYC started"]
                    : ["Booking created", "No guest eKYC required", "Booking approved"]);

            _bookings.Add(booking);
        }

        if (booking.RequiresGuestVerification)
        {
            await StartEkycAsync(booking, request, cancellationToken);
        }
        else
        {
            await QueueNotificationsAsync(booking, BuildApprovalNotifications(booking), cancellationToken);
            await AuthorizePaymentAfterApprovalAsync(booking, cancellationToken);
        }

        lock (_gate)
        {
            return ToDto(booking);
        }
    }

    public async Task<BookingDto?> ResolveVerificationAsync(Guid bookingId, ResolveVerificationRequest request, CancellationToken cancellationToken)
    {
        PhaseOneBooking? booking;
        IReadOnlyList<PendingNotification> notifications = [];

        lock (_gate)
        {
            booking = _bookings.SingleOrDefault(item => item.Id == bookingId);
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

            if (!request.Passed)
            {
                booking.Status = BookingStatus.Rejected;
                booking.VerificationStatus = VerificationStatus.Failed;
                booking.PaymentStatus = PaymentStatus.Cancelled;
                booking.HoldExpiresAt = null;
                booking.Timeline.Add("Alibaba Cloud eKYC failed");
                booking.Timeline.Add("Booking rejected");
                booking.Timeline.Add("Dates released");
                notifications = BuildRejectionNotifications(booking);
            }
            else
            {
                booking.Status = BookingStatus.Approved;
                booking.VerificationStatus = VerificationStatus.Passed;
                booking.HoldExpiresAt = null;
                booking.Timeline.Add("Alibaba Cloud eKYC approved");
                booking.Timeline.Add("Booking approved");
                notifications = BuildApprovalNotifications(booking);
            }
        }

        await QueueNotificationsAsync(booking, notifications, cancellationToken);

        if (request.Passed)
        {
            await AuthorizePaymentAfterApprovalAsync(booking, cancellationToken);
        }

        lock (_gate)
        {
            return ToDto(booking);
        }
    }

    public async Task<BookingDto?> CapturePaymentAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        PhaseOneBooking? booking;
        lock (_gate)
        {
            booking = _bookings.SingleOrDefault(item => item.Id == bookingId);
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
        }

        await AuthorizePaymentAfterApprovalAsync(booking, cancellationToken);

        PaymentCaptureRequest captureRequest;
        lock (_gate)
        {
            if (booking.PaymentAuthorizationReference is null)
            {
                throw new InvalidOperationException("Stripe payment must have an authorization reference before it can be captured.");
            }

            captureRequest = new PaymentCaptureRequest(
                booking.PaymentAuthorizationReference,
                booking.TotalAmount,
                booking.Currency);
        }

        var capture = await paymentGateway.CaptureAsync(captureRequest, cancellationToken);
        var notifications = BuildPaymentCapturedNotifications(booking);

        lock (_gate)
        {
            booking.PaymentProvider = capture.ProviderName;
            booking.PaymentCaptureReference = capture.CaptureReference;
            booking.PaymentStatus = capture.Status;
            booking.Timeline.Add("Stripe payment captured after approval");
        }

        await QueueNotificationsAsync(booking, notifications, cancellationToken);

        lock (_gate)
        {
            return ToDto(booking);
        }
    }

    private async Task StartEkycAsync(PhaseOneBooking booking, CreateBookingRequest request, CancellationToken cancellationToken)
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

            lock (_gate)
            {
                booking.EkycProvider = result.ProviderName;
                booking.EkycTransactionId = result.TransactionId;
                booking.EkycTransactionUrl = result.TransactionUrl;
                booking.VerificationStatus = result.Status;
                booking.Timeline.Add($"Alibaba Cloud eKYC transaction created: {result.TransactionId}");
            }
        }
        catch (Exception exception)
        {
            lock (_gate)
            {
                booking.Status = BookingStatus.Rejected;
                booking.VerificationStatus = VerificationStatus.Failed;
                booking.HoldExpiresAt = null;
                booking.Timeline.Add("Alibaba Cloud eKYC could not be started");
                booking.Timeline.Add("Dates released");
            }

            throw new InvalidOperationException("Alibaba Cloud eKYC could not be started for this booking.", exception);
        }
    }

    private async Task AuthorizePaymentAfterApprovalAsync(PhaseOneBooking booking, CancellationToken cancellationToken)
    {
        PaymentAuthorizationRequest authorizationRequest;
        lock (_gate)
        {
            if (booking.Status != BookingStatus.Approved ||
                booking.PaymentStatus is PaymentStatus.Authorized or PaymentStatus.Captured ||
                booking.PaymentAuthorizationReference is not null)
            {
                return;
            }

            authorizationRequest = new PaymentAuthorizationRequest(
                booking.Id,
                booking.TotalAmount,
                booking.Currency,
                $"NestyStay booking {booking.Id:N}");
        }

        var authorization = await paymentGateway.AuthorizeAsync(authorizationRequest, cancellationToken);

        lock (_gate)
        {
            booking.PaymentProvider = authorization.ProviderName;
            booking.PaymentAuthorizationReference = authorization.AuthorizationReference;
            booking.PaymentClientSecret = authorization.ClientSecret;
            booking.PaymentStatus = authorization.Status;
            booking.Timeline.Add("Stripe manual-capture payment authorized after approval");
        }
    }

    private async Task QueueNotificationsAsync(PhaseOneBooking booking, IReadOnlyList<PendingNotification> notifications, CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        var queuedAt = timeProvider.GetUtcNow();
        lock (_gate)
        {
            foreach (var notification in notifications)
            {
                booking.Notifications.Add(new BookingNotificationDto(
                    notification.RecipientType,
                    notification.Message.Recipient,
                    notification.Message.Subject,
                    queuedAt));
                booking.Timeline.Add($"Notification queued for {notification.RecipientType}");
            }
        }

        foreach (var notification in notifications)
        {
            await notificationGateway.QueueAsync(notification.Message, cancellationToken);
        }
    }

    private BookingQuoteDto BuildQuote(
        PhaseOneProperty property,
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

    private PhaseOneProperty FindProperty(Guid propertyId) =>
        _properties.SingleOrDefault(property => property.Id == propertyId)
        ?? throw new InvalidOperationException("Property not found.");

    private PhaseOneBooking? FindBlockingBookingNoLock(Guid propertyId, DateOnly checkIn, DateOnly checkOut, DateTimeOffset now) =>
        _bookings.FirstOrDefault(booking =>
            booking.PropertyId == propertyId &&
            BlocksDates(booking, now) &&
            DateRangesOverlap(booking.CheckIn, booking.CheckOut, checkIn, checkOut));

    private static bool BlocksDates(PhaseOneBooking booking, DateTimeOffset now) =>
        booking.Status is BookingStatus.Approved or BookingStatus.PaymentCaptured or BookingStatus.Confirmed ||
        (booking.Status == BookingStatus.PendingVerification && booking.HoldExpiresAt > now);

    private void ExpirePendingHoldsNoLock(DateTimeOffset now)
    {
        foreach (var booking in _bookings.Where(booking =>
                     booking.Status == BookingStatus.PendingVerification &&
                     booking.HoldExpiresAt is not null &&
                     booking.HoldExpiresAt <= now))
        {
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Expired;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.HoldExpiresAt = null;
            booking.Timeline.Add("Pending verification hold expired");
            booking.Timeline.Add("Dates released");
        }
    }

    private static bool DateRangesOverlap(DateOnly firstCheckIn, DateOnly firstCheckOut, DateOnly secondCheckIn, DateOnly secondCheckOut) =>
        firstCheckIn < secondCheckOut && secondCheckIn < firstCheckOut;

    private static IReadOnlyList<PendingNotification> BuildApprovalNotifications(PhaseOneBooking booking) =>
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

    private static IReadOnlyList<PendingNotification> BuildRejectionNotifications(PhaseOneBooking booking) =>
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

    private static IReadOnlyList<PendingNotification> BuildPaymentCapturedNotifications(PhaseOneBooking booking) =>
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

    private static PropertyListingDto ToListingDto(PhaseOneProperty property) =>
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
            property.Highlights);

    private static BookingPropertySummaryDto ToSummaryDto(PhaseOneProperty property) =>
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

    private BookingDto ToDto(PhaseOneBooking booking) =>
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
            booking.PriceBreakdown.ToList(),
            booking.Notifications.ToList(),
            booking.Timeline.ToList());

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

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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

    private sealed class PhaseOneUser(
        Guid id,
        string email,
        string passwordHash,
        string displayName,
        string? phone,
        byte[] twoFactorSecret,
        IReadOnlyList<UserRole> roles)
    {
        public Guid Id { get; } = id;
        public string Email { get; } = email;
        public string PasswordHash { get; set; } = passwordHash;
        public string DisplayName { get; } = displayName;
        public string? Phone { get; } = phone;
        public byte[] TwoFactorSecret { get; } = twoFactorSecret;
        public IReadOnlyList<UserRole> Roles { get; } = roles;
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? LockoutEndsAt { get; set; }
        public DateTimeOffset? SessionInvalidatedAt { get; set; }
        public long? LastAcceptedTotpCounter { get; set; }
    }

    private sealed class PhaseOneChallenge(string id, Guid userId, DateTimeOffset expiresAt)
    {
        public string Id { get; } = id;
        public Guid UserId { get; } = userId;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public int FailedAttempts { get; set; }
    }

    private sealed class PhaseOnePasswordReset(
        string requestId,
        Guid userId,
        string email,
        string secretSalt,
        string tokenHash,
        string status,
        DateTimeOffset expiresAt)
    {
        public string RequestId { get; } = requestId;
        public Guid UserId { get; } = userId;
        public string Email { get; } = email;
        public string SecretSalt { get; } = secretSalt;
        public string TokenHash { get; } = tokenHash;
        public string Status { get; set; } = status;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public DateTimeOffset? UsedAt { get; set; }
        public DateTimeOffset? InvalidatedAt { get; set; }
    }

    private sealed record PhaseOneProperty(
        Guid Id,
        Guid HostUserId,
        string HostName,
        string HostEmail,
        string Title,
        string Location,
        string Country,
        decimal NightlyRate,
        string Currency,
        BadgeLevel BadgeLevel,
        bool GuestVerificationEnabled,
        bool InsuraGuestEnabled,
        string CancellationPolicy,
        IReadOnlyList<string> Highlights);

    private sealed record PendingNotification(string RecipientType, NotificationMessage Message);

    private sealed class PhaseOneBooking(
        Guid id,
        Guid propertyId,
        Guid hostUserId,
        string hostName,
        string hostEmail,
        Guid guestUserId,
        string guestEmail,
        string guestName,
        DateOnly checkIn,
        DateOnly checkOut,
        BookingStatus status,
        VerificationStatus verificationStatus,
        PaymentStatus paymentStatus,
        bool requiresGuestVerification,
        DateTimeOffset? holdExpiresAt,
        int nights,
        decimal nightlyRate,
        decimal staySubtotal,
        decimal guestPlatformFee,
        decimal totalAmount,
        string currency,
        string propertyTitle,
        string? ekycProvider,
        string? ekycTransactionId,
        string? ekycTransactionUrl,
        string? paymentProvider,
        string? paymentAuthorizationReference,
        string? paymentClientSecret,
        string? paymentCaptureReference,
        IReadOnlyList<BookingPriceLineDto> priceBreakdown,
        IReadOnlyList<string> timeline)
    {
        public Guid Id { get; } = id;
        public Guid PropertyId { get; } = propertyId;
        public Guid HostUserId { get; } = hostUserId;
        public string HostName { get; } = hostName;
        public string HostEmail { get; } = hostEmail;
        public Guid GuestUserId { get; } = guestUserId;
        public string GuestEmail { get; } = guestEmail;
        public string GuestName { get; } = guestName;
        public DateOnly CheckIn { get; } = checkIn;
        public DateOnly CheckOut { get; } = checkOut;
        public BookingStatus Status { get; set; } = status;
        public VerificationStatus VerificationStatus { get; set; } = verificationStatus;
        public PaymentStatus PaymentStatus { get; set; } = paymentStatus;
        public bool RequiresGuestVerification { get; } = requiresGuestVerification;
        public DateTimeOffset? HoldExpiresAt { get; set; } = holdExpiresAt;
        public int Nights { get; } = nights;
        public decimal NightlyRate { get; } = nightlyRate;
        public decimal StaySubtotal { get; } = staySubtotal;
        public decimal GuestPlatformFee { get; } = guestPlatformFee;
        public decimal TotalAmount { get; } = totalAmount;
        public string Currency { get; } = currency;
        public string PropertyTitle { get; } = propertyTitle;
        public string? EkycProvider { get; set; } = ekycProvider;
        public string? EkycTransactionId { get; set; } = ekycTransactionId;
        public string? EkycTransactionUrl { get; set; } = ekycTransactionUrl;
        public string? PaymentProvider { get; set; } = paymentProvider;
        public string? PaymentAuthorizationReference { get; set; } = paymentAuthorizationReference;
        public string? PaymentClientSecret { get; set; } = paymentClientSecret;
        public string? PaymentCaptureReference { get; set; } = paymentCaptureReference;
        public List<BookingPriceLineDto> PriceBreakdown { get; } = [.. priceBreakdown];
        public List<BookingNotificationDto> Notifications { get; } = [];
        public List<string> Timeline { get; } = [.. timeline];
    }
}
