using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.GetById;

public class GetTodoByIdInputValidator : TodoValidator<GetTodoByIdInput>
{
    public GetTodoByIdInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
