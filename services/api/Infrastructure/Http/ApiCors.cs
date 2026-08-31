using GymAlternatief.Api.Infrastructure.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace GymAlternatief.Api.Infrastructure.Http;

public static class ApiCorsPolicy
{
    public const string Frontend = "Frontend";
}

public sealed class ApiCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}

public static class ApiCorsServiceCollectionExtensions
{
    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<ApiCorsOptions, ApiCorsOptionsValidator>(
            configuration.GetSection(ApiCorsOptions.SectionName));
        services.AddSingleton<
            IConfigureOptions<CorsOptions>,
            ConfigureApiCorsOptions>();
        services.AddCors();

        return services;
    }
}

internal sealed class ApiCorsOptionsValidator : IValidateOptions<ApiCorsOptions>
{
    private const string MissingOriginsMessage =
        "At least one CORS origin must be configured.";

    private const string InvalidOriginsMessage =
        "CORS origins must be unique HTTPS origins or HTTP loopback origins without " +
        "wildcards, paths, query strings, fragments, or credentials.";

    public ValidateOptionsResult Validate(string? name, ApiCorsOptions options)
    {
        if (options.AllowedOrigins.Length == 0)
        {
            return ValidateOptionsResult.Fail(MissingOriginsMessage);
        }

        var origins = options.AllowedOrigins;
        if (origins.Any(origin => !IsValidOrigin(origin)) ||
            origins.Distinct(StringComparer.OrdinalIgnoreCase).Count() != origins.Length)
        {
            return ValidateOptionsResult.Fail(InvalidOriginsMessage);
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidOrigin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains('*', StringComparison.Ordinal) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var origin) ||
            (origin.Scheme != Uri.UriSchemeHttps && origin.Scheme != Uri.UriSchemeHttp) ||
            (origin.Scheme == Uri.UriSchemeHttp && !origin.IsLoopback) ||
            !string.IsNullOrEmpty(origin.UserInfo) ||
            !string.IsNullOrEmpty(origin.Query) ||
            !string.IsNullOrEmpty(origin.Fragment) ||
            origin.AbsolutePath != "/")
        {
            return false;
        }

        return value.Equals(
            origin.GetLeftPart(UriPartial.Authority),
            StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class ConfigureApiCorsOptions(
    IOptions<ApiCorsOptions> apiCorsOptions) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        options.AddPolicy(
            ApiCorsPolicy.Frontend,
            policy => policy
                .WithOrigins(apiCorsOptions.Value.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    }
}
