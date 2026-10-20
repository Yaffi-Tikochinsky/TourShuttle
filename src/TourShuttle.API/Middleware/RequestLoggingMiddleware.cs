using System.Diagnostics;

namespace TourShuttle.API.Middleware;

/// <summary>
/// One Information line per request, written when it ends: method, path, status and duration.
/// Bodies, headers and query strings are never logged, so passwords (login, register) and tokens
/// (Authorization header) cannot reach the log.
/// Registered outside the exception middleware, so a failed request is logged with its final 500.
/// </summary>
public class RequestLoggingMiddleware
{
    /// <summary>Render calls it every few seconds; logging each call would bury the real requests.</summary>
    public const string HealthPath = "/health";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals(HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var start = Stopwatch.GetTimestamp();
        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }
}
