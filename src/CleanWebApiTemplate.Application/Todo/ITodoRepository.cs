using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo;

/// <summary>
/// The persistence port of the Todo feature. Every read that can be scoped to one owner takes
/// <c>visibleOwner</c>: null means "every owner" (admins), a value restricts the query to that owner.
/// </summary>
public interface ITodoRepository
{
    /// <summary>Gets a Todo by id, restricted to <paramref name="visibleOwner"/> when it is not null.</summary>
    Task<TodoEntity?> GetByIdAsync(Ulid id, string? visibleOwner = null, CancellationToken cancellationToken = default);

    /// <summary>Reads only the id and title columns, with paging and sorting, restricted to <paramref name="visibleOwner"/> when it is not null.</summary>
    Task<IReadOnlyList<TodoTitle>> GetTitlesAsync(int? pageNumber,
                                                  int? pageSize,
                                                  IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                  string? visibleOwner = null,
                                                  CancellationToken cancellationToken = default);

    /// <summary>Reads the rows that match <paramref name="filter"/>.</summary>
    Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new Todo.
    /// </summary>
    /// <returns>
    /// The stored entity, or a failure with <see cref="ConflictError"/> when the owner already has
    /// a Todo with the same title (the unique index rejected a concurrent writer).
    /// </returns>
    Task<Result<TodoEntity>> InsertAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Todo.
    /// </summary>
    /// <returns>
    /// Success with true when a row was updated and false when the id doesn't exist, or a failure
    /// with <see cref="ConflictError"/> when the new title is already used by another Todo of the
    /// same owner (the unique index rejected a concurrent writer).
    /// </returns>
    Task<Result<bool>> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Todo by id, restricted to <paramref name="visibleOwner"/> when it is not null.
    /// </summary>
    /// <returns>True when a row was deleted, false when the id doesn't exist or is not visible.</returns>
    Task<bool> DeleteAsync(Ulid id, string? visibleOwner = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether one owner already has a Todo with this title, optionally excluding one id.
    /// Titles are unique per owner, not globally.
    /// </summary>
    Task<bool> TitleExistsAsync(string title,
                                string owner,
                                Ulid? excludeId = null,
                                CancellationToken cancellationToken = default);
}
