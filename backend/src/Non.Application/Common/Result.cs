namespace Non.Application.Common;

public class Result
{
    protected Result(bool isSuccess, string? error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    public static Result Success() => new(true, null, ErrorType.None);

    public static Result Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, error, errorType);
}

public class Result<T> : Result
{
    private Result(T? value, bool isSuccess, string? error, ErrorType errorType)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, null, ErrorType.None);

    public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(default, false, error, errorType);
}