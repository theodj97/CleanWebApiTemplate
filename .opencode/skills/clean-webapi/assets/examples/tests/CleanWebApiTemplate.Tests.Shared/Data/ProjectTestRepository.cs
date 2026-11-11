using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Domain.Project;
using CleanWebApiTemplate.Infrastructure.Project;
using CleanWebApiTemplate.Tests.Shared.Fixtures;
using Npgsql;

namespace CleanWebApiTemplate.Tests.Shared.Data;

/// <summary>
/// Arrange: <see cref="AddAsync"/> inserts and returns the entity. Assert: query methods read current state.
/// Uses the application's own ADO.NET repository (there is no EF Core in this repository), bypassing the HTTP
/// endpoints on purpose.
/// </summary>
public sealed class ProjectTestRepository : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IProjectRepository _repository;

    public ProjectTestRepository(string connectionString)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
        _repository = new ProjectRepository(_dataSource);
    }

    public async Task<ProjectEntity> AddAsync(ProjectEntity entity)
    {
        Result<ProjectEntity> result = await _repository.InsertAsync(entity, TestCancellation.Token);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Description);

        return result.Value!;
    }

    /// <summary>Inserts without unwrapping the result, so tests can assert the duplicate-name failure.</summary>
    public Task<Result<ProjectEntity>> TryAddAsync(ProjectEntity entity) =>
        _repository.InsertAsync(entity, TestCancellation.Token);

    public Task<ProjectEntity?> FindAsync(Ulid id) => _repository.GetByIdAsync(id, cancellationToken: TestCancellation.Token);

    public async Task<ProjectEntity?> FindByNameAsync(string name, string? owner = null)
    {
        IReadOnlyList<ProjectEntity> matches = await _repository.SearchAsync(
            new ProjectFilter
            {
                Names = [name],
                Owners = owner is null ? null : [owner],
            },
            TestCancellation.Token);

        return matches.SingleOrDefault();
    }

    public async Task<int> CountAsync()
    {
        IReadOnlyList<ProjectEntity> all = await _repository.SearchAsync(new ProjectFilter(), TestCancellation.Token);
        return all.Count;
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
