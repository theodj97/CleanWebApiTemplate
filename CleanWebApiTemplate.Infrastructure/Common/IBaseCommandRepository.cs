using CleanWebApiTemplate.Domain.Models;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseCommandRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    Task<TEntity> CreateAsync(TEntity entity,
                              CancellationToken cancellationToken = default);
    Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities,
                               CancellationToken cancellationToken = default);
    bool BulkInsert(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity,
                              CancellationToken cancellationToken = default);
    Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities,
                               CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id,
                           CancellationToken cancellationToken = default);
    bool Delete(TKey id);
    Task<bool> BulkDeleteAsync(IEnumerable<TKey> ids,
                               CancellationToken cancellationToken = default);
}
