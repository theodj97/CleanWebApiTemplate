using CleanWebApiTemplate.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanWebApiTemplate.Testing;

public static class TestServerFixtureExtension
{
    private static readonly object IdLock = new();
    private static byte[] lastIdBytes = new byte[16]; // Starts as Ulid.MinValue

    public static async Task<TodoEntity> AddDefaultTodo(this TestServerFixture testServerFixture,
                                                        string title = "defaultTitle",
                                                        string description = "defaultDescription",
                                                        DateTime? createdAt = null,
                                                        DateTime? updatedAt = null,
                                                        int status = 0,
                                                        string createdBy = "defaultCreatedBy",
                                                        string updatedBy = "defaultUpdatedBy")
    {
        TodoEntity todoEntity = new()
        {
            Id = NewMonotonicUlid(),
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

    public static async Task<TodoEntity?> GetTodo(this TestServerFixture testServerFixture, Ulid id)
    {
        TodoEntity? todoDb = null;
        await testServerFixture.ExecuteDbContextAsync(async context =>
        {
            todoDb = await context.Set<TodoEntity>().FirstOrDefaultAsync(x => x.Id == id);
        });
        return todoDb;
    }


    /// <summary>
    /// Generates ULIDs that are monotonically increasing by creation order.
    /// Ulid.NewUlid() alone is not sortable when several rows are created within
    /// the same millisecond (common against a fast local SQLite file), so the
    /// random part is incremented when the timestamp does not advance.
    /// </summary>
    private static Ulid NewMonotonicUlid()
    {
        lock (IdLock)
        {
            var candidate = Ulid.NewUlid();
            var last = new Ulid(lastIdBytes);
            if (candidate.CompareTo(last) <= 0)
                candidate = new Ulid(Increment(lastIdBytes));

            lastIdBytes = candidate.ToByteArray();
            return candidate;
        }
    }

    private static byte[] Increment(byte[] bytes)
    {
        var copy = (byte[])bytes.Clone();
        for (int i = copy.Length - 1; i >= 0 && ++copy[i] == 0; i--) { }
        return copy;
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
