namespace CleanWebApiTemplate.Domain.ResultModel;

/// <summary>
/// Allows cross-cutting code (e.g. validation decorators) to create a failed result
/// in a strongly-typed, reflection-free and Native AOT compatible way.
/// </summary>
/// <typeparam name="TResult">The concrete result type to create.</typeparam>
public interface IResultFactory<TResult>
{
    public abstract static TResult Failure(Error error);
}
