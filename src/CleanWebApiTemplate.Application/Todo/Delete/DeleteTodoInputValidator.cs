using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Delete;

public class DeleteTodoInputValidator : TodoValidator<DeleteTodoInput>
{
    public DeleteTodoInputValidator(ITodoRepository todoRepository) : base(todoRepository)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
