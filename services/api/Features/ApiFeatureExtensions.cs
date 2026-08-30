using System.Globalization;
using FluentValidation;

namespace GymAlternatief.Api.Features;

public static class ApiFeatureExtensions
{
    public static IServiceCollection AddApiFeatures(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en");
        services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Transient);

        return services;
    }

    public static RouteGroupBuilder MapApiFeatures(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        // Register each feature's endpoint extension on the shared API group.

        return api;
    }
}
