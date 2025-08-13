using CleanWebApiTemplate.Domain.Models;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseCommandRepository<TEntity> where TEntity : BaseEntity
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
    Task<bool> DeleteAsync(string id,
                             CancellationToken cancellationToken = default);
    bool Delete(string id);
    Task<bool> BulkDeleteAsync(IEnumerable<string> ids,
                                CancellationToken cancellationToken = default);
}
