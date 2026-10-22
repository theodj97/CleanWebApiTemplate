using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Create;

public class CreateTodoInputValidator : TodoValidator<CreateTodoInput>
{
    public CreateTodoInputValidator(ITodoRepository todoRepository) : base(todoRepository)
    {
        RuleFor(x => x.Title)
            .Custom(NotNullNotEmpty)
            .CustomAsync(ValidateTitle);

        RuleFor(x => x.CreatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUserEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription)
            .When(x => string.IsNullOrEmpty(x.Description) is false);
    }
}
