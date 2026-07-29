using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NestyStay.Application.Admin;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Tests;

public sealed class AdminIdentityEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private const string Password = "NestyStay1";
    private readonly NestyStayApiFactory _factory;

    public AdminIdentityEndpointTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SelfServiceRegistrationRejectsAdministratorRole()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"self-admin-{Guid.NewGuid():N}@nestystay.local",
            password = Password,
            confirmPassword = Password,
            displayName = "Self Admin",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BootstrapCreatesNamedAdministratorAndDoesNotResetPasswordOrPermissions()
    {
        var email = $"boot-admin-{Guid.NewGuid():N}@nestystay.local";
        using var factory = WithBootstrap(email, permissions: [AdminPermissionCatalog.BookingManagement]);
        using var client = factory.CreateClient();

        var session = await LoginAsync(client, email);
        Assert.Contains(UserRole.Admin.ToString(), session.Roles);
        Assert.Contains(AdminPermissionCatalog.BookingManagement, session.Permissions);
        Assert.DoesNotContain(AdminPermissionCatalog.AuditLogAccess, session.Permissions);

        client.DefaultRequestHeaders.Authorization = Bearer(session.AccessToken);
        var authorizedMissingBooking = await client.PostAsJsonAsync($"/api/bookings/{Guid.NewGuid()}/verification-result", new
        {
            passed = true,
            providerReference = "bootstrap-admin"
        });
        Assert.Equal(HttpStatusCode.NotFound, authorizedMissingBooking.StatusCode);

        var missingPermission = await client.GetAsync("/api/spec/admin/audit-log");
        Assert.Equal(HttpStatusCode.Forbidden, missingPermission.StatusCode);

        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPhaseOneStore>();
        var repeated = await store.BootstrapAdministratorAsync(
            new AdministratorBootstrapRequest(
                email,
                "OtherNesty1",
                "Changed Admin",
                [AdminPermissionCatalog.AuditLogAccess]),
            CancellationToken.None);
        Assert.False(repeated.Created);
        Assert.Contains(AdminPermissionCatalog.BookingManagement, repeated.Permissions);
        Assert.DoesNotContain(AdminPermissionCatalog.AuditLogAccess, repeated.Permissions);

        _ = await LoginAsync(client, email);
        var newPasswordResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "OtherNesty1"
        });
        Assert.Equal(HttpStatusCode.BadRequest, newPasswordResponse.StatusCode);
    }

    [Fact]
    public async Task PrivilegedEndpointsRejectAnonymousNonAdminAndAdminWithoutPermission()
    {
        var email = $"audit-admin-{Guid.NewGuid():N}@nestystay.local";
        using var factory = WithBootstrap(email, permissions: [AdminPermissionCatalog.AuditLogAccess]);
        using var client = factory.CreateClient();
        var route = $"/api/bookings/{Guid.NewGuid()}/verification-result";

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);

        foreach (var role in new[] { UserRole.Guest, UserRole.Host, UserRole.Officer })
        {
            client.DefaultRequestHeaders.Authorization = Bearer(NestyStayApiFactory.UserToken(Guid.NewGuid(), role));
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);
        }

        var adminSession = await LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = Bearer(adminSession.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);
    }

    [Fact]
    public async Task DisabledLockedAndRevokedAdministratorSessionsAreRejected()
    {
        var email = $"inactive-admin-{Guid.NewGuid():N}@nestystay.local";
        using var factory = WithBootstrap(email, permissions: [AdminPermissionCatalog.BookingManagement]);
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, email);
        var route = $"/api/bookings/{Guid.NewGuid()}/verification-result";

        client.DefaultRequestHeaders.Authorization = Bearer(session.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);

        await MutateAdminAsync(factory, email, user => user.Status = "Disabled");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);

        await MutateAdminAsync(factory, email, user =>
        {
            user.Status = "Active";
            user.LockoutEndsAt = DateTimeOffset.UtcNow.AddMinutes(10);
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);

        await MutateAdminAsync(factory, email, user =>
        {
            user.LockoutEndsAt = null;
            user.SessionInvalidatedAt = DateTimeOffset.UtcNow.AddMinutes(1);
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(route, new { passed = true })).StatusCode);
    }

    [Fact]
    public async Task TotpEnabledAdministratorMustCompleteChallengeBeforeSessionIsIssued()
    {
        var email = $"totp-admin-{Guid.NewGuid():N}@nestystay.local";
        using var factory = WithBootstrap(
            email,
            permissions: [AdminPermissionCatalog.BookingManagement],
            requireTwoFactor: true);
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<LoginBody>();
        Assert.NotNull(loginBody);
        Assert.True(loginBody.RequiresTwoFactor);
        Assert.Null(loginBody.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.ChallengeId));

        var challenge = await client.GetFromJsonAsync<DevelopmentChallengeBody>($"/api/auth/development/challenges/{loginBody.ChallengeId}");
        Assert.NotNull(challenge);

        var verified = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = loginBody.ChallengeId,
            code = challenge.Code
        });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var session = await verified.Content.ReadFromJsonAsync<VerifiedSessionBody>();
        Assert.NotNull(session);
        Assert.Contains(UserRole.Admin.ToString(), session.Roles);
        Assert.Contains(AdminPermissionCatalog.BookingManagement, session.Permissions);

        client.DefaultRequestHeaders.Authorization = Bearer(session.AccessToken);
        var authorized = await client.PostAsJsonAsync($"/api/bookings/{Guid.NewGuid()}/verification-result", new { passed = true });
        Assert.Equal(HttpStatusCode.NotFound, authorized.StatusCode);
    }

    private WebApplicationFactory<Program> WithBootstrap(
        string email,
        IReadOnlyList<string> permissions,
        bool requireTwoFactor = false) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:AdminBootstrap:Enabled"] = "true",
                    ["Security:AdminBootstrap:Email"] = email,
                    ["Security:AdminBootstrap:Password"] = Password,
                    ["Security:AdminBootstrap:DisplayName"] = "Bootstrap Admin",
                    ["Security:AdminBootstrap:Permissions"] = string.Join(",", permissions),
                    ["Security:AdminBootstrap:RequireTwoFactor"] = requireTwoFactor.ToString()
                });
            });
        });

    private static async Task<AdminSessionBody> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminSessionBody>();
        Assert.NotNull(body);
        Assert.False(body.RequiresTwoFactor);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        return body;
    }

    private static async Task MutateAdminAsync(WebApplicationFactory<Program> factory, string email, Action<MilestoneUser> mutate)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var normalizedEmail = email.ToLowerInvariant();
        var user = await db.MilestoneUsers.SingleAsync(item => item.NormalizedEmail == normalizedEmail);
        mutate(user);
        await db.SaveChangesAsync();
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private sealed record AdminSessionBody(
        Guid UserId,
        string Email,
        bool RequiresTwoFactor,
        string? ChallengeId,
        DateTimeOffset? ChallengeExpiresAt,
        string AccessToken,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);

    private sealed record LoginBody(
        Guid UserId,
        string Email,
        bool RequiresTwoFactor,
        string? ChallengeId,
        DateTimeOffset? ChallengeExpiresAt,
        string? AccessToken);

    private sealed record VerifiedSessionBody(
        Guid UserId,
        string AccessToken,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);

    private sealed record DevelopmentChallengeBody(string ChallengeId, string Code, DateTimeOffset ExpiresAt);
}
