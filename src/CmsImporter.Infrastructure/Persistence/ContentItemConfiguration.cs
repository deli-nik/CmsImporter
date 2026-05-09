using CmsImporter.Core.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsImporter.Infrastructure.Persistence;

internal sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.SourceSystem)
            .HasColumnName("source_system")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Author)
            .HasColumnName("author")
            .HasMaxLength(256);

        builder.Property(c => c.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(c => c.Body)
            .HasColumnName("body")
            .HasColumnType("jsonb")
            .HasConversion(new ContentBodyJsonConverter(), new ContentBodyValueComparer())
            .IsRequired();

        builder.Property(c => c.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(new StringDictionaryJsonConverter(), new StringDictionaryValueComparer())
            .IsRequired();

        builder.Property(c => c.ImportedAt)
            .HasColumnName("imported_at");

        builder.Property(c => c.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(c => new { c.SourceSystem, c.ExternalId })
            .IsUnique()
            .HasDatabaseName("ix_content_items_source_external");

        builder.HasIndex(c => c.ImportedAt)
            .HasDatabaseName("ix_content_items_imported_at");

        builder.HasIndex(c => c.Type)
            .HasDatabaseName("ix_content_items_type");
    }
}
