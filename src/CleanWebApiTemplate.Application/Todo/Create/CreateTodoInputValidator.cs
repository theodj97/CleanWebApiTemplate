using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Create;

public sealed class CreateTodoInputValidator : TodoValidator<CreateTodoInput>
{
    public CreateTodoInputValidator()
    {
        RuleFor(x => x.Title)
            .Custom(NotNullNotEmpty)
            .Custom((title, context) => ValidateTitleLength(title!, context));

        RuleFor(x => x.Owner)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateOwnerEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription)
            .When(x => string.IsNullOrEmpty(x.Description) is false);
    }
}
