using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace CleanWebApiTemplate.Infrastructure.Context;

// EF Core annotations are conservative: this context uses a compiled model
// (SqlDbContextModel, generated via 'dotnet ef dbcontext optimize'), which is the
// supported pattern for running EF Core in trimmed/Native AOT applications.
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
    Justification = "EF Core is configured with a precompiled model, which is compatible with trimming.")]
[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
    Justification = "EF Core is configured with a precompiled model, which is compatible with Native AOT.")]
public class SqlDbContext(DbContextOptions<SqlDbContext> options) : DbContext(options)
{
    public DbSet<TodoEntity> TodoDb { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicit registration (no assembly scanning, Native AOT friendly).
        // Register each IEntityTypeConfiguration here when adding new entities.
        modelBuilder.ApplyConfiguration(new TodoEntityConfiguration());
    }
}
