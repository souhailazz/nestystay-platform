using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Domain.Notifications;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Infrastructure.Notifications;

public interface IEmailDeliveryTransport
{
    string ProviderName { get; }
    Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailTransportResult(bool Success, string? ProviderMessageId = null, string? Error = null);

/// <summary>Safe, provider-neutral notification templates. Values are HTML encoded before insertion.</summary>
public static class EmailTemplateCatalog
{
    private static readonly IReadOnlyDictionary<string, EmailTemplate> Templates =
        new Dictionary<string, EmailTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["auth-code"] = new("auth-code", "Your NestyStay verification code", "Use code {{code}}. It expires at {{expiresAt}}.", "<h1>NestyStay verification</h1><p>Use code <strong>{{code}}</strong>. It expires at {{expiresAt}}.</p><p>Need help? Reply to this email or contact your NestyStay support team.</p>"),
            ["password-reset"] = new("password-reset", "Reset your NestyStay password", "Use this password reset token: {{token}}. It expires at {{expiresAt}}.", "<h1>Reset your NestyStay password</h1><p>Use this one-time reset token:</p><p><strong>{{token}}</strong></p><p>It expires at {{expiresAt}}. If you did not request this, contact support immediately.</p>"),
            ["booking-update"] = new("booking-update", "Your NestyStay booking update", "Your booking status is now {{status}}.", "<h1>Your NestyStay booking</h1><p>Your booking status is now <strong>{{status}}</strong>.</p><p>Keep this email for your records.</p>")
        };

    public static EmailTemplate? Find(string? key) =>
        key is not null && Templates.TryGetValue(key, out var template) ? template : null;

    public static EmailMessage Apply(EmailMessage message, IReadOnlyDictionary<string, string>? values = null)
    {
        var template = Find(message.TemplateKey);
        if (template is null || values is null || values.Count == 0)
        {
            return message;
        }

        var subject = Replace(template.Subject, values, html: false);
        var textBody = Replace(template.TextBody, values, html: false);
        var htmlBody = template.HtmlBody is null ? null : Replace(template.HtmlBody, values, html: true);
        return message with
        {
            Subject = subject,
            Body = message.IsHtml && htmlBody is not null ? htmlBody : textBody,
            TextBody = textBody,
            HtmlBody = htmlBody
        };
    }

    private static string Replace(string value, IReadOnlyDictionary<string, string> values, bool html)
    {
        foreach (var (key, replacement) in values)
        {
            var safe = html ? HtmlEncoder.Default.Encode(replacement) : replacement;
            value = value.Replace("{{" + key + "}}", safe, StringComparison.Ordinal);
        }

        return value;
    }
}

/// <summary>Queues email in the existing PostgreSQL notification outbox; it never performs network I/O.</summary>
public sealed class EmailOutboxSender(IServiceScopeFactory scopeFactory, TimeProvider timeProvider) : IEmailSender
{
    public string ProviderName => "NestyStay email outbox";

    public async Task<EmailDeliveryResult> QueueAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Validate(message);
        var idempotencyKey = string.IsNullOrWhiteSpace(message.IdempotencyKey)
            ? message.CorrelationId?.ToString("N")
            : message.IdempotencyKey.Trim();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.NotificationQueue.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Channel == "Email" && item.IdempotencyKey == idempotencyKey && !item.IsDeleted, cancellationToken);
            if (existing is not null)
            {
                return ToResult(existing, ProviderName);
            }
        }

        var now = timeProvider.GetUtcNow();
        var item = new NotificationQueueItem
        {
            Channel = "Email",
            Recipient = message.To.Trim(),
            Subject = message.Subject.Trim(),
            Body = message.Body,
            TextBody = message.TextBody ?? (!message.IsHtml ? message.Body : null),
            HtmlBody = message.HtmlBody ?? (message.IsHtml ? message.Body : null),
            ReplyToEmail = message.ReplyToEmail,
            ReplyToName = message.ReplyToName,
            Status = NotificationStatus.Queued,
            DeliveryStatus = nameof(EmailDeliveryStatus.Pending).ToUpperInvariant(),
            NextAttemptAt = now,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.NotificationQueue.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return new EmailDeliveryResult(item.Id, EmailDeliveryStatus.Pending, ProviderName, AcceptedAt: now);
    }

    private static void Validate(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.To) || !MailAddress.TryCreate(message.To.Trim(), out _))
        {
            throw new ArgumentException("A valid email recipient is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Subject) || message.Subject.Length > 512)
        {
            throw new ArgumentException("Email subject is required and must be at most 512 characters.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Body) && string.IsNullOrWhiteSpace(message.TextBody) && string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            throw new ArgumentException("Email body is required.", nameof(message));
        }

        if (!string.IsNullOrWhiteSpace(message.ReplyToEmail) && !MailAddress.TryCreate(message.ReplyToEmail.Trim(), out _))
        {
            throw new ArgumentException("Reply-to email must be valid when supplied.", nameof(message));
        }
    }

    private static EmailDeliveryResult ToResult(NotificationQueueItem item, string providerName) =>
        new(item.Id, Enum.TryParse<EmailDeliveryStatus>(item.DeliveryStatus, true, out var status) ? status : EmailDeliveryStatus.Pending,
            providerName, item.ProviderMessageId, item.CreatedAt, item.LastError);
}

public sealed class FileEmailDeliveryTransport(IConfiguration configuration, TimeProvider timeProvider) : IEmailDeliveryTransport
{
    public string ProviderName => "Local file email";

    public async Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var root = configuration["Email:LocalOutboxRoot"] ?? Environment.GetEnvironmentVariable("NESTYSTAY_EMAIL_OUTBOX_ROOT") ??
                   Path.Combine(Path.GetTempPath(), "nestystay-email-outbox");
        Directory.CreateDirectory(root);
        var id = $"{timeProvider.GetUtcNow():yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml";
        var target = Path.Combine(Path.GetFullPath(root), id);
        var temp = target + ".tmp";
        var textBody = message.TextBody ?? (!message.IsHtml ? message.Body : null);
        var htmlBody = message.HtmlBody ?? (message.IsHtml ? message.Body : null);
        var headers = $"To: {message.To}\nSubject: {message.Subject}\n" +
                      (string.IsNullOrWhiteSpace(message.ReplyToEmail) ? string.Empty : $"Reply-To: {message.ReplyToEmail}\n");
        var content = textBody is not null && htmlBody is not null
            ? $"{headers}MIME-Version: 1.0\nContent-Type: multipart/alternative; boundary=nesty-stay-boundary\n\n--nesty-stay-boundary\nContent-Type: text/plain; charset=utf-8\n\n{textBody}\n--nesty-stay-boundary\nContent-Type: text/html; charset=utf-8\n\n{htmlBody}\n--nesty-stay-boundary--\n"
            : $"{headers}Content-Type: {(htmlBody is not null ? "text/html" : "text/plain")}; charset=utf-8\n\n{htmlBody ?? textBody ?? message.Body}\n";
        await File.WriteAllTextAsync(temp, content, cancellationToken);
        File.Move(temp, target, true);
        return new EmailTransportResult(true, id);
    }
}

public sealed class BrevoEmailDeliveryTransport(HttpClient httpClient, IConfiguration configuration) : IEmailDeliveryTransport
{
    public string ProviderName => "Brevo transactional email";

    public async Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Email:Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");
        var senderEmail = configuration["Email:Brevo:SenderEmail"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL");
        var senderName = configuration["Email:Brevo:SenderName"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_NAME") ?? "NestyStay";
        var replyToEmail = message.ReplyToEmail ?? configuration["Email:Brevo:ReplyToEmail"] ?? Environment.GetEnvironmentVariable("BREVO_REPLY_TO_EMAIL");
        var replyToName = message.ReplyToName ?? configuration["Email:Brevo:ReplyToName"] ?? Environment.GetEnvironmentVariable("BREVO_REPLY_TO_NAME");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
        {
            return new EmailTransportResult(false, Error: "Brevo credentials are not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { email = senderEmail, name = senderName },
            to = new[] { new { email = message.To } },
            subject = message.Subject,
            textContent = message.TextBody ?? (!message.IsHtml ? message.Body : null),
            htmlContent = message.HtmlBody ?? (message.IsHtml ? message.Body : null),
            replyTo = string.IsNullOrWhiteSpace(replyToEmail) ? null : new { email = replyToEmail, name = replyToName },
            tags = new[] { "nesty-stay" }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = payload.Length > 500 ? payload[..500] : payload;
            return new EmailTransportResult(false, Error: $"Brevo returned {(int)response.StatusCode}: {detail}");
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            return new EmailTransportResult(true, document.RootElement.TryGetProperty("messageId", out var id) ? id.GetString() : null);
        }
        catch (JsonException)
        {
            return new EmailTransportResult(true);
        }
    }
}

public sealed class EmailDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IEmailDeliveryTransport transport,
    TimeProvider timeProvider,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Email delivery worker failed; the next poll will retry.");
            }

            await Task.Delay(PollInterval, timeProvider, stoppingToken);
        }
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var now = timeProvider.GetUtcNow();
        var pending = await db.NotificationQueue
            .Where(item => item.Channel == "Email" && !item.IsDeleted &&
                          (item.DeliveryStatus == "PENDING" || item.DeliveryStatus == "RETRYING") &&
                          (item.NextAttemptAt == null || item.NextAttemptAt <= now))
            .OrderBy(item => item.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var item in pending)
        {
            item.DeliveryStatus = "PROCESSING";
            item.AttemptCount++;
            item.UpdatedAt = now;
        }
        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var item in pending)
        {
            var result = await transport.SendAsync(new EmailMessage(
                item.Recipient,
                item.Subject,
                item.Body,
                item.Id,
                TextBody: item.TextBody,
                HtmlBody: item.HtmlBody,
                ReplyToEmail: item.ReplyToEmail,
                ReplyToName: item.ReplyToName), cancellationToken);
            item.UpdatedAt = timeProvider.GetUtcNow();
            if (result.Success)
            {
                item.DeliveryStatus = "SENT";
                item.Status = NotificationStatus.Sent;
                item.SentAt = item.UpdatedAt;
                item.ProviderMessageId = result.ProviderMessageId;
                item.LastError = null;
                item.NextAttemptAt = null;
            }
            else if (item.AttemptCount >= MaxAttempts)
            {
                item.DeliveryStatus = "DEAD_LETTER";
                item.Status = NotificationStatus.Failed;
                item.LastError = result.Error;
                item.NextAttemptAt = null;
            }
            else
            {
                item.DeliveryStatus = "RETRYING";
                item.Status = NotificationStatus.Failed;
                item.LastError = result.Error;
                item.NextAttemptAt = item.UpdatedAt.AddMinutes(Math.Pow(2, item.AttemptCount - 1));
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
