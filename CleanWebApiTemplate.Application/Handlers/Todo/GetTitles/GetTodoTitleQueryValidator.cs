using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQueryValidator : AbstractValidator<GetTodoTitleQuery>
{
    public GetTodoTitleQueryValidator()
    {
        RuleFor(x => x.PageNumber).Must(static value => value > 0).WithMessage($"Property {nameof(GetTodoTitleQuery.PageNumber)} must be greater than 0");
        RuleFor(x => x.PageSize).Must(static value => value > 0).WithMessage($"Property {nameof(GetTodoTitleQuery.PageSize)} must be greater than 0");
    }
}
