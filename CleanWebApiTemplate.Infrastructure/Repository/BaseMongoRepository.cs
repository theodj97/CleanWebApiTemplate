using CleanWebApiTemplate.Domain.Models;
using CleanWebApiTemplate.Infrastructure.Context;
using CleanWebApiTemplate.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Repository;

public class BaseMongoRepository<TDocument>(MongoDbContext context) : IBaseRepository<TDocument> where TDocument : BaseCollection
{
    private readonly MongoDbContext context = context;

    public async Task<bool> BulkDeleteAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default)
    {
        var idsParsed = ids
            .Select(id => ObjectId.TryParse(id.ToString(), out var value) ? value : (ObjectId?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        return await context.Set<TDocument>().Where(x => idsParsed.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public bool BulkInsert(IEnumerable<TDocument> entities)
    {
        context.Set<TDocument>().AddRange(entities);
        return context.SaveChanges() > 0;
    }

    public async Task<bool> BulkInsertAsync(IEnumerable<TDocument> entities, CancellationToken cancellationToken = default)
    {
        await context.Set<TDocument>().AddRangeAsync(entities, cancellationToken);
        return await context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> BulkUpdateAsync(IEnumerable<TDocument> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            context.Update(entity);

        return await context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<TDocument?> CreateAsync(TDocument entity, CancellationToken cancellationToken = default)
    {
        var result = await context.AddAsync(entity, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) > 0)
            return result.Entity;

        return null;
    }

    public bool Delete(object id)
    {
        var idParsed = ObjectId.TryParse(id.ToString(), out var value) ? value : (ObjectId?)null;
        if (idParsed is not null)
            return context.Set<TDocument>().Where(x => x.Id == idParsed).ExecuteDelete() > 0;

        return false;
    }

    public async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        var idParsed = ObjectId.TryParse(id.ToString(), out var value) ? value : (ObjectId?)null;
        if (idParsed is not null)
            return await context.Set<TDocument>()
                                .AsNoTracking()
                                .Where(x => x.Id == idParsed)
                                .ExecuteDeleteAsync(cancellationToken: cancellationToken) > 0;

        return false;
    }

    public async Task<IEnumerable<TDocument>> FilterAsyncANT(Expression<Func<TDocument, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await context.Set<TDocument>().AsNoTracking().Where(expression).ToArrayAsync(cancellationToken);
    }

    public Task<TDocument?> GetByIdAsyncANT(object id, CancellationToken cancellationToken = default)
    {
        var idParsed = ObjectId.TryParse(id.ToString(), out var value) ? value : (ObjectId?)null;
        if (idParsed is not null)
            return context.Set<TDocument>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == idParsed, cancellationToken);

        return Task.FromResult<TDocument?>(null);
    }

    public async Task<TDocument> UpdateAsync(TDocument entity, CancellationToken cancellationToken = default)
    {
        context.Update(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
