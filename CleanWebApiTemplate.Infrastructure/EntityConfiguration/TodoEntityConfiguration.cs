﻿using CleanWebApiTemplate.Domain.Models.Entities;
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
        builder.Property(e => e.Id)
              .HasColumnName(nameof(TodoEntity.Id))
              .HasColumnType("binary(16)")
              .HasConversion(
                  ulid => ulid.ToByteArray(),
                  bytes => new Ulid(bytes)
              );
        builder.Property(e => e.Title)
            .HasColumnName(nameof(TodoEntity.Title))
            .HasColumnType("NVARCHAR")
            .HasMaxLength(TitleLenght)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName(nameof(TodoEntity.Description))
            .HasColumnType("NVARCHAR")
            .HasMaxLength(DescriptionLenght)
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName(nameof(TodoEntity.CreatedAt))
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName(nameof(TodoEntity.UpdatedAt))
            .HasColumnType("datetime2");

        builder.Property(e => e.Status)
            .HasColumnName(nameof(TodoEntity.Status))
            .HasColumnType("tinyint");

        builder.Property(e => e.CreatedBy)
            .HasColumnName(nameof(TodoEntity.CreatedBy))
            .HasColumnType("NVARCHAR")
            .HasMaxLength(CreatedByLenght)
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasColumnName(nameof(TodoEntity.UpdatedBy))
            .HasColumnType("NVARCHAR")
            .HasMaxLength(UpdatedByLenght)
            .IsRequired();
    }

    public static byte TitleLenght => 255;
    public static int DescriptionLenght => 1000;
    public static byte CreatedByLenght => 255;
    public static byte UpdatedByLenght => 255;
}
