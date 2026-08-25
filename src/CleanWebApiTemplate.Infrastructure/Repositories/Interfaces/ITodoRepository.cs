using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

public interface ITodoRepository
{
    public Task<TodoEntity?> GetByIdAsync(Ulid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads only the Id/Title columns (projection query for the titles endpoint).
    /// </summary>
    public Task<IReadOnlyList<TodoDto>> GetTitlesAsync(int? pageNumber,
                                                int? pageSize,
                                                IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken cancellationToken = default);

    public Task<TodoEntity> InsertAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Todo entity.
    /// </summary>
    /// <returns>True when a row was updated, false when the id doesn't exist.</returns>
    public Task<bool> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Todo entity by id.
    /// </summary>
    /// <returns>True when a row was deleted, false when the id doesn't exist.</returns>
    public Task<bool> DeleteAsync(Ulid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a Todo with the given title exists.
    /// </summary>
    /// <returns>True when a Todo with the given title exists (optionally excluding one id).</returns>
    public Task<bool> TitleExistsAsync(string title, Ulid? excludeId = null, CancellationToken cancellationToken = default);
}
