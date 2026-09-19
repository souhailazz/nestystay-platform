using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;
using NestyStay.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NestyStay.Api.Tests;

public sealed class NestyStayApiFactory : WebApplicationFactory<Program>
{
    public const string AdminToken = "test-admin-token";
    public const string OperatorToken = "test-operator-token";
    private const string TestingSessionSecret = "development-testing-session-secret-change-before-production";
    private const string SessionTokenPrefix = "nsty.v1.";
    private const string SessionTokenKeyId = "dev";
    private readonly string _databaseName = $"nestystay-api-tests-{Guid.NewGuid():N}";
    private readonly ServiceProvider _inMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AdminTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(AdminToken),
                ["Security:OperatorTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(OperatorToken)
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<NestyStayDbContext>();
            services.RemoveAll<DbContextOptions<NestyStayDbContext>>();
            services.RemoveAll<IGoogleIdentityValidator>();
            services.AddDbContext<NestyStayDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .UseInternalServiceProvider(_inMemoryProvider));
            services.AddSingleton<IGoogleIdentityValidator, TestGoogleIdentityValidator>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inMemoryProvider.Dispose();
        }

        base.Dispose(disposing);
    }

    public async Task SeedBadgePaymentFactsAsync(
        Guid hostId,
        int approvedBookingCount = 0,
        bool hasPropertyAddress = false,
        bool hasWellnessSubscription = false)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        await db.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        var host = await db.MilestoneUsers.SingleOrDefaultAsync(user => user.Id == hostId);
        if (host is null)
        {
            host = new MilestoneUser
            {
                Id = hostId,
                Email = $"badge-payment-{hostId:N}@test.local",
                NormalizedEmail = $"badge-payment-{hostId:N}@test.local",
                PasswordHash = "test-only",
                DisplayName = "Badge Payment Test Host",
                Status = "Active",
                IsTwoFactorEnabled = false,
                RolesJson = JsonSerializer.Serialize(new[] { UserRole.Host }),
                AdminPermissionsJson = "[]"
            };
            db.MilestoneUsers.Add(host);
        }

        host.HostVerificationStatus = "Approved";
        host.HostVerificationDocumentType = "Passport";
        host.HostVerificationReviewedAt = now;

        if (hasPropertyAddress)
        {
            db.MilestoneProperties.Add(new MilestoneProperty
            {
                Id = Guid.NewGuid(),
                HostUserId = hostId,
                HostName = host.DisplayName,
                HostEmail = host.Email,
                Title = "Badge Payment Test Property",
                Location = "Kingston, Jamaica",
                Country = "Jamaica",
                NightlyRate = 100m,
                Currency = "USD",
                BadgeLevel = BadgeLevel.Free,
                CancellationPolicy = "Flexible",
                IsDraft = false,
                IsArchived = false
            });
        }

        for (var index = 0; index < approvedBookingCount; index++)
        {
            db.MilestoneBookings.Add(new MilestoneBooking
            {
                Id = Guid.NewGuid(),
                HostUserId = hostId,
                HostName = host.DisplayName,
                HostEmail = host.Email,
                GuestUserId = Guid.NewGuid(),
                GuestEmail = $"badge-guest-{hostId:N}-{index}@test.local",
                GuestName = "Badge Payment Test Guest",
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(index + 1)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(index + 2)),
                Status = BookingStatus.Confirmed,
                VerificationStatus = VerificationStatus.Passed,
                PaymentStatus = PaymentStatus.Captured,
                Nights = 1,
                NightlyRate = 100m,
                StaySubtotal = 100m,
                GuestPlatformFee = 0m,
                TotalAmount = 100m,
                Currency = "USD",
                PropertyTitle = "Badge Payment Test Property"
            });
        }

        if (hasWellnessSubscription)
        {
            db.MilestoneWellnessSubscriptions.Add(new MilestoneWellnessSubscription
            {
                Id = Guid.NewGuid(),
                HostUserId = hostId,
                PlanKey = "wellness-monthly",
                MonthlyAmount = 19m,
                Currency = "USD",
                Status = "Active",
                CurrentPeriodStart = now.AddDays(-1),
                CurrentPeriodEnd = now.AddDays(30),
                PaymentProvider = "Test",
                PaymentReference = $"test-subscription-{hostId:N}"
            });
        }

        await db.SaveChangesAsync();
    }

    public static string UserToken(Guid userId, params UserRole[] roles)
    {
        var now = DateTimeOffset.UtcNow;
        return UserTokenWithLifetime(userId, now, now.AddHours(1), roles);
    }

    public static string UserTokenWithLifetime(
        Guid userId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        params UserRole[] roles)
    {
        var payload = JsonSerializer.Serialize(new
        {
            sub = userId,
            roles = roles.Length == 0 ? [UserRole.Guest.ToString()] : roles.Select(role => role.ToString()).ToArray(),
            jti = Guid.NewGuid().ToString("N"),
            iat = issuedAt.ToUnixTimeSeconds(),
            exp = expiresAt.ToUnixTimeSeconds()
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestingSessionSecret));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{SessionTokenKeyId}.{payloadSegment}")));
        return $"{SessionTokenPrefix}{SessionTokenKeyId}.{payloadSegment}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class TestGoogleIdentityValidator : IGoogleIdentityValidator
    {
        public string ProviderName => "Test Google Identity";
        public bool IsConfigured => true;

        public Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken)
        {
            if (!credential.Equals("valid-google-credential", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Google credential validation failed.");
            }

            return Task.FromResult(new GoogleIdentity(
                "google-test-subject",
                "validated-google@test.local",
                "Validated Google Guest",
                true,
                DateTimeOffset.UtcNow.AddMinutes(30),
                "https://accounts.google.com",
                "test-google-client",
                null));
        }
    }
}
