using CleanWebApiTemplate.Domain.Models.Collections;
using CleanWebApiTemplate.Infrastructure.Constants;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace CleanWebApiTemplate.Infrastructure.Context;

public class MongoDbContext(DbContextOptions<MongoDbContext> options) : DbContext(options)
{
    public DbSet<TaskCollection> TaskDb { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskCollection>().ToCollection(MongoDbConstants.TASK_COLLECTION);
    }
}
