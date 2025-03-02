using FluentValidation;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Domain.Helpers.Validators;

public class BaseAbstractValidator<T> : AbstractValidator<T> where T : class
{
    /// <summary>
    /// Validate ULID when its required.
    /// </summary>
    /// <param name="propertySelector">Property selector expression, you must select the ID query/command property</param>
    protected void ValidateRequiredUlid(Expression<Func<T, string>> propertySelector)
    {
        RuleFor(propertySelector)
            .NotNull().NotEmpty().WithMessage("ID is required")
            .Custom(ValidateUlidLogic);
    }

    /// <summary>
    /// Validate ULID when its optional.
    /// </summary>
    /// <param name="propertySelector">Property selector expression, you must select the ID query/command property</param>
    protected void ValidateOptionalUlid(Expression<Func<T, string>> propertySelector)
    {
        RuleFor(propertySelector)
            .Custom(ValidateUlidLogic)
            .When(x => string.IsNullOrEmpty(propertySelector.Compile()(x)) is false);
    }

    /// <summary>
    /// Validate ULID optional collection.
    /// </summary>
    /// <param name="propertySelector"></param>
    protected void ValidateUlidCollection(Expression<Func<T, IEnumerable<string>?>> propertySelector)
    {
        RuleForEach(propertySelector)
            .NotNull().WithMessage("ID cannot be null")
            .NotEmpty().WithMessage("ID cannot be empty")
            .Custom(ValidateUlidLogic)
            .When(x => propertySelector.Compile()(x) is not null
            && propertySelector.Compile()(x)!.Any());
    }


    /// <summary>
    /// Common ULID validation logic.
    /// </summary>
    /// <param name="id">The ID value to validate</param>
    /// <param name="context">Validation context</param>
    private static void ValidateUlidLogic(string id, ValidationContext<T> context)
    {
        if (Ulid.TryParse(id, out _) is false)
            context.AddFailure("ID must be a valid ULID");

        if (id.Length is not 26)
            context.AddFailure("ID must have exactly 26 characters");
    }
}
