namespace Dashboard.Core.Common;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string what) => new($"{what}.NotFound", $"{what} was not found.");
    public static Error Validation(string message) => new("Validation", message);
    public static Error Conflict(string message) => new("Conflict", message);
    public static Error Unauthorized(string message = "Unauthorized") => new("Unauthorized", message);
}

public readonly record struct Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }

    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new ArgumentException("Success result cannot have an error.", nameof(error));
        if (!isSuccess && error == Error.None)
            throw new ArgumentException("Failure result requires an error.", nameof(error));
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

public readonly record struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error Error { get; }

    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
