using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Update;

public class UpdateTodoInputValidator : TodoValidator<UpdateTodoInput>
{
    public UpdateTodoInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(ValidateUlid);

        RuleFor(x => x.Title)
            .Custom((title, context) => ValidateTitleLength(title!, context))
            .When(x => x.Title is not null);

        RuleFor(x => x.UpdatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUserEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription!)
            .When(x => string.IsNullOrEmpty(x.Description) is false);

        RuleFor(x => (int)x.Status!)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null);
    }
}
