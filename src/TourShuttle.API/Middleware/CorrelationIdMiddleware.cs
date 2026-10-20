using System.Text.RegularExpressions;

namespace TourShuttle.API.Middleware;

/// <summary>
/// Gives every request an id that appears in all of its log lines and in the X-Correlation-Id response header,
/// so a client reporting an error can quote the id and the matching log lines can be found.
/// Registered first, so its logging scope is still open when the exception middleware logs an error.
/// </summary>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // A client may send its own id to tie its logs to ours. It is accepted only if it is short and plain:
        // an id with a line break could forge extra lines in the log (log injection).
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N")[..12];

        context.Items[ItemKey] = correlationId;
        // ASP.NET Core's own log lines and error responses use the trace identifier; keep them the same id.
        context.TraceIdentifier = correlationId;

        // Headers can change only until the response starts, so the header is added at that moment,
        // whether the request succeeded or failed.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Every log line written inside this scope, in any class, carries CorrelationId
        // (nlog.config prints it with ${scopeproperty:CorrelationId}).
        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            await _next(context);
        }
    }

    /// <summary>The id of the current request, for code that builds responses (ProblemDetails).</summary>
    public static string? Get(HttpContext context) => context.Items[ItemKey] as string;

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex SafeId();
}
