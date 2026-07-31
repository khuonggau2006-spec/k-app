using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Common.Exceptions;

namespace TaskMgmt.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(validationException.Errors)
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                }),

            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = notFoundException.Message,
                    Status = StatusCodes.Status404NotFound,
                }),

            UnauthorizedException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = unauthorizedException.Message,
                    Status = StatusCodes.Status401Unauthorized,
                }),

            ForbiddenException forbiddenException => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = forbiddenException.Message,
                    Status = StatusCodes.Status403Forbidden,
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "An unexpected error occurred",
                    Status = StatusCodes.Status500InternalServerError,
                }),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync((object)problemDetails, cancellationToken);

        return true;
    }
}
