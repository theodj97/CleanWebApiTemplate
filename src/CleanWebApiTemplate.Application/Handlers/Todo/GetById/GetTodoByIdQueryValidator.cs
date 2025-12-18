using CleanWebApiTemplate.Infrastructure.Context;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public class GetTodoByIdQueryValidator : TodoValidator<GetTodoByIdQuery>
{
    public GetTodoByIdQueryValidator(MariaDbContext dbContext) : base(dbContext)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
