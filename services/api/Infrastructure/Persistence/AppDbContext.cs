using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GymAlternatief.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected AppDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<Guid>()
            .HaveColumnType("uuid");
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp with time zone");
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveColumnType("timestamp with time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureEntities(modelBuilder);
        modelBuilder.ApplyPersistenceConventions();
    }

    protected virtual void ConfigureEntities(ModelBuilder modelBuilder)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureUtcTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureUtcTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureUtcTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            EnsureUtcTimestamps(entry);
        }
    }

    private static void EnsureUtcTimestamps(EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            switch (property.CurrentValue)
            {
                case DateTime timestamp when timestamp.Kind is not DateTimeKind.Utc:
                    throw CreateUtcException(entry, property.Metadata.Name);
                case DateTimeOffset timestamp when timestamp.Offset != TimeSpan.Zero:
                    throw CreateUtcException(entry, property.Metadata.Name);
            }
        }
    }

    private static InvalidOperationException CreateUtcException(
        EntityEntry entry,
        string propertyName)
    {
        return new InvalidOperationException(
            $"{entry.Metadata.ClrType.Name}.{propertyName} must contain a UTC timestamp.");
    }
}
