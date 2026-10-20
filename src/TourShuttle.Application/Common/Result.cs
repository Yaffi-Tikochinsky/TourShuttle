namespace TourShuttle.Application.Common;

/// <summary>Business outcome of a service call. The API layer translates it to an HTTP status.</summary>
public enum ResultStatus
{
    Success,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

/// <summary>
/// Services return a Result instead of throwing or knowing about HTTP,
/// so they can be unit tested without any web infrastructure.
/// </summary>
public abstract class ResultBase
{
    protected ResultBase(ResultStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public ResultStatus Status { get; }
    public string? Error { get; }
    public bool IsSuccess => Status == ResultStatus.Success;
}

/// <summary>An outcome with no value on success, such as an update or a delete (HTTP 204).</summary>
public class Result : ResultBase
{
    private Result(ResultStatus status, string? error) : base(status, error)
    {
    }

    public static Result Success() => new(ResultStatus.Success, null);
    public static Result Validation(string error) => new(ResultStatus.Validation, error);
    public static Result Forbidden(string error) => new(ResultStatus.Forbidden, error);
    public static Result NotFound(string error) => new(ResultStatus.NotFound, error);
    public static Result Conflict(string error) => new(ResultStatus.Conflict, error);
}

/// <summary>An outcome that carries a value on success.</summary>
public class Result<T> : ResultBase
{
    private Result(ResultStatus status, T? value, string? error) : base(status, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, null);
    public static Result<T> Validation(string error) => new(ResultStatus.Validation, default, error);
    public static Result<T> Unauthorized(string error) => new(ResultStatus.Unauthorized, default, error);
    public static Result<T> Forbidden(string error) => new(ResultStatus.Forbidden, default, error);
    public static Result<T> NotFound(string error) => new(ResultStatus.NotFound, default, error);
    public static Result<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
}
