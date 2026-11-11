using {App}.Application.Project.Handlers.Queries;
using {App}.Application.Results;
using {App}.Domain.Models.Entities;

namespace {App}.Application.Abstractions.Repositories;

public interface IProjectRepository
{
    /// <summary>
    /// Gets a project by id. When <paramref name="createdBy"/> is not null the query is restricted to
    /// that owner, so non-administrators never read another user's project.
    /// </summary>
    public Task<ProjectEntity?> GetByIdAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ProjectEntity>> SearchAsync(ProjectFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new project.
    /// </summary>
    /// <returns>
    /// The stored entity, or a failure with <see cref="ConflictError"/> when the owner already has a
    /// project with the same name (a concurrent writer passed the application pre-check).
    /// </returns>
    public Task<Result<ProjectEntity>> InsertAsync(ProjectEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <returns>
    /// Success with true when a row was updated and false when the id doesn't exist, or a failure with
    /// <see cref="ConflictError"/> when the new name is already used by another project of the same owner.
    /// </returns>
    public Task<Result<bool>> UpdateAsync(ProjectEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes a project by id. When <paramref name="createdBy"/> is not null the delete is owner-scoped.</summary>
    public Task<bool> DeleteAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>True when the owner already has a project with that name (optionally excluding one id).</summary>
    public Task<bool> NameExistsAsync(string name,
                                      string createdBy,
                                      Ulid? excludeId = null,
                                      CancellationToken cancellationToken = default);
}
