using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NestyStay.Application.Admin;
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
    IDevelopmentAuthSecretStore? developmentAuthSecrets = null,
    ISecretProtector? secretProtector = null,
    IStorageProvider? storageProvider = null,
    IFileSafetyScanner? fileSafetyScanner = null) : IPhaseOneStore, IPropertyEnhancementStore, ISessionActivityStore, IBookingDecisionStore, IHostVerificationStore, IPropertyModerationStore
{
    private const int PasswordHashIterations = 120_000;
    private const int TotpStepSeconds = 30;
    private const int MaximumLoginAttempts = 5;
    private const int MaximumChallengeAttempts = 5;
    private const string PaymentOperationAuthorize = "Authorize";
    private const string PaymentOperationCapture = "Capture";
    private const string PaymentOperationRefund = "Refund";
    private const string PaymentOperationWebhook = "Webhook";
    private const string PasswordResetStatusPending = "Pending";
    private const string PasswordResetStatusCompleted = "Completed";
    private const string PasswordResetStatusExpired = "Expired";
    private const string PasswordResetStatusFailed = "Failed";
    private const string PasswordResetStatusInvalidated = "Invalidated";
    private const string UploadStatusPending = "PendingUpload";
    private const string UploadStatusUploaded = "Uploaded";
    private const string UploadStatusExpired = "Expired";
    private const string UploadStatusQuarantined = "Quarantined";
    private const string ScanStatusPending = "PendingScan";
    private const string ScanStatusClean = "Clean";
    private const string TotpSecretPurpose = "MilestoneUser.TotpSecret";
    private const long MaximumProfilePhotoBytes = 10 * 1024 * 1024;
    private const long MaximumPropertyPhotoBytes = 10 * 1024 * 1024;
    private const int BookingCreationPersistenceRetries = 3;
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ProfilePhotoUploadLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ProfilePhotoDownloadLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PropertyPhotoUploadLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan TwoFactorEnrollmentLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan BookingCreationRateLimitWindow = TimeSpan.FromMinutes(NestyStayBusinessRules.BookingCreationRateLimitWindowMinutes);
    private static readonly SemaphoreSlim NonRelationalBookingCreationGate = new(1, 1);
    private static readonly IReadOnlyDictionary<string, string[]> AllowedPropertyPhotoExtensions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"]
    };
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
            TwoFactorSecret = ProtectTotpSecret(GenerateSecret()),
            IsTwoFactorEnabled = false,
            Status = "Active",
            AdminPermissionsJson = MilestoneJson.Serialize<IReadOnlyList<string>>([]),
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
            false);
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

        if (!user.IsTwoFactorEnabled)
        {
            await db.SaveChangesAsync(cancellationToken);
            var directRoles = MilestoneJson.DeserializeList<UserRole>(user.RolesJson);
            var directPermissions = ReadAdminPermissions(user);
            var directTokenExpiresAt = now.AddHours(8);
            var directToken = await IssueSessionAsync(user.Id, directRoles, directTokenExpiresAt, request.DeviceName, request.UserAgent, request.IpAddress, request.RememberDevice, cancellationToken);
            return new LoginResponse(
                user.Id,
                user.Email,
                false,
                null,
                null,
                directToken,
                directTokenExpiresAt,
                directRoles,
                directPermissions);
        }

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
        if (!user.IsTwoFactorEnabled)
        {
            return null;
        }

        return new DevelopmentAuthCodeResponse(
            challenge.ChallengeId,
            GenerateTotp(UnprotectTotpSecret(user.TwoFactorSecret), timeProvider.GetUtcNow()),
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
        user.PendingTwoFactorSecret = ProtectTotpSecret(secret);
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

        var pendingSecret = UnprotectTotpSecret(user.PendingTwoFactorSecret);
        if (!TryVerifyTotp(pendingSecret, request.Code, now, out var acceptedCounter))
        {
            throw new InvalidOperationException("Authenticator code is invalid.");
        }

        user.TwoFactorSecret = ProtectTotpSecret(pendingSecret);
        user.IsTwoFactorEnabled = true;
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
                TwoFactorSecret = ProtectTotpSecret(GenerateSecret()),
                IsTwoFactorEnabled = false,
                Status = "Active",
                AdminPermissionsJson = MilestoneJson.Serialize<IReadOnlyList<string>>([]),
                RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>([role])
            };
            db.MilestoneUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        var tokenExpiresAt = timeProvider.GetUtcNow().AddHours(8);
        var googleToken = await IssueSessionAsync(user.Id, MilestoneJson.DeserializeList<UserRole>(user.RolesJson), tokenExpiresAt, null, null, null, false, cancellationToken);
        return new GoogleSignInResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            googleToken,
            tokenExpiresAt,
            MilestoneJson.DeserializeList<UserRole>(user.RolesJson),
            "Google",
            ReadAdminPermissions(user));
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
        if (!user.IsTwoFactorEnabled)
        {
            db.MilestoneTwoFactorChallenges.Remove(challenge);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("2FA is not enabled for this account.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code) &&
            await TryConsumeRecoveryCodeAsync(user.Id, request.Code, now, cancellationToken))
        {
            db.MilestoneTwoFactorChallenges.Remove(challenge);
            await db.SaveChangesAsync(cancellationToken);

            var recoveryTokenExpiresAt = now.AddHours(8);
            var recoveryRoles = MilestoneJson.DeserializeList<UserRole>(user.RolesJson);
            var recoveryPermissions = ReadAdminPermissions(user);
            var recoveryToken = await IssueSessionAsync(user.Id, recoveryRoles, recoveryTokenExpiresAt, request.DeviceName, request.UserAgent, request.IpAddress, request.RememberDevice, cancellationToken);
            return new VerifyTwoFactorResponse(
                user.Id,
                recoveryToken,
                recoveryTokenExpiresAt,
                recoveryRoles,
                recoveryPermissions);
        }

        var twoFactorSecret = UnprotectTotpSecret(user.TwoFactorSecret);
        if (string.IsNullOrWhiteSpace(request.Code) ||
            !TryVerifyTotp(twoFactorSecret, request.Code, now, out var acceptedCounter) ||
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
        var permissions = ReadAdminPermissions(user);
        var token = await IssueSessionAsync(user.Id, roles, tokenExpiresAt, request.DeviceName, request.UserAgent, request.IpAddress, request.RememberDevice, cancellationToken);
        return new VerifyTwoFactorResponse(
            user.Id,
            token,
            tokenExpiresAt,
            roles,
            permissions);
    }

    public async Task<DisableTwoFactorResponse> DisableTwoFactorAsync(
        Guid userId,
        DisableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        if (!user.IsTwoFactorEnabled)
        {
            return new DisableTwoFactorResponse(true);
        }

        var now = timeProvider.GetUtcNow();
        var code = request.Code?.Trim() ?? string.Empty;
        var twoFactorSecret = UnprotectTotpSecret(user.TwoFactorSecret);
        var verified = await TryConsumeRecoveryCodeAsync(user.Id, code, now, cancellationToken) ||
            TryVerifyTotp(twoFactorSecret, code, now, out var acceptedCounter) &&
            (user.LastAcceptedTotpCounter is null || acceptedCounter > user.LastAcceptedTotpCounter.Value);
        if (!verified)
        {
            throw new InvalidOperationException("A valid authenticator or recovery code is required to disable 2FA.");
        }

        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecret = ProtectTotpSecret(GenerateSecret());
        user.LastAcceptedTotpCounter = null;
        ClearPendingTwoFactorEnrollment(user);
        db.MilestoneTwoFactorChallenges.RemoveRange(db.MilestoneTwoFactorChallenges.Where(item => item.UserId == user.Id));
        db.MilestoneRecoveryCodes.RemoveRange(db.MilestoneRecoveryCodes.Where(item => item.UserId == user.Id));
        await db.SaveChangesAsync(cancellationToken);
        return new DisableTwoFactorResponse(true);
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

    public async Task<LogoutResponse> LogoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User account not found.");
        var now = timeProvider.GetUtcNow();
        user.SessionInvalidatedAt = now;
        user.UpdatedAt = now;
        db.MilestoneTwoFactorChallenges.RemoveRange(
            db.MilestoneTwoFactorChallenges.Where(item => item.UserId == userId));
        await db.SaveChangesAsync(cancellationToken);
        return new LogoutResponse(true, now);
    }

    public async Task<bool> IsSessionActiveAsync(Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user?.SessionInvalidatedAt is null || issuedAt > user.SessionInvalidatedAt.Value;
    }

    public async Task<bool> IsSessionTokenActiveAsync(Guid userId, DateTimeOffset issuedAt, string? tokenId, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        // Keep compatibility with valid signed tokens issued by an external
        // identity source before its profile is replicated locally.  Existing
        // users still receive full timestamp and per-device revocation checks.
        if (user is null)
        {
            return true;
        }
        if (user.SessionInvalidatedAt is not null && issuedAt <= user.SessionInvalidatedAt.Value)
        {
            return false;
        }

        // Tokens issued before per-device sessions were introduced continue to
        // be accepted by the legacy timestamp rule. New tokens are checked
        // against their hashed JTI, which makes revocation device-specific.
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return true;
        }

        var session = await db.MilestoneUserSessions.AsNoTracking().SingleOrDefaultAsync(
            item => item.UserId == userId && item.TokenIdHash == HashOpaque(tokenId) && !item.IsDeleted,
            cancellationToken);
        if (session is null)
        {
            return true;
        }

        return session.RevokedAt is null && session.ExpiresAt > timeProvider.GetUtcNow();
    }

    private async Task<string> IssueSessionAsync(
        Guid userId,
        IReadOnlyList<UserRole> roles,
        DateTimeOffset expiresAt,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        bool trusted,
        CancellationToken cancellationToken)
    {
        var token = _accessTokenService.Issue(userId, roles, expiresAt);
        var tokenId = _accessTokenService.Validate(token)?.TokenId;
        if (!string.IsNullOrWhiteSpace(tokenId))
        {
            await RecordSessionAsync(userId, tokenId, DateTimeOffset.UtcNow, expiresAt, deviceName, userAgent, ipAddress, trusted, cancellationToken);
        }
        return token;
    }

    public async Task RecordSessionAsync(Guid userId, string tokenId, DateTimeOffset issuedAt, DateTimeOffset expiresAt, string? deviceName, string? userAgent, string? ipAddress, bool trusted, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenId)) return;
        var now = timeProvider.GetUtcNow();
        var existing = await db.MilestoneUserSessions.SingleOrDefaultAsync(
            item => item.UserId == userId && item.TokenIdHash == HashOpaque(tokenId) && !item.IsDeleted,
            cancellationToken);
        if (existing is not null) return;
        db.MilestoneUserSessions.Add(new MilestoneUserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenIdHash = HashOpaque(tokenId),
            DeviceName = NormalizeDeviceName(deviceName),
            Browser = ResolveBrowser(userAgent),
            ApproximateLocation = null,
            IpAddressHash = string.IsNullOrWhiteSpace(ipAddress) ? null : HashOpaque(ipAddress),
            IssuedAt = issuedAt,
            LastUsedAt = now,
            ExpiresAt = expiresAt,
            TrustedUntil = trusted ? now.AddDays(30) : null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetSessionsAsync(Guid userId, string? currentTokenId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await db.MilestoneUserSessions.AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderByDescending(item => item.LastUsedAt)
            .Select(item => new UserSessionDto(
                item.Id,
                item.DeviceName,
                item.Browser,
                item.ApproximateLocation,
                item.IssuedAt,
                item.LastUsedAt,
                item.ExpiresAt,
                !string.IsNullOrWhiteSpace(currentTokenId) && item.TokenIdHash == HashOpaque(currentTokenId),
                item.TrustedUntil != null && item.TrustedUntil > now,
                item.TrustedUntil,
                item.RevokedAt != null || item.ExpiresAt <= now))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserSessionDto?> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await db.MilestoneUserSessions.SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId && !item.IsDeleted, cancellationToken);
        if (session is null) return null;
        session.RevokedAt ??= timeProvider.GetUtcNow();
        session.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToSessionDto(session, false);
    }

    public async Task<int> RevokeOtherSessionsAsync(Guid userId, Guid? keepSessionId, string? keepTokenId, CancellationToken cancellationToken)
    {
        var keepTokenHash = string.IsNullOrWhiteSpace(keepTokenId) ? null : HashOpaque(keepTokenId);
        var sessions = await db.MilestoneUserSessions.Where(item => item.UserId == userId && !item.IsDeleted && item.RevokedAt == null &&
            (!keepSessionId.HasValue || item.Id != keepSessionId.Value) &&
            (keepTokenHash == null || item.TokenIdHash != keepTokenHash)).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.UpdatedAt = now;
        }
        if (sessions.Count > 0) await db.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }

    public async Task<AdministratorSessionDto?> GetAdministratorSessionAsync(Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var user = await db.MilestoneUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null ||
            !MilestoneJson.DeserializeList<UserRole>(user.RolesJson).Contains(UserRole.Admin) ||
            !user.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
            user.LockoutEndsAt is not null && user.LockoutEndsAt > now ||
            user.SessionInvalidatedAt is not null && issuedAt <= user.SessionInvalidatedAt.Value)
        {
            return null;
        }

        return new AdministratorSessionDto(
            user.Id,
            user.Email,
            MilestoneJson.DeserializeList<UserRole>(user.RolesJson),
            ReadAdminPermissions(user));
    }

    public async Task<AdministratorBootstrapResponse> BootstrapAdministratorAsync(AdministratorBootstrapRequest request, CancellationToken cancellationToken)
    {
        ValidateAdministratorBootstrap(request);
        var email = request.Email.Trim().ToLowerInvariant();
        var permissions = AdminPermissionCatalog.Normalize(request.Permissions, defaultToSuperAdministration: true);
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(
            item => item.NormalizedEmail == email,
            cancellationToken);

        if (user is not null)
        {
            var roles = MilestoneJson.DeserializeList<UserRole>(user.RolesJson).ToList();
            if (!roles.Contains(UserRole.Admin))
            {
                roles.Add(UserRole.Admin);
                user.RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>(roles);
            }

            if (ReadAdminPermissions(user).Count == 0)
            {
                user.AdminPermissionsJson = MilestoneJson.Serialize(permissions);
            }

            await db.SaveChangesAsync(cancellationToken);
            return new AdministratorBootstrapResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                ReadAdminPermissions(user),
                false);
        }

        user = new MilestoneUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.DisplayName.Trim(),
            Phone = null,
            TwoFactorSecret = ProtectTotpSecret(GenerateSecret()),
            IsTwoFactorEnabled = request.RequireTwoFactor,
            Status = "Active",
            AdminPermissionsJson = MilestoneJson.Serialize(permissions),
            RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>([UserRole.Admin])
        };

        db.MilestoneUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new AdministratorBootstrapResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            ReadAdminPermissions(user),
            true);
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Profile is not available for this session.");
        var photo = await db.MilestoneUserProfilePhotos
            .AsNoTracking()
            .Where(item =>
                item.UserId == userId &&
                item.IsCurrent &&
                item.Status == UploadStatusUploaded &&
                item.ScanStatus == ScanStatusClean &&
                !item.IsDeleted)
            .OrderByDescending(item => item.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return ToProfileDto(user, photo);
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 120)
        {
            throw new InvalidOperationException("Display name must be between 1 and 120 characters.");
        }

        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Profile is not available for this session.");
        user.DisplayName = request.DisplayName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return await GetUserProfileAsync(userId, cancellationToken);
    }

    public async Task<ProfilePhotoUploadDto> PrepareProfilePhotoUploadAsync(Guid userId, PrepareProfilePhotoUploadRequest request, CancellationToken cancellationToken)
    {
        _ = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Profile is not available for this session.");
        var storage = RequireStorageProvider();
        var safeFileName = ValidateProfilePhoto(request.FileName, request.ContentType, request.SizeBytes);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var photoId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();
        var photo = new MilestoneUserProfilePhoto
        {
            Id = photoId,
            UserId = userId,
            OriginalFileName = Path.GetFileName(request.FileName.Trim()),
            SafeFileName = safeFileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            ObjectKey = $"profiles/{userId:N}/photos/{photoId:N}{extension}",
            UploadUrl = $"/api/auth/profile/photo/uploads/{photoId}/content",
            Status = UploadStatusPending,
            StorageProviderName = storage.ProviderName,
            ScanStatus = ScanStatusPending,
            UploadExpiresAt = now.Add(ProfilePhotoUploadLifetime),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        db.MilestoneUserProfilePhotos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(photo);
    }

    public async Task<ProfilePhotoUploadDto> UploadProfilePhotoContentAsync(Guid userId, Guid photoId, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken)
    {
        _ = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Profile is not available for this session.");
        var photo = await db.MilestoneUserProfilePhotos
            .SingleOrDefaultAsync(item => item.Id == photoId && item.UserId == userId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Profile photo is not available to this user.");

        var now = await RequirePendingProfilePhotoUploadAsync(photo, userId, cancellationToken);
        ValidateProfilePhotoUploadMetadata(photo, contentType, sizeBytes);

        var upload = await RequireStorageProvider().SaveObjectAsync(
            new StorageObjectWriteRequest(photo.ObjectKey, photo.ContentType, MaximumProfilePhotoBytes),
            content,
            cancellationToken);
        ValidateProfilePhotoUploadMetadata(photo, upload.ContentType, upload.SizeBytes);

        return await FinalizeProfilePhotoUploadAsync(
            photo,
            userId,
            now,
            upload.ProviderName,
            upload.ContentType,
            upload.SizeBytes,
            upload.Sha256Hash,
            upload.HeaderBytes,
            cancellationToken);
    }

    public async Task<ProfilePhotoDownloadDto> GetProfilePhotoDownloadAsync(Guid userId, Guid photoId, CancellationToken cancellationToken)
    {
        var photo = await db.MilestoneUserProfilePhotos
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == photoId &&
                item.UserId == userId &&
                item.Status == UploadStatusUploaded &&
                item.ScanStatus == ScanStatusClean &&
                !item.IsDeleted,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Profile photo is not available to this user.");

        var expiresAt = timeProvider.GetUtcNow().Add(ProfilePhotoDownloadLifetime);
        var url = await RequireStorageProvider().CreateDownloadUrlAsync(photo.ObjectKey, expiresAt, cancellationToken);
        return new ProfilePhotoDownloadDto(photo.Id, photo.SafeFileName, photo.ContentType, photo.SizeBytes, url, expiresAt);
    }

    public IReadOnlyList<PropertyListingDto> GetProperties(Guid? hostUserId = null)
    {
        EnsurePhaseOneSeeded();
        return db.MilestoneProperties
            .AsNoTracking()
            .Where(property =>
                !property.IsDeleted &&
                (hostUserId != null || (!property.IsArchived && !property.IsDraft)) &&
                (hostUserId == null || property.HostUserId == hostUserId) &&
                (hostUserId != null || property.ModerationStatus == "Approved"))
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
            .SingleOrDefault(property => property.Id == id && !property.IsDeleted && !property.IsArchived && !property.IsDraft) is { } property
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
            Parish = request.Parish?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            Bedrooms = Math.Max(1, request.Bedrooms),
            Bathrooms = Math.Max(1, request.Bathrooms),
            MaxGuests = Math.Max(1, request.MaxGuests),
            AmenitiesJson = MilestoneJson.Serialize(NormalizeStringList(request.Amenities)),
            SleepingArrangementsJson = MilestoneJson.Serialize(NormalizeStringList(request.SleepingArrangements)),
            HouseRulesJson = MilestoneJson.Serialize(NormalizeStringList(request.HouseRules)),
            CleaningFee = decimal.Round(Math.Max(0, request.CleaningFee), 2),
            ServiceFee = decimal.Round(Math.Max(0, request.ServiceFee), 2),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ImageUrl = request.ImageUrl?.Trim(),
            GalleryUrlsJson = MilestoneJson.Serialize(NormalizeStringList(request.GalleryUrls)),
            ModerationStatus = "Pending",
            HighlightsJson = MilestoneJson.Serialize(
                request.Highlights is null || request.Highlights.Count == 0
                    ? ["Host-created listing"]
                    : request.Highlights.Select(item => item.Trim()).Where(item => item.Length > 0).ToList())
        };

        db.MilestoneProperties.Add(property);
        await AddPropertyRevisionAsync(property, request.HostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToListingDto(property);
    }

    public async Task<PropertyListingDto> UpdatePropertyAsync(Guid hostUserId, Guid propertyId, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        ValidateProperty(request);

        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        ApplyPropertyChanges(property, request);
        property.IsDraft = false;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;

        await AddPropertyRevisionAsync(property, hostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    public async Task<PropertyListingDto> ArchivePropertyAsync(Guid hostUserId, Guid propertyId, bool isArchived, CancellationToken cancellationToken)
    {
        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        property.IsArchived = isArchived;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;

        await AddPropertyRevisionAsync(property, hostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    public async Task<IReadOnlyList<PropertyListingDto>> BulkArchivePropertiesAsync(Guid hostUserId, IReadOnlyCollection<Guid> propertyIds, bool isArchived, CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0 || propertyIds.Count > 100)
        {
            throw new ArgumentException("Select between 1 and 100 properties.", nameof(propertyIds));
        }

        var distinctIds = propertyIds.Distinct().ToArray();
        var properties = await db.MilestoneProperties
            .Where(property => distinctIds.Contains(property.Id) && !property.IsDeleted && property.HostUserId == hostUserId)
            .ToListAsync(cancellationToken);
        if (properties.Count != distinctIds.Length)
        {
            throw new UnauthorizedAccessException("One or more properties are not available to this host.");
        }

        foreach (var property in properties)
        {
            property.IsArchived = isArchived;
            property.UpdatedAt = timeProvider.GetUtcNow();
            property.UpdatedByUserId = hostUserId;
            await AddPropertyRevisionAsync(property, hostUserId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return properties.Select(ToListingDto).ToList();
    }

    public async Task<BulkPropertyEditPreviewDto> PreviewBulkPropertyEditAsync(Guid hostUserId, BulkPropertyEditRequest request, CancellationToken cancellationToken)
    {
        var ids = ValidateBulkEditRequest(request);
        var matched = await db.MilestoneProperties.AsNoTracking()
            .Where(property => ids.Contains(property.Id) && !property.IsDeleted && property.HostUserId == hostUserId)
            .Select(property => property.Id)
            .ToListAsync(cancellationToken);
        if (matched.Count != ids.Length)
        {
            throw new UnauthorizedAccessException("One or more properties are not available to this host.");
        }

        return new BulkPropertyEditPreviewDto(
            request.PropertyIds.Count,
            matched.Count,
            matched,
            ChangedFields(request),
            request.NightlyRate,
            request.CancellationPolicy,
            request.GuestVerificationEnabled,
            request.InsuraGuestEnabled);
    }

    public async Task<IReadOnlyList<PropertyListingDto>> BulkEditPropertiesAsync(Guid hostUserId, BulkPropertyEditRequest request, CancellationToken cancellationToken)
    {
        var ids = ValidateBulkEditRequest(request);
        var properties = await db.MilestoneProperties
            .Where(property => ids.Contains(property.Id) && !property.IsDeleted && property.HostUserId == hostUserId)
            .ToListAsync(cancellationToken);
        if (properties.Count != ids.Length)
        {
            throw new UnauthorizedAccessException("One or more properties are not available to this host.");
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var now = timeProvider.GetUtcNow();
        foreach (var property in properties)
        {
            if (request.NightlyRate.HasValue)
            {
                if (request.NightlyRate.Value <= 0 || request.NightlyRate.Value > 1_000_000)
                    throw new InvalidOperationException("Nightly rate must be greater than zero and less than 1,000,000.");
                property.NightlyRate = decimal.Round(request.NightlyRate.Value, 2);
            }
            if (!string.IsNullOrWhiteSpace(request.CancellationPolicy))
            {
                property.CancellationPolicy = request.CancellationPolicy.Trim()[..Math.Min(100, request.CancellationPolicy.Trim().Length)];
            }
            if (request.GuestVerificationEnabled.HasValue) property.GuestVerificationEnabled = request.GuestVerificationEnabled.Value;
            if (request.InsuraGuestEnabled.HasValue) property.InsuraGuestEnabled = request.InsuraGuestEnabled.Value;
            property.UpdatedAt = now;
            property.UpdatedByUserId = hostUserId;
            await AddPropertyRevisionAsync(property, hostUserId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return properties.Select(ToListingDto).ToList();
    }

    private static Guid[] ValidateBulkEditRequest(BulkPropertyEditRequest request)
    {
        if (request.PropertyIds is null || request.PropertyIds.Count == 0 || request.PropertyIds.Count > 100)
            throw new ArgumentException("Select between 1 and 100 properties.", nameof(request));
        if (request.PropertyIds.Any(id => id == Guid.Empty) || request.PropertyIds.Distinct().Count() != request.PropertyIds.Count)
            throw new ArgumentException("Property ids must be unique and valid.", nameof(request));
        if (request.NightlyRate is null && string.IsNullOrWhiteSpace(request.CancellationPolicy) && !request.GuestVerificationEnabled.HasValue && !request.InsuraGuestEnabled.HasValue)
            throw new ArgumentException("At least one field must be changed.", nameof(request));
        return request.PropertyIds.ToArray();
    }

    private static IReadOnlyList<string> ChangedFields(BulkPropertyEditRequest request)
    {
        var fields = new List<string>();
        if (request.NightlyRate.HasValue) fields.Add(nameof(request.NightlyRate));
        if (!string.IsNullOrWhiteSpace(request.CancellationPolicy)) fields.Add(nameof(request.CancellationPolicy));
        if (request.GuestVerificationEnabled.HasValue) fields.Add(nameof(request.GuestVerificationEnabled));
        if (request.InsuraGuestEnabled.HasValue) fields.Add(nameof(request.InsuraGuestEnabled));
        return fields;
    }

    public async Task<PropertyListingDto> DuplicatePropertyAsync(Guid hostUserId, Guid propertyId, string? title, CancellationToken cancellationToken)
    {
        var source = await db.MilestoneProperties.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Property is not available to this host.");
        var now = timeProvider.GetUtcNow();
        var duplicate = new MilestoneProperty
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            HostName = source.HostName,
            HostEmail = source.HostEmail,
            Title = string.IsNullOrWhiteSpace(title) ? $"Copy of {source.Title}" : title.Trim(),
            Location = source.Location,
            Country = source.Country,
            NightlyRate = source.NightlyRate,
            Currency = source.Currency,
            BadgeLevel = source.BadgeLevel,
            GuestVerificationEnabled = source.GuestVerificationEnabled,
            InsuraGuestEnabled = source.InsuraGuestEnabled,
            CancellationPolicy = source.CancellationPolicy,
            HighlightsJson = source.HighlightsJson,
            Parish = source.Parish,
            Description = source.Description,
            Bedrooms = source.Bedrooms,
            Bathrooms = source.Bathrooms,
            MaxGuests = source.MaxGuests,
            AmenitiesJson = source.AmenitiesJson,
            SleepingArrangementsJson = source.SleepingArrangementsJson,
            HouseRulesJson = source.HouseRulesJson,
            CleaningFee = source.CleaningFee,
            ServiceFee = source.ServiceFee,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            ImageUrl = source.ImageUrl,
            GalleryUrlsJson = source.GalleryUrlsJson,
            ModerationStatus = "Pending",
            IsDraft = true,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = hostUserId,
            UpdatedByUserId = hostUserId
        };
        db.MilestoneProperties.Add(duplicate);
        var photos = await db.MilestonePropertyPhotos.AsNoTracking()
            .Where(photo => photo.PropertyId == propertyId && photo.HostUserId == hostUserId && !photo.IsDeleted && photo.Status == UploadStatusUploaded && photo.ScanStatus == ScanStatusClean)
            .OrderBy(photo => photo.SortOrder)
            .ToListAsync(cancellationToken);
        foreach (var photo in photos)
        {
            db.MilestonePropertyPhotos.Add(new MilestonePropertyPhoto
            {
                Id = Guid.NewGuid(),
                PropertyId = duplicate.Id,
                HostUserId = hostUserId,
                OriginalFileName = photo.OriginalFileName,
                SafeFileName = photo.SafeFileName,
                ContentType = photo.ContentType,
                SizeBytes = photo.SizeBytes,
                SortOrder = photo.SortOrder,
                ObjectKey = photo.ObjectKey,
                UploadUrl = photo.UploadUrl,
                Status = photo.Status,
                StorageProviderName = photo.StorageProviderName,
                VerifiedContentType = photo.VerifiedContentType,
                UploadedSizeBytes = photo.UploadedSizeBytes,
                Sha256Hash = photo.Sha256Hash,
                ScanStatus = photo.ScanStatus,
                ScanProviderName = photo.ScanProviderName,
                ScanCheckedAt = photo.ScanCheckedAt,
                ThumbnailObjectKey = photo.ThumbnailObjectKey,
                UploadExpiresAt = now.Add(PropertyPhotoUploadLifetime),
                UploadedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = hostUserId,
                UpdatedByUserId = hostUserId
            });
        }
        await AddPropertyRevisionAsync(duplicate, hostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(duplicate);
    }

    public async Task<PropertyListingDto> PublishPropertyAsync(Guid hostUserId, Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        property.IsDraft = false;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;
        await AddPropertyRevisionAsync(property, hostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    public async Task<IReadOnlyList<PropertyRevisionDto>> GetPropertyRevisionsAsync(Guid hostUserId, Guid propertyId, CancellationToken cancellationToken)
    {
        _ = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        return await db.MilestonePropertyRevisions.AsNoTracking()
            .Where(item => item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted)
            .OrderByDescending(item => item.Version)
            .Select(item => new PropertyRevisionDto(item.Id, item.PropertyId, item.Version, item.SnapshotJson, item.CreatedAt, item.CreatedByUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyListingDto> RestorePropertyRevisionAsync(Guid hostUserId, Guid propertyId, Guid revisionId, CancellationToken cancellationToken)
    {
        var property = await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        var revision = await db.MilestonePropertyRevisions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == revisionId && item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Property revision is not available to this host.");
        var snapshot = MilestoneJson.Deserialize<PropertyRevisionSnapshot>(revision.SnapshotJson)
            ?? throw new InvalidOperationException("Property revision snapshot is invalid.");

        property.Title = snapshot.Title.Trim();
        property.Location = snapshot.Location.Trim();
        property.Country = string.IsNullOrWhiteSpace(snapshot.Country) ? "Jamaica" : snapshot.Country.Trim();
        property.NightlyRate = decimal.Round(snapshot.NightlyRate, 2);
        property.Currency = snapshot.Currency.Trim().ToUpperInvariant();
        property.BadgeLevel = snapshot.BadgeLevel;
        property.GuestVerificationEnabled = snapshot.GuestVerificationEnabled;
        property.InsuraGuestEnabled = snapshot.InsuraGuestEnabled;
        property.CancellationPolicy = snapshot.CancellationPolicy.Trim();
        property.HighlightsJson = MilestoneJson.Serialize(snapshot.Highlights ?? []);
        property.Parish = snapshot.Parish ?? string.Empty;
        property.Description = snapshot.Description ?? string.Empty;
        property.Bedrooms = Math.Max(1, snapshot.Bedrooms);
        property.Bathrooms = Math.Max(1, snapshot.Bathrooms);
        property.MaxGuests = Math.Max(1, snapshot.MaxGuests);
        property.AmenitiesJson = MilestoneJson.Serialize(snapshot.Amenities ?? []);
        property.SleepingArrangementsJson = MilestoneJson.Serialize(snapshot.SleepingArrangements ?? []);
        property.HouseRulesJson = MilestoneJson.Serialize(snapshot.HouseRules ?? []);
        property.CleaningFee = Math.Max(0, snapshot.CleaningFee);
        property.ServiceFee = Math.Max(0, snapshot.ServiceFee);
        property.Latitude = snapshot.Latitude;
        property.Longitude = snapshot.Longitude;
        property.ImageUrl = snapshot.ImageUrl;
        property.GalleryUrlsJson = MilestoneJson.Serialize(snapshot.GalleryUrls ?? []);
        property.IsArchived = snapshot.IsArchived;
        property.IsDraft = true;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = hostUserId;
        await AddPropertyRevisionAsync(property, hostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    private async Task AddPropertyRevisionAsync(MilestoneProperty property, Guid actorUserId, CancellationToken cancellationToken)
    {
        var version = (await db.MilestonePropertyRevisions
            .Where(item => item.PropertyId == property.Id)
            .Select(item => (int?)item.Version)
            .MaxAsync(cancellationToken) ?? 0) + 1;
        db.MilestonePropertyRevisions.Add(new MilestonePropertyRevision
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            HostUserId = property.HostUserId,
            Version = version,
            SnapshotJson = MilestoneJson.Serialize(new
            {
                property.Title,
                property.Location,
                property.Country,
                property.NightlyRate,
                property.Currency,
                property.BadgeLevel,
                property.GuestVerificationEnabled,
                property.InsuraGuestEnabled,
                property.CancellationPolicy,
                Highlights = MilestoneJson.DeserializeList<string>(property.HighlightsJson),
                property.Parish,
                property.Description,
                property.Bedrooms,
                property.Bathrooms,
                property.MaxGuests,
                Amenities = MilestoneJson.DeserializeList<string>(property.AmenitiesJson),
                SleepingArrangements = MilestoneJson.DeserializeList<string>(property.SleepingArrangementsJson),
                HouseRules = MilestoneJson.DeserializeList<string>(property.HouseRulesJson),
                property.CleaningFee,
                property.ServiceFee,
                property.Latitude,
                property.Longitude,
                property.ImageUrl,
                GalleryUrls = MilestoneJson.DeserializeList<string>(property.GalleryUrlsJson),
                property.IsArchived,
                property.IsDraft
            }),
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow(),
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId
        });
    }

    private sealed record PropertyRevisionSnapshot(
        string Title,
        string Location,
        string Country,
        decimal NightlyRate,
        string Currency,
        BadgeLevel BadgeLevel,
        bool GuestVerificationEnabled,
        bool InsuraGuestEnabled,
        string CancellationPolicy,
        IReadOnlyList<string>? Highlights,
        bool IsArchived,
        bool IsDraft,
        string? Parish = null,
        string? Description = null,
        int Bedrooms = 1,
        int Bathrooms = 1,
        int MaxGuests = 2,
        IReadOnlyList<string>? Amenities = null,
        IReadOnlyList<string>? SleepingArrangements = null,
        IReadOnlyList<string>? HouseRules = null,
        decimal CleaningFee = 0,
        decimal ServiceFee = 0,
        decimal? Latitude = null,
        decimal? Longitude = null,
        string? ImageUrl = null,
        IReadOnlyList<string>? GalleryUrls = null);

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

    public async Task<PropertyPhotoUploadDto> PreparePropertyPhotoUploadAsync(Guid hostUserId, Guid propertyId, PreparePropertyPhotoUploadRequest request, CancellationToken cancellationToken)
    {
        await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        var storage = RequireStorageProvider();
        var safeFileName = ValidatePropertyPhoto(request.FileName, request.ContentType, request.SizeBytes);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var photoId = Guid.NewGuid();
        var objectKey = $"properties/{propertyId:N}/photos/{photoId:N}{extension}";
        var uploadUrl = $"/api/properties/{propertyId}/photos/{photoId}/content";
        var now = timeProvider.GetUtcNow();

        var photo = new MilestonePropertyPhoto
        {
            Id = photoId,
            PropertyId = propertyId,
            HostUserId = hostUserId,
            OriginalFileName = Path.GetFileName(request.FileName.Trim()),
            SafeFileName = safeFileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            SortOrder = request.SortOrder,
            ObjectKey = objectKey,
            UploadUrl = uploadUrl,
            Status = UploadStatusPending,
            StorageProviderName = storage.ProviderName,
            ScanStatus = ScanStatusPending,
            UploadExpiresAt = now.Add(PropertyPhotoUploadLifetime),
            CreatedByUserId = hostUserId,
            UpdatedByUserId = hostUserId
        };

        db.MilestonePropertyPhotos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(photo);
    }

    public async Task<PropertyPhotoUploadDto> UploadPropertyPhotoContentAsync(Guid hostUserId, Guid propertyId, Guid photoId, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken)
    {
        await FindHostPropertyAsync(hostUserId, propertyId, cancellationToken);
        var photo = await db.MilestonePropertyPhotos
            .SingleOrDefaultAsync(item => item.Id == photoId && item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Property photo is not available to this host.");

        var now = await RequirePendingPropertyPhotoUploadAsync(photo, hostUserId, cancellationToken);
        ValidatePropertyPhotoUploadMetadata(photo, contentType, sizeBytes);

        var upload = await RequireStorageProvider().SaveObjectAsync(
            new StorageObjectWriteRequest(photo.ObjectKey, photo.ContentType, MaximumPropertyPhotoBytes),
            content,
            cancellationToken);
        ValidatePropertyPhotoUploadMetadata(photo, upload.ContentType, upload.SizeBytes);

        return await FinalizePropertyPhotoUploadAsync(
            photo,
            hostUserId,
            now,
            upload.ProviderName,
            upload.ContentType,
            upload.SizeBytes,
            upload.Sha256Hash,
            upload.HeaderBytes,
            cancellationToken);
    }

    public async Task<BookingQuoteDto> QuoteBookingAsync(BookingQuoteRequest request, CancellationToken cancellationToken)
    {
        await EnsurePhaseOneSeededAsync(cancellationToken);
        var property = await FindPropertyAsync(request.PropertyId, cancellationToken);
        var quote = await BuildQuoteAsync(property, request.CheckIn, request.CheckOut, true, null, request.Adults, request.Children, cancellationToken);
        var now = timeProvider.GetUtcNow();

        await ExpirePendingHoldsAsync(now, cancellationToken);
        if (await FindBlockingBookingAsync(property.Id, request.CheckIn, request.CheckOut, now, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Requested dates are already held or approved for this property.");
        }
        if (await FindBlockingCalendarAsync(property.Id, request.CheckIn, request.CheckOut, cancellationToken))
        {
            throw new InvalidOperationException("Requested dates are blocked by the property calendar.");
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
        var now = timeProvider.GetUtcNow();
        var booking = await PersistCreatedBookingAsync(request, now, cancellationToken);

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

    private async Task<MilestoneBooking> PersistCreatedBookingAsync(
        CreateBookingRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!SupportsSerializableTransactions())
        {
            await NonRelationalBookingCreationGate.WaitAsync(cancellationToken);
            try
            {
                return await PersistCreatedBookingCoreAsync(request, now, cancellationToken);
            }
            finally
            {
                NonRelationalBookingCreationGate.Release();
            }
        }

        for (var attempt = 1; attempt <= BookingCreationPersistenceRetries; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var booking = await PersistCreatedBookingCoreAsync(request, now, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return booking;
            }
            catch (Exception exception) when (IsBookingCreationConflict(exception))
            {
                // Npgsql can surface a serializable 40001 as an outer
                // InvalidOperationException from its execution strategy. In
                // that case the provider may already have completed the
                // transaction while unwinding the failed command; rollback is
                // still best effort and must not mask the retryable conflict.
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (InvalidOperationException rollbackException) when (
                    rollbackException.Message.Contains("transaction", StringComparison.OrdinalIgnoreCase) &&
                    rollbackException.Message.Contains("completed", StringComparison.OrdinalIgnoreCase))
                {
                    // The failed transaction is already unusable; the next
                    // attempt gets a clean EF change tracker/connection.
                }
                db.ChangeTracker.Clear();
                if (attempt == BookingCreationPersistenceRetries)
                {
                    throw new InvalidOperationException("Booking could not be created because another booking claimed the requested dates.", exception);
                }
            }
        }

        throw new InvalidOperationException("Booking could not be created after retrying a concurrent persistence conflict.");
    }

    private async Task<MilestoneBooking> PersistCreatedBookingCoreAsync(
        CreateBookingRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var property = await FindPropertyAsync(request.PropertyId, cancellationToken);
        var guest = await db.MilestoneUsers.SingleOrDefaultAsync(user => user.Id == request.GuestUserId, cancellationToken)
            ?? throw new InvalidOperationException("Guest user must register before booking.");
        var quote = await BuildQuoteAsync(property, request.CheckIn, request.CheckOut, true, null, request.Adults, request.Children, cancellationToken);

        await ExpirePendingHoldsAsync(now, cancellationToken);
        if (await FindBlockingBookingAsync(property.Id, request.CheckIn, request.CheckOut, now, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Requested dates are already held or approved for this property.");
        }
        if (await FindBlockingCalendarAsync(property.Id, request.CheckIn, request.CheckOut, cancellationToken))
        {
            throw new InvalidOperationException("Requested dates are blocked by the property calendar.");
        }

        await EnforceBookingCreationRateLimitAsync(guest.Id, now, cancellationToken);

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
                ? ["Booking created", "Dates held", $"{ekycProvider.ProviderName} started"]
                : ["Booking created", "No guest eKYC required", "Booking approved"])
        };

        db.MilestoneBookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    private bool SupportsSerializableTransactions() =>
        db.Database.IsRelational() &&
        !string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);

    private static bool IsBookingCreationConflict(Exception exception)
    {
        // Walk the provider/EF wrapper chain. Npgsql's execution strategy
        // commonly wraps DbUpdateException (and the underlying 40001) in an
        // InvalidOperationException, which previously bypassed this retry.
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateException || current is PostgresException { SqlState: "40001" })
            {
                return true;
            }
        }

        return false;
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
            if (BookingPaymentStateMachine.IsIdempotentVerificationResolution(booking.Status, booking.VerificationStatus, request.Passed))
            {
                return ToDto(booking);
            }

            BookingPaymentStateMachine.EnsureVerificationCanResolve(booking.Status, booking.VerificationStatus, request.Passed);
        }

        IReadOnlyList<PendingNotification> notifications;
        if (!request.Passed)
        {
            BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, BookingStatus.Rejected, "resolve_verification");
            BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, PaymentStatus.Cancelled, "resolve_verification", booking.Status);
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Failed;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.HoldExpiresAt = null;
            booking.RejectionReason = string.IsNullOrWhiteSpace(request.FailureReason)
                ? "identity verification failed with the configured provider"
                : request.FailureReason.Trim();
            booking.RejectionSource = "GuestVerification";
            booking.RejectedAt = timeProvider.GetUtcNow();
            AddTimeline(booking, $"{ekycProvider.ProviderName} failed", "Booking rejected", "Dates released");
            notifications = BuildRejectionNotifications(booking);
        }
        else
        {
            BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, BookingStatus.Approved, "resolve_verification");
            booking.Status = BookingStatus.Approved;
            booking.VerificationStatus = VerificationStatus.Passed;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, $"{ekycProvider.ProviderName} approved", "Booking approved");
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

    public async Task<BookingDto?> RejectBookingAsync(Guid hostUserId, Guid bookingId, BookingDecisionRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(item => item.Id == bookingId && !item.IsDeleted, cancellationToken);
        if (booking is null) return null;
        if (booking.HostUserId != hostUserId)
        {
            throw new UnauthorizedAccessException("Booking is not available to this host.");
        }

        if (booking.Status is not (BookingStatus.PendingVerification or BookingStatus.Approved))
        {
            throw new InvalidOperationException("Only pending or approved booking requests can be rejected by the host.");
        }

        if (booking.PaymentStatus is PaymentStatus.Captured or PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("Captured bookings must be cancelled or refunded through the payment workflow.");
        }

        var reason = NormalizeDecisionReason(request.Reason);
        BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, BookingStatus.Rejected, "host_reject_booking");
        if (booking.PaymentStatus is PaymentStatus.Pending or PaymentStatus.Authorized)
        {
            BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, PaymentStatus.Cancelled, "host_reject_booking", booking.Status);
            booking.PaymentStatus = PaymentStatus.Cancelled;
        }

        booking.Status = BookingStatus.Rejected;
        booking.HoldExpiresAt = null;
        booking.RejectionReason = reason;
        booking.RejectionSource = "Host";
        booking.RejectedByUserId = hostUserId;
        booking.RejectedAt = timeProvider.GetUtcNow();
        AddTimeline(booking, "Host rejected booking", $"Reason: {reason}", "Dates released");
        await QueueNotificationsAsync(booking, BuildHostRejectionNotifications(booking, reason), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(booking);
    }

    public async Task<HostVerificationDto> GetHostVerificationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.MilestoneUsers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Host account was not found.");
        if (!MilestoneJson.DeserializeList<UserRole>(user.RolesJson).Contains(UserRole.Host))
        {
            throw new UnauthorizedAccessException("Only host accounts can access host verification.");
        }

        return ToHostVerificationDto(user);
    }

    public async Task<HostVerificationDto> SubmitHostVerificationAsync(Guid userId, SubmitHostVerificationRequest request, CancellationToken cancellationToken)
    {
        var documentType = NormalizeHostDocumentType(request.DocumentType);
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == userId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Host account was not found.");
        if (!MilestoneJson.DeserializeList<UserRole>(user.RolesJson).Contains(UserRole.Host))
        {
            throw new UnauthorizedAccessException("Only host accounts can submit host verification.");
        }

        user.HostVerificationStatus = "Pending";
        user.HostVerificationDocumentType = documentType;
        user.HostVerificationReason = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()[..Math.Min(500, request.Notes.Trim().Length)];
        user.HostVerificationSubmittedAt = timeProvider.GetUtcNow();
        user.HostVerificationReviewedAt = null;
        user.HostVerificationReviewedByUserId = null;
        user.UpdatedAt = timeProvider.GetUtcNow();
        user.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
        return ToHostVerificationDto(user);
    }

    public async Task<IReadOnlyList<HostVerificationQueueItemDto>> GetHostVerificationQueueAsync(CancellationToken cancellationToken)
    {
        await EnsurePhaseOneSeededAsync(cancellationToken);
        var users = await db.MilestoneUsers.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.HostVerificationStatus == "Pending" ? 0 : 1)
            .ThenByDescending(item => item.HostVerificationSubmittedAt)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(cancellationToken);

        return users
            .Where(item => MilestoneJson.DeserializeList<UserRole>(item.RolesJson).Contains(UserRole.Host))
            .Select(ToHostVerificationQueueItemDto)
            .ToList();
    }

    public async Task<HostVerificationQueueItemDto?> ReviewHostVerificationAsync(
        Guid adminUserId,
        Guid hostUserId,
        HostVerificationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var status = NormalizeHostVerificationDecision(request.Status);
        var user = await db.MilestoneUsers.SingleOrDefaultAsync(item => item.Id == hostUserId && !item.IsDeleted, cancellationToken);
        if (user is null) return null;
        if (!MilestoneJson.DeserializeList<UserRole>(user.RolesJson).Contains(UserRole.Host))
        {
            throw new InvalidOperationException("Only host accounts can be reviewed here.");
        }

        var reason = status == "Rejected" ? NormalizeDecisionReason(request.Reason) : NormalizeOptionalReason(request.Reason);
        var now = timeProvider.GetUtcNow();
        user.HostVerificationStatus = status;
        user.HostVerificationReason = reason;
        user.HostVerificationReviewedAt = now;
        user.HostVerificationReviewedByUserId = adminUserId;
        user.UpdatedAt = now;
        user.UpdatedByUserId = adminUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToHostVerificationQueueItemDto(user);
    }

    public async Task<IReadOnlyList<PropertyListingDto>> GetModerationQueueAsync(CancellationToken cancellationToken)
    {
        EnsurePhaseOneSeeded();
        var properties = await db.MilestoneProperties.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.ModerationStatus == "Approved")
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
        return properties.Select(ToListingDto).ToList();
    }

    public async Task<PropertyListingDto?> ModeratePropertyAsync(Guid adminUserId, Guid propertyId, PropertyModerationRequest request, CancellationToken cancellationToken)
    {
        var status = NormalizeModerationStatus(request.Status);
        var reason = status is "Rejected" or "ChangesRequested" ? NormalizeDecisionReason(request.Reason) : null;
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && !item.IsDeleted, cancellationToken);
        if (property is null) return null;

        property.ModerationStatus = status;
        property.ModerationReason = reason;
        property.ModeratedAt = timeProvider.GetUtcNow();
        property.ModeratedByUserId = adminUserId;
        if (status == "Approved") property.IsDraft = false;
        property.UpdatedAt = timeProvider.GetUtcNow();
        property.UpdatedByUserId = adminUserId;
        db.MilestoneTravelerNotifications.Add(new MilestoneTravelerNotification
        {
            UserId = property.HostUserId,
            Type = "Moderation",
            Title = $"Listing {status.ToLowerInvariant()}",
            Body = status == "Approved"
                ? $"{property.Title} is approved and visible in Explore."
                : $"{property.Title} needs attention: {reason ?? "Please review the listing details."}",
            DeepLink = $"/host/properties/edit?id={property.Id}",
            IsRead = false,
            CreatedAt = property.ModeratedAt.Value,
            UpdatedAt = property.ModeratedAt.Value,
            CreatedByUserId = adminUserId,
            UpdatedByUserId = adminUserId
        });
        await db.SaveChangesAsync(cancellationToken);
        return ToListingDto(property);
    }

    public async Task<BookingDto?> CapturePaymentAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        BookingPaymentStateMachine.EnsureCaptureCanStart(booking.Status, booking.PaymentStatus);

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

        BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, capture.Status, "capture_payment", booking.Status);
        BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, BookingStatus.PaymentCaptured, "capture_payment");
        booking.PaymentProvider = capture.ProviderName;
        booking.PaymentCaptureReference = capture.CaptureReference;
        booking.PaymentStatus = capture.Status;
        booking.Status = BookingStatus.PaymentCaptured;
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

        BookingPaymentStateMachine.EnsureRefundCanStart(booking.Status, booking.PaymentStatus);

        if (string.IsNullOrWhiteSpace(booking.PaymentCaptureReference))
        {
            throw new BookingStateConflictException(
                "Refunds require a payment capture reference.",
                "refund_payment",
                booking.Status,
                booking.PaymentStatus);
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

    public async Task<BookingDto?> ApplyPaymentWebhookAsync(PaymentWebhookUpdateRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.MilestoneBookings.SingleOrDefaultAsync(item =>
                item.PaymentAuthorizationReference == request.PaymentIntentReference ||
                item.PaymentCaptureReference == request.PaymentIntentReference,
            cancellationToken);
        if (booking is null)
        {
            return null;
        }

        var idempotencyKey = $"{request.ProviderName.ToLowerInvariant()}:webhook:{request.ProviderEventId}";
        var amount = request.Amount ?? 0m;
        var currency = request.Currency ?? booking.Currency;
        var attempt = await BeginPaymentAttemptAsync(booking.Id, $"{PaymentOperationWebhook}:{request.EventType}", idempotencyKey, amount, currency, cancellationToken);
        if (attempt.CompletedAt is not null)
        {
            return ToDto(booking);
        }

        ApplyPaymentWebhookToBooking(booking, request);
        CompletePaymentAttempt(attempt, request.ProviderName, request.ProviderReference ?? request.PaymentIntentReference, request.Status);
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
                    string.IsNullOrWhiteSpace(request.DocumentType) ? "GLB03002" : request.DocumentType.Trim(),
                    request.EkycCallbackUrl),
                cancellationToken);

            booking.EkycProvider = result.ProviderName;
            booking.EkycTransactionId = result.TransactionId;
            booking.EkycTransactionUrl = result.TransactionUrl;
            booking.VerificationStatus = result.Status;
            AddTimeline(booking, $"{result.ProviderName} transaction created: {result.TransactionId}");
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            booking.Status = BookingStatus.Rejected;
            booking.VerificationStatus = VerificationStatus.Failed;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.HoldExpiresAt = null;
            AddTimeline(booking, $"{ekycProvider.ProviderName} could not be started", "Dates released");
            await db.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException($"{ekycProvider.ProviderName} could not be started for this booking.", exception);
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

        BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, authorization.Status, "authorize_payment", booking.Status);
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
            db.MilestoneTravelerNotifications.Add(new MilestoneTravelerNotification
            {
                UserId = notification.RecipientUserId,
                Type = notification.Type,
                Title = notification.Message.Subject,
                Body = notification.Message.Body,
                DeepLink = notification.DeepLink,
                IsRead = false,
                CreatedAt = queuedAt,
                UpdatedAt = queuedAt,
                CreatedByUserId = notification.RecipientUserId,
                UpdatedByUserId = notification.RecipientUserId
            });
            AddTimeline(booking, $"Notification queued for {notification.RecipientType}");
        }

        booking.NotificationsJson = MilestoneJson.Serialize(existing);

        foreach (var notification in notifications)
        {
            await notificationGateway.QueueAsync(notification.Message, cancellationToken);
        }
    }

    private async Task<BookingQuoteDto> BuildQuoteAsync(
        MilestoneProperty property,
        DateOnly checkIn,
        DateOnly checkOut,
        bool datesAvailable,
        DateTimeOffset? holdExpiresAt,
        int adults,
        int children,
        CancellationToken cancellationToken)
    {
        if (checkOut <= checkIn)
        {
            throw new InvalidOperationException("Check-out must be after check-in.");
        }

        if (adults < 1 || children < 0 || adults + children > property.MaxGuests)
        {
            throw new InvalidOperationException($"This stay accommodates up to {property.MaxGuests} guests.");
        }

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var pricingRules = await db.MilestoneHostPricingRules.AsNoTracking()
            .Where(rule => rule.PropertyId == property.Id && rule.HostUserId == property.HostUserId && rule.IsActive && rule.StartsOn <= checkOut.AddDays(-1) && rule.EndsOn >= checkIn)
            .ToListAsync(cancellationToken);
        var promotions = await db.MilestoneHostPromotions.AsNoTracking()
            .Where(promotion => promotion.PropertyId == property.Id && promotion.HostUserId == property.HostUserId && promotion.IsActive && promotion.StartsOn <= checkOut.AddDays(-1) && promotion.EndsOn >= checkIn)
            .ToListAsync(cancellationToken);
        var nightlyRates = Enumerable.Range(0, nights).Select(offset =>
        {
            var date = checkIn.AddDays(offset);
            return pricingRules
                .Where(rule => rule.StartsOn <= date && rule.EndsOn >= date)
                .OrderByDescending(rule => rule.StartsOn)
                .ThenByDescending(rule => rule.CreatedAt)
                .Select(rule => rule.NightlyRate)
                .FirstOrDefault(property.NightlyRate);
        }).ToList();
        var averageNightlyRate = decimal.Round(nightlyRates.Average(), 2);
        var baseStaySubtotal = decimal.Round(nightlyRates.Sum(), 2);
        var promotion = promotions
            .Where(item => item.MinimumNights <= nights && item.StartsOn <= checkIn && item.EndsOn >= checkOut.AddDays(-1))
            .OrderByDescending(item => item.DiscountPercent)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        var discountAmount = promotion is null ? 0m : decimal.Round(baseStaySubtotal * promotion.DiscountPercent / 100m, 2);
        var staySubtotal = decimal.Round(baseStaySubtotal - discountAmount, 2);
        var guestFeePercent = NestyStayBusinessRules.ResolveStandardGuestFeePercent(staySubtotal, nights);
        var guestPlatformFee = decimal.Round(staySubtotal * guestFeePercent / 100m, 2);
        var total = decimal.Round(staySubtotal + guestPlatformFee, 2);
        var lines = new List<BookingPriceLineDto>
        {
            new("stay", $"{averageNightlyRate:0.00} average x {nights} night stay", baseStaySubtotal, property.Currency, true),
            new("guest-platform-fee", $"{guestFeePercent:0}% NestyStay guest platform fee", guestPlatformFee, property.Currency, false)
        };

        if (pricingRules.Count > 0 && nightlyRates.Any(rate => rate != property.NightlyRate))
            lines.Insert(1, new("pricing-override", "Server-applied date range pricing", 0m, property.Currency, true));
        if (promotion is not null)
            lines.Insert(2, new("promotion-discount", $"{promotion.DiscountPercent:0.##}% discount · {promotion.Name}", -discountAmount, property.Currency, true));

        if (property.CleaningFee > 0)
        {
            lines.Add(new("cleaning-fee", "Cleaning fee", property.CleaningFee, property.Currency, true));
            total += property.CleaningFee;
        }

        if (property.ServiceFee > 0)
        {
            lines.Add(new("service-fee", "Service fee", property.ServiceFee, property.Currency, false));
            total += property.ServiceFee;
        }

        total = decimal.Round(total, 2);

        if (property.GuestVerificationEnabled)
        {
            lines.Add(new("guest-verification", $"{ekycProvider.ProviderName} verification required before approval", 0m, property.Currency, false));
        }

        return new BookingQuoteDto(
            ToSummaryDto(property),
            checkIn,
            checkOut,
            nights,
            averageNightlyRate,
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

    private async Task<DateTimeOffset> RequirePendingProfilePhotoUploadAsync(MilestoneUserProfilePhoto photo, Guid userId, CancellationToken cancellationToken)
    {
        if (photo.Status != UploadStatusPending)
        {
            throw new InvalidOperationException("Profile photo upload is not pending.");
        }

        var now = timeProvider.GetUtcNow();
        if (photo.UploadExpiresAt <= now)
        {
            photo.Status = UploadStatusExpired;
            photo.UpdatedAt = now;
            photo.UpdatedByUserId = userId;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Profile photo upload URL has expired.");
        }

        return now;
    }

    private async Task<ProfilePhotoUploadDto> FinalizeProfilePhotoUploadAsync(
        MilestoneUserProfilePhoto photo,
        Guid userId,
        DateTimeOffset verifiedAt,
        string providerName,
        string contentType,
        long sizeBytes,
        string sha256Hash,
        byte[] headerBytes,
        CancellationToken cancellationToken)
    {
        var scanner = RequireFileSafetyScanner();
        var scan = await scanner.ScanAsync(
            new FileSafetyScanRequest(photo.ObjectKey, photo.SafeFileName, contentType, sizeBytes, sha256Hash, headerBytes),
            cancellationToken);

        photo.StorageProviderName = providerName;
        photo.VerifiedContentType = contentType;
        photo.UploadedSizeBytes = sizeBytes;
        photo.Sha256Hash = sha256Hash;
        photo.ScanStatus = scan.Status;
        photo.ScanProviderName = scanner.ProviderName;
        photo.ScanCheckedAt = verifiedAt;
        photo.UpdatedAt = verifiedAt;
        photo.UpdatedByUserId = userId;

        if (!scan.Status.Equals(ScanStatusClean, StringComparison.OrdinalIgnoreCase))
        {
            photo.Status = UploadStatusQuarantined;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(scan.Reason ?? "Profile photo failed safety scanning.");
        }

        var currentPhotos = await db.MilestoneUserProfilePhotos
            .Where(item => item.UserId == userId && item.IsCurrent)
            .ToListAsync(cancellationToken);
        foreach (var currentPhoto in currentPhotos)
        {
            currentPhoto.IsCurrent = false;
        }

        photo.Status = UploadStatusUploaded;
        photo.UploadedAt = verifiedAt;
        photo.IsCurrent = true;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(photo);
    }

    private async Task<DateTimeOffset> RequirePendingPropertyPhotoUploadAsync(MilestonePropertyPhoto photo, Guid hostUserId, CancellationToken cancellationToken)
    {
        if (photo.Status != UploadStatusPending)
        {
            throw new InvalidOperationException("Property photo upload is not pending.");
        }

        var now = timeProvider.GetUtcNow();
        if (photo.UploadExpiresAt <= now)
        {
            photo.Status = UploadStatusExpired;
            photo.UpdatedAt = now;
            photo.UpdatedByUserId = hostUserId;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Property photo upload URL has expired.");
        }

        return now;
    }

    private async Task<PropertyPhotoUploadDto> FinalizePropertyPhotoUploadAsync(
        MilestonePropertyPhoto photo,
        Guid hostUserId,
        DateTimeOffset verifiedAt,
        string providerName,
        string contentType,
        long sizeBytes,
        string sha256Hash,
        byte[] headerBytes,
        CancellationToken cancellationToken)
    {
        var scanner = RequireFileSafetyScanner();
        var scan = await scanner.ScanAsync(
            new FileSafetyScanRequest(photo.ObjectKey, photo.SafeFileName, contentType, sizeBytes, sha256Hash, headerBytes),
            cancellationToken);

        photo.StorageProviderName = providerName;
        photo.VerifiedContentType = contentType;
        photo.UploadedSizeBytes = sizeBytes;
        photo.Sha256Hash = sha256Hash;
        photo.ScanStatus = scan.Status;
        photo.ScanProviderName = scanner.ProviderName;
        photo.ScanCheckedAt = verifiedAt;
        photo.UpdatedAt = verifiedAt;
        photo.UpdatedByUserId = hostUserId;

        if (!scan.Status.Equals(ScanStatusClean, StringComparison.OrdinalIgnoreCase))
        {
            photo.Status = UploadStatusQuarantined;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(scan.Reason ?? "Property photo failed safety scanning.");
        }

        photo.Status = UploadStatusUploaded;
        photo.UploadedAt = verifiedAt;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(photo);
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

    private async Task<bool> FindBlockingCalendarAsync(Guid propertyId, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken) =>
        await db.MilestoneCalendarBlocks.AnyAsync(block => block.PropertyId == propertyId && !block.IsDeleted && block.StartsOn < checkOut && checkIn < block.EndsOn, cancellationToken) ||
        await db.MilestoneCalendarManualBlocks.AnyAsync(block => block.PropertyId == propertyId && block.Status == "ACTIVE" && !block.IsDeleted && block.StartsOn < checkOut && checkIn < block.EndsOn, cancellationToken);

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
            booking.RejectionReason = "identity verification timed out before completion";
            booking.RejectionSource = "GuestVerification";
            booking.RejectedAt = now;
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
            booking.RejectionReason = "identity verification timed out before completion";
            booking.RejectionSource = "GuestVerification";
            booking.RejectedAt = now;
            AddTimeline(booking, "Pending verification hold expired", "Dates released");
        }

        if (expired.Count > 0)
        {
            db.SaveChanges();
        }
    }

    private void EnsurePhaseOneSeeded()
    {
        var changed = false;
        foreach (var seed in DefaultHostUsers())
        {
            var exists = db.MilestoneUsers.Any(item =>
                item.Id == seed.Id || item.NormalizedEmail == seed.NormalizedEmail);
            if (!exists)
            {
                db.MilestoneUsers.Add(seed);
                changed = true;
            }
        }

        if (!db.MilestoneProperties.Any())
        {
            db.MilestoneProperties.AddRange(DefaultProperties());
            changed = true;
        }
        else
        {
            changed |= BackfillDefaultPropertyDetails(DefaultProperties());
        }

        if (changed)
        {
            db.SaveChanges();
        }
    }

    private async Task EnsurePhaseOneSeededAsync(CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var seed in DefaultHostUsers())
        {
            var exists = await db.MilestoneUsers.AnyAsync(item =>
                item.Id == seed.Id || item.NormalizedEmail == seed.NormalizedEmail,
                cancellationToken);
            if (!exists)
            {
                db.MilestoneUsers.Add(seed);
                changed = true;
            }
        }

        if (!await db.MilestoneProperties.AnyAsync(cancellationToken))
        {
            db.MilestoneProperties.AddRange(DefaultProperties());
            changed = true;
        }
        else
        {
            changed |= BackfillDefaultPropertyDetails(DefaultProperties());
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private bool BackfillDefaultPropertyDetails(IReadOnlyList<MilestoneProperty> seeds)
    {
        var changed = false;
        foreach (var seed in seeds)
        {
            // Reconcile missing deterministic fixtures even when an older
            // database already contains some properties. This keeps existing
            // development/test databases aligned with a clean seed without
            // reviving a fixture that was explicitly soft-deleted.
            var property = db.MilestoneProperties.SingleOrDefault(item => item.Id == seed.Id);
            if (property is null)
            {
                db.MilestoneProperties.Add(seed);
                changed = true;
                continue;
            }
            if (property.IsDeleted) continue;
            if (string.IsNullOrWhiteSpace(property.ModerationStatus)) { property.ModerationStatus = "Approved"; changed = true; }
            if (string.IsNullOrWhiteSpace(property.Parish)) { property.Parish = seed.Parish; changed = true; }
            if (string.IsNullOrWhiteSpace(property.Description)) { property.Description = seed.Description; changed = true; }
            if (property.Bedrooms <= 0) { property.Bedrooms = seed.Bedrooms; changed = true; }
            if (property.Bathrooms <= 0) { property.Bathrooms = seed.Bathrooms; changed = true; }
            if (property.MaxGuests <= 0) { property.MaxGuests = seed.MaxGuests; changed = true; }
            if (property.AmenitiesJson is "[]" or "" or "{}") { property.AmenitiesJson = seed.AmenitiesJson; changed = true; }
            if (property.SleepingArrangementsJson is "[]" or "" or "{}") { property.SleepingArrangementsJson = seed.SleepingArrangementsJson; changed = true; }
            if (property.HouseRulesJson is "[]" or "" or "{}") { property.HouseRulesJson = seed.HouseRulesJson; changed = true; }
            if (property.CleaningFee == 0) { property.CleaningFee = seed.CleaningFee; changed = true; }
            if (property.ServiceFee == 0) { property.ServiceFee = seed.ServiceFee; changed = true; }
            if (property.Latitude is null) { property.Latitude = seed.Latitude; changed = true; }
            if (property.Longitude is null) { property.Longitude = seed.Longitude; changed = true; }
            if (string.IsNullOrWhiteSpace(property.ImageUrl)) { property.ImageUrl = seed.ImageUrl; changed = true; }
            if (property.GalleryUrlsJson is "[]" or "" or "{}") { property.GalleryUrlsJson = seed.GalleryUrlsJson; changed = true; }
        }
        return changed;
    }

    private IReadOnlyList<PendingNotification> BuildApprovalNotifications(MilestoneBooking booking) =>
    [
        new("guest", booking.GuestUserId, "Booking", $"/booking/{booking.Id}/checkout", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay booking approved",
            $"Your booking for {booking.PropertyTitle} is APPROVED after {ekycProvider.ProviderName}.")),
        new("host", booking.HostUserId, "Booking", $"/bookings?bookingId={booking.Id}", new NotificationMessage(
            booking.HostEmail,
            "NestyStay booking approved",
            $"{booking.GuestName}'s booking for {booking.PropertyTitle} is APPROVED."))
    ];

    private static IReadOnlyList<PendingNotification> BuildRejectionNotifications(MilestoneBooking booking) =>
    [
        new("guest", booking.GuestUserId, "Booking", $"/booking/{booking.Id}/rejected", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay booking rejected",
            $"Your booking for {booking.PropertyTitle} was REJECTED because {booking.RejectionReason ?? "the booking could not be approved"}.")),
        new("host", booking.HostUserId, "Booking", $"/bookings?bookingId={booking.Id}", new NotificationMessage(
            booking.HostEmail,
            "NestyStay booking dates released",
            $"{booking.GuestName}'s booking for {booking.PropertyTitle} was rejected and the dates were released."))
    ];

    private static IReadOnlyList<PendingNotification> BuildHostRejectionNotifications(MilestoneBooking booking, string reason) =>
    [
        new("guest", booking.GuestUserId, "Booking", $"/booking/{booking.Id}/rejected", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay booking request declined",
            $"Your booking request for {booking.PropertyTitle} was declined by the host. Reason: {reason}")),
        new("host", booking.HostUserId, "Booking", "/bookings", new NotificationMessage(
            booking.HostEmail,
            "NestyStay booking request declined",
            $"You declined {booking.GuestName}'s booking request for {booking.PropertyTitle}."))
    ];

    private static string NormalizeDecisionReason(string? reason)
    {
        var normalized = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("A clear decision reason is required.");
        }

        return normalized[..Math.Min(500, normalized.Length)];
    }

    private static string? NormalizeOptionalReason(string? reason)
    {
        var normalized = reason?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized[..Math.Min(500, normalized.Length)];
    }

    private static string NormalizeHostDocumentType(string documentType)
    {
        var normalized = documentType?.Trim();
        if (normalized is not ("Passport" or "National ID" or "Driver License"))
        {
            throw new InvalidOperationException("Choose Passport, National ID, or Driver License.");
        }

        return normalized;
    }

    private static string NormalizeHostVerificationDecision(string status)
    {
        var normalized = status?.Trim();
        if (normalized is not ("Approved" or "Rejected"))
        {
            throw new InvalidOperationException("Host verification decisions must be Approved or Rejected.");
        }

        return normalized;
    }

    private static string NormalizeModerationStatus(string status)
    {
        var normalized = status?.Trim();
        if (normalized is not ("Pending" or "Approved" or "Rejected" or "ChangesRequested"))
        {
            throw new InvalidOperationException("Moderation status must be Pending, Approved, Rejected, or ChangesRequested.");
        }

        return normalized;
    }

    private static HostVerificationDto ToHostVerificationDto(MilestoneUser user) =>
        new(
            user.Id,
            user.HostVerificationStatus,
            user.HostVerificationDocumentType,
            user.HostVerificationReason,
            user.HostVerificationSubmittedAt,
            user.HostVerificationReviewedAt,
            user.HostVerificationReviewedByUserId,
            [
                "Complete your host profile",
                "Submit a government-issued identity document",
                "Keep your payout details up to date"
            ]);

    private static HostVerificationQueueItemDto ToHostVerificationQueueItemDto(MilestoneUser user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.HostVerificationStatus,
            user.HostVerificationDocumentType,
            user.HostVerificationReason,
            user.HostVerificationSubmittedAt,
            user.HostVerificationReviewedAt,
            user.HostVerificationReviewedByUserId,
            [
                "Complete your host profile",
                "Submit a government-issued identity document",
                "Keep your payout details up to date"
            ]);

    private static IReadOnlyList<PendingNotification> BuildPaymentCapturedNotifications(MilestoneBooking booking) =>
    [
        new("guest", booking.GuestUserId, "Payments", $"/booking/{booking.Id}/receipt", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay payment processed",
            $"Stripe payment for {booking.PropertyTitle} has been captured.")),
        new("host", booking.HostUserId, "Payments", $"/bookings?bookingId={booking.Id}", new NotificationMessage(
            booking.HostEmail,
            "NestyStay payment processed",
            $"Stripe payment for {booking.GuestName}'s booking has been captured."))
    ];

    private static IReadOnlyList<PendingNotification> BuildPaymentRefundedNotifications(MilestoneBooking booking, decimal amount, string currency) =>
    [
        new("guest", booking.GuestUserId, "Payments", "/traveler/invoices", new NotificationMessage(
            booking.GuestEmail,
            "NestyStay payment refunded",
            $"Refund of {currency.ToUpperInvariant()} {amount:0.00} has been issued for {booking.PropertyTitle}.")),
        new("host", booking.HostUserId, "Payments", $"/bookings?bookingId={booking.Id}", new NotificationMessage(
            booking.HostEmail,
            "NestyStay payment refunded",
            $"Refund of {currency.ToUpperInvariant()} {amount:0.00} has been issued for {booking.GuestName}'s booking."))
    ];

    private IStorageProvider RequireStorageProvider() =>
        storageProvider ?? throw new InvalidOperationException("Storage provider is not configured.");

    private IFileSafetyScanner RequireFileSafetyScanner() =>
        fileSafetyScanner ?? throw new InvalidOperationException("File safety scanner is not configured.");

    private PropertyListingDto ToListingDto(MilestoneProperty property)
    {
        var reviewStats = db.MilestoneReviews
            .AsNoTracking()
            .Where(review => review.PropertyId == property.Id && !review.IsDeleted && review.Status == "Published")
            .GroupBy(review => review.PropertyId)
            .Select(group => new
            {
                Average = group.Average(review => (decimal)review.Rating),
                Count = group.Count()
            })
            .SingleOrDefault();

        return new(
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
            property.IsArchived,
            property.IsDraft,
            property.Parish,
            property.Description,
            property.Bedrooms,
            property.Bathrooms,
            property.MaxGuests,
            MilestoneJson.DeserializeList<string>(property.AmenitiesJson),
            MilestoneJson.DeserializeList<string>(property.SleepingArrangementsJson),
            MilestoneJson.DeserializeList<string>(property.HouseRulesJson),
            property.CleaningFee,
            property.ServiceFee,
            property.Latitude,
            property.Longitude,
            property.ImageUrl,
            MilestoneJson.DeserializeList<string>(property.GalleryUrlsJson),
            reviewStats?.Average ?? 0,
            reviewStats?.Count ?? 0,
            property.ModerationStatus,
            property.ModerationReason,
            property.ModeratedAt,
            property.ModeratedByUserId,
            db.MilestoneUsers.AsNoTracking().Where(user => user.Id == property.HostUserId).Select(user => user.HostVerificationStatus).FirstOrDefault() is { } verificationStatus && !string.IsNullOrWhiteSpace(verificationStatus) ? verificationStatus : "NotStarted");
    }

    private static UserProfileDto ToProfileDto(MilestoneUser user, MilestoneUserProfilePhoto? photo) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Phone,
            MilestoneJson.DeserializeList<UserRole>(user.RolesJson),
            user.IsTwoFactorEnabled,
            photo is null
                ? null
                : new UserProfilePhotoDto(
                    photo.Id,
                    photo.SafeFileName,
                    photo.ContentType,
                    photo.SizeBytes,
                    photo.Status,
                    photo.ScanStatus,
                    photo.UploadedAt ?? photo.UpdatedAt,
                    photo.Sha256Hash));

    private static ProfilePhotoUploadDto ToDto(MilestoneUserProfilePhoto photo) =>
        new(
            photo.Id,
            photo.UserId,
            photo.SafeFileName,
            photo.ContentType,
            photo.SizeBytes,
            photo.ObjectKey,
            photo.UploadUrl,
            photo.Status,
            photo.ScanStatus,
            photo.UploadExpiresAt,
            photo.Sha256Hash);

    private static PropertyPhotoUploadDto ToDto(MilestonePropertyPhoto photo) =>
        new(
            photo.Id,
            photo.PropertyId,
            photo.HostUserId,
            photo.SafeFileName,
            photo.ContentType,
            photo.SizeBytes,
            photo.ObjectKey,
            photo.UploadUrl,
            photo.Status,
            photo.ScanStatus,
            photo.UploadExpiresAt,
            photo.Sha256Hash);

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
            property.CancellationPolicy,
            property.MaxGuests,
            property.CleaningFee,
            property.ServiceFee);

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
            MilestoneJson.DeserializeList<string>(booking.TimelineJson),
            booking.RejectionReason,
            booking.RejectionSource,
            booking.RejectedByUserId,
            booking.RejectedAt);

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

        if (request.Role is not (UserRole.Guest or UserRole.Host or UserRole.Owner or UserRole.PropertyManager or UserRole.Officer or UserRole.ServiceProvider or UserRole.LocalBusiness))
        {
            throw new InvalidOperationException("Only traveler, host, owner, property manager, officer, and provider self-service registration is available.");
        }
    }

    private static void ValidateAdministratorBootstrap(AdministratorBootstrapRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("Administrator email, password, and display name are required.");
        }

        try
        {
            var address = new MailAddress(request.Email.Trim());
            if (!address.Address.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A valid administrator email address is required.");
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("A valid administrator email address is required.");
        }

        ValidatePasswordPolicy(request.Password);
        _ = AdminPermissionCatalog.Normalize(request.Permissions, defaultToSuperAdministration: true);
    }

    private static IReadOnlyList<string> ReadAdminPermissions(MilestoneUser user) =>
        AdminPermissionCatalog.Normalize(MilestoneJson.DeserializeList<string>(user.AdminPermissionsJson));

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

    private static string ValidateProfilePhoto(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumProfilePhotoBytes)
        {
            throw new InvalidOperationException("Profile photos must be 10 MB or smaller.");
        }

        var normalizedContentType = NormalizeUploadContentType(contentType);
        if (!AllowedPropertyPhotoExtensions.TryGetValue(normalizedContentType, out var allowedExtensions))
        {
            throw new InvalidOperationException("Profile photo type is not allowed.");
        }

        var originalFileName = Path.GetFileName(fileName.Trim());
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Profile photo extension does not match the content type.");
        }

        var stem = Path.GetFileNameWithoutExtension(originalFileName).Trim().ToLowerInvariant();
        var safeStem = new string(stem.Select(character => IsSafeUploadFileNameCharacter(character) ? character : '-').ToArray());
        safeStem = string.Join("-", safeStem.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeStem))
        {
            safeStem = "profile-photo";
        }

        if (safeStem.Length > 80)
        {
            safeStem = safeStem[..80];
        }

        return $"{safeStem}{extension}";
    }

    private static string ValidatePropertyPhoto(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumPropertyPhotoBytes)
        {
            throw new InvalidOperationException("Property photos must be 10 MB or smaller.");
        }

        var normalizedContentType = NormalizeUploadContentType(contentType);
        if (!AllowedPropertyPhotoExtensions.TryGetValue(normalizedContentType, out var allowedExtensions))
        {
            throw new InvalidOperationException("Property photo type is not allowed.");
        }

        var originalFileName = Path.GetFileName(fileName.Trim());
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Property photo extension does not match the content type.");
        }

        var stem = Path.GetFileNameWithoutExtension(originalFileName).Trim().ToLowerInvariant();
        var safeStem = new string(stem.Select(character => IsSafeUploadFileNameCharacter(character) ? character : '-').ToArray());
        safeStem = string.Join("-", safeStem.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeStem))
        {
            safeStem = "property-photo";
        }

        if (safeStem.Length > 80)
        {
            safeStem = safeStem[..80];
        }

        return $"{safeStem}{extension}";
    }

    private static void ValidateProfilePhotoUploadMetadata(MilestoneUserProfilePhoto photo, string? contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumProfilePhotoBytes)
        {
            throw new InvalidOperationException("Profile photos must be 10 MB or smaller.");
        }

        if (!NormalizeUploadContentType(contentType).Equals(photo.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded profile photo content type does not match the prepared upload.");
        }

        if (sizeBytes != photo.SizeBytes)
        {
            throw new InvalidOperationException("Uploaded profile photo size does not match the prepared upload.");
        }
    }

    private static void ValidatePropertyPhotoUploadMetadata(MilestonePropertyPhoto photo, string? contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumPropertyPhotoBytes)
        {
            throw new InvalidOperationException("Property photos must be 10 MB or smaller.");
        }

        if (!NormalizeUploadContentType(contentType).Equals(photo.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded property photo content type does not match the prepared upload.");
        }

        if (sizeBytes != photo.SizeBytes)
        {
            throw new InvalidOperationException("Uploaded property photo size does not match the prepared upload.");
        }
    }

    private static string NormalizeUploadContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new InvalidOperationException("Uploaded content type is required.");
        }

        return contentType.Trim().ToLowerInvariant();
    }

    private static bool IsSafeUploadFileNameCharacter(char character) =>
        (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-';

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
        property.Parish = request.Parish?.Trim() ?? string.Empty;
        property.Description = request.Description?.Trim() ?? string.Empty;
        property.Bedrooms = Math.Max(1, request.Bedrooms);
        property.Bathrooms = Math.Max(1, request.Bathrooms);
        property.MaxGuests = Math.Max(1, request.MaxGuests);
        property.AmenitiesJson = MilestoneJson.Serialize(NormalizeStringList(request.Amenities));
        property.SleepingArrangementsJson = MilestoneJson.Serialize(NormalizeStringList(request.SleepingArrangements));
        property.HouseRulesJson = MilestoneJson.Serialize(NormalizeStringList(request.HouseRules));
        property.CleaningFee = decimal.Round(Math.Max(0, request.CleaningFee), 2);
        property.ServiceFee = decimal.Round(Math.Max(0, request.ServiceFee), 2);
        property.Latitude = request.Latitude;
        property.Longitude = request.Longitude;
        property.ImageUrl = request.ImageUrl?.Trim();
        property.GalleryUrlsJson = MilestoneJson.Serialize(NormalizeStringList(request.GalleryUrls));
        // A host edit requires a fresh moderation review before the listing is
        // public again. Admin approval is the only path back to Approved.
        property.ModerationStatus = "Pending";
        property.ModerationReason = null;
        property.ModeratedAt = null;
        property.ModeratedByUserId = null;
    }

    private static IReadOnlyList<string> NormalizeHighlights(IReadOnlyList<string>? highlights) =>
        highlights is null || highlights.Count == 0
            ? ["Host-created listing"]
            : highlights.Select(item => item.Trim()).Where(item => item.Length > 0).ToList();

    private static IReadOnlyList<string> NormalizeStringList(IReadOnlyList<string>? values) =>
        values is null
            ? []
            : values.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList();

    private static string ToMilestoneStatus(BookingStatus status) =>
        status switch
        {
            BookingStatus.Approved or BookingStatus.PaymentCaptured or BookingStatus.Confirmed => "APPROVED",
            BookingStatus.Rejected or BookingStatus.Cancelled => "REJECTED",
            _ => "PENDING"
        };

    private static string ToApiStatus<TStatus>(TStatus status) where TStatus : struct, Enum =>
        status.ToString().ToUpperInvariant();

    private async Task EnforceBookingCreationRateLimitAsync(
        Guid guestUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var bucket = await db.MilestoneBookingCreationRateLimits.SingleOrDefaultAsync(
            item => item.GuestUserId == guestUserId,
            cancellationToken);

        if (bucket is null)
        {
            db.MilestoneBookingCreationRateLimits.Add(new MilestoneBookingCreationRateLimit
            {
                Id = Guid.NewGuid(),
                GuestUserId = guestUserId,
                WindowStartedAt = now,
                WindowEndsAt = now.Add(BookingCreationRateLimitWindow),
                RequestCount = 1,
                LastRequestAt = now
            });
            return;
        }

        if (bucket.WindowEndsAt <= now)
        {
            bucket.WindowStartedAt = now;
            bucket.WindowEndsAt = now.Add(BookingCreationRateLimitWindow);
            bucket.RequestCount = 1;
            bucket.LastRequestAt = now;
            bucket.UpdatedAt = now;
            return;
        }

        if (bucket.RequestCount >= NestyStayBusinessRules.BookingCreationRateLimitMaximum)
        {
            throw new RateLimitExceededException(
                $"Too many booking requests. Try again in {FormatRetryAfter(bucket.WindowEndsAt - now)}.",
                bucket.WindowEndsAt - now);
        }

        bucket.RequestCount++;
        bucket.LastRequestAt = now;
        bucket.UpdatedAt = now;
    }

    private void ApplyPaymentWebhookToBooking(MilestoneBooking booking, PaymentWebhookUpdateRequest request)
    {
        booking.PaymentProvider = request.ProviderName;
        if (!BookingPaymentStateMachine.ShouldApplyWebhookPaymentTransition(booking.PaymentStatus, request.Status))
        {
            AddTimeline(
                booking,
                $"Stripe webhook ignored {request.Status} for payment already {booking.PaymentStatus}: {request.ProviderEventId}");
            return;
        }

        switch (request.Status)
        {
            case PaymentStatus.Captured:
                BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, PaymentStatus.Captured, "stripe_webhook", booking.Status);
                if (booking.Status == BookingStatus.Approved)
                {
                    BookingPaymentStateMachine.EnsureBookingTransition(booking.Status, BookingStatus.PaymentCaptured, "stripe_webhook");
                    booking.Status = BookingStatus.PaymentCaptured;
                }

                booking.PaymentCaptureReference = request.ProviderReference ?? request.PaymentIntentReference;
                booking.PaymentStatus = PaymentStatus.Captured;
                AddTimeline(booking, $"Stripe webhook confirmed capture: {request.ProviderEventId}");
                break;
            case PaymentStatus.Refunded:
                BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, PaymentStatus.Refunded, "stripe_webhook", booking.Status);
                booking.PaymentRefundReference = request.ProviderReference;
                if (request.Amount is > 0m)
                {
                    var refundAmount = request.EventType.Equals("charge.refunded", StringComparison.OrdinalIgnoreCase)
                        ? request.Amount.Value
                        : booking.RefundedAmount + request.Amount.Value;
                    booking.RefundedAmount = decimal.Round(Math.Min(booking.TotalAmount, refundAmount), 2, MidpointRounding.AwayFromZero);
                    booking.RefundReason = string.IsNullOrWhiteSpace(request.Reason) ? booking.RefundReason : request.Reason.Trim();
                    booking.RefundedAt = request.OccurredAt ?? timeProvider.GetUtcNow();
                }

                if (BookingRefundPolicy.IsFullyRefunded(booking.TotalAmount, booking.RefundedAmount))
                {
                    booking.PaymentStatus = PaymentStatus.Refunded;
                }

                AddTimeline(booking, $"Stripe webhook confirmed refund: {request.ProviderEventId}");
                break;
            case PaymentStatus.Failed:
                BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, PaymentStatus.Failed, "stripe_webhook", booking.Status);
                booking.PaymentStatus = PaymentStatus.Failed;
                AddTimeline(booking, $"Stripe webhook marked payment failed: {request.ProviderEventId}");
                break;
            case PaymentStatus.Cancelled:
                BookingPaymentStateMachine.EnsurePaymentTransition(booking.PaymentStatus, PaymentStatus.Cancelled, "stripe_webhook", booking.Status);
                booking.PaymentStatus = PaymentStatus.Cancelled;
                AddTimeline(booking, $"Stripe webhook marked payment cancelled: {request.ProviderEventId}");
                break;
        }
    }

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
            Parish = "St. Ann",
            Description = "A bright, private villa near Ocho Rios with quiet garden views, a plunge pool, and a verified host team.",
            Bedrooms = 3,
            Bathrooms = 2,
            MaxGuests = 6,
            AmenitiesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wi-Fi", "Pool", "Air conditioning", "Kitchen", "Free parking"]),
            SleepingArrangementsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Bedroom 1: king bed", "Bedroom 2: queen bed", "Bedroom 3: two single beds"]),
            HouseRulesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["No smoking indoors", "Quiet hours after 10:00 PM", "No unregistered guests"]),
            CleaningFee = 35m,
            ServiceFee = 24m,
            Latitude = 18.4074m,
            Longitude = -77.1031m,
            ImageUrl = "https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?auto=format&fit=crop&w=1600&q=85",
            GalleryUrlsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?auto=format&fit=crop&w=1600&q=85", "https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?auto=format&fit=crop&w=1200&q=85", "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=1200&q=85"]),
            HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Stripe Identity", "QR gate access", "InsuraGuest available", "Emergency 119 displayed"])
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
            Parish = "St. Andrew",
            Description = "A calm business-ready apartment in New Kingston with a dedicated workspace and fast access to the city centre.",
            Bedrooms = 2,
            Bathrooms = 2,
            MaxGuests = 4,
            AmenitiesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wi-Fi", "Workspace", "Air conditioning", "Washer", "Free parking"]),
            SleepingArrangementsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Bedroom 1: queen bed", "Bedroom 2: two single beds"]),
            HouseRulesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["No parties", "No smoking indoors", "Check-in after 3:00 PM"]),
            CleaningFee = 25m,
            ServiceFee = 18m,
            Latitude = 18.0179m,
            Longitude = -76.8099m,
            ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1600&q=85",
            GalleryUrlsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1600&q=85", "https://images.unsplash.com/photo-1600566753086-00f18fb6b3ea?auto=format&fit=crop&w=1200&q=85", "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=85"]),
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
            Parish = "St. James",
            Description = "A simple, well-kept apartment for a practical Montego Bay stay, close to local shops and transport.",
            Bedrooms = 1,
            Bathrooms = 1,
            MaxGuests = 2,
            AmenitiesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wi-Fi", "Air conditioning", "Kitchen", "Free parking"]),
            SleepingArrangementsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Bedroom: queen bed"]),
            HouseRulesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["No smoking indoors", "No parties", "Quiet hours after 10:00 PM"]),
            CleaningFee = 15m,
            ServiceFee = 12m,
            Latitude = 18.4712m,
            Longitude = -77.9188m,
            ImageUrl = "https://images.unsplash.com/photo-1600585154526-990dced4db0d?auto=format&fit=crop&w=1600&q=85",
            GalleryUrlsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["https://images.unsplash.com/photo-1600585154526-990dced4db0d?auto=format&fit=crop&w=1600&q=85", "https://images.unsplash.com/photo-1600566753051-f0b89df2dd90?auto=format&fit=crop&w=1200&q=85"]),
            HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Free listing", "Calendar", "Messaging", "Host keeps 97% payout"])
        },
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-8444-444444444444"),
            HostUserId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
            HostName = "Negril Wellness Retreats",
            HostEmail = "host-wellness@nestystay.local",
            Title = "Negril Wellness Beach House",
            Location = "Negril, Westmoreland",
            Country = "Jamaica",
            NightlyRate = 245m,
            Currency = "USD",
            BadgeLevel = BadgeLevel.Wellness,
            GuestVerificationEnabled = true,
            InsuraGuestEnabled = true,
            CancellationPolicy = "Moderate",
            Parish = "Westmoreland",
            Description = "A wellness-qualified beach house with a trusted host, optional wellness visits, and sunset views near Seven Mile Beach.",
            Bedrooms = 3,
            Bathrooms = 3,
            MaxGuests = 6,
            AmenitiesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wi-Fi", "Beach access", "Pool", "Air conditioning", "Wellness-ready"]),
            SleepingArrangementsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Bedroom 1: king bed", "Bedroom 2: queen bed", "Bedroom 3: two single beds"]),
            HouseRulesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["No smoking indoors", "Quiet hours after 10:00 PM", "Registered guests only"]),
            CleaningFee = 45m,
            ServiceFee = 30m,
            Latitude = 18.2683m,
            Longitude = -78.3472m,
            ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1600&q=85",
            GalleryUrlsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1600&q=85", "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1200&q=85"]),
            HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Wellness badge", "Wellness visits", "Police directory access", "Stripe Identity"])
        }
    ];

    private static IReadOnlyList<MilestoneUser> DefaultHostUsers() =>
    [
        CreateDisabledSeedHost(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            "host-villa@nestystay.local",
            "Island Villa Hosting"),
        CreateDisabledSeedHost(
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            "host-kingston@nestystay.local",
            "Kingston Corporate Homes"),
        CreateDisabledSeedHost(
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            "host-mobay@nestystay.local",
            "Montego Bay Apartments"),
        CreateDisabledSeedHost(
            Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
            "host-wellness@nestystay.local",
            "Negril Wellness Retreats")
    ];

    private static MilestoneUser CreateDisabledSeedHost(Guid id, string email, string displayName)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return new MilestoneUser
        {
            Id = id,
            Email = normalizedEmail,
            NormalizedEmail = normalizedEmail,
            PasswordHash = HashPassword($"seed-disabled-{id:N}"),
            DisplayName = displayName,
            Status = "Disabled",
            IsTwoFactorEnabled = false,
            AdminPermissionsJson = MilestoneJson.Serialize<IReadOnlyList<string>>([]),
            RolesJson = MilestoneJson.Serialize<IReadOnlyList<UserRole>>([UserRole.Host])
        };
    }

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

    private byte[] ProtectTotpSecret(byte[] secret) =>
        secretProtector?.Protect(TotpSecretPurpose, secret) ?? secret;

    private byte[] UnprotectTotpSecret(byte[] secret) =>
        secretProtector?.Unprotect(TotpSecretPurpose, secret) ?? secret;

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

    private static string NormalizeDeviceName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Unknown device" : value.Trim()[..Math.Min(100, value.Trim().Length)];

    private static string ResolveBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown browser";
        var value = userAgent.Trim();
        var browser = value.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Microsoft Edge" :
            value.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Google Chrome" :
            value.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Mozilla Firefox" :
            value.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari" : "Browser";
        return browser;
    }

    private static UserSessionDto ToSessionDto(MilestoneUserSession session, bool isCurrent)
    {
        var now = DateTimeOffset.UtcNow;
        return new UserSessionDto(
            session.Id,
            session.DeviceName,
            session.Browser,
            session.ApproximateLocation,
            session.IssuedAt,
            session.LastUsedAt,
            session.ExpiresAt,
            isCurrent,
            session.TrustedUntil is not null && session.TrustedUntil > now,
            session.TrustedUntil,
            session.RevokedAt is not null || session.ExpiresAt <= now);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string FormatRetryAfter(TimeSpan retryAfter)
    {
        var totalSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        if (totalSeconds < 60)
        {
            return $"{totalSeconds} second{(totalSeconds == 1 ? string.Empty : "s")}";
        }

        var totalMinutes = (int)Math.Ceiling(totalSeconds / 60m);
        return $"{totalMinutes} minute{(totalMinutes == 1 ? string.Empty : "s")}";
    }

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

    private sealed record PendingNotification(
        string RecipientType,
        Guid RecipientUserId,
        string Type,
        string DeepLink,
        NotificationMessage Message);
}
