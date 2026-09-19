using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Configuration;

public static class AdministratorBootstrapper
{
    public static async Task BootstrapAdministratorAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!ResolveBoolean(app.Configuration, "Security:AdminBootstrap:Enabled", "NESTYSTAY_ADMIN_BOOTSTRAP_ENABLED"))
        {
            return;
        }

        var email = Resolve(app.Configuration, "Security:AdminBootstrap:Email", "NESTYSTAY_ADMIN_BOOTSTRAP_EMAIL");
        var password = Resolve(app.Configuration, "Security:AdminBootstrap:Password", "NESTYSTAY_ADMIN_BOOTSTRAP_PASSWORD");
        var displayName = Resolve(app.Configuration, "Security:AdminBootstrap:DisplayName", "NESTYSTAY_ADMIN_BOOTSTRAP_DISPLAY_NAME")
            ?? "NestyStay Administrator";
        var permissions = ResolvePermissions(
            Resolve(app.Configuration, "Security:AdminBootstrap:Permissions", "NESTYSTAY_ADMIN_BOOTSTRAP_PERMISSIONS"));
        var requireTwoFactor = ResolveBoolean(app.Configuration, "Security:AdminBootstrap:RequireTwoFactor", "NESTYSTAY_ADMIN_BOOTSTRAP_REQUIRE_TOTP");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Administrator bootstrap is enabled but administrator email or password is missing.");
        }

        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPhaseOneStore>();
        _ = await store.BootstrapAdministratorAsync(
            new AdministratorBootstrapRequest(email, password, displayName, permissions, requireTwoFactor),
            cancellationToken);
    }

    private static string? Resolve(IConfiguration configuration, string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }

    private static bool ResolveBoolean(IConfiguration configuration, string configurationKey, string environmentKey)
    {
        if (bool.TryParse(configuration[configurationKey], out var configured))
        {
            return configured;
        }

        return bool.TryParse(Environment.GetEnvironmentVariable(environmentKey), out var environmentValue) && environmentValue;
    }

    private static IReadOnlyList<string>? ResolvePermissions(string? rawPermissions) =>
        string.IsNullOrWhiteSpace(rawPermissions)
            ? null
            : rawPermissions
                .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
}
