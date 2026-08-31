using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GymAlternatief.Api.Tests;

public sealed class ApiCorsTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=gymalternatief;Username=test;Password=test";

    [Theory]
    [InlineData("Development", "http://localhost:5173")]
    [InlineData("Production", "https://app.example.nl")]
    public async Task ConfiguredOriginReceivesCorsHeaders(
        string environment,
        string origin)
    {
        await using var factory = CreateFactory(environment, origin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            origin,
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task UnknownOriginReceivesNoCorsHeaders()
    {
        await using var factory = CreateFactory(
            Environments.Production,
            "https://app.example.nl");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "https://unknown.example.nl");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            response.Headers,
            header => header.Key.StartsWith(
                "Access-Control-",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissingRequiredOriginStopsStartupWithoutLeakingConfigurationValues()
    {
        const string sensitiveValue = "do-not-leak-this-connection-secret";
        const string connectionString =
            "Host=localhost;Port=5432;Database=gymalternatief;Username=test;" +
            "Password=" + sensitiveValue;
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Production);
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.UseSetting(
                    "ConnectionStrings:gymalternatief",
                    connectionString);
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var error = exception.ToString();

        Assert.Contains(
            "At least one CORS origin must be configured.",
            error,
            StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveValue, error, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string environment,
        string allowedOrigin)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting(
                    "ConnectionStrings:gymalternatief",
                    TestConnectionString);
                builder.UseSetting("Cors:AllowedOrigins:0", allowedOrigin);
            });
    }
}
