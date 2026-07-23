using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using NestyStay.Api.Configuration;
using NestyStay.Application;
using NestyStay.Application.Abstractions;
using NestyStay.Api.Middleware;
using NestyStay.Api.Auth;
using NestyStay.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

ProductionIntegrationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174",
                "https://localhost:5174")
            .AllowAnyHeader()
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
    options.AddPolicy(AdminTokenAuthenticationHandler.AdminPolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration.GetConnectionString("Postgres"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
