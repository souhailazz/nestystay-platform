using Microsoft.EntityFrameworkCore;
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
    public void SeedDataIncludesConflictingPricebookValuesAsConfigurableEntries()
    {
        var pricebook = NestyStaySeed.DefaultPricebook();

        Assert.Contains(pricebook, item => item.Key == "trusted-host-pdf-campaign" && item.Amount == 49m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "verified-host-standard-annual" && item.Amount == 60m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "guest-ekyc-first-html" && item.Amount == 9.99m && item.IsConfigurable);
        Assert.Contains(pricebook, item => item.Key == "guest-ekyc-host-paid-pdf" && item.Amount == 0.14m && item.IsConfigurable);
    }

    private static NestyStayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseNpgsql("Host=localhost;Database=nestystay;Username=nestystay;Password=nestystay")
            .Options;

        return new NestyStayDbContext(options);
    }
}
