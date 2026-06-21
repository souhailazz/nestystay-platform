using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NestyStay.Api.Auth;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Api.Tests;

public sealed class NestyStayApiFactory : WebApplicationFactory<Program>
{
    public const string AdminToken = "test-admin-token";
    public const string OperatorToken = "test-operator-token";
    private readonly string _databaseName = $"nestystay-api-tests-{Guid.NewGuid():N}";
    private readonly ServiceProvider _inMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AdminTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(AdminToken),
                ["Security:OperatorTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(OperatorToken)
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<NestyStayDbContext>();
            services.RemoveAll<DbContextOptions<NestyStayDbContext>>();
            services.AddDbContext<NestyStayDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .UseInternalServiceProvider(_inMemoryProvider));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inMemoryProvider.Dispose();
        }

        base.Dispose(disposing);
    }
}
