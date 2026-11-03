using CleanWebApiTemplate.Application.Results;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Validation;

/// <summary>
/// Shared validation logic used by <see cref="ValidationService"/>.
/// </summary>
internal static class ValidationRunner
{
    /// <summary>
    /// Runs every registered validator for the input.
    /// Returns a <see cref="BadRequestError"/> when at least one validation failure is found, otherwise null.
    /// </summary>
    internal static async Task<Error?> ValidateAsync<TInput>(TInput input,
                                                             IEnumerable<IValidator<TInput>> validators,
                                                             CancellationToken cancellationToken)
    {
        var validatorArray = validators as IValidator<TInput>[] ?? [.. validators];
        if (validatorArray.Length is 0)
            return null;

        var context = new ValidationContext<TInput>(input);

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
