namespace CleanWebApiTemplate.Domain.ResultModel;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public T? Value { get; }
    public bool IsNoContent { get; } = false;
    public bool IsCreated { get; } = false;

    protected Result(T? value, bool isSuccess, Error? error, bool isCreated = false)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException();
        if (!isSuccess && error is null)
            throw new InvalidOperationException();

        if (value is null) IsNoContent = true;
        if (value is IEnumerable<T> listValue && !listValue.Any()) IsNoContent = true;

        // Response codes that are also valid, i.e., isCreated and isNoContent cannot be true at the same time.
        var countPossibleResponses = new[] { isCreated, IsNoContent }.Count(x => x);
        if (countPossibleResponses > 1)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
        Value = value;
        IsCreated = isCreated;
    }

    public static Result<T> Success(T? value) => new(value, true, null);
    public static Result<T> Failure(Error error) => new(default, false, error);
    public static Result<T> Created(T value) => new(value, true, null, isCreated: true);
}
