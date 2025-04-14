using CleanWebApiTemplate.Domain.Models;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Repository;

public sealed class BaseSqlRepository<TEntity>(SqlDbContext context) : IBaseRepository<TEntity> where TEntity : BaseEntity
{
    private readonly SqlDbContext context = context;

    public async Task<bool> BulkDeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idsParsed = ids.Select(Ulid.Parse).ToList();

        return await context.Set<TEntity>().Where(x => idsParsed.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public bool BulkInsert(IEnumerable<TEntity> entities)
    {
        context.Set<TEntity>().AddRange(entities);
        return context.SaveChanges() > 0;
    }

    public async Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        return await context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            context.Update(entity);

        return await context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await context.AddAsync(entity, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) > 0)
            return result.Entity;

        throw new Exception("Failed to create entity");
    }

    public bool Delete(string id) => context.Set<TEntity>().Where(x => x.Id == Ulid.Parse(id)).ExecuteDelete() > 0;

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(x => x.Id == Ulid.Parse(id))
                     .ExecuteDeleteAsync(cancellationToken: cancellationToken) > 0;


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

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .FirstOrDefaultAsync(x => x.Id == Ulid.Parse(id), cancellationToken);


    public async Task<TOutput?> GetByIdAsync<TOutput>(string id, Expression<Func<TEntity, TOutput>> selector, CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(x => x.Id == Ulid.Parse(id))
                     .Select(selector)
                     .FirstOrDefaultAsync(cancellationToken);


    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Update(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
