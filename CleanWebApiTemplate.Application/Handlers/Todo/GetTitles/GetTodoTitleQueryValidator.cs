using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQueryValidator : TodoValidator<GetTodoTitleQuery>
{
    public GetTodoTitleQueryValidator(IBaseRepository<TodoEntity> repository) : base(repository)
    {
        RuleFor(x => x.PageNumber).Must(static value => value > 0)
            .WithMessage($"Property {nameof(GetTodoTitleQuery.PageNumber)} must be greater than 0");

        RuleFor(x => x.PageSize).Must(static value => value > 0)
            .WithMessage($"Property {nameof(GetTodoTitleQuery.PageSize)} must be greater than 0");

        RuleFor(x => x.OrderBy).Custom(ValidateTitleOrderProperty)
            .When(x => x.OrderBy is not null);
    }
}
