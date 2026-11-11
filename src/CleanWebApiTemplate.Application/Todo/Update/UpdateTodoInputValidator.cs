using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Update;

public sealed class UpdateTodoInputValidator : TodoValidator<UpdateTodoInput>
{
    public UpdateTodoInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);

        RuleFor(x => x.Title)
            .Custom((title, context) => ValidateTitleLength(title!, context))
            .When(x => x.Title is not null);

        RuleFor(x => x.LastWriter)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateOwnerEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription!)
            .When(x => string.IsNullOrEmpty(x.Description) is false);

        RuleFor(x => x.Status!.Value)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null);
    }
}
