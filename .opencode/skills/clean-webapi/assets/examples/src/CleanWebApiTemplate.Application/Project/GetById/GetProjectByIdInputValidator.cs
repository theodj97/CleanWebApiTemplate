using FluentValidation;

namespace CleanWebApiTemplate.Application.Project.GetById;

public sealed class GetProjectByIdInputValidator : ProjectValidator<GetProjectByIdInput>
{
    public GetProjectByIdInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);
    }
}
