using CleanWebApiTemplate.Domain.Models;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanWebApiTemplate.Infrastructure.Repository;

public class BaseCommandRepository<TEntity, TKey>(SqlDbContext context) : IBaseCommandRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    private readonly SqlDbContext context = context;

    public async Task<bool> BulkDeleteAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) =>
    await context.Set<TEntity>().Where(x => ids.Contains(x.Id!)).ExecuteDeleteAsync(cancellationToken) > 0;

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

        throw new Exception($"Failed to create entity: {result}");
    }

    public bool Delete(TKey id) => context.Set<TEntity>().Where(x => x.Id!.Equals(id)).ExecuteDelete() > 0;

    public async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default) =>
        await context.Set<TEntity>()
                     .AsNoTracking()
                     .Where(x => x.Id!.Equals(id))
                     .ExecuteDeleteAsync(cancellationToken: cancellationToken) > 0;

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Update(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
