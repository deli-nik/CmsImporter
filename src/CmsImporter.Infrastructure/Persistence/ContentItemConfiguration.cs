using CmsImporter.Core.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsImporter.Infrastructure.Persistence;

/// <summary>
/// EF Core fluent-API mapping for <see cref="ContentItem"/>. Picked up by
/// <see cref="AppDbContext.OnModelCreating"/> via <c>ApplyConfigurationsFromAssembly</c>.
/// Defines snake_case column names, JSONB columns for <c>Body</c>/<c>Metadata</c>, the
/// optimistic-concurrency token, and the unique + lookup indexes.
/// </summary>
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

        // Stored as text rather than int for human-friendly inspection in psql.
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

        // JSONB column with a System.Text.Json converter and a structural value comparer so EF
        // can detect changes without triggering spurious updates on every load.
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

        // Optimistic concurrency token — EF includes this in WHERE clauses on UPDATE so racing
        // writers fail with DbUpdateConcurrencyException rather than silently overwriting.
        builder.Property(c => c.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        // Natural key — every (source CMS, external id) pair appears at most once.
        builder.HasIndex(c => new { c.SourceSystem, c.ExternalId })
            .IsUnique()
            .HasDatabaseName("ix_content_items_source_external");

        // Supports the "newest first" ordering used by ContentQueryService.
        builder.HasIndex(c => c.ImportedAt)
            .HasDatabaseName("ix_content_items_imported_at");

        // Supports filtering by content type.
        builder.HasIndex(c => c.Type)
            .HasDatabaseName("ix_content_items_type");
    }
}
