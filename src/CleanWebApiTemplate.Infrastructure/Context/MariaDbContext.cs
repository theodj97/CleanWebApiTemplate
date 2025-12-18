using CleanWebApiTemplate.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CleanWebApiTemplate.Infrastructure.Context;

public class MariaDbContext(DbContextOptions<MariaDbContext> options) : DbContext(options)
{
    public DbSet<TodoEntity> TodoDb { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        Assembly assembly = Assembly.GetExecutingAssembly();
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
