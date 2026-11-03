using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Create;

public class CreateTodoInputValidator : TodoValidator<CreateTodoInput>
{
    public CreateTodoInputValidator()
    {
        RuleFor(x => x.Title)
            .Custom(NotNullNotEmpty)
            .Custom((title, context) => ValidateTitleLength(title!, context));

        RuleFor(x => x.CreatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUserEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription)
            .When(x => string.IsNullOrEmpty(x.Description) is false);
    }
}
