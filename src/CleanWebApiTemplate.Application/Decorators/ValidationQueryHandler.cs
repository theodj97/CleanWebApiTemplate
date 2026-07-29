using CleanWebApiTemplate.Application.Abstractions.Messages;
using CleanWebApiTemplate.Domain.ResultModel;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Decorators;

/// <summary>
/// Decorator over an <see cref="IQueryHandler{TQuery, TResult}"/> that runs all
/// registered FluentValidation validators for the query before invoking the real handler.
/// When validation fails, the inner handler is short-circuited and a failed result is returned.
/// </summary>
public sealed class ValidationQueryHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> inner,
                                                            IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : IResultFactory<TResult>
{
    private readonly IQueryHandler<TQuery, TResult> inner = inner;
    private readonly IEnumerable<IValidator<TQuery>> validators = validators;

    public async Task<TResult> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var error = await ValidationRunner.ValidateAsync(query, validators, cancellationToken);

        return error is not null
            ? TResult.Failure(error)
            : await inner.Handle(query, cancellationToken);
    }
}
