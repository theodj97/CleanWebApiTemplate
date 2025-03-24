using CleanWebApiTemplate.Application.Handlers.Todo.Create;
using CleanWebApiTemplate.Application.Handlers.Todo.Update;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

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
            Status = entity.Status,
            UpdatedBy = entity.UpdatedBy
        };
    }

    public static IEnumerable<TodoResponse> FromEntityToResponse(IEnumerable<TodoEntity> entities)
    {
        IEnumerable<TodoResponse> response = entities.Select(FromEntityToResponse);
        return response;
    }

    public static TodoEntity FromCommandToEntity(CreateTodoCommand command)
    {
        DateTime actualUtcMoment = DateTime.UtcNow;
        return new TodoEntity
        {
            Id = Ulid.NewUlid(),
            Title = command.Title,
            Description = command.Description,
            CreatedBy = command.CreatedBy,
            CreatedAt = actualUtcMoment,
            Status = 1,
            UpdatedAt = actualUtcMoment,
            UpdatedBy = command.CreatedBy
        };
    }

    public static (bool isUpdated, TodoEntity updatedEntity) FromCommandToEntity(UpdateTodoCommand command, TodoEntity entity)
    {
        bool isUpdated = false;
        if (command.Title is not null && entity.Title != command.Title)
        {
            entity.Title = command.Title;
            isUpdated = true;
        }
        if (command.Description is not null)
        {
            entity.Description = command.Description;
            isUpdated = true;
        }
        if (command.Status is not null)
        {
            entity.Status = (int)command.Status;
            isUpdated = true;
        }
        if (isUpdated)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = command.UpdatedBy;
        }
        return (isUpdated, entity);
    }
}
