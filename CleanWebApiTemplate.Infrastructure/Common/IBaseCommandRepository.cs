using CleanWebApiTemplate.Domain.Models;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseCommandRepository<TEntity> where TEntity : BaseEntity
{
    public Task<TEntity> CreateAsync(TEntity entity,
                                     CancellationToken cancellationToken = default);
    public Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities,
                                      CancellationToken cancellationToken = default);
    public bool BulkInsert(IEnumerable<TEntity> entities);
    public Task<TEntity> UpdateAsync(TEntity entity,
                                     CancellationToken cancellationToken = default);
    public Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities,
                                      CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync(string id,
                                  CancellationToken cancellationToken = default);
    public bool Delete(string id);
    public Task<bool> BulkDeleteAsync(IEnumerable<string> ids,
                                      CancellationToken cancellationToken = default);
}
