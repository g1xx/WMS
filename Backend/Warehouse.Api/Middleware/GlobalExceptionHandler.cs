using Microsoft.AspNetCore.Diagnostics;
using Warehouse.Application.Common;

namespace Warehouse.Api.Middleware;

// Last line of defense for anything that escapes a controller/service unhandled.
// Writes a plain string body (not a ProblemDetails object) so it matches the shape
// ResultExtensions.ToActionResult() already produces for expected failures — the
// frontend's `alert(error.response?.data || fallback)` pattern works the same way
// whether the failure was anticipated (Result.Failure) or not.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            // A concurrency race (two workers/actions touching the same row) is an
            // expected contention scenario, not a bug — every endpoint should surface
            // it the same friendly way, not just the one that happens to catch it locally.
            ConcurrencyConflictException => (
                StatusCodes.Status409Conflict,
                "This record was just changed by someone else. Please refresh and try again."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again or contact support."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled conflict on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(message, cancellationToken);

        return true;
    }
}
