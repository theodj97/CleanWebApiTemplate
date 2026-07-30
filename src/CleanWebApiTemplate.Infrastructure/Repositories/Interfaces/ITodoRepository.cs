using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

public interface ITodoRepository
{
    Task<TodoEntity?> GetByIdAsync(Ulid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads only the Id/Title columns (projection query for the titles endpoint).
    /// </summary>
    Task<IReadOnlyList<TodoDto>> GetTitlesAsync(int? pageNumber,
                                                int? pageSize,
                                                IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken cancellationToken = default);

    Task<TodoEntity> InsertAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <returns>True when a row was updated, false when the id doesn't exist.</returns>
    Task<bool> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default);

    /// <returns>True when a row was deleted, false when the id doesn't exist.</returns>
    Task<bool> DeleteAsync(Ulid id, CancellationToken cancellationToken = default);

    /// <returns>True when a Todo with the given title exists (optionally excluding one id).</returns>
    Task<bool> TitleExistsAsync(string title, Ulid? excludeId = null, CancellationToken cancellationToken = default);
}
