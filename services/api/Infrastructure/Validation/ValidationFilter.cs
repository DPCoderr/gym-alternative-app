using System.Diagnostics;
using FluentValidation;
using GymAlternatief.Api.Infrastructure.Http;

namespace GymAlternatief.Api.Infrastructure.Validation;

public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments
            .OfType<TRequest>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"Endpoint has no {typeof(TRequest).Name} argument.");

        var result = await validator.ValidateAsync(
            request,
            context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(
                result.ToDictionary(),
                type: ProblemDetailsConstants.ValidationType,
                instance: context.HttpContext.Request.Path,
                extensions: new Dictionary<string, object?>
                {
                    [ProblemDetailsConstants.TraceIdKey] =
                        Activity.Current?.Id ?? context.HttpContext.TraceIdentifier,
                });
        }

        return await next(context);
    }
}
