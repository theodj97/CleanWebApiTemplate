using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Testing;

public static class TestServerFixtureExtension
{
    public static async Task<TodoEntity> AddDefaultTodo(this TestServerFixture testServerFixture, string title = "defaultTitle", string description = "defaultDescription", DateTime? createdAt = null,
    DateTime? updatedAt = null, byte status = 0, string createdBy = "defaultCreatedBy", string updatedBy = "defaultUpdatedBy")
    {
        TodoEntity todoEntity = new()
        {
            Title = title,
            Description = description,
            CreatedAt = createdAt ?? DateTime.Now,
            UpdatedAt = updatedAt ?? DateTime.Now,
            Status = status,
            CreatedBy = createdBy,
            UpdatedBy = updatedBy
        };

        await testServerFixture.ExecuteDbContextAsync(async context =>
        {
            await context.TodoDb.AddAsync(todoEntity);
            await context.SaveChangesAsync();
        });

        return todoEntity;
    }
}
