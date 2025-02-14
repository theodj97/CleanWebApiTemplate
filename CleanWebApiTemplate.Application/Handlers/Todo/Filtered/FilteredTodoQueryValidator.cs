using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Filtered;

public class FilteredTodoQueryValidator : TodoValidator<FilteredTodoQuery>
{
    public FilteredTodoQueryValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;

        RuleFor(x => x.Title)
            .Must(titles => titles!.Contains(string.Empty) is false)
            .WithMessage("Title fields can't contain null or be empty.")
            .Must(titles => titles!.All(title => title.Length <= MaxTitleLength))
            .WithMessage($"Titles max lenght is {MaxTitleLength}.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Status)
            .Must(status => status!.Contains(0) is false)
            .WithMessage("Status field can't be null or 0.")
            .Must(status => status!.All(s => s >= 1 && s <= 2))
            .WithMessage("Status must be between 1 and 2.")
            .When(x => x.Status is not null);

        RuleFor(x => x.CreatedBy)
            .Must(createdBy => createdBy!.Contains(string.Empty) is false)
            .WithMessage("CreatedBy fields can't contain null or be empty.")
            .Must(createdBy => createdBy!.All(createdBy => createdBy.Length <= MaxCreatedByLength))
            .WithMessage($"CreatedBy max lenght is {MaxCreatedByLength}.")
            .When(x => x.CreatedBy is not null);
    }
}
