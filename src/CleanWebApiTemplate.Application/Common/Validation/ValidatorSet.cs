using CleanWebApiTemplate.Application.Common.Results;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Common.Validation;

/// <summary>
/// The validators registered for one input type. The constructor refuses an empty set, and the composition
/// root resolves every set at startup, so a missing validator registration stops the application instead of
/// silently skipping validation on the first request.
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
    /// one failure is found, otherwise null. Duplicate messages are reported once.
    /// </summary>
    public async Task<Error?> ValidateAsync(TInput input, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TInput>(input);

        FluentValidation.Results.ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        List<string> failures = validationResults.SelectMany(result => result.Errors)
                                                 .Select(failure => failure.ErrorMessage)
                                                 .Distinct(StringComparer.Ordinal)
                                                 .ToList();

        return failures.Count is 0
            ? null
            : new BadRequestError("Validation Errors", string.Join('\n', failures));
    }
}
