using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Delete;

public class DeleteTodoCommandValidator : TodoValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator(ITodoRepository todoRepository) : base(todoRepository)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
