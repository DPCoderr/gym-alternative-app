using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GymAlternatief.Api.Infrastructure.Http;

public sealed partial class UnhandledExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(logger, httpContext.Request.Path, exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = ProblemDetailsConstants.InternalServerErrorType,
                Title = "An unexpected error occurred.",
                Detail = environment.IsDevelopment() ? exception.Message : null,
            },
        });
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred while processing {RequestPath}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string requestPath,
        Exception exception);
}
