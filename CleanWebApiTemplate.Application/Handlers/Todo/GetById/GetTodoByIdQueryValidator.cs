using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public class GetTodoByIdQueryValidator : TodoValidator<GetTodoByIdQuery>
{
    public GetTodoByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotNull().NotEmpty().WithMessage("Id is required")
            .Must(id => int.TryParse(id, out _)).WithMessage("Id must be a valid number")
            .Length(1, 6).WithMessage("Id length must be between 1 and 6 characters");
    }
}
