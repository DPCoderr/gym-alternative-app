using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymAlternatief.Api.Infrastructure.Persistence;

public interface IHasUuidPrimaryKey
{
    Guid Id { get; set; }
}

public interface IHasUtcTimestamps
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
}

public interface IHasPostgresConcurrencyToken
{
    uint Version { get; set; }
}

internal static class PersistenceConventions
{
    public static void ApplyPersistenceConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var entity = modelBuilder.Entity(clrType);

            if (typeof(IHasUuidPrimaryKey).IsAssignableFrom(clrType))
            {
                ConfigureUuidPrimaryKey(entity);
            }

            if (typeof(IHasUtcTimestamps).IsAssignableFrom(clrType))
            {
                ConfigureUtcTimestamps(entity);
            }

            if (typeof(IHasPostgresConcurrencyToken).IsAssignableFrom(clrType))
            {
                ConfigurePostgresConcurrency(entity);
            }
        }
    }

    private static void ConfigureUuidPrimaryKey(EntityTypeBuilder entity)
    {
        entity.HasKey(nameof(IHasUuidPrimaryKey.Id));
        entity.Property<Guid>(nameof(IHasUuidPrimaryKey.Id))
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();
    }

    private static void ConfigureUtcTimestamps(EntityTypeBuilder entity)
    {
        entity.Property<DateTime>(nameof(IHasUtcTimestamps.CreatedAt))
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        entity.Property<DateTime>(nameof(IHasUtcTimestamps.UpdatedAt))
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }

    private static void ConfigurePostgresConcurrency(EntityTypeBuilder entity)
    {
        entity.Property<uint>(nameof(IHasPostgresConcurrencyToken.Version))
            .IsRowVersion();
    }
}
