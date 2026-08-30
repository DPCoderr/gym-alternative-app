using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace GymAlternatief.Api.Infrastructure.Http;

public static class ApiProblemDetailsExtensions
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<UnhandledExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions.TryAdd(
                    ProblemDetailsConstants.TraceIdKey,
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            };
        });

        return services;
    }
}
