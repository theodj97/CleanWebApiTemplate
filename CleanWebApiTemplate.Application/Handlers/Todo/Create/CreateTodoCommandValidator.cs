using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public class CreateTodoCommandValidator : TodoValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator(IBaseQueryRepository<TodoEntity> repository) : base(repository)
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
