using Microsoft.EntityFrameworkCore;
using NestyStay.Infrastructure.Persistence;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    await Console.Error.WriteLineAsync("ConnectionStrings__Postgres is not set.");
    return 2;
}

var applyMigrations = args.Any(argument =>
    string.Equals(argument, "--apply", StringComparison.OrdinalIgnoreCase));

var options = new DbContextOptionsBuilder<NestyStayDbContext>()
    .UseNpgsql(connectionString)
    .Options;

using var context = new NestyStayDbContext(options);

List<string> pending;
try
{
    pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync("Failed to check migration status: " + ex.Message);
    return 2;
}

if (pending.Count > 0)
{
    if (!applyMigrations)
    {
        await Console.Error.WriteLineAsync($"Deploy blocked: {pending.Count} pending EF Core migration(s) are not applied to this database:");
        foreach (var migration in pending)
        {
            await Console.Error.WriteLineAsync("  - " + migration);
        }
        await Console.Error.WriteLineAsync("Apply them with the reviewed deployment migration step, or rerun this tool with --apply.");
        return 1;
    }

    Console.WriteLine($"Applying {pending.Count} pending EF Core migration(s) before release activation...");
    await context.Database.MigrateAsync();

    var remaining = (await context.Database.GetPendingMigrationsAsync()).ToList();
    if (remaining.Count > 0)
    {
        await Console.Error.WriteLineAsync($"Migration application did not complete: {remaining.Count} migration(s) remain pending.");
        foreach (var migration in remaining)
        {
            await Console.Error.WriteLineAsync("  - " + migration);
        }
        return 1;
    }

    Console.WriteLine("All pending migrations were applied successfully.");
    return 0;
}

Console.WriteLine("No pending migrations. Schema is up to date.");
return 0;
