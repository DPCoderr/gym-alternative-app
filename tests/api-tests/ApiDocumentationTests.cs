using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace GymAlternatief.Api.Tests;

public sealed class ApiDocumentationTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=gymalternatief;Username=test;Password=test";

    [Theory]
    [InlineData("/openapi/v1.json", "application/json")]
    [InlineData("/scalar", "text/html")]
    public async Task DocumentationEndpointsAreAvailableInDevelopment(
        string path,
        string expectedMediaType)
    {
        await using var factory = CreateFactory(Environments.Development);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task DocumentationEndpointsAreNotAvailableInProduction(string path)
    {
        await using var factory = CreateFactory(Environments.Production);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting(
                    "ConnectionStrings:gymalternatief",
                    TestConnectionString);
                builder.UseSetting(
                    "Cors:AllowedOrigins:0",
                    environment == Environments.Development
                        ? "http://localhost:5173"
                        : "https://app.example.nl");
            });
    }
}
