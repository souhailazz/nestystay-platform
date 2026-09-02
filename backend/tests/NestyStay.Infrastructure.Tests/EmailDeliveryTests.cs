using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Notifications;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Infrastructure.Tests;

public sealed class EmailDeliveryTests
{
    [Fact]
    public async Task QueueIsDurableAndIdempotent()
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseInMemoryDatabase($"email-outbox-{Guid.NewGuid():N}", new InMemoryDatabaseRoot())
            .Options;
        await using var db = new NestyStayDbContext(options);
        using var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var sender = new EmailOutboxSender(services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System);
        var message = new EmailMessage("guest@example.test", "Welcome", "Hello", IdempotencyKey: "welcome-1");

        var first = await sender.QueueAsync(message, CancellationToken.None);
        await db.SaveChangesAsync();
        var second = await sender.QueueAsync(message, CancellationToken.None);

        Assert.Equal(EmailDeliveryStatus.Pending, first.Status);
        Assert.Equal(first.MessageId, second.MessageId);
        Assert.Single(await db.NotificationQueue.Where(item => item.Channel == "Email").ToListAsync());
        Assert.Equal("PENDING", (await db.NotificationQueue.SingleAsync()).DeliveryStatus);
    }

    [Fact]
    public async Task FileTransportWritesAnEmlWithoutNetworkAccess()
    {
        var root = Path.Combine(Path.GetTempPath(), "nesty-email-test", Guid.NewGuid().ToString("N"));
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:LocalOutboxRoot"] = root
            }).Build();
            var transport = new FileEmailDeliveryTransport(configuration, TimeProvider.System);
            var result = await transport.SendAsync(new EmailMessage("guest@example.test", "Subject", "Body"), CancellationToken.None);
        Assert.True(result.Success);
        var files = Directory.GetFiles(root, "*.eml");
        Assert.Single(files);
        var content = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("Subject: Subject", content);
        Assert.Contains("Body", content);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TemplateCatalogDoesNotAllowRawHtmlInjection()
    {
        var message = new EmailMessage("guest@example.test", "ignored", "ignored", TemplateKey: "auth-code", IsHtml: true);
        var rendered = EmailTemplateCatalog.Apply(message, new Dictionary<string, string>
        {
            ["code"] = "<script>alert(1)</script>",
            ["expiresAt"] = "2026-09-02T12:00:00Z"
        });

        Assert.DoesNotContain("<script>", rendered.Body, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", rendered.Body, StringComparison.Ordinal);
        Assert.Contains("<script>", rendered.TextBody, StringComparison.Ordinal);
        Assert.NotNull(rendered.HtmlBody);
    }
}
