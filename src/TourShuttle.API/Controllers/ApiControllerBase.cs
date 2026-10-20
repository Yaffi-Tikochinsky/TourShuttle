using Microsoft.AspNetCore.Mvc;
using TourShuttle.Application.Common;

namespace TourShuttle.API.Controllers;

/// <summary>Shared translation from a service <see cref="Result{T}"/> to an HTTP response.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Returns 200 with the value on success, or the matching error status as ProblemDetails.</summary>
    protected ActionResult<T> FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Failure(result);

    /// <summary>Returns 204 on success (updates and deletes), or the matching error status.</summary>
    protected ActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : Failure(result);

    /// <summary>Maps a failed result to 400/401/403/404/409.</summary>
    protected ActionResult Failure(ResultBase result)
    {
        var status = result.Status switch
        {
            ResultStatus.Validation => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => throw new InvalidOperationException($"{result.Status} is not a failure status.")
        };

        return Problem(detail: result.Error, statusCode: status);
    }
}
