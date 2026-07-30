using CleanWebApiTemplate.Application.CQRS.Messages;
using CleanWebApiTemplate.Domain.ResultModel;
using FluentValidation;

namespace CleanWebApiTemplate.Application.CQRS.Decorators;

/// <summary>
/// Decorator over an <see cref="ICommandHandler{TCommand, TResult}"/> that runs all
/// registered FluentValidation validators for the command before invoking the real handler.
/// When validation fails, the inner handler is short-circuited and a failed result is returned.
/// </summary>
public sealed class ValidationCommandHandler<TCommand, TResult>(ICommandHandler<TCommand, TResult> inner,
                                                                IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : IResultFactory<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> inner = inner;
    private readonly IEnumerable<IValidator<TCommand>> validators = validators;

    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var error = await ValidationRunner.ValidateAsync(command, validators, cancellationToken);

        return error is not null
            ? TResult.Failure(error)
            : await inner.Handle(command, cancellationToken);
    }
}
