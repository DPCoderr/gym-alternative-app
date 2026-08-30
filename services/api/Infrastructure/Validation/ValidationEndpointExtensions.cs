namespace GymAlternatief.Api.Infrastructure.Validation;

public static class ValidationEndpointExtensions
{
    public static RouteHandlerBuilder ValidateRequest<TRequest>(
        this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
    }
}
