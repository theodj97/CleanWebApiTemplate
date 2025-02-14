using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanWebApiTemplate.Infrastructure.EntityConfiguration;

public class TodoEntityConfiguration : IEntityTypeConfiguration<TodoEntity>
{
    public void Configure(EntityTypeBuilder<TodoEntity> builder)
    {
        builder.ToTable(name: SqlDbConstants.TODO_TABLE, schema: SqlDbConstants.DB_SCHEMA);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName(nameof(TodoEntity.Id));
        builder.Property(e => e.Title).HasColumnName(nameof(TodoEntity.Title));
        builder.Property(e => e.Description).HasColumnName(nameof(TodoEntity.Description)).IsRequired(false);
        builder.Property(e => e.CreatedAt).HasColumnType(nameof(TodoEntity.CreatedAt));
        builder.Property(e => e.UpdatedAt).HasColumnType(nameof(TodoEntity.UpdatedAt));
        builder.Property(e => e.Status).HasColumnType(nameof(TodoEntity.Status));
        builder.Property(e => e.CreatedBy).HasColumnType(nameof(TodoEntity.CreatedBy));
    }
}
