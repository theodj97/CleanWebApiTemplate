using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using CleanWebApiTemplate.Tests.Shared.Fixtures;

namespace CleanWebApiTemplate.Tests.Shared.Data;

/// <summary>
/// Arrange: <see cref="AddAsync"/> inserts and returns the entity. Assert: query methods read current state.
/// Uses the application's own ADO.NET repository (there is no EF Core in this repository), bypassing the HTTP
/// endpoints on purpose.
/// </summary>
public sealed class TodoTestRepository(string connectionString)
{
    private readonly ITodoRepository _repository = new TodoRepository(new NpgsqlConnectionFactory(connectionString));

    public async Task<TodoEntity> AddAsync(TodoEntity entity)
    {
        await _repository.InsertAsync(entity, TestCancellation.Token);
        return entity;
    }

    public Task<TodoEntity?> FindAsync(Ulid id) => _repository.GetByIdAsync(id, TestCancellation.Token);

    public async Task<TodoEntity?> FindByTitleAsync(string title)
    {
        var matches = await _repository.SearchAsync(new TodoFilter { Titles = [title] }, TestCancellation.Token);
        return matches.SingleOrDefault();
    }

    public async Task<int> CountAsync()
    {
        var all = await _repository.SearchAsync(new TodoFilter(), TestCancellation.Token);
        return all.Count;
    }
}
