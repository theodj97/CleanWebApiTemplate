using CleanWebApiTemplate.Application.Results;

namespace CleanWebApiTemplate.Application.Abstractions.Handlers;

/// <summary>Application use case that validates its input and returns a typed result.</summary>
/// <typeparam name="TInput">The input contract of the use case.</typeparam>
/// <typeparam name="TResult">The payload returned when the use case succeeds.</typeparam>
public interface IHandler<TInput, TResult>
{
    Task<Result<TResult>> HandleAsync(TInput input, CancellationToken cancellationToken);
}
