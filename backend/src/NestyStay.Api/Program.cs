using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using NestyStay.Api.Configuration;
using NestyStay.Application;
using NestyStay.Application.Abstractions;
using NestyStay.Api.Middleware;
using NestyStay.Api.Auth;
using NestyStay.Api.Services;
using NestyStay.Infrastructure;
using NestyStay.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

ProductionIntegrationValidator.Validate(builder.Configuration, builder.Environment);

var defaultCorsOrigins = new[]
{
    "http://localhost:3000",
    "http://127.0.0.1:3000",
    "https://localhost:3000",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://localhost:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5174",
    "https://localhost:5174"
};

// A split deployment (for example app.example.com + api.example.com) cannot
// use the old localhost-only policy.  Keep the local defaults, while allowing
// the operator to provide an explicit comma-separated production allow-list.
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(child => child.Value)
    .Concat((builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Concat((Environment.GetEnvironmentVariable("NESTYSTAY_CORS_ALLOWED_ORIGINS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var corsOrigins = configuredCorsOrigins.Length == 0 ? defaultCorsOrigins : configuredCorsOrigins;
if (corsOrigins.Any(origin => origin == "*"))
{
    throw new InvalidOperationException("CORS credentials require explicit origins; wildcard origins are not allowed.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowCredentials()
            .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
builder.Services.AddSingleton<IAccessTokenService, SignedAccessTokenService>();
builder.Services.AddAuthentication(AdminTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdminTokenAuthenticationHandler>(
        AdminTokenAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    AdminAuthorizationPolicies.AddPolicies(options);
});
builder.Services.AddNestyStayRateLimiting(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(
    builder.Configuration.GetConnectionString("Postgres"),
    builder.Configuration.GetValue("BackgroundJobs:Enabled", true));
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true))
{
    builder.Services.AddHostedService<MilestoneMaintenanceService>();
    builder.Services.AddHostedService<CalendarSyncMaintenanceService>();
    builder.Services.AddHostedService<PropertyManagerDocumentExpiryService>();
    builder.Services.AddHostedService<PropertyManagerDocumentExportService>();
    builder.Services.AddHostedService<PropertyManagerSubscriptionMaintenanceService>();
    builder.Services.AddHostedService<PropertyManagerProfessionalWorker>();
}

var app = builder.Build();

// The deployment runner can apply reviewed EF migrations inside the private
// Compose network without exposing PostgreSQL. This is opt-in and exits before
// starting HTTP/background services.
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnly") ||
    string.Equals(Environment.GetEnvironmentVariable("NESTYSTAY_MIGRATE_ONLY"), "true", StringComparison.OrdinalIgnoreCase))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
    await db.Database.MigrateAsync();
    return;
}

await app.BootstrapAdministratorAsync();

app.MapOpenApi();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseProductionSecurityHeaders(app.Environment);
if (builder.Configuration.GetValue<bool>("Security:EnableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");

app.UseAuthentication();
app.UseMiddleware<CookieCsrfMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

// The optional compose worker sidecar runs the same host without publishing a port.
// It is still a normal ASP.NET host so migrations, DI and health diagnostics stay identical.
if (builder.Configuration.GetValue<bool>("Worker:Enabled"))
{
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");
}

app.Run();

public partial class Program;
