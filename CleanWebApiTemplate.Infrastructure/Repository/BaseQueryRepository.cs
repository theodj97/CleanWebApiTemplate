using System.Linq.Expressions;
using CleanWebApiTemplate.Domain.Models;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanWebApiTemplate.Infrastructure.Repository;

public class BaseQueryRepository<TEntity, TKey> : IBaseQueryRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    private readonly SqlDbContext context;

    public BaseQueryRepository(SqlDbContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context),
                                            $"While initiating {nameof(BaseQueryRepository<TEntity, TKey>)}, the service {typeof(SqlDbContext)} was not found!");

        this.context = context;
    }

    public async Task<List<TEntity>> FilterAsync(Expression<Func<TEntity, bool>> expression,
                                                 Expression<Func<TEntity, object>>? orderBy = null,
                                                 bool descending = false,
                                                 int? pageNumber = null,
                                                 int? pageSize = null,
                                                 CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(expression)
                     .OrderBy(orderBy, descending)
                     .ManagePagination(pageNumber, pageSize)
                     .ToListAsync(cancellationToken);


    public async Task<List<TOutput>> FilterAsync<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                          Expression<Func<TEntity, TOutput>> selector,
                                                          Expression<Func<TOutput, object>>? orderBy = null,
                                                          bool descending = false,
                                                          int? pageNumber = null,
                                                          int? pageSize = null,
                                                          CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(expression)
                     .Select(selector)
                     .OrderBy(orderBy, descending)
                     .ManagePagination(pageNumber, pageSize)
                     .ToListAsync(cancellationToken);

    public async Task<List<TEntity>> FilterATAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .Where(expression)
                     .ToListAsync(cancellationToken);

    public async Task<List<TEntity>> FilterSortAsync(Expression<Func<TEntity, bool>> expression,
                                                     IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                     int? pageNumber = null,
                                                     int? pageSize = null,
                                                     CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(expression)
                     .DynamicOrderBy(sortProperties)
                     .ManagePagination(pageNumber, pageSize)
                     .ToListAsync(cancellationToken);

    public async Task<List<TOutput>> FilterSortAsync<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                              Expression<Func<TEntity, TOutput>> selector,
                                                              IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                              int? pageNumber = null,
                                                              int? pageSize = null,
                                                              CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(expression)
                     .Select(selector)
                     .DynamicOrderBy(sortProperties)
                     .ManagePagination(pageNumber, pageSize)
                     .ToListAsync(cancellationToken);

    public async Task<TEntity?> GetByIdAsync(TKey id,
                                             CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);


    public async Task<TOutput?> GetByIdAsync<TOutput>(TKey id,
                                                      Expression<Func<TEntity, TOutput>> selector,
                                                      CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(x => x.Id!.Equals(id))
                     .Select(selector)
                     .FirstOrDefaultAsync(cancellationToken);

    public async Task<TEntity?> GetByIdATAsync(TKey id, CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .Where(x => x.Id!.Equals(id))
                     .FirstOrDefaultAsync(cancellationToken);
}
