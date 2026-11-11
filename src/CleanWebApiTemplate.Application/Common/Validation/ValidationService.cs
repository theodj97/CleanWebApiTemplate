using CleanWebApiTemplate.Application.Common.Results;

namespace CleanWebApiTemplate.Application.Common.Validation;

/// <summary>
/// Base class for application handlers that must validate their input before running.
/// </summary>
public abstract class ValidationService
{
    /// <summary>
    /// Validates <paramref name="input"/> with every registered validator and, when validation
    /// succeeds, invokes <paramref name="action"/>. Otherwise the action is skipped and a
    /// failed result is returned.
    /// </summary>
    protected static async Task<TResult> ExecuteAsync<TInput, TResult>(TInput input,
                                                                       ValidatorSet<TInput> validators,
                                                                       Func<Task<TResult>> action,
                                                                       CancellationToken cancellationToken)
        where TResult : IResultFactory<TResult>
    {
        Error? error = await validators.ValidateAsync(input, cancellationToken);

        return error is not null
            ? TResult.Failure(error)
            : await action();
    }
}
