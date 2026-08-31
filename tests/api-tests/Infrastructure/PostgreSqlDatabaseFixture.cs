using System.Collections.Concurrent;
using GymAlternatief.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GymAlternatief.Api.Tests.Infrastructure;

public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(
            "postgres:18-alpine")
        .Build();
    private readonly ConcurrentDictionary<string, byte> _databaseNames = [];
    private readonly SemaphoreSlim _databaseLock = new(1, 1);

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = await CreateDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public void Dispose()
    {
        _databaseLock.Dispose();
    }

    public async Task<string> CreateDatabaseAsync(
        CancellationToken cancellationToken = default)
    {
        var databaseName = $"gymalternatief_tests_{Guid.NewGuid():N}";
        var connectionString = BuildConnectionString(databaseName);

        await _databaseLock.WaitAsync(cancellationToken);
        try
        {
            await CreateEmptyDatabaseAsync(databaseName, cancellationToken);
            _databaseNames.TryAdd(databaseName, 0);
        }
        finally
        {
            _databaseLock.Release();
        }

        await ApplyApplicationSchemaAsync(connectionString, cancellationToken);
        return connectionString;
    }

    public async Task ResetDatabaseAsync(
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        connectionString ??= ConnectionString;
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;

        if (string.IsNullOrWhiteSpace(databaseName)
            || !_databaseNames.ContainsKey(databaseName))
        {
            throw new ArgumentException(
                "The connection string was not created by this fixture.",
                nameof(connectionString));
        }

        await _databaseLock.WaitAsync(cancellationToken);
        try
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(BuildMaintenanceConnectionString());
            await connection.OpenAsync(cancellationToken);

            await ExecuteNonQueryAsync(
                connection,
                $"DROP DATABASE {QuoteIdentifier(databaseName)} WITH (FORCE);",
                cancellationToken);
            await ExecuteNonQueryAsync(
                connection,
                $"CREATE DATABASE {QuoteIdentifier(databaseName)};",
                cancellationToken);
        }
        finally
        {
            _databaseLock.Release();
        }

        await ApplyApplicationSchemaAsync(connectionString, cancellationToken);
    }

    private async Task CreateEmptyDatabaseAsync(
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(BuildMaintenanceConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplyApplicationSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.GetName().Name))
            .Options;

        await using var context = new AppDbContext(options);

        if (context.Database.GetMigrations().Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
            return;
        }

        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    private string BuildConnectionString(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(
            _container.GetConnectionString())
        {
            Database = databaseName,
            Pooling = false,
        };

        return builder.ConnectionString;
    }

    private string BuildMaintenanceConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            _container.GetConnectionString())
        {
            Database = "postgres",
            Pooling = false,
        };

        return builder.ConnectionString;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
