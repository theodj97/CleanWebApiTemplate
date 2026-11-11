namespace CleanWebApiTemplate.Application.Common.Results;

/// <summary>
/// The outcome of a use case: a value, or a typed <see cref="Error"/>. The type refuses to exist in an
/// inconsistent state, so a handler cannot return success with an error or failure without one.
/// </summary>
public sealed class Result<T> : IResultFactory<Result<T>>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public T? Value { get; }

    private Result(T? value, bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
            throw new InvalidOperationException("A result is either a value or an error, never both and never neither.");

        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public static Result<T> Success(T? value) => new(value, true, null);

    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new(default, false, error);
    }
}
