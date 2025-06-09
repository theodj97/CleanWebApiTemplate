using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public class GetTodoByIdQueryValidator : TodoValidator<GetTodoByIdQuery>
{
    public GetTodoByIdQueryValidator(IBaseQueryRepository<TodoEntity> repository) : base(repository)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
