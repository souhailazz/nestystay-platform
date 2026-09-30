using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

/// <summary>
/// Focused API-level authorization regression coverage for the M5 portfolio
/// boundaries.  These tests deliberately use real HTTP requests and valid
/// sessions; they do not rely on frontend role hiding or localStorage state.
/// </summary>
public sealed class PropertyManagerAuthorizationMatrixTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    private const string P0 = "/api/property-manager/p0";

    [Fact]
    public async Task OwnerAndManagerResourcesAreNotReadableAcrossPortfolios()
    {
        var first = await CreatePortfolioAsync();
        var second = await CreatePortfolioAsync();

        using var firstManager = SignedClient(first.Manager, UserRole.PropertyManager);
        var invoice = await ReadObjectAsync(await firstManager.PostAsJsonAsync("/api/property-manager/invoices", new
        {
            ownerUserId = first.Owner,
            propertyId = first.Property,
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            tax = 0m,
            lines = new[] { new { description = "Scoped PM fee", quantity = 1m, unitAmount = 75m } }
        }));
        var invoiceId = invoice.GetProperty("id").GetGuid();

        var document = await ReadObjectAsync(await firstManager.PostAsJsonAsync("/api/property-manager/documents", new
        {
            ownerUserId = first.Owner,
            propertyId = first.Property,
            title = "Scoped owner document",
            category = "FINANCE",
            fileName = "statement.pdf",
            contentType = "application/pdf",
            sizeBytes = 9,
            contentBase64 = "JVBERi0xLjQK"
        }));
        var documentId = document.GetProperty("id").GetGuid();

        using var secondOwner = SignedClient(second.Owner, UserRole.Owner);
        Assert.Equal(HttpStatusCode.NotFound, (await secondOwner.GetAsync($"/api/property-manager/invoices/{invoiceId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondOwner.GetAsync($"/api/property-manager/documents/{documentId}/download")).StatusCode);
        Assert.False((await secondOwner.GetAsync($"/api/property-manager/owners/{first.Owner}/statement")).IsSuccessStatusCode);
        var secondOwnerPortal = await secondOwner.GetStringAsync("/api/property-manager/owner/portal");
        Assert.DoesNotContain(invoiceId.ToString(), secondOwnerPortal, StringComparison.OrdinalIgnoreCase);

        using var secondManager = SignedClient(second.Manager, UserRole.PropertyManager);
        Assert.Equal(HttpStatusCode.NotFound, (await secondManager.GetAsync($"/api/property-manager/invoices/{invoiceId}")).StatusCode);
        Assert.False((await secondManager.GetAsync($"/api/property-manager/professional-completion/documents?propertyId={first.Property}")).IsSuccessStatusCode);
        Assert.False((await secondManager.PostAsJsonAsync("/api/property-manager/professional-completion/documents", new
        {
            resourceType = "DOCUMENT",
            status = "DRAFT",
            ownerUserId = first.Owner,
            propertyId = first.Property,
            payloadJson = "{}",
            idempotencyKey = Guid.NewGuid().ToString("N")
        })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task ScopedStaffCanUseAssignedCompletionRecordsButCannotCrossOwnerOrFinanceBoundaries()
    {
        var first = await CreatePortfolioAsync();
        var second = await CreatePortfolioAsync();

        using var manager = SignedClient(first.Manager, UserRole.PropertyManager);
        var record = await ReadObjectAsync(await manager.PostAsJsonAsync("/api/property-manager/professional-completion/assets", new
        {
            resourceType = "ASSET",
            status = "ACTIVE",
            ownerUserId = first.Owner,
            propertyId = first.Property,
            payloadJson = "{\"assetTag\":\"A-001\"}",
            idempotencyKey = Guid.NewGuid().ToString("N")
        }));
        var staff = await RegisterAsync("Scoped matrix staff");
        var invitation = await ReadObjectAsync(await manager.PostAsJsonAsync($"{P0}/members", new
        {
            staffUserId = staff.UserId,
            role = "OPERATIONS",
            propertyIds = new[] { first.Property },
            ownerIds = Array.Empty<Guid>(),
            canManageFinance = false,
            canApprovePayouts = false,
            approvalLimit = 0m
        }));
        var membershipId = invitation.GetProperty("id").GetGuid();

        using var staffClient = SignedClient(staff.UserId, UserRole.Guest);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.PostAsync($"{P0}/members/{membershipId}/accept", null)).StatusCode);
        using var activeManager = SignedClient(first.Manager, UserRole.PropertyManager);
        var currentMembership = (await ReadArrayAsync(await activeManager.GetAsync($"{P0}/members"))).Single(x => x.GetProperty("id").GetGuid() == membershipId);
        var updated = await activeManager.PatchAsJsonAsync($"{P0}/members/{membershipId}", new
        {
            role = "OPERATIONS",
            propertyIds = new[] { first.Property },
            ownerIds = Array.Empty<Guid>(),
            canManageFinance = false,
            canApprovePayouts = false,
            approvalLimit = 0m,
            status = "ACTIVE",
            rowVersion = currentMembership.GetProperty("rowVersion").GetInt64()
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var assigned = await ReadArrayAsync(await staffClient.GetAsync($"/api/property-manager/professional-completion/assets?propertyId={first.Property}"));
        Assert.Contains(assigned, item => item.GetProperty("id").GetGuid() == record.GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await staffClient.PostAsJsonAsync("/api/property-manager/professional-completion/assets", new
        {
            resourceType = "ASSET",
            status = "ACTIVE",
            ownerUserId = first.Owner,
            propertyId = first.Property,
            payloadJson = "{\"assetTag\":\"A-002\"}",
            idempotencyKey = Guid.NewGuid().ToString("N")
        })).StatusCode);

        Assert.False((await staffClient.GetAsync($"/api/property-manager/professional-completion/assets?propertyId={second.Property}")).IsSuccessStatusCode);
        Assert.False((await staffClient.PostAsJsonAsync("/api/property-manager/professional-completion/assets", new
        {
            resourceType = "ASSET",
            status = "ACTIVE",
            ownerUserId = second.Owner,
            propertyId = second.Property,
            payloadJson = "{\"assetTag\":\"B-001\"}",
            idempotencyKey = Guid.NewGuid().ToString("N")
        })).IsSuccessStatusCode);
        Assert.False((await staffClient.GetAsync($"/api/property-manager/professional-completion/utilities?propertyId={first.Property}")).IsSuccessStatusCode);
        Assert.False((await staffClient.GetAsync($"{P0}/owners/{second.Owner}/profile")).IsSuccessStatusCode);
        Assert.False((await staffClient.PostAsJsonAsync($"{P0}/members", new
        {
            staffUserId = Guid.NewGuid(),
            role = "OPERATIONS",
            propertyIds = new[] { first.Property },
            ownerIds = Array.Empty<Guid>(),
            canManageFinance = false,
            canApprovePayouts = false,
            approvalLimit = 0m
        })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task EveryRoleRestrictedPropertyManagerEndpointRejectsRolesOutsideItsDeclaredPolicy()
    {
        var apiExplorer = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = apiExplorer.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(description => description.RelativePath?.StartsWith("api/property-manager/", StringComparison.OrdinalIgnoreCase) == true)
            .Where(description => description.ActionDescriptor.EndpointMetadata?.OfType<IAllowAnonymous>().Any() != true)
            .ToList();
        // Controller source contains 193 HTTP attributes; one is the
        // anonymous QR validation action and is intentionally excluded here.
        Assert.Equal(192, descriptions.Count);

        var tested = 0;
        foreach (var description in descriptions)
        {
            var routeRoles = description.ActionDescriptor.EndpointMetadata?
                .OfType<IAuthorizeData>()
                .SelectMany(item => (item.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            if (routeRoles.Count == 0) continue;

            var path = BuildProbePath(description.RelativePath!);
            foreach (var role in Enum.GetValues<UserRole>())
            {
                if (routeRoles.Contains(role.ToString()) || role == UserRole.Admin) continue;
                using var client = SignedClient(Guid.NewGuid(), role);
                using var request = new HttpRequestMessage(new HttpMethod(description.HttpMethod ?? "GET"), path);
                if (!string.Equals(description.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(description.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(description.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    request.Content = JsonContent.Create(new { });
                }

                using var response = await client.SendAsync(request);
                Assert.False(response.IsSuccessStatusCode,
                    $"{role} unexpectedly received {(int)response.StatusCode} from {description.HttpMethod} /{description.RelativePath}");
                tested++;
            }
        }

        Assert.True(tested > 0);
    }

    private async Task<Portfolio> CreatePortfolioAsync()
    {
        using var client = factory.CreateClient();
        var owner = await RegisterAsync("Scoped matrix owner");
        var manager = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(manager, UserRole.PropertyManager));
        await ExpectSuccessAsync(await client.PostAsJsonAsync("/api/property-manager/owners", new { email = owner.Email, displayName = owner.DisplayName }));
        var property = await ReadObjectAsync(await client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = owner.UserId, title = $"Matrix property {Guid.NewGuid():N}", unitNumber = "M-1", address = "Kingston" }));
        return new Portfolio(manager, owner.UserId, property.GetProperty("id").GetGuid(), owner.Email, owner.DisplayName);
    }

    private async Task<RegisteredUser> RegisterAsync(string displayName)
    {
        using var client = factory.CreateClient();
        var email = $"matrix-{Guid.NewGuid():N}@nestystay.local";
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

    private HttpClient SignedClient(Guid userId, UserRole role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));
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

    private static async Task<JsonElement[]> ReadArrayAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
            return JsonSerializer.Deserialize<JsonElement[]>(body) ?? [];
        }
    }

    private static async Task ExpectSuccessAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        }
    }

    private static string BuildProbePath(string relativePath)
    {
        var path = Regex.Replace(relativePath, @"\{[^}:]+(?::[^}]+)?\}", match => Guid.NewGuid().ToString());
        path = path.Replace("{area}", "assets", StringComparison.OrdinalIgnoreCase);
        path = path.Replace("{format}", "json", StringComparison.OrdinalIgnoreCase);
        return "/" + path;
    }

    private sealed record RegisteredUser(Guid UserId, string Email, string DisplayName);
    private sealed record Portfolio(Guid Manager, Guid Owner, Guid Property, string OwnerEmail, string OwnerDisplayName);
}
