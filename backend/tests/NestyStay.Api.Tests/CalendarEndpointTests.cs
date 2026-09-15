using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class CalendarEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public CalendarEndpointTests(NestyStayApiFactory factory) => this.factory = factory;

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

    private sealed record IdResponse(Guid Id);
    private sealed record FeedResponse(Guid Id, string Status, int BlockCount);
}
