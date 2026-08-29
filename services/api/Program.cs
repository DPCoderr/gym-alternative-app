using GymAlternatief.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("gymalternatief");
builder.Services.AddProblemDetails();

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

app.MapGet("/", () => Results.Ok(new
{
    service = "GymAlternatief API",
    status = "foundation-ready",
}));

app.Run();

public partial class Program;
