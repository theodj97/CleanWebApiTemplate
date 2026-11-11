using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project;

/// <summary>
/// The persistence port of the Project feature. Every read that can be scoped to one owner takes
/// <c>visibleOwner</c>: null means "every owner" (admins), a value restricts the query to that owner.
/// </summary>
public interface IProjectRepository
{
    /// <summary>Gets a Project by id, restricted to <paramref name="visibleOwner"/> when it is not null.</summary>
    Task<ProjectEntity?> GetByIdAsync(Ulid id, string? visibleOwner = null, CancellationToken cancellationToken = default);

    /// <summary>Reads only the id and name columns, with paging and sorting, restricted to <paramref name="visibleOwner"/> when it is not null.</summary>
    Task<IReadOnlyList<ProjectName>> GetNamesAsync(int? pageNumber,
                                                  int? pageSize,
                                                  IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                  string? visibleOwner = null,
                                                  CancellationToken cancellationToken = default);

    /// <summary>Reads the rows that match <paramref name="filter"/>.</summary>
    Task<IReadOnlyList<ProjectEntity>> SearchAsync(ProjectFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new Project.
    /// </summary>
    /// <returns>
    /// The stored entity, or a failure with <see cref="ConflictError"/> when the owner already has
    /// a Project with the same name (the unique index rejected a concurrent writer).
    /// </returns>
    Task<Result<ProjectEntity>> InsertAsync(ProjectEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Project.
    /// </summary>
    /// <returns>
    /// Success with true when a row was updated and false when the id doesn't exist, or a failure
    /// with <see cref="ConflictError"/> when the new name is already used by another Project of the
    /// same owner (the unique index rejected a concurrent writer).
    /// </returns>
    Task<Result<bool>> UpdateAsync(ProjectEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Project by id, restricted to <paramref name="visibleOwner"/> when it is not null.
    /// </summary>
    /// <returns>True when a row was deleted, false when the id doesn't exist or is not visible.</returns>
    Task<bool> DeleteAsync(Ulid id, string? visibleOwner = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether one owner already has a Project with this name, optionally excluding one id.
    /// Names are unique per owner, not globally.
    /// </summary>
    Task<bool> NameExistsAsync(string name,
                                string owner,
                                Ulid? excludeId = null,
                                CancellationToken cancellationToken = default);
}
