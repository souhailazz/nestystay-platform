using Microsoft.EntityFrameworkCore;
using NestyStay.Domain.Badges;
using NestyStay.Domain.Integrations;
using NestyStay.Domain.Pricing;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Infrastructure.Tests;

public sealed class SchemaModelTests
{
    [Fact]
    public void DbContextContainsThePlannedBackendSchema()
    {
        using var db = CreateContext();

        var entityNames = db.Model.GetEntityTypes()
            .Select(entity => entity.ClrType.Name)
            .ToHashSet();

        Assert.Contains(nameof(PricebookEntry), entityNames);
        Assert.Contains(nameof(ProviderConfig), entityNames);
        Assert.True(entityNames.Count >= 70);
    }

    [Fact]
    public void SchemaCatalogDocumentsTheImplementedTables()
    {
        Assert.Contains(SchemaCatalog.Tables, table => table.TableName == "booking");
        Assert.Contains(SchemaCatalog.Tables, table => table.TableName == "wellness_visit");
        Assert.Contains(SchemaCatalog.Tables, table => table.TableName == "financial_statement_version");
    }

    [Fact]
    public void SeedDataIncludesSignedContractPricebookValuesAsConfigurableEntries()
    {
        var pricebook = NestyStaySeed.DefaultPricebook();

        Assert.Contains(pricebook, item => item.Key == "trusted-host-pdf-campaign" && item.Amount == 49m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "verified-host-standard-annual" && item.Amount == 0m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "guest-fee-large-long" && item.Amount == 9m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "guest-ekyc-first-html" && item.Amount == 9.99m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "guest-ekyc-host-paid-pdf" && item.Amount == 0.14m && item.IsConfigurable);
    }

    [Fact]
    public void SeedDataIncludesSignedHostBadgeUnlocks()
    {
        var seed = NestyStaySeed.DefaultBadges();
        var free = seed.Single(row => row.Key == "host-free");
        var trusted = seed.Single(row => row.Key == "host-trusted");
        var wellness = seed.Single(row => row.Key == "host-wellness");

        Assert.Equal("[\"Listings\",\"Calendar\",\"Messaging\",\"QR code access\",\"97% payout\"]", free.UnlocksJson);
        Assert.Equal("[\"Trusted badge\",\"Trades directory\",\"Search boost\",\"Referral program\"]", trusted.UnlocksJson);
        Assert.Equal("[\"Police directory\",\"Wellness visits\",\"In-person guest ID check\",\"Drive-by property patrol\",\"Wellness badge\",\"Police Verified filter\"]", wellness.UnlocksJson);
    }

    private static NestyStayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseNpgsql("Host=localhost;Database=nestystay;Username=nestystay;Password=nestystay")
            .Options;

        return new NestyStayDbContext(options);
    }
}
