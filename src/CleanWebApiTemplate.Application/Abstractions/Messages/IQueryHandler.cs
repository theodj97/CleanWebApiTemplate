namespace CleanWebApiTemplate.Application.Abstractions.Messages;

/// <summary>
/// Handles a <typeparamref name="TQuery"/> and produces a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TQuery">The query type to handle.</typeparam>
/// <typeparam name="TResult">The type returned by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
