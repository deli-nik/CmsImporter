using CmsImporter.Core.Entities;

using Microsoft.EntityFrameworkCore;

namespace CmsImporter.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> for the importer. Registered as scoped via DI; each import
/// job gets its own scope and therefore its own context instance.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>The <c>content_items</c> table.</summary>
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discovers ContentItemConfiguration automatically — adding new entities
        // means just adding a new IEntityTypeConfiguration class in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
