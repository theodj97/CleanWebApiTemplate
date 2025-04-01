using CleanWebApiTemplate.Domain.Models.Collections;
using CleanWebApiTemplate.Infrastructure.Constants;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace CleanWebApiTemplate.Infrastructure.Context;

public class MongoDbContext(DbContextOptions<MongoDbContext> options) : DbContext(options)
{
    public DbSet<TodoCollection> TodoDb { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoCollection>().ToCollection(MongoDbConstants.TODO_COLLECTION);
    }
}
