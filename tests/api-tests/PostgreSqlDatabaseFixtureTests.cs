using GymAlternatief.Api.Tests.Infrastructure;
using Npgsql;

namespace GymAlternatief.Api.Tests;

[Collection(PostgreSqlIntegrationTestGroup.Name)]
public sealed class PostgreSqlDatabaseFixtureTests(
    PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task WritesReadsAndRemovesMutableDataOnReset()
    {
        await using (var connection = new NpgsqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE fixture_smoke_test (
                    id uuid PRIMARY KEY,
                    value text NOT NULL
                );
                INSERT INTO fixture_smoke_test (id, value)
                VALUES (@id, @value);
                SELECT value FROM fixture_smoke_test WHERE id = @id;
                """;
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("value", "echte PostgreSQL");

            Assert.Equal("echte PostgreSQL", await command.ExecuteScalarAsync());
        }

        await database.ResetDatabaseAsync();

        await using var resetConnection = new NpgsqlConnection(database.ConnectionString);
        await resetConnection.OpenAsync();
        await using var resetCommand = resetConnection.CreateCommand();
        resetCommand.CommandText = "SELECT to_regclass('fixture_smoke_test')::text;";

        Assert.Equal(DBNull.Value, await resetCommand.ExecuteScalarAsync());
    }

    [Fact]
    public async Task CreatesAnIsolatedDatabaseWithoutSharingMutableData()
    {
        var isolatedConnectionString = await database.CreateDatabaseAsync();

        await using var connection = new NpgsqlConnection(isolatedConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('fixture_smoke_test')::text;";

        Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
        Assert.NotEqual(database.ConnectionString, isolatedConnectionString);
    }
}
