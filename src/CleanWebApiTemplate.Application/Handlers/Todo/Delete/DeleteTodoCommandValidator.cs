using CleanWebApiTemplate.Infrastructure.Context;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Delete;

public class DeleteTodoCommandValidator : TodoValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator(MariaDbContext dbContext) : base(dbContext)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
