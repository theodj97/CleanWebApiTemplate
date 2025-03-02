namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public class GetTodoByIdQueryValidator : TodoValidator<GetTodoByIdQuery>
{
    public GetTodoByIdQueryValidator()
    {
        ValidateRequiredUlid(x => x.Id);
    }
}
