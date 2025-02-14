using CleanWebApiTemplate.Application.Handlers.Todo.Create;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;

namespace CleanWebApiTemplate.Application.Mappers.Todo;

public static class TodoMappers
{
    public static TodoResponse FromEntityToResponse(TodoEntity entity)
    {
        return new TodoResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status
        };
    }

    public static IEnumerable<TodoResponse> FromEntityToResponse(IEnumerable<TodoEntity> entities)
    {
        IEnumerable<TodoResponse> response = entities.Select(FromEntityToResponse);
        return response;
    }

    public static TodoEntity FromCommandToEntity(CreateTodoCommand command)
    {
        return new TodoEntity
        {
            Id = 0,
            Title = command.Title,
            Description = command.Description,
            CreatedBy = command.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            Status = 1,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
