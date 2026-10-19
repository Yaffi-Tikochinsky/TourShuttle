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
public class Result<T>
{
    private Result(ResultStatus status, T? value, string? error)
    {
        Status = status;
        Value = value;
        Error = error;
    }

    public ResultStatus Status { get; }
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, null);
    public static Result<T> Validation(string error) => new(ResultStatus.Validation, default, error);
    public static Result<T> Unauthorized(string error) => new(ResultStatus.Unauthorized, default, error);
    public static Result<T> Forbidden(string error) => new(ResultStatus.Forbidden, default, error);
    public static Result<T> NotFound(string error) => new(ResultStatus.NotFound, default, error);
    public static Result<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
}
