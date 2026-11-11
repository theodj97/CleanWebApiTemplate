using FluentValidation;

namespace CleanWebApiTemplate.Application.Project.Delete;

public sealed class DeleteProjectInputValidator : ProjectValidator<DeleteProjectInput>
{
    public DeleteProjectInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
