using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Delete;

public class DeleteTodoInputValidator : TodoValidator<DeleteTodoInput>
{
    public DeleteTodoInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
