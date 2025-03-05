using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
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
            .CustomAsync(ValidateTitle)
            .When(x => x.Title is not null && x.Title.Any());

        RuleForEach(x => x.Status)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null);

        RuleForEach(x => x.CreatedBy)
            .Custom(ValidateUserEmail)
            .When(x => x.CreatedBy is not null);
    }
}
