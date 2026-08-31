using GymAlternatief.Api.Infrastructure.Persistence;
using GymAlternatief.Api.Features;
using GymAlternatief.Api.Infrastructure.Http;
using GymAlternatief.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("gymalternatief");
builder.Services.AddProblemDetails();
builder.Services.AddApiProblemDetails();
builder.Services
    .AddOptions<ApiCorsOptions>()
    .BindConfiguration(ApiCorsOptions.SectionName)
    .Validate(
        options => options.AllowedOrigins is { Length: > 0 },
        "At least one CORS origin must be configured.")
    .ValidateOnStart();
var allowedOrigins = builder.Configuration
    .GetSection($"{ApiCorsOptions.SectionName}:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(
    ApiCorsOptions.PolicyName,
    policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddApiFeatures();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(ApiCorsOptions.PolicyName);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => !registration.Tags.Contains("live"),
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
        .AllowAnonymous();

    app.MapScalarApiReference(options =>
        {
            options.WithTitle("GymAlternatief API");
        })
        .AllowAnonymous();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "GymAlternatief API",
    status = "foundation-ready",
}));

app.MapApiFeatures();

app.Run();

public partial class Program;
