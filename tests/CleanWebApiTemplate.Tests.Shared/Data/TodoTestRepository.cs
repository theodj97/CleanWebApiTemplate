using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Domain.Todo;
using CleanWebApiTemplate.Infrastructure.Todo;
using CleanWebApiTemplate.Tests.Shared.Fixtures;
using Npgsql;

namespace CleanWebApiTemplate.Tests.Shared.Data;

/// <summary>
/// Arrange: <see cref="AddAsync"/> inserts and returns the entity. Assert: query methods read current state.
/// Uses the application's own ADO.NET repository (there is no EF Core in this repository), bypassing the HTTP
/// endpoints on purpose.
/// </summary>
public sealed class TodoTestRepository : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ITodoRepository _repository;

    public TodoTestRepository(string connectionString)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
        _repository = new TodoRepository(_dataSource);
    }

    public async Task<TodoEntity> AddAsync(TodoEntity entity)
    {
        Result<TodoEntity> result = await _repository.InsertAsync(entity, TestCancellation.Token);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Description);

        return result.Value!;
    }

    /// <summary>Inserts without unwrapping the result, so tests can assert the duplicate-title failure.</summary>
    public Task<Result<TodoEntity>> TryAddAsync(TodoEntity entity) =>
        _repository.InsertAsync(entity, TestCancellation.Token);

    public Task<TodoEntity?> FindAsync(Ulid id) => _repository.GetByIdAsync(id, cancellationToken: TestCancellation.Token);

    public async Task<TodoEntity?> FindByTitleAsync(string title, string? owner = null)
    {
        IReadOnlyList<TodoEntity> matches = await _repository.SearchAsync(
            new TodoFilter
            {
                Titles = [title],
                Owners = owner is null ? null : [owner],
            },
            TestCancellation.Token);

        return matches.SingleOrDefault();
    }

    public async Task<int> CountAsync()
    {
        IReadOnlyList<TodoEntity> all = await _repository.SearchAsync(new TodoFilter(), TestCancellation.Token);
        return all.Count;
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
