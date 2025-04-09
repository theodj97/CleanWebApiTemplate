using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseRepository<TEntity> where TEntity : class
{
    public Task<List<TEntity>> FilterAsync(Expression<Func<TEntity, bool>> expression,
                                           Expression<Func<TEntity, object>>? orderBy = null,
                                           bool descending = false,
                                           int? pageNumber = null,
                                           int? pageSize = null,
                                           CancellationToken cancellationToken = default);
    public Task<List<TOutput>> FilterAsync<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                    Expression<Func<TEntity, TOutput>> selector,
                                                    Expression<Func<TOutput, object>>? orderBy = null,
                                                    bool descending = false,
                                                    int? pageNumber = null,
                                                    int? pageSize = null,
                                                    CancellationToken cancellationToken = default);

    public Task<List<TEntity>> FilterSortAsync(Expression<Func<TEntity, bool>> expression,
                                               IEnumerable<KeyValuePair<string, bool>>? sortProperties = null,
                                               int? pageNumber = null,
                                               int? pageSize = null,
                                               CancellationToken cancellationToken = default);

    public Task<List<TOutput>> FilterSortAsync<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                        Expression<Func<TEntity, TOutput>> selector,
                                                        IEnumerable<KeyValuePair<string, bool>>? sortProperties = null,
                                                        int? pageNumber = null,
                                                        int? pageSize = null,
                                                        CancellationToken cancellationToken = default);
    public Task<TEntity?> GetByIdAsync(string id,
                                       CancellationToken cancellationToken = default);
    public Task<TOutput?> GetByIdAsync<TOutput>(string id,
                                                Expression<Func<TEntity, TOutput>> selector,
                                                CancellationToken cancellationToken = default);
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
