using Microsoft.EntityFrameworkCore;
using NestyStay.Domain;
using NestyStay.Domain.AssociationManagement;
using NestyStay.Domain.Badges;
using NestyStay.Domain.Common;
using NestyStay.Domain.Identity;
using NestyStay.Domain.Integrations;
using NestyStay.Domain.Pricing;
using NestyStay.Domain.Wellness;

namespace NestyStay.Infrastructure.Persistence;

public static class NestyStaySeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(Enum.GetValues<UserRole>().Select((role, index) => new Role
        {
            Id = SeedGuid("role", index),
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
            Key = role,
            Name = role.ToString()
        }).ToArray());

        modelBuilder.Entity<PricebookEntry>().HasData(DefaultPricebook().ToArray());
        modelBuilder.Entity<BadgeDefinition>().HasData(DefaultBadges().ToArray());
        modelBuilder.Entity<WellnessVisitTypeDefinition>().HasData(DefaultWellnessVisitTypes().ToArray());
        modelBuilder.Entity<AssociationStoragePlan>().HasData(DefaultAssociationStoragePlans().ToArray());
        modelBuilder.Entity<DocumentRetentionRule>().HasData(DefaultRetentionRules().ToArray());
        modelBuilder.Entity<ProviderConfig>().HasData(DefaultProviders().ToArray());
    }

    public static IReadOnlyList<PricebookEntry> DefaultPricebook() =>
    [
        Price("host-listing", 0m, "USD", "Always", "Hosts"),
        Price("host-commission-standard", 3m, "PERCENT", "Per booking", "Hosts"),
        Price("guest-fee-large-long", 8m, "PERCENT", "Per booking", "Guests"),
        Price("guest-fee-mid", 10m, "PERCENT", "Per booking", "Guests"),
        Price("guest-fee-single-night", 12m, "PERCENT", "Per booking", "Guests"),
        Price("guest-ekyc-first-html", 9.99m, "USD", "Per first check", "Guests"),
        Price("guest-ekyc-return-html", 4.99m, "USD", "Per return check", "Guests"),
        Price("guest-ekyc-host-paid-pdf", 0.14m, "USD", "Per booking", "Hosts"),
        Price("alibaba-ekyc-vendor-cost", 0.14m, "USD", "Per check", "NestyStay"),
        Price("verified-host-standard-annual", 60m, "USD", "Annual", "Hosts"),
        Price("trusted-host-standard-annual", 120m, "USD", "Annual", "Hosts"),
        Price("trusted-host-pdf-campaign", 49m, "USD", "One time", "Hosts"),
        Price("founding-platinum-guest-flat", 29m, "USD", "Per booking lifetime", "Founding properties"),
        Price("founding-gold-guest-flat", 36m, "USD", "Per booking lifetime", "Founding properties"),
        Price("founding-silver-guest-flat", 45m, "USD", "Per booking lifetime", "Founding properties"),
        Price("wellness-subscription-pdf", 19m, "USD", "Monthly", "Hosts"),
        Price("association-starter-monthly", 19m, "USD", "Monthly", "Communities"),
        Price("association-pro-monthly", 39m, "USD", "Monthly", "Communities"),
        Price("association-elite-monthly", 79m, "USD", "Monthly", "Communities"),
        Price("officer-commission-min", 8m, "PERCENT", "Per completed visit", "Officers"),
        Price("officer-commission-max", 15m, "PERCENT", "Per completed visit", "Officers")
    ];

    private static IReadOnlyList<BadgeDefinition> DefaultBadges() =>
    [
        Badge("host-free", BadgeLevel.Free, "Host", ["Listings", "Calendar", "Messaging", "QR", "Stripe", "InsuraGuest", "97% payout"]),
        Badge("host-verified", BadgeLevel.Verified, "Host", ["Verified badge", "Custodian directory", "Local business directory", "Guest verification upsell"]),
        Badge("host-trusted", BadgeLevel.Trusted, "Host", ["Trades directory", "Search boost", "Referral program"]),
        Badge("host-wellness", BadgeLevel.Wellness, "Host", ["Police directory", "Wellness visits", "Wellness badge", "Security verified filter"]),
        Badge("officer-verified", BadgeLevel.Verified, "Officer", ["Officer onboarding"]),
        Badge("officer-trusted", BadgeLevel.Trusted, "Officer", ["Wellness jobs"]),
        Badge("business-verified", BadgeLevel.Verified, "LocalBusiness", ["Mild search boost"]),
        Badge("business-trusted", BadgeLevel.Trusted, "LocalBusiness", ["Guest promotion", "Strong search boost"])
    ];

    private static IReadOnlyList<WellnessVisitTypeDefinition> DefaultWellnessVisitTypes() =>
    [
        Wellness(WellnessVisitType.DriveByPatrol, "Drive-By Patrol", 0, "Officer drives past at agreed intervals. No entry. Photos submitted."),
        Wellness(WellnessVisitType.InPersonWithGuest, "In-Person With Guest", 30, "Officer enters and meets guest. Verifies safety. Photo report submitted."),
        Wellness(WellnessVisitType.InPersonWithoutGuest, "In-Person Without Guest", 30, "Officer inspects property while guest is away. Photo report submitted."),
        Wellness(WellnessVisitType.PropertyEscort, "Property Escort", 120, "Officer accompanies owner or guest around property and surroundings."),
        Wellness(WellnessVisitType.PersonalEscort, "Personal Escort", 240, "Officer accompanies owner or guest anywhere."),
        Wellness(WellnessVisitType.HalfDaySecurity, "Half Day Security", 240, "Officer covers property and personal security as agreed."),
        Wellness(WellnessVisitType.FullDaySecurity, "Full Day Security", 480, "Full property and personal security coverage."),
        Wellness(WellnessVisitType.OvernightSecurity, "Overnight Security", 720, "Officer stationed at property from dusk to dawn.")
    ];

    private static IReadOnlyList<AssociationStoragePlan> DefaultAssociationStoragePlans() =>
    [
        StoragePlan("free", 0m, 0m, 100, 2, false),
        StoragePlan("starter", 19m, 190m, 1024, 7, false),
        StoragePlan("pro", 39m, 390m, 10240, 7, true),
        StoragePlan("elite", 79m, 790m, 51200, 99, true)
    ];

    private static IReadOnlyList<DocumentRetentionRule> DefaultRetentionRules() =>
    [
        Retention("MeetingMinutes", NestyStayBusinessRules.AssociationMinimumRetentionYears),
        Retention("FinancialStatement", NestyStayBusinessRules.AssociationMinimumRetentionYears),
        Retention("Proxy", NestyStayBusinessRules.AssociationMinimumRetentionYears),
        Retention("VoteResult", NestyStayBusinessRules.AssociationMinimumRetentionYears),
        Retention("WellnessReport", 7)
    ];

    private static IReadOnlyList<ProviderConfig> DefaultProviders() =>
    [
        Provider(ProviderKind.Ekyc, "AlibabaCloud", true),
        Provider(ProviderKind.Ekyc, "Jumio", false),
        Provider(ProviderKind.Ekyc, "Onfido", false),
        Provider(ProviderKind.Payment, "Stripe", true),
        Provider(ProviderKind.Payment, "PayPal", false),
        Provider(ProviderKind.Storage, "CloudflareR2", true),
        Provider(ProviderKind.Storage, "DigitalOceanSpaces", false),
        Provider(ProviderKind.Storage, "AmazonS3", false),
        Provider(ProviderKind.Notification, "AwsSesTwilioFirebase", true),
        Provider(ProviderKind.Insurance, "InsuraGuest", true)
    ];

    private static PricebookEntry Price(string key, decimal amount, string unit, string cadence, string appliesTo) => new()
    {
        Id = SeedGuid("price", key),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
        Key = key,
        Label = key.Replace('-', ' '),
        Amount = amount,
        CurrencyOrUnit = unit,
        Cadence = cadence,
        AppliesTo = appliesTo,
        IsConfigurable = true
    };

    private static BadgeDefinition Badge(string key, BadgeLevel level, string appliesTo, IReadOnlyList<string> unlocks) => new()
    {
        Id = SeedGuid("badge", key),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
        Key = key,
        Level = level,
        AppliesTo = appliesTo,
        UnlocksJson = System.Text.Json.JsonSerializer.Serialize(unlocks)
    };

    private static WellnessVisitTypeDefinition Wellness(WellnessVisitType type, string name, int minutes, string description) => new()
    {
        Id = SeedGuid("wellness", type.ToString()),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
        VisitType = type,
        Name = name,
        MinimumDurationMinutes = minutes,
        Description = description
    };

    private static AssociationStoragePlan StoragePlan(string key, decimal monthly, decimal annual, int megabytes, int years, bool zoom) => new()
    {
        Id = SeedGuid("storage-plan", key),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
        Key = key,
        MonthlyPrice = monthly,
        AnnualPrice = annual,
        StorageMegabytes = megabytes,
        RetentionYears = years,
        IncludesZoomArchive = zoom
    };

    private static DocumentRetentionRule Retention(string documentType, int years) => new()
    {
        Id = SeedGuid("retention", documentType),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
        DocumentType = documentType,
        RetentionYears = years
    };

    private static ProviderConfig Provider(ProviderKind kind, string name, bool primary) => new()
    {
        Id = SeedGuid("provider", $"{kind}-{name}"),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
        Kind = kind,
        ProviderName = name,
        IsPrimary = primary,
        EncryptedConfigReference = $"vault://nestystay/{kind.ToString().ToLowerInvariant()}/{name.ToLowerInvariant()}"
    };

    private static Guid SeedGuid(string group, int index) => SeedGuid(group, index.ToString());

    private static Guid SeedGuid(string group, string key)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"nestystay:{group}:{key}"));
        return new Guid(bytes);
    }
}
