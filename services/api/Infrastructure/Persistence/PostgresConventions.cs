using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymAlternatief.Api.Infrastructure.Persistence;

public static class PostgresConventions
{
    public static void ConfigurePostgresTypes(
        this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Guid>()
            .HaveColumnType("uuid");
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp with time zone");
    }

    public static PropertyBuilder<uint> IsPostgresConcurrencyToken(
        this PropertyBuilder<uint> property)
    {
        return property.IsRowVersion();
    }
}
