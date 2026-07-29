namespace CleanWebApiTemplate.Domain.ResultModel;

/// <summary>
/// Allows cross-cutting code (e.g. validation decorators) to create a failed result
/// in a strongly-typed, reflection-free and Native AOT compatible way.
/// </summary>
/// <typeparam name="TResult">The concrete result type to create.</typeparam>
public interface IResultFactory<TResult>
{
    static abstract TResult Failure(Error error);
}
