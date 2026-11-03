using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Queries;
using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Application.Todo.Abstractions;

public interface ITodoRepository
{
    /// <summary>
    /// Gets a TODO by id. When <paramref name="createdBy"/> is not null the query is
    /// restricted to that owner, so non-administrators never read another user's TODO.
    /// </summary>
    public Task<TodoEntity?> GetByIdAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads only the Id/Title columns (projection query for the titles endpoint).
    /// When <paramref name="createdBy"/> is not null the query is restricted to that owner.
    /// </summary>
    /// <returns>Partial entities: only Id and Title are populated.</returns>
    public Task<IReadOnlyList<TodoEntity>> GetTitlesAsync(int? pageNumber,
                                                int? pageSize,
                                                IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                string? createdBy = null,
                                                CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new Todo entity.
    /// </summary>
    /// <returns>
    /// The stored entity, or a failure with <see cref="ConflictError"/> when the owner already has
    /// a TODO with the same title (a concurrent writer passed the application pre-check).
    /// </returns>
    public Task<Result<TodoEntity>> InsertAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Todo entity.
    /// </summary>
    /// <returns>
    /// Success with true when a row was updated and false when the id doesn't exist, or a failure
    /// with <see cref="ConflictError"/> when the new title is already used by another TODO of the
    /// same owner (a concurrent writer passed the application pre-check).
    /// </returns>
    public Task<Result<bool>> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Todo entity by id. When <paramref name="createdBy"/> is not null the
    /// delete is restricted to that owner.
    /// </summary>
    /// <returns>True when a row was deleted, false when the id doesn't exist or belongs to another owner.</returns>
    public Task<bool> DeleteAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a TODO with the given title exists for one owner. Titles are unique per owner,
    /// not globally.
    /// </summary>
    /// <returns>True when the owner already has a TODO with that title (optionally excluding one id).</returns>
    public Task<bool> TitleExistsAsync(string title,
                                       string createdBy,
                                       Ulid? excludeId = null,
                                       CancellationToken cancellationToken = default);
}
