using CleanWebApiTemplate.Domain.ResultModel;
using FluentValidation;

namespace CleanWebApiTemplate.Application.CQRS.Decorators;

/// <summary>
/// Shared validation logic used by the command and query validation decorators.
/// </summary>
internal static class ValidationRunner
{
    /// <summary>
    /// Runs every registered validator for the message.
    /// Returns a <see cref="BadRequestError"/> when at least one validation failure is found, otherwise null.
    /// </summary>
    internal static async Task<Error?> ValidateAsync<TMessage>(TMessage message,
                                                               IEnumerable<IValidator<TMessage>> validators,
                                                               CancellationToken cancellationToken)
    {
        var validatorArray = validators as IValidator<TMessage>[] ?? [.. validators];
        if (validatorArray.Length is 0)
            return null;

        var context = new ValidationContext<TMessage>(message);

        var validationResults = await Task.WhenAll(validatorArray.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(r => r.Errors)
                                        .Select(f => f.ErrorMessage)
                                        .Distinct()
                                        .ToList();

        return failures.Count is 0
            ? null
            : new BadRequestError("Validation Errors", string.Join('\n', failures));
    }
}
