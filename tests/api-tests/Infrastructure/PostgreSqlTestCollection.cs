namespace GymAlternatief.Api.Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationTestGroup
    : ICollectionFixture<PostgreSqlDatabaseFixture>
{
    public const string Name = "PostgreSQL integration tests";
}
