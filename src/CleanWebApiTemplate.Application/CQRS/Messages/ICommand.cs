namespace CleanWebApiTemplate.Application.CQRS.Messages;

/// <summary>
/// A command: an intention to mutate state, producing a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type returned by the command handler.</typeparam>
public interface ICommand<TResult> : IMessage;
