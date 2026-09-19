using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NestyStay.Api.Controllers;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class CalendarEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public CalendarEndpointTests(NestyStayApiFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("http://127.0.0.1/feed.ics")]
    [InlineData("http://[::1]/feed.ics")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.5/feed.ics")]
    [InlineData("http://example.test:8080/feed.ics")]
    [InlineData("http://user:password@example.test/feed.ics")]
    public void CalendarFeedValidationRejectsRestrictedDestinations(string url)
    {
        Assert.Throws<InvalidOperationException>(() => CalendarController.ValidateFeedUrl(url));
    }

    [Fact]
    public void CalendarFeedValidationAllowsPublicWebPorts()
    {
        Assert.Equal("https://calendar.example.test/feed.ics", CalendarController.ValidateFeedUrl("https://calendar.example.test/feed.ics"));
        Assert.Equal("http://calendar.example.test/feed.ics", CalendarController.ValidateFeedUrl("http://calendar.example.test/feed.ics"));
    }

    [Fact]
    public async Task HostCanConnectSyncAndExportIcsCalendar()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var property = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = hostId,
            hostName = "Calendar Host",
            hostEmail = $"calendar-{hostId:N}@test.local",
            title = "Calendar Villa",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 100,
            currency = "USD",
            badgeLevel = "Free",
            cancellationPolicy = "Flexible"
        });
        Assert.Equal(HttpStatusCode.OK, property.StatusCode);
        var propertyId = (await property.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var connect = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/feeds", new { feedUrl = "https://calendar.example.test/feed.ics" });
        Assert.Equal(HttpStatusCode.OK, connect.StatusCode);
        var feed = await connect.Content.ReadFromJsonAsync<FeedResponse>();
        Assert.NotNull(feed);

        const string ics = "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nUID:external-1\r\nDTSTART;VALUE=DATE:20260910\r\nDTEND;VALUE=DATE:20260912\r\nSUMMARY:Owner block\r\nEND:VEVENT\r\nEND:VCALENDAR";
        var sync = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/feeds/{feed.Id}/sync", new { icsContent = ics });
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var synced = await sync.Content.ReadFromJsonAsync<FeedResponse>();
        Assert.NotNull(synced);
        Assert.Equal("Healthy", synced.Status);
        Assert.Equal(1, synced.BlockCount);

        var export = await client.GetAsync($"/api/properties/{propertyId}/calendar/export.ics");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Contains("DTSTART;VALUE=DATE:20260910", await export.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ImportedEventsAreIdempotentAndUpdatesRemoveCancelledEvents()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var property = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = hostId, hostName = "Calendar Host", hostEmail = $"calendar-{hostId:N}@test.local",
            title = "Idempotent Calendar Villa", location = "Kingston", country = "Jamaica", nightlyRate = 100,
            currency = "USD", badgeLevel = "Free", cancellationPolicy = "Flexible"
        });
        var propertyId = (await property.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var connect = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/feeds", new { feedUrl = "https://calendar.example.test/airbnb.ics", channel = "Airbnb" });
        var feedId = (await connect.Content.ReadFromJsonAsync<FeedResponse>())!.Id;

        const string first = "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nUID:stable-event\r\nDTSTART;VALUE=DATE:20261010\r\nDTEND;VALUE=DATE:20261012\r\nSUMMARY:Original\r\nEND:VEVENT\r\nEND:VCALENDAR";
        var firstSync = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/feeds/{feedId}/sync", new { icsContent = first });
        Assert.Equal(HttpStatusCode.OK, firstSync.StatusCode);

        const string updated = "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nUID:stable-event\r\nDTSTART;VALUE=DATE:20261011\r\nDTEND;VALUE=DATE:20261013\r\nSUMMARY:Updated\r\nEND:VEVENT\r\nBEGIN:VEVENT\r\nUID:second-event\r\nDTSTART;VALUE=DATE:20261015\r\nDTEND;VALUE=DATE:20261016\r\nSUMMARY:Second\r\nEND:VEVENT\r\nEND:VCALENDAR";
        var secondSync = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/feeds/{feedId}/sync", new { icsContent = updated });
        var second = await secondSync.Content.ReadFromJsonAsync<FeedResponse>();
        Assert.Equal(HttpStatusCode.OK, secondSync.StatusCode);
        Assert.Equal(2, second!.BlockCount);

        const string cancelled = "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nUID:stable-event\r\nDTSTART;VALUE=DATE:20261011\r\nDTEND;VALUE=DATE:20261013\r\nSTATUS:CANCELLED\r\nEND:VEVENT\r\nBEGIN:VEVENT\r\nUID:second-event\r\nDTSTART;VALUE=DATE:20261015\r\nDTEND;VALUE=DATE:20261016\r\nSUMMARY:Second\r\nEND:VEVENT\r\nEND:VCALENDAR";
        var cancelledSync = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/feeds/{feedId}/sync", new { icsContent = cancelled });
        var cancelledResult = await cancelledSync.Content.ReadFromJsonAsync<FeedResponse>();
        Assert.Equal(HttpStatusCode.OK, cancelledSync.StatusCode);
        Assert.Equal(1, cancelledResult!.BlockCount);
    }

    [Fact]
    public async Task HostCanEditManualBlocksAndUseRevocableOutboundFeedToken()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var property = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = hostId, hostName = "Calendar Host", hostEmail = $"calendar-{hostId:N}@test.local",
            title = "Operations Villa", location = "Kingston", country = "Jamaica", nightlyRate = 100,
            currency = "USD", badgeLevel = "Free", cancellationPolicy = "Flexible"
        });
        var propertyId = (await property.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var created = await client.PostAsJsonAsync($"/api/properties/{propertyId}/calendar/blocks", new { startsOn = "2026-10-20", endsOn = "2026-10-22", reason = "Owner stay" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var block = await created.Content.ReadFromJsonAsync<ManualBlockResponse>();
        Assert.NotNull(block);

        var updated = await client.PatchAsJsonAsync($"/api/properties/{propertyId}/calendar/blocks/{block!.Id}", new { startsOn = "2026-10-21", endsOn = "2026-10-23", reason = "Maintenance" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var availability = await client.GetAsync($"/api/properties/{propertyId}/availability?from=2026-10-20&to=2026-10-24");
        Assert.Equal(HttpStatusCode.OK, availability.StatusCode);
        Assert.Contains("Manual", await availability.Content.ReadAsStringAsync());

        var tokenResponse = await client.PostAsync($"/api/properties/{propertyId}/calendar/export-token", null);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var token = await tokenResponse.Content.ReadFromJsonAsync<ExportTokenResponse>();
        Assert.NotNull(token);
        Assert.Contains("export/", token!.Url);
        var feed = await client.GetAsync(new Uri(token.Url).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
        var ics = await feed.Content.ReadAsStringAsync();
        Assert.Contains("UID:manual-", ics);
        Assert.Contains("Maintenance", ics);
        Assert.DoesNotContain(hostId.ToString("N"), ics);

        var revoke = await client.DeleteAsync($"/api/properties/{propertyId}/calendar/export-token");
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        var revoked = await client.GetAsync(new Uri(token.Url).PathAndQuery);
        Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);

        var delete = await client.DeleteAsync($"/api/properties/{propertyId}/calendar/blocks/{block.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    private sealed record IdResponse(Guid Id);
    private sealed record FeedResponse(Guid Id, string Status, int BlockCount);
    private sealed record ManualBlockResponse(Guid Id, Guid PropertyId, DateOnly StartsOn, DateOnly EndsOn, string Reason, string Status, string SourceType);
    private sealed record ExportTokenResponse(string Url, DateTimeOffset CreatedAt, DateTimeOffset? RevokedAt, bool IsActive);
}
