namespace CleanWebApiTemplate.Application.Abstractions.Messages;

/// <summary>
/// A query: a read-only request that produces a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type returned by the query handler.</typeparam>
public interface IQuery<TResult> : IMessage;
