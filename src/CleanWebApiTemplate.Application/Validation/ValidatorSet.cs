using CleanWebApiTemplate.Application.Results;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Validation;

/// <summary>
/// The validators registered for one input type. The constructor refuses an empty set, so a
/// missing registration fails at composition time instead of silently skipping validation.
/// </summary>
public sealed class ValidatorSet<TInput>
{
    private readonly IValidator<TInput>[] validators;

    public ValidatorSet(IEnumerable<IValidator<TInput>> validators)
    {
        this.validators = validators as IValidator<TInput>[] ?? [.. validators];

        if (this.validators.Length is 0)
        {
            throw new InvalidOperationException(
                $"No validator is registered for {typeof(TInput).Name}. Register one before resolving the handler.");
        }
    }

    /// <summary>
    /// Runs every registered validator and returns a <see cref="BadRequestError"/> when at least
    /// one failure is found, otherwise null.
    /// </summary>
    public async Task<Error?> ValidateAsync(TInput input, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TInput>(input);

        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(result => result.Errors)
                                        .Select(failure => failure.ErrorMessage)
                                        .Distinct()
                                        .ToList();

        return failures.Count is 0
            ? null
            : new BadRequestError("Validation Errors", string.Join('\n', failures));
    }
}
