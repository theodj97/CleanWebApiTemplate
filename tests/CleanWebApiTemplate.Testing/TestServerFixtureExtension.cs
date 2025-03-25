using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Testing;

public static class TestServerFixtureExtension
{
    public static async Task<TodoEntity> AddDefaultTodo(this TestServerFixture testServerFixture, string title = "defaultTitle", string description = "defaultDescription", DateTime? createdAt = null,
    DateTime? updatedAt = null, int status = 0, string createdBy = "defaultCreatedBy", string updatedBy = "defaultUpdatedBy")
    {
        TodoEntity todoEntity = new()
        {
            Title = title,
            Description = description,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt ?? createdAt ?? DateTime.UtcNow,
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


    /// <summary>
    /// Generates a random string of the specified length.
    /// </summary>
    /// <param name="length">The desired lenght of the random string.</param>
    /// <returns>A random string of the specified length.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GenerateRandomString(int length)
    {
        if (length <= 0) throw new ArgumentException("Length must be greater than zero", nameof(length));

        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random random = new();
        return new string([.. Enumerable.Repeat(characters, length).Select(s => s[random.Next(s.Length)])]);
    }
}
