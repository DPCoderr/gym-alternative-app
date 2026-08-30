using GymAlternatief.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace GymAlternatief.Api.Tests;

public sealed class AppDbContextModelTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=gymalternatief;Username=test;Password=test";

    [Fact]
    public void ModelAppliesUuidUtcAndPostgresConcurrencyConventions()
    {
        using var context = CreateContext(TestConnectionString);

        var entity = context.Model.FindEntityType(typeof(ConventionEntity));

        Assert.NotNull(entity);
        var table = StoreObjectIdentifier.Table(
            Assert.IsType<string>(entity.GetTableName()),
            entity.GetSchema());

        var id = Assert.IsAssignableFrom<IProperty>(
            entity.FindProperty(nameof(ConventionEntity.Id)));
        Assert.Equal(id, Assert.Single(entity.FindPrimaryKey()!.Properties));
        Assert.Equal("uuid", id.GetColumnType());
        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);

        var createdAt = Assert.IsAssignableFrom<IProperty>(
            entity.FindProperty(nameof(ConventionEntity.CreatedAt)));
        var updatedAt = Assert.IsAssignableFrom<IProperty>(
            entity.FindProperty(nameof(ConventionEntity.UpdatedAt)));
        Assert.Equal("timestamp with time zone", createdAt.GetColumnType());
        Assert.Equal("timestamp with time zone", updatedAt.GetColumnType());
        Assert.False(createdAt.IsNullable);
        Assert.False(updatedAt.IsNullable);

        var version = Assert.IsAssignableFrom<IProperty>(
            entity.FindProperty(nameof(ConventionEntity.Version)));
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
        Assert.Equal("xmin", version.GetColumnName(table));
    }

    private static ConventionDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ConventionDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ConventionDbContext(options);
    }
}

public sealed class AppDbContextPostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder(
        "postgres:18-alpine")
        .Build();

    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _database.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task ApiConnectsWithoutMigratingAndPostgresConventionsWork()
    {
        var connectionString = _database.GetConnectionString();

        await AssertApiConnectsWithoutMigrationHistory(connectionString);
        await AssertUuidGenerationAndOptimisticConcurrency(connectionString);
        await AssertNonUtcTimestampIsRejected(connectionString);
    }

    private static async Task AssertApiConnectsWithoutMigrationHistory(
        string connectionString)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Development);
                builder.UseSetting(
                    "ConnectionStrings:gymalternatief",
                    connectionString);
            });
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await context.Database.CanConnectAsync());

        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT to_regclass('\"__EFMigrationsHistory\"')::text;";

        Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
    }

    private static async Task AssertUuidGenerationAndOptimisticConcurrency(
        string connectionString)
    {
        await using (var setup = CreateContext(connectionString))
        {
            await setup.Database.EnsureCreatedAsync();

            var entity = CreateEntity("initial");
            setup.Entities.Add(entity);
            await setup.SaveChangesAsync();

            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.NotEqual(0u, entity.Version);
        }

        await using var firstContext = CreateContext(connectionString);
        await using var secondContext = CreateContext(connectionString);
        var first = await firstContext.Entities.SingleAsync();
        var second = await secondContext.Entities.SingleAsync();

        first.Name = "first update";
        first.UpdatedAt = DateTime.UtcNow;
        await firstContext.SaveChangesAsync();

        second.Name = "stale update";
        second.UpdatedAt = DateTime.UtcNow;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    private static async Task AssertNonUtcTimestampIsRejected(string connectionString)
    {
        await using var context = CreateContext(connectionString);
        var entity = CreateEntity("invalid timestamp");
        entity.CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Local);
        context.Entities.Add(entity);

        var exception = await Record.ExceptionAsync(
            () => context.SaveChangesAsync());

        Assert.NotNull(exception);
        Assert.Contains(
            "UTC",
            exception.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static ConventionDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ConventionDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ConventionDbContext(options);
    }

    private static ConventionEntity CreateEntity(string name)
    {
        var now = DateTime.UtcNow;
        return new ConventionEntity
        {
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}

internal sealed class ConventionDbContext(
    DbContextOptions<ConventionDbContext> options) : DbContext(options)
{
    public DbSet<ConventionEntity> Entities => Set<ConventionEntity>();

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigurePostgresTypes();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ConventionEntity>(entity =>
        {
            entity.ToTable("convention_entities");
            entity.Property(item => item.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.Version)
                .IsPostgresConcurrencyToken();
        });
    }
}

internal sealed class ConventionEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public uint Version { get; set; }
}
