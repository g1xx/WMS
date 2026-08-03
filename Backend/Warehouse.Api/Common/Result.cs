namespace Warehouse.Api.Common;

public enum ResultErrorType
{
    NotFound,
    Conflict,
    BadRequest
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null, default);

    public static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.BadRequest) =>
        new(false, default, error, errorType);
}
