using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Project;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Project.Filter;

public sealed class FilteredProjectInputValidator : ProjectValidator<FilteredProjectInput>
{
    public FilteredProjectInputValidator()
    {
        RuleForEach(x => x.Ids)
            .Custom(ValidateUlid)
            .When(x => x.Ids is not null && x.Ids.Any());

        RuleForEach(x => x.Names)
            .Must(name => name is not null
                           && string.IsNullOrWhiteSpace(name) is false
                           && name.Length <= ProjectConstraints.NameMaxLength)
            .WithMessage($"Name must contain text and be at most {ProjectConstraints.NameMaxLength} characters")
            .When(x => x.Names is not null && x.Names.Any());

        RuleForEach(x => x.Statuses)
            .Custom(ValidateStatus)
            .When(x => x.Statuses is not null);

        RuleForEach(x => x.Owners)
            .Custom(ValidateOwnerEmail)
            .When(x => x.Owners is not null);

        RuleFor(x => x.StartDate)
            .Custom(ValidateDateTime!)
            .When(x => x.StartDate is not null);

        RuleFor(x => x.EndDate)
            .Custom(ValidateDateTime!)
            .When(x => x.EndDate is not null);

        RuleFor(x => new DateRange(x.StartDate, x.EndDate))
            .Custom(ValidateStartDateAndEndDate);

        RuleFor(x => x.PageNumber).Must(value => value is not null && value > 0)
            .When(x => x.PageSize is not null || x.PageNumber is not null)
            .WithMessage($"Property {nameof(FilteredProjectInput.PageNumber)} can't be null or 0 if property {nameof(FilteredProjectInput.PageSize)} is not null.");

        RuleFor(x => x.PageSize).Must(value => value is not null && value > 0)
            .When(x => x.PageNumber is not null || x.PageSize is not null)
            .WithMessage($"Property {nameof(FilteredProjectInput.PageSize)} must be greater than 0 if property {nameof(FilteredProjectInput.PageNumber)} is not null.");

        RuleFor(x => x.SortProperties)
            .Custom((sortProperties, context) => ValidateSortBy(sortProperties, ProjectSortCatalog.KeySet, context))
            .When(x => x.SortProperties is not null);
    }
}
