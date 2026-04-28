namespace AssistantEngineer.SharedKernel.Primitives;

public enum ResultErrorType
{
    None,
    Failure,
    Validation,
    NotFound,
    Conflict
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public ResultErrorType ErrorType { get; }

    protected Result(bool isSuccess, string error, ResultErrorType errorType)
    {
        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message is required for failure.", nameof(error));

        if (isSuccess && errorType != ResultErrorType.None)
            throw new ArgumentException("Successful result cannot have an error type.", nameof(errorType));

        if (!isSuccess && errorType == ResultErrorType.None)
            throw new ArgumentException("Failed result must have an error type.", nameof(errorType));

        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, string.Empty, ResultErrorType.None);

    public static Result Failure(string error, ResultErrorType errorType = ResultErrorType.Failure) =>
        new(false, error, errorType);

    public static Result Validation(string error) => Failure(error, ResultErrorType.Validation);
    public static Result NotFound(string error) => Failure(error, ResultErrorType.NotFound);
    public static Result Conflict(string error) => Failure(error, ResultErrorType.Conflict);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of failed result.");

    private Result(T? value, bool isSuccess, string error, ResultErrorType errorType)
        : base(isSuccess, error, errorType)
    {
        _value = value;
    }

    public static Result<T> Success(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Successful result value cannot be null.");

        return new(value, true, string.Empty, ResultErrorType.None);
    }

    public new static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.Failure) =>
        new(default, false, error, errorType);

    public static Result<T> Failure(Result result)
    {
        if (result.IsSuccess)
            throw new ArgumentException("Cannot create failure from a successful result.", nameof(result));

        return Failure(result.Error, result.ErrorType);
    }

    public new static Result<T> Validation(string error) => Failure(error, ResultErrorType.Validation);
    public new static Result<T> NotFound(string error) => Failure(error, ResultErrorType.NotFound);
    public new static Result<T> Conflict(string error) => Failure(error, ResultErrorType.Conflict);
}
