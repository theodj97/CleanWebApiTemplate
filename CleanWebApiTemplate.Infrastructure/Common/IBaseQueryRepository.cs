using System.Linq.Expressions;
using CleanWebApiTemplate.Domain.Models;

namespace CleanWebApiTemplate.Infrastructure.Common;

public interface IBaseQueryRepository<TEntity> where TEntity : BaseEntity
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
}
