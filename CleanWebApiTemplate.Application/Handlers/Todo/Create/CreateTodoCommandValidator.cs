using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public class CreateTodoCommandValidator : TodoValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotNull().NotEmpty().WithMessage("Title is required")
            .Must(title => title!.Length <= MaxTitleLength)
            .WithMessage($"Title max length is {MaxTitleLength}");

        RuleFor(x => x.CreatedBy)
            .NotNull().NotEmpty().WithMessage("CreatedBy is required")
            .Must(createdBy => createdBy!.Length <= MaxCreatedByLength)
            .WithMessage($"CreatedBy max length is {MaxCreatedByLength}");

        RuleFor(x => x.Description)
            .Must(description => description!.Length <= MaxDescriptionLength)
            .WithMessage($"Description max length is {MaxDescriptionLength}")
            .When(x => x.Description is not null);
    }
}
