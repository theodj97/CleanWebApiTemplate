using CleanWebApiTemplate.Application.Results;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Validation;

/// <summary>
/// Base class for application services that must validate their input before running.
/// Reproduces the behaviour of the former validation decorators without commands or queries.
/// </summary>
public abstract class ValidationService
{
    /// <summary>
    /// Validates <paramref name="input"/> with every registered validator and, when validation
    /// succeeds, invokes <paramref name="action"/>. Otherwise the action is skipped and a
    /// failed result is returned.
    /// </summary>
    protected static async Task<TResult> ExecuteAsync<TInput, TResult>(TInput input,
                                                                       IEnumerable<IValidator<TInput>> validators,
                                                                       Func<Task<TResult>> action,
                                                                       CancellationToken cancellationToken)
        where TResult : IResultFactory<TResult>
    {
        var error = await ValidationRunner.ValidateAsync(input, validators, cancellationToken);

        return error is not null
            ? TResult.Failure(error)
            : await action();
    }
}
