using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

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
    public Task<IReadOnlyList<TodoDto>> GetTitlesAsync(int? pageNumber,
                                                int? pageSize,
                                                IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                string? createdBy = null,
                                                CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken cancellationToken = default);

    public Task<TodoEntity> InsertAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Todo entity.
    /// </summary>
    /// <returns>True when a row was updated, false when the id doesn't exist.</returns>
    public Task<bool> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Todo entity by id. When <paramref name="createdBy"/> is not null the
    /// delete is restricted to that owner.
    /// </summary>
    /// <returns>True when a row was deleted, false when the id doesn't exist or belongs to another owner.</returns>
    public Task<bool> DeleteAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a Todo with the given title exists.
    /// </summary>
    /// <returns>True when a Todo with the given title exists (optionally excluding one id).</returns>
    public Task<bool> TitleExistsAsync(string title, Ulid? excludeId = null, CancellationToken cancellationToken = default);
}
