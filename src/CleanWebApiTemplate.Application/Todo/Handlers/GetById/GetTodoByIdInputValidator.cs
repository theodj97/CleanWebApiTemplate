using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.GetById;

public class GetTodoByIdInputValidator : TodoValidator<GetTodoByIdInput>
{
    public GetTodoByIdInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
