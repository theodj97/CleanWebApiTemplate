using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Delete;

public class DeleteTodoCommandValidator : TodoValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator(IBaseQueryRepository<TodoEntity> repository) : base(repository)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
