namespace CleanWebApiTemplate.Application.Results;

public class Result<T> : IResultFactory<Result<T>>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException();
        if (!isSuccess && error is null)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public static Result<T> Success(T? value) => new(value, true, null);
    public static Result<T> Failure(Error error) => new(default, false, error);
}
