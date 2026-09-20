using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Domain.Notifications;
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
    public async Task WorkerRetriesTransportExceptionsInsteadOfLeavingRowsProcessing()
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseInMemoryDatabase($"email-worker-{Guid.NewGuid():N}")
            .Options;
        await using (var seed = new NestyStayDbContext(options))
        {
            seed.NotificationQueue.Add(new NotificationQueueItem
            {
                Channel = "Email",
                Recipient = "guest@example.test",
                Subject = "Retry me",
                Body = "This delivery should retry.",
                Status = NotificationStatus.Queued,
                DeliveryStatus = "PENDING",
                NextAttemptAt = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        using var services = new ServiceCollection()
            .AddScoped(_ => new NestyStayDbContext(options))
            .BuildServiceProvider();
        var worker = new EmailDeliveryWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            new ThrowingEmailTransport(),
            TimeProvider.System,
            NullLogger<EmailDeliveryWorker>.Instance);
        using var stopping = new CancellationTokenSource();

        await worker.StartAsync(stopping.Token);
        try
        {
            NotificationQueueItem? processed = null;
            for (var attempt = 0; attempt < 40 && processed is null; attempt++)
            {
                await Task.Delay(25);
                await using var check = new NestyStayDbContext(options);
                processed = await check.NotificationQueue.SingleAsync();
                if (processed.DeliveryStatus is "PENDING" or "PROCESSING")
                {
                    processed = null;
                }
            }

            Assert.NotNull(processed);
            Assert.Equal("RETRYING", processed!.DeliveryStatus);
            Assert.Contains("HttpRequestException", processed.LastError, StringComparison.Ordinal);
            Assert.Equal(1, processed.AttemptCount);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
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

    [Fact]
    public void TemplateCatalogCoversMilestoneWorkflowsWithOneUnifiedShell()
    {
        var expectedKeys = new[]
        {
            "passwordless-login", "auth-code", "password-reset", "owner-invitation", "two-factor-enabled", "new-login-alert",
            "booking-request-received", "booking-request-to-host", "booking-pending-verification", "verification-processing",
            "verification-approved", "verification-rejected", "booking-approved", "booking-rejected", "booking-host-declined",
            "booking-cancelled", "payment-authorized", "payment-failed", "payment-confirmed", "payment-refunded", "receipt-issued", "trip-reminder",
            "badge-upgrade-submitted", "badge-upgrade-approved", "badge-upgrade-rejected", "badge-renewal-due",
            "officer-application-submitted", "officer-application-approved", "officer-application-changes-requested",
            "wellness-assignment-confirmed", "wellness-report-ready", "wellness-booking-cancelled", "subscription-renewal-due",
            "subscription-payment-failed", "payout-statement-ready", "provider-application-submitted", "provider-approved",
            "provider-rejected", "provider-changes-requested", "directory-quote-received", "review-response", "qr-issued", "qr-revoked",
            "invoice-issued", "invoice-payment-received", "invoice-payment-reminder", "maintenance-update", "community-notice",
            "governance-vote-opened", "gate-pass-issued", "gate-pass-revoked", "document-expiry", "booking-update"
        };

        Assert.Equal(expectedKeys.Length, EmailTemplateCatalog.All.Count);
        foreach (var key in expectedKeys)
        {
            var template = EmailTemplateCatalog.Find(key);
            Assert.NotNull(template);
            Assert.Contains("NESTY STAY", template!.HtmlBody!, StringComparison.Ordinal);
            Assert.Contains("support@nestystay.net", template.HtmlBody, StringComparison.Ordinal);
            Assert.Contains("Jamaica stays, made personal.", template.HtmlBody, StringComparison.Ordinal);
        }
    }

    private sealed class ThrowingEmailTransport : IEmailDeliveryTransport
    {
        public string ProviderName => "Test transport";

        public Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            throw new HttpRequestException("simulated provider timeout");
    }
}
