using System.Text.Json.Serialization;
using ErrorTypeEnum = Non.Domain.Enums.ErrorType;

namespace Non.Application.Common;

public class Result
{
    protected Result(bool isSuccess, string? error, ErrorTypeEnum? errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public bool IsSuccess { get; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorTypeEnum? ErrorType { get; }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string error, ErrorTypeEnum errorType = ErrorTypeEnum.Validation) =>
        new(false, error, errorType);
}

public class Result<T> : Result
{
    private Result(T? value, bool isSuccess, string? error, ErrorTypeEnum? errorType)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, null, null);

    public new static Result<T> Failure(string error, ErrorTypeEnum errorType = ErrorTypeEnum.Validation) =>
        new(default, false, error, errorType);
}