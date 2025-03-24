using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseRepository<TEntity> where TEntity : class
{
    public Task<List<TEntity>> FilterAsyncANT(Expression<Func<TEntity, bool>> expression,
                                                     int? pageNumber = null,
                                                     int? pageSize = null,
                                                     CancellationToken cancellationToken = default);
    public Task<List<TOutput>> FilterAsyncANT<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                              Expression<Func<TEntity, TOutput>> selector,
                                                              Expression<Func<TOutput, object>> orderBy,
                                                              bool descending = false,
                                                              int? pageNumber = null,
                                                              int? pageSize = null,
                                                              CancellationToken cancellationToken = default);
    public Task<TEntity?> GetByIdAsyncANT(string id, CancellationToken cancellationToken = default);
    public Task<TOutput?> GetByIdAsyncANT<TOutput>(string id,
                                                   Expression<Func<TEntity, TOutput>> selector,
                                                   CancellationToken cancellationToken = default);
    public Task<TEntity?> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    public bool BulkInsert(IEnumerable<TEntity> entities);
    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    public bool Delete(string id);
    public Task<bool> BulkDeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}
