using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseRepository<TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> FilterAsyncANT(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsyncANT(string id, CancellationToken cancellationToken = default);
    Task<TEntity?> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    bool BulkInsert(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    bool Delete(string id);
    Task<bool> BulkDeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}
