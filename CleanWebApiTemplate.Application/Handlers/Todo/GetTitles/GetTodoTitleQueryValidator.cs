using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQueryValidator : TodoValidator<GetTodoTitleQuery>
{
    public GetTodoTitleQueryValidator(IBaseRepository<TodoEntity> repository) : base(repository)
    {
        RuleFor(x => x.PageNumber).Must(value => value > 0)
            .WithMessage($"Property {nameof(GetTodoTitleQuery.PageNumber)} must be greater than 0")
            .When(x => x.PageNumber is not null);

        RuleFor(x => x.PageSize).Must(value => value > 0)
            .WithMessage($"Property {nameof(GetTodoTitleQuery.PageSize)} must be greater than 0")
            .When(x => x.PageSize is not null);

        RuleFor(x => x.OrderBy).Custom(ValidTodoOrderBy)
            .When(x => x.OrderBy is not null);
    }
}
