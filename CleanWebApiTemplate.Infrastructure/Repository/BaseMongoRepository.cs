// using CleanWebApiTemplate.Domain.Models;
// using CleanWebApiTemplate.Infrastructure.Common;
// using CleanWebApiTemplate.Infrastructure.Context;
// using Microsoft.EntityFrameworkCore;
// using MongoDB.Bson;
// using System.Linq.Expressions;

// namespace CleanWebApiTemplate.Infrastructure.Repository;

// public sealed class BaseMongoRepository<TDocument>(MongoDbContext context) : IBaseRepository<TDocument> where TDocument : BaseCollection
// {
//     private readonly MongoDbContext context = context;

//     public async Task<bool> BulkDeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
//     {
//         var idsParsed = ids
//             .Select(id => ObjectId.TryParse(id, out var value) ? value : (ObjectId?)null)
//             .Where(value => value.HasValue)
//             .Select(value => value!.Value)
//             .ToList();

//         return await context.Set<TDocument>().Where(x => idsParsed.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken) > 0;
//     }

//     public bool BulkInsert(IEnumerable<TDocument> entities)
//     {
//         context.Set<TDocument>().AddRange(entities);
//         return context.SaveChanges() > 0;
//     }

//     public async Task<bool> BulkInsertAsync(IEnumerable<TDocument> entities, CancellationToken cancellationToken = default)
//     {
//         await context.Set<TDocument>().AddRangeAsync(entities, cancellationToken);
//         return await context.SaveChangesAsync(cancellationToken) > 0;
//     }

//     public async Task<bool> BulkUpdateAsync(IEnumerable<TDocument> entities, CancellationToken cancellationToken = default)
//     {
//         foreach (var entity in entities)
//             context.Update(entity);

//         return await context.SaveChangesAsync(cancellationToken) > 0;
//     }

//     public async Task<TDocument> CreateAsync(TDocument entity, CancellationToken cancellationToken = default)
//     {
//         var result = await context.AddAsync(entity, cancellationToken);
//         if (await context.SaveChangesAsync(cancellationToken) > 0)
//             return result.Entity;

//         throw new Exception("Failed to create document.");
//     }

//     public bool Delete(string id)
//     {
//         var idParsed = ObjectId.TryParse(id, out var value) ? value : (ObjectId?)null;
//         if (idParsed is not null)
//             return context.Set<TDocument>().Where(x => x.Id == idParsed).ExecuteDelete() > 0;

//         return false;
//     }

//     public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
//     {
//         var idParsed = ObjectId.TryParse(id, out var value) ? value : (ObjectId?)null;
//         if (idParsed is not null)
//             return await context.Set<TDocument>()
//                                 .AsNoTracking()
//                                 .Where(x => x.Id == idParsed)
//                                 .ExecuteDeleteAsync(cancellationToken: cancellationToken) > 0;

//         return false;
//     }

//     public async Task<List<TDocument>> FilterAsync(Expression<Func<TDocument, bool>> expression,
//                                                       Expression<Func<TDocument, object>>? orderBy = null,
//                                                       bool descending = false,
//                                                       int? pageNumber = null,
//                                                       int? pageSize = null,
//                                                       CancellationToken cancellationToken = default)
//     {
//         var query = context.Set<TDocument>().AsNoTracking().Where(expression);
//         if (orderBy is not null)
//             query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
//         query = RepositoryExtension.ManagePagination(query, pageNumber, pageSize);

//         return await query.ToListAsync(cancellationToken);
//     }

//     public async Task<List<TOutput>> FilterAsync<TOutput>(Expression<Func<TDocument, bool>> expression,
//                                                              Expression<Func<TDocument, TOutput>> selector,
//                                                              Expression<Func<TOutput, object>>? orderBy = null,
//                                                              bool descending = false,
//                                                              int? pageNumber = null,
//                                                              int? pageSize = null,
//                                                              CancellationToken cancellationToken = default)
//     {
//         var query = context.Set<TDocument>()
//                     .AsNoTracking()
//                     .Where(expression)
//                     .Select(selector);
//         if (orderBy is not null)
//             query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
//         query = RepositoryExtension.ManagePagination(query, pageNumber, pageSize);

//         return await query.ToListAsync(cancellationToken);
//     }

//     public async Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
//     {
//         var idParsed = ObjectId.TryParse(id, out var value) ? value : (ObjectId?)null;
//         if (idParsed is not null)
//             return await context.Set<TDocument>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == idParsed, cancellationToken);

//         return await Task.FromResult<TDocument?>(null);
//     }

//     public async Task<TOutput?> GetByIdAsync<TOutput>(string id,
//                                                          Expression<Func<TDocument, TOutput>> selector,
//                                                          CancellationToken cancellationToken = default)
//     {
//         var idParsed = ObjectId.TryParse(id, out var value) ? value : (ObjectId?)null;
//         if (idParsed is not null)
//             return await context.Set<TDocument>()
//                 .AsNoTracking()
//                 .Where(x => x.Id == idParsed)
//                 .Select(selector)
//                 .FirstOrDefaultAsync(cancellationToken);

//         return default;
//     }

//     public async Task<TDocument> UpdateAsync(TDocument entity, CancellationToken cancellationToken = default)
//     {
//         context.Update(entity);
//         await context.SaveChangesAsync(cancellationToken);

//         return entity;
//     }
// }
