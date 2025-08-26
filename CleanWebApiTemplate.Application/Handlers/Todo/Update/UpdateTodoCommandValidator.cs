using CleanWebApiTemplate.Infrastructure.Context;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Update;

public class UpdateTodoCommandValidator : TodoValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator(SqlDbContext dbContext) : base(dbContext)
    {
        RuleFor(x => x.Id)
            .Custom(ValidateUlid);

        RuleFor(x => new { x.Id, x.Title })
            .CustomAsync(ValidateTitle!)
            .When(x => string.IsNullOrEmpty(x.Title) is false && Ulid.TryParse(x.Id, out _) is true);

        RuleFor(x => x.UpdatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUserEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription!)
            .When(x => string.IsNullOrEmpty(x.Description) is false);

        RuleFor(x => (int)x.Status!)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null && x.Status != 0);
    }
}
