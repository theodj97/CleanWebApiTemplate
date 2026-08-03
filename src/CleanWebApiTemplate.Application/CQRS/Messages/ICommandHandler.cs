namespace CleanWebApiTemplate.Application.CQRS.Messages;

/// <summary>
/// Handles a <typeparamref name="TCommand"/> and produces a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
/// <typeparam name="TResult">The type returned by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
