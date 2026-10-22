using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.GetById;

public class GetTodoByIdInputValidator : TodoValidator<GetTodoByIdInput>
{
    public GetTodoByIdInputValidator(ITodoRepository todoRepository) : base(todoRepository)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
