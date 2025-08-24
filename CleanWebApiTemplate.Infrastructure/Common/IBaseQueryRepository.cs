using System.Linq.Expressions;
using CleanWebApiTemplate.Domain.Models;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseQueryRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    Task<List<TEntity>> FilterAsync(Expression<Func<TEntity, bool>> expression,
                                    Expression<Func<TEntity, object>>? orderBy = null,
                                    bool descending = false,
                                    int? pageNumber = null,
                                    int? pageSize = null,
                                    CancellationToken cancellationToken = default);
    Task<List<TOutput>> FilterAsync<TOutput>(Expression<Func<TEntity, bool>> expression,
                                             Expression<Func<TEntity, TOutput>> selector,
                                             Expression<Func<TOutput, object>>? orderBy = null,
                                             bool descending = false,
                                             int? pageNumber = null,
                                             int? pageSize = null,
                                             CancellationToken cancellationToken = default);

    Task<List<TEntity>> FilterSortAsync(Expression<Func<TEntity, bool>> expression,
                                        IEnumerable<KeyValuePair<string, bool>>? sortProperties = null,
                                        int? pageNumber = null,
                                        int? pageSize = null,
                                        CancellationToken cancellationToken = default);

    Task<List<TOutput>> FilterSortAsync<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                 Expression<Func<TEntity, TOutput>> selector,
                                                 IEnumerable<KeyValuePair<string, bool>>? sortProperties = null,
                                                 int? pageNumber = null,
                                                 int? pageSize = null,
                                                 CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(TKey id,
                                CancellationToken cancellationToken = default);
    Task<TOutput?> GetByIdAsync<TOutput>(TKey id,
                                         Expression<Func<TEntity, TOutput>> selector,
                                         CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdATAsync(TKey id,
                                  CancellationToken cancellationToken = default);

    Task<List<TEntity>> FilterATAsync(Expression<Func<TEntity, bool>> expression,
                                      CancellationToken cancellationToken = default);
}
