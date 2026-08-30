using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using GymAlternatief.Api.Features;
using GymAlternatief.Api.Infrastructure.Http;
using GymAlternatief.Api.Infrastructure.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GymAlternatief.Api.Tests;

public sealed class ApiFoundationTests
{
    [Fact]
    public async Task ValidRequestRunsAsynchronousValidatorWithRequestCancellation()
    {
        var observation = new ValidationObservation();
        var validator = new TestRequestValidator(observation);
        var handlerCalled = false;

        await using var app = await CreateTestApplicationAsync(
            Environments.Production,
            services => services.AddSingleton<IValidator<TestRequest>>(validator),
            application =>
            {
                application.Use(async (context, next) =>
                {
                    observation.RequestCancellation = context.RequestAborted;
                    await next(context);
                });

                application.MapPost("/test/validation", (TestRequest _) =>
                    {
                        handlerCalled = true;
                        return TypedResults.NoContent();
                    })
                    .ValidateRequest<TestRequest>();
            });
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/test/validation",
            new TestRequest("Squat"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(handlerCalled);
        Assert.True(observation.ValidatorWasCalledAsynchronously);
        Assert.Equal(observation.RequestCancellation, observation.ValidatorCancellation);
    }

    [Fact]
    public async Task InvalidRequestReturnsFieldBoundValidationProblemAndSkipsHandler()
    {
        var observation = new ValidationObservation();
        var handlerCalled = false;

        await using var app = await CreateTestApplicationAsync(
            Environments.Production,
            services => services.AddSingleton<IValidator<TestRequest>>(
                new TestRequestValidator(observation)),
            application =>
            {
                application.MapPost("/test/validation", (TestRequest _) =>
                    {
                        handlerCalled = true;
                        return TypedResults.NoContent();
                    })
                    .ValidateRequest<TestRequest>();
            });
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/test/validation",
            new TestRequest(string.Empty));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal(ProblemDetailsConstants.ValidationType, root.GetProperty("type").GetString());
        Assert.Equal("/test/validation", root.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.Equal(
            "'Name' must not be empty.",
            root.GetProperty("errors").GetProperty("Name")[0].GetString());
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task ValidationConventionAdvertisesValidationAndExpectedProblems()
    {
        await using var app = await CreateTestApplicationAsync(
            Environments.Production,
            services => services.AddSingleton<IValidator<TestRequest>>(
                new TestRequestValidator(new ValidationObservation())),
            application =>
            {
                application.MapPost(
                        "/test/validation",
                        (TestRequest _) => TypedResults.NoContent())
                    .ValidateRequest<TestRequest>()
                    .ProducesProblem(StatusCodes.Status409Conflict);
            });

        var endpoint = Assert.Single(
            app.Services
                .GetRequiredService<EndpointDataSource>()
                .Endpoints,
            endpoint => endpoint.DisplayName?.Contains(
                "/test/validation",
                StringComparison.Ordinal) is true);
        var responseStatuses = endpoint.Metadata
            .GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Select(metadata => metadata.StatusCode)
            .ToArray();

        Assert.Contains(StatusCodes.Status400BadRequest, responseStatuses);
        Assert.Contains(StatusCodes.Status409Conflict, responseStatuses);
    }

    [Fact]
    public async Task UnexpectedFailureIncludesSafeDevelopmentDetail()
    {
        await using var app = await CreateFailureApplicationAsync(Environments.Development);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/test/failure");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Sensitive internal failure", root.GetProperty("detail").GetString());
        Assert.Equal(ProblemDetailsConstants.InternalServerErrorType, root.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task UnexpectedFailureHidesInternalDetailsInProduction()
    {
        await using var app = await CreateFailureApplicationAsync(Environments.Production);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/test/failure");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("An unexpected error occurred.", root.GetProperty("title").GetString());
        Assert.False(root.TryGetProperty("detail", out _));
        Assert.DoesNotContain("Sensitive internal failure", body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
    }

    private static Task<WebApplication> CreateFailureApplicationAsync(string environment)
    {
        return CreateTestApplicationAsync(
            environment,
            _ => { },
            application =>
            {
                application.MapGet("/test/failure", ThrowUnexpectedFailure);
            });
    }

    private static IResult ThrowUnexpectedFailure()
    {
        throw new InvalidOperationException("Sensitive internal failure");
    }

    private static async Task<WebApplication> CreateTestApplicationAsync(
        string environment,
        Action<IServiceCollection> configureServices,
        Action<WebApplication> configureApplication)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddApiProblemDetails();
        builder.Services.AddApiFeatures();
        configureServices(builder.Services);

        var app = builder.Build();
        app.UseExceptionHandler();
        configureApplication(app);
        await app.StartAsync();

        return app;
    }

    public sealed record TestRequest(string Name);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator(ValidationObservation observation)
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .MustAsync((_, cancellationToken) =>
                {
                    observation.ValidatorWasCalledAsynchronously = true;
                    observation.ValidatorCancellation = cancellationToken;
                    return Task.FromResult(true);
                });
        }
    }

    private sealed class ValidationObservation
    {
        public CancellationToken RequestCancellation { get; set; }

        public CancellationToken ValidatorCancellation { get; set; }

        public bool ValidatorWasCalledAsynchronously { get; set; }
    }
}
