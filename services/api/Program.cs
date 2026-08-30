using GymAlternatief.Api.Features;
using GymAlternatief.Api.Infrastructure.Http;
using GymAlternatief.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("gymalternatief");
builder.Services.AddApiProblemDetails();
builder.Services.AddApiFeatures();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

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
