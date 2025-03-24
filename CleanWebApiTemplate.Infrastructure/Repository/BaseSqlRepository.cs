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
        var idsParsed = ids
            .Select(id => Ulid.TryParse(id, out var value) ? value : (Ulid?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

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

    public async Task<TEntity?> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await context.AddAsync(entity, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) > 0)
            return result.Entity;

        return null;
    }

    public bool Delete(string id)
    {
        var idParsed = Ulid.TryParse(id, out var value) ? value : (Ulid?)null;
        if (idParsed is not null)
            return context.Set<TEntity>().Where(x => x.Id == idParsed).ExecuteDelete() > 0;

        return false;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var idParsed = Ulid.TryParse(id, out var value) ? value : (Ulid?)null;
        if (idParsed is not null)
            return await context.Set<TEntity>()
                                .AsNoTracking()
                                .Where(x => x.Id == idParsed)
                                .ExecuteDeleteAsync(cancellationToken: cancellationToken) > 0;

        return false;
    }

    public async Task<List<TEntity>> FilterAsyncANT(Expression<Func<TEntity, bool>> expression,
                                                           int? pageNumber = null,
                                                           int? pageSize = null,
                                                           CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>().AsNoTracking().Where(expression);
        query = RepositoryHelper.ManagePagination(query, pageNumber, pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<TOutput>> FilterAsyncANT<TOutput>(Expression<Func<TEntity, bool>> expression,
                                                                    Expression<Func<TEntity, TOutput>> selector,
                                                                    Expression<Func<TOutput, object>> orderBy,
                                                                    bool descending = false,
                                                                    int? pageNumber = null,
                                                                    int? pageSize = null,
                                                                    CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>()
                    .AsNoTracking()
                    .Where(expression)
                    .Select(selector);
        query = RepositoryHelper.ManagePagination(query, pageNumber, pageSize);

        query = descending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsyncANT(string id, CancellationToken cancellationToken = default)
    {
        var idParsed = Ulid.TryParse(id, out var value) ? value : (Ulid?)null;
        if (idParsed is not null)
            return await context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == idParsed, cancellationToken);

        return await Task.FromResult<TEntity?>(null);
    }

    public async Task<TOutput?> GetByIdAsyncANT<TOutput>(string id, Expression<Func<TEntity, TOutput>> selector, CancellationToken cancellationToken = default)
    {
        var idParsed = Ulid.TryParse(id, out var value) ? value : (Ulid?)null;
        if (idParsed is not null)
            return await context.Set<TEntity>()
                .AsNoTracking()
                .Where(x => x.Id == idParsed)
                .Select(selector)
                .FirstOrDefaultAsync(cancellationToken);

        return default;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Update(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
