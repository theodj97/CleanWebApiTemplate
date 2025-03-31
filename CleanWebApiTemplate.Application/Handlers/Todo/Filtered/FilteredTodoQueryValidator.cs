using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Filtered;

public class FilteredTodoQueryValidator : TodoValidator<FilteredTodoQuery>
{
    public FilteredTodoQueryValidator(IBaseRepository<TodoEntity> repository) : base(repository)
    {
        RuleForEach(x => x.Ids)
            .Custom(ValidateUlid)
            .When(x => x.Ids is not null && x.Ids.Any());

        RuleForEach(x => x.Title)
            .Must(title => title.Length < TodoEntityConfiguration.TitleLenght)
            .WithMessage($"Title must be less than {TodoEntityConfiguration.TitleLenght} characters")
            .When(x => x.Title is not null && x.Title.Any());

        RuleForEach(x => x.Status)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null);

        RuleForEach(x => x.CreatedBy)
            .Custom(ValidateUserEmail)
            .When(x => x.CreatedBy is not null);

        RuleFor(x => x.StartDate)
            .Custom(ValidateDateTime!)
            .When(x => x.StartDate is not null);

        RuleFor(x => x.EndDate)
            .Custom(ValidateDateTime!)
            .When(x => x.EndDate is not null);

        RuleFor(x => new StartDateEndDateType(x.StartDate, x.EndDate))
            .Custom(ValidateStartDateAndEndDate);

        RuleFor(x => x.PageNumber).Must(value => value > 0)
            .WithMessage($"Property {nameof(FilteredTodoQuery.PageNumber)} must be greater than 0")
            .When(x => x.PageNumber is not null);

        RuleFor(x => x.PageSize).Must(value => value > 0)
            .WithMessage($"Property {nameof(FilteredTodoQuery.PageSize)} must be greater than 0")
            .When(x => x.PageSize is not null);

        RuleFor(x => x.OrderBy).Custom(ValidTodoOrderBy)
            .When(x => x.OrderBy is not null);
    }
}
