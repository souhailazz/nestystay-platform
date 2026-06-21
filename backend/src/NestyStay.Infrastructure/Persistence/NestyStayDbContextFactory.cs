using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NestyStay.Infrastructure.Persistence;

public sealed class NestyStayDbContextFactory : IDesignTimeDbContextFactory<NestyStayDbContext>
{
    public NestyStayDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=nestystay_dev;Username=nestystay;Password=nestystay")
            .Options;

        return new NestyStayDbContext(options);
    }
}
