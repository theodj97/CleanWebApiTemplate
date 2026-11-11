using FluentValidation;

namespace CleanWebApiTemplate.Application.Project.GetNames;

public sealed class GetProjectNamesInputValidator : ProjectValidator<GetProjectNamesInput>
{
    public GetProjectNamesInputValidator()
    {
        RuleFor(x => x.PageNumber).Must(value => value is not null && value > 0)
            .When(x => x.PageSize is not null || x.PageNumber is not null)
            .WithMessage($"Property {nameof(GetProjectNamesInput.PageNumber)} can't be null or 0 if property {nameof(GetProjectNamesInput.PageSize)} is not null.");

        RuleFor(x => x.PageSize).Must(value => value is not null && value > 0)
            .When(x => x.PageNumber is not null || x.PageSize is not null)
            .WithMessage($"Property {nameof(GetProjectNamesInput.PageSize)} must be greater than 0 if property {nameof(GetProjectNamesInput.PageNumber)} is not null.");

        RuleFor(x => x.SortProperties)
            .Custom((sortProperties, context) => ValidateSortBy(sortProperties, ProjectSortCatalog.KeySet, context))
            .When(x => x.SortProperties is not null);
    }
}
