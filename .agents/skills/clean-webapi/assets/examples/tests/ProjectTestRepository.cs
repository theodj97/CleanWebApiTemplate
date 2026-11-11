using {App}.Application.Abstractions.Repositories;
using {App}.Application.Project.Handlers.Queries;
using {App}.Application.Results;
using {App}.Domain.Models.Entities;
using {App}.Infrastructure.Data;
using {App}.Infrastructure.Repositories.Project;
using {App}.Tests.Shared.Fixtures;

namespace {App}.Tests.Shared.Data;

/// <summary>
/// Arrange: <see cref="AddAsync"/> inserts and returns the entity. Assert: query methods read current
/// state. Uses the application's own ADO.NET repository (there is no EF Core in this repository),
/// bypassing the HTTP endpoints on purpose.
/// </summary>
public sealed class ProjectTestRepository(string connectionString)
{
    private readonly IProjectRepository _repository = new ProjectRepository(new NpgsqlConnectionFactory(connectionString));

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

    public Task<ProjectEntity?> FindAsync(Ulid id) =>
        _repository.GetByIdAsync(id, cancellationToken: TestCancellation.Token);

    public async Task<ProjectEntity?> FindByNameAsync(string name, string? createdBy = null)
    {
        IReadOnlyList<ProjectEntity> matches = await _repository.SearchAsync(
            new ProjectFilter
            {
                Names = [name],
                CreatedBys = createdBy is null ? null : [createdBy],
            },
            TestCancellation.Token);

        return matches.SingleOrDefault();
    }

    public async Task<int> CountAsync()
    {
        IReadOnlyList<ProjectEntity> all = await _repository.SearchAsync(new ProjectFilter(), TestCancellation.Token);
        return all.Count;
    }
}
