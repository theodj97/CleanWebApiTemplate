using CleanWebApiTemplate.Domain.Models;
using CleanWebApiTemplate.Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Common;

public class BaseRepositoryDispatcher<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    private readonly IBaseRepository<TEntity> Repository;
    public BaseRepositoryDispatcher(IServiceProvider serviceProvider)
    {
        var repoType = typeof(TEntity) switch
        {
            _ when typeof(BaseEntity).IsAssignableFrom(typeof(TEntity))
                => typeof(BaseSqlRepository<>).MakeGenericType(typeof(TEntity)),

            _ when typeof(BaseCollection).IsAssignableFrom(typeof(TEntity))
                => typeof(BaseMongoRepository<>).MakeGenericType(typeof(TEntity)),

            _ => throw new InvalidOperationException($"No repository found for entity type {typeof(TEntity).Name}")
        };

        Repository = (IBaseRepository<TEntity>)serviceProvider.GetRequiredService(repoType);
    }

    public Task<bool> BulkDeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Repository.BulkDeleteAsync(ids, cancellationToken);

    public bool BulkInsert(IEnumerable<TEntity> entities) => Repository.BulkInsert(entities);

    public Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => Repository.BulkInsertAsync(entities, cancellationToken);

    public Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => Repository.BulkUpdateAsync(entities, cancellationToken);

    public Task<TEntity?> CreateAsync(TEntity entity, CancellationToken cancellationToken = default) => Repository.CreateAsync(entity, cancellationToken);

    public bool Delete(string id) => Repository.Delete(id);

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Repository.DeleteAsync(id, cancellationToken);

    public Task<IEnumerable<TEntity>> FilterAsyncANT(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) => Repository.FilterAsyncANT(expression, cancellationToken);

    public Task<TEntity?> GetByIdAsyncANT(string id, CancellationToken cancellationToken = default) => Repository.GetByIdAsyncANT(id, cancellationToken);

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) => Repository.UpdateAsync(entity, cancellationToken);
}
