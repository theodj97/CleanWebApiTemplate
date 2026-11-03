using CleanWebApiTemplate.Application.Todo.Sorting;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.GetTitles;

public class GetTodoTitlesInputValidator : TodoValidator<GetTodoTitlesInput>
{
    public GetTodoTitlesInputValidator()
    {
        RuleFor(x => x.PageNumber).Must(value => value is not null && value > 0)
            .When(x => x.PageSize is not null || x.PageNumber is not null)
            .WithMessage($"Property {nameof(GetTodoTitlesInput.PageNumber)} can't be null or 0 if property {nameof(GetTodoTitlesInput.PageSize)} is not null.");

        RuleFor(x => x.PageSize).Must(value => value is not null && value > 0)
            .When(x => x.PageNumber is not null || x.PageSize is not null)
            .WithMessage($"Property {nameof(GetTodoTitlesInput.PageSize)} must be greater than 0 if property {nameof(GetTodoTitlesInput.PageNumber)} is not null.");

        RuleFor(x => x.SortProperties).Custom((sortProperties, context) => ValidateSortBy(sortProperties,
                                                                                           TodoSortCatalog.KeySet,
                                                                                           context)).When(x => x.SortProperties is not null);
    }
}
