using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerGateGuardRegressionTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    private const string P0 = "/api/property-manager/p0";

    [Fact]
    public async Task LegacyStaffEndpointRejectsGateGuardAssignments()
    {
        var staff = await RegisterAsync("Legacy gate guard");
        using var manager = SignedClient(Guid.NewGuid(), UserRole.PropertyManager);

        using var response = await manager.PostAsJsonAsync("/api/property-manager/staff", new
        {
            staffUserId = staff.UserId,
            role = "GATE_GUARD",
            propertyIds = Array.Empty<Guid>(),
            ownerIds = Array.Empty<Guid>(),
            canManageFinance = false,
            approvalLimit = 0m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScopedGateGuardCanValidateAssignedPropertyQrAndIsRevoked()
    {
        var owner = await RegisterAsync("Gate guard owner");
        var managerId = Guid.NewGuid();
        using var manager = SignedClient(managerId, UserRole.PropertyManager);
        await EnsureSuccessAsync(await manager.PostAsJsonAsync("/api/property-manager/owners", new
        {
            email = owner.Email,
            displayName = owner.DisplayName
        }));
        var property = await ReadObjectAsync(await manager.PostAsJsonAsync("/api/property-manager/properties", new
        {
            ownerUserId = owner.UserId,
            title = "Gate guard property",
            unitNumber = "GG-1",
            address = "Kingston"
        }));
        var propertyId = property.GetProperty("id").GetGuid();
        var qr = await ReadObjectAsync(await manager.PostAsJsonAsync("/api/property-manager/qr", new
        {
            propertyId,
            subjectType = "BOOKING",
            validFrom = DateTimeOffset.UtcNow.AddMinutes(-1),
            validUntil = DateTimeOffset.UtcNow.AddHours(1)
        }));

        var guard = await RegisterAsync("Scoped gate guard");
        var invitation = await ReadObjectAsync(await manager.PostAsJsonAsync($"{P0}/members", new
        {
            staffUserId = guard.UserId,
            role = "GATE_GUARD",
            propertyIds = new[] { propertyId },
            ownerIds = Array.Empty<Guid>(),
            canManageFinance = false,
            canApprovePayouts = false,
            approvalLimit = 0m
        }));
        var membershipId = invitation.GetProperty("id").GetGuid();

        using var pendingGuard = SignedClient(guard.UserId, UserRole.Guest);
        Assert.False((await pendingGuard.PostAsJsonAsync("/api/property-manager/qr/validate-authenticated", new
        {
            token = qr.GetProperty("token").GetString(),
            propertyId
        })).IsSuccessStatusCode);

        Assert.Equal(HttpStatusCode.OK, (await pendingGuard.PostAsync($"{P0}/members/{membershipId}/accept", null)).StatusCode);

        using var gateGuard = SignedClient(guard.UserId, UserRole.GateGuard);
        var validation = await ReadObjectAsync(await gateGuard.PostAsJsonAsync("/api/property-manager/qr/validate-authenticated", new
        {
            token = qr.GetProperty("token").GetString(),
            propertyId
        }));
        Assert.Equal("VALID", validation.GetProperty("result").GetString());
        Assert.False((await gateGuard.GetAsync($"{P0}/accounting/accounts")).IsSuccessStatusCode);

        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsJsonAsync($"{P0}/members/{membershipId}/revoke", new
        {
            status = "REVOKED",
            reason = "Assignment ended"
        })).StatusCode);
        Assert.False((await gateGuard.PostAsJsonAsync("/api/property-manager/qr/validate-authenticated", new
        {
            token = qr.GetProperty("token").GetString(),
            propertyId
        })).IsSuccessStatusCode);
    }

    private async Task<RegisteredUser> RegisterAsync(string displayName)
    {
        using var client = factory.CreateClient();
        var email = $"gate-guard-{Guid.NewGuid():N}@nestystay.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "NestyStay1",
            confirmPassword = "NestyStay1",
            displayName,
            phone = $"+1555{Random.Shared.Next(1000000, 9999999)}",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        var body = await ReadObjectAsync(response);
        return new RegisteredUser(body.GetProperty("userId").GetGuid(), email, displayName);
    }

    private HttpClient SignedClient(Guid userId, params UserRole[] roles)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, roles));
        return client;
    }

    private static async Task<JsonElement> ReadObjectAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
            return JsonDocument.Parse(body).RootElement.Clone();
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        }
    }

    private sealed record RegisteredUser(Guid UserId, string Email, string DisplayName);
}
