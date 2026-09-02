using Microsoft.Extensions.Configuration;

namespace NestyStay.Application.Configuration;

/// <summary>
/// Single, provider-neutral view of runtime integrations.  Individual adapters
/// still own their transport details; feature selection is kept in one place so
/// health pages, workers and deployment validation report the same decision.
/// </summary>
public sealed record ProviderFeatureFlags(
    string EmailProvider,
    string BusinessMailProvider,
    string EkycProvider,
    string PaymentProvider,
    string PayoutMode,
    string ObjectStorageProvider,
    bool WebPushEnabled,
    bool SmsEnabled)
{
    public static ProviderFeatureFlags From(IConfiguration configuration) => new(
        // Local/test runs intentionally use the file capture transport unless
        // production explicitly selects Brevo through EMAIL_PROVIDER.
        Resolve(configuration, "Email:Provider", "EMAIL_PROVIDER", "file"),
        Resolve(configuration, "BusinessMail:Provider", "BUSINESS_MAIL_PROVIDER", "zoho"),
        Resolve(configuration, "Integrations:EkycProvider", "EKYC_PROVIDER", "alibaba"),
        Resolve(configuration, "Integrations:PaymentProvider", "PAYMENT_PROVIDER", "stripe"),
        Resolve(configuration, "Payout:Mode", "PAYOUT_MODE", "manual"),
        Resolve(configuration, "Integrations:StorageProvider", "OBJECT_STORAGE_PROVIDER", "local"),
        ResolveBoolean(configuration, "WebPush:Enabled", "WEB_PUSH_ENABLED"),
        ResolveBoolean(configuration, "Sms:Enabled", "SMS_ENABLED"));

    private static string Resolve(IConfiguration configuration, string key, string environmentKey, string fallback)
    {
        var value = configuration[key] ?? Environment.GetEnvironmentVariable(environmentKey);
        if (string.IsNullOrWhiteSpace(value) && environmentKey == "EMAIL_PROVIDER")
            value = Environment.GetEnvironmentVariable("NESTYSTAY_EMAIL_PROVIDER");
        if (string.IsNullOrWhiteSpace(value) && environmentKey == "OBJECT_STORAGE_PROVIDER")
            value = Environment.GetEnvironmentVariable("NESTYSTAY_STORAGE_PROVIDER");
        return (value ?? fallback).Trim();
    }

    private static bool ResolveBoolean(IConfiguration configuration, string key, string environmentKey) =>
        bool.TryParse(configuration[key] ?? Environment.GetEnvironmentVariable(environmentKey), out var value) && value;
}
