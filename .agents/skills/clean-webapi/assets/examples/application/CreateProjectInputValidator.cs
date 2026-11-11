using FluentValidation;

namespace {App}.Application.Project.Handlers.Create;

public class CreateProjectInputValidator : ProjectValidator<CreateProjectInput>
{
    public CreateProjectInputValidator()
    {
        RuleFor(x => x.Name)
            .Custom(NotNullNotEmpty)
            .Custom((name, context) => ValidateName(name!, context));

        RuleFor(x => x.CreatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateOwnerEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription)
            .When(x => string.IsNullOrEmpty(x.Description) is false);
    }
}
