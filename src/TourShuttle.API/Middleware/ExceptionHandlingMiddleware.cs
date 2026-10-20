using Microsoft.AspNetCore.Mvc;

namespace TourShuttle.API.Middleware;

/// <summary>
/// Catches any exception nobody else handled (a bug, a database that is down) and turns it into
/// a 500 ProblemDetails. The full exception goes to the log only; the client gets a generic message
/// and the correlation id, because exception text can reveal internals (table names, user names, hosts).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    // Middleware is created once, so whatever its constructor receives lives as long as the application.
    // A scoped service (AppDbContext, repositories) must therefore come through InvokeAsync, which is resolved
    // per request. IProblemDetailsService is a singleton and would work in the constructor too; it is taken
    // here so the class follows the one pattern that is always safe.
    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client closed the connection; the CancellationToken stopped our work on purpose.
            // Not a server error, so no Error line and no response (nobody is listening).
            _logger.LogInformation("Request aborted by the client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path.Value);

            if (context.Response.HasStarted)
            {
                // Part of the response is already on its way; the status can no longer change.
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = ex,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    // Development shows the cause to speed up debugging; production never does.
                    Detail = _environment.IsDevelopment()
                        ? ex.Message
                        : "The error was logged. Please quote the correlation id when reporting it."
                }
            });
        }
    }
}
