using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQueryValidator : TodoValidator<GetTodoTitleQuery>
{
    public GetTodoTitleQueryValidator(IBaseRepository<TodoEntity> repository) : base(repository)
    {
        RuleFor(x => x.PageNumber).Must(value => value is not null && value > 0)
            .When(x => x.PageSize is not null || x.PageNumber is not null)
            .WithMessage($"Property {nameof(GetTodoTitleQuery.PageNumber)} can't be null or 0 if property {nameof(GetTodoTitleQuery.PageSize)} is not null.");

        RuleFor(x => x.PageSize).Must(value => value is not null && value > 0)
            .When(x => x.PageNumber is not null || x.PageSize is not null)
            .WithMessage($"Property {nameof(GetTodoTitleQuery.PageSize)} must be greater than 0 if property {nameof(GetTodoTitleQuery.PageNumber)} is not null.");

        RuleFor(x => x.SortProperties).Custom((sortProperties, context) => ValidateSortBy(sortProperties,
                                                                                          typeof(TodoDto),
                                                                                          context)).When(x => x.SortProperties is not null);
    }
}
