using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories;
using CleanWebApiTemplate.Infrastructure.Repositories.Helpers.Todo;
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
        var result = await _repository.InsertAsync(entity, TestCancellation.Token);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Description);

        return result.Value!;
    }

    /// <summary>Inserts without unwrapping the result, so tests can assert the duplicate-title failure.</summary>
    public Task<Result<TodoEntity>> TryAddAsync(TodoEntity entity) =>
        _repository.InsertAsync(entity, TestCancellation.Token);

    public Task<TodoEntity?> FindAsync(Ulid id) => _repository.GetByIdAsync(id, cancellationToken: TestCancellation.Token);

    public async Task<TodoEntity?> FindByTitleAsync(string title, string? createdBy = null)
    {
        var matches = await _repository.SearchAsync(
            new TodoFilter
            {
                Titles = [title],
                CreatedBys = createdBy is null ? null : [createdBy],
            },
            TestCancellation.Token);
        return matches.SingleOrDefault();
    }

    public async Task<int> CountAsync()
    {
        var all = await _repository.SearchAsync(new TodoFilter(), TestCancellation.Token);
        return all.Count;
    }
}
