using CleanWebApiTemplate.Infrastructure.Context;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public class CreateTodoCommandValidator : TodoValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator(MariaDbContext dbContext) : base(dbContext)
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
