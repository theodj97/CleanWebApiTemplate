using CleanWebApiTemplate.Domain.Models;
using CleanWebApiTemplate.Infrastructure.Context;
using CleanWebApiTemplate.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Repository;

public abstract class BaseSqlRepository<TEntity>(SqlDbContext context) : IBaseRepository<TEntity> where TEntity : BaseEntity
{
    private readonly SqlDbContext context = context;

    public async Task<bool> BulkDeleteAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default)
    {
        var idsParsed = ids
            .Select(id => int.TryParse(id.ToString(), out var value) ? value : (int?)null)
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

    public bool Delete(object id)
    {
        var idParsed = int.TryParse(id.ToString(), out var value) ? value : (int?)null;
        if (idParsed is not null && idParsed > 0)
            return context.Set<TEntity>().Where(x => x.Id == idParsed).ExecuteDelete() > 0;

        return false;
    }

    public async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        var idParsed = int.TryParse(id.ToString(), out var value) ? value : (int?)null;
        if (idParsed is not null && idParsed > 0)
            return await context.Set<TEntity>()
                                .AsNoTracking()
                                .Where(x => x.Id == idParsed)
                                .ExecuteDeleteAsync(cancellationToken: cancellationToken) > 0;

        return false;
    }

    public async Task<IEnumerable<TEntity>> FilterAsyncANT(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await context.Set<TEntity>().AsNoTracking().Where(expression).ToArrayAsync(cancellationToken);
    }

    public Task<TEntity?> GetByIdAsyncANT(object id, CancellationToken cancellationToken = default)
    {
        var idParsed = int.TryParse(id.ToString(), out var value) ? value : (int?)null;
        if (idParsed is not null && idParsed > 0)
            return context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == idParsed, cancellationToken);

        return Task.FromResult<TEntity?>(null);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Update(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
