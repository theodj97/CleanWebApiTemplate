namespace CleanWebApiTemplate.Application.Common.Results;

/// <summary>
/// Lets cross-cutting code create a failed result in a strongly-typed, reflection-free and Native AOT
/// compatible way: the concrete result type supplies <see cref="Failure"/> as a static abstract member.
/// </summary>
/// <typeparam name="TResult">The concrete result type to create.</typeparam>
public interface IResultFactory<TResult>
{
    static abstract TResult Failure(Error error);
}
