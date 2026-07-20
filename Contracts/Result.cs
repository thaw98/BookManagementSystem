namespace Contracts;

public enum ResultStatus
{
    Success,
    Validation,
    NotFound,
    Duplicate,
    Unauthorized,
    Forbidden,
    Error
}

public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = "";
    public ResultStatus Status { get; init; }
    public T? Data { get; init; }

    public static Result<T> Success(T data, string message = "Success") =>
        new() { IsSuccess = true, Status = ResultStatus.Success, Message = message, Data = data };

    public static Result<T> Ok(string message = "Success") =>
        new() { IsSuccess = true, Status = ResultStatus.Success, Message = message };

    public static Result<T> Validation(string message) =>
        new() { Status = ResultStatus.Validation, Message = message };

    public static Result<T> NotFound(string message = "Record not found.") =>
        new() { Status = ResultStatus.NotFound, Message = message };

    public static Result<T> Duplicate(string message = "Duplicate record.") =>
        new() { Status = ResultStatus.Duplicate, Message = message };

    public static Result<T> Unauthorized(string message = "Authentication failed.") =>
        new() { Status = ResultStatus.Unauthorized, Message = message };

    public static Result<T> Forbidden(string message = "Forbidden.") =>
        new() { Status = ResultStatus.Forbidden, Message = message };

    public static Result<T> Error(string message = "Unexpected error.") =>
        new() { Status = ResultStatus.Error, Message = message };
}
