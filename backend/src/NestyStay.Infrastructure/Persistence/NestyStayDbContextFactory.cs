using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NestyStay.Infrastructure.Persistence;

public sealed class NestyStayDbContextFactory : IDesignTimeDbContextFactory<NestyStayDbContext>
{
    public NestyStayDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ??
            "Host=127.0.0.1;Port=55432;Database=nestystay_dev;Username=nestystay;Password=nestystay";

        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new NestyStayDbContext(options);
    }
}
