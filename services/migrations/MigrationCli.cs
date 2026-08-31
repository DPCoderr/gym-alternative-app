using GymAlternatief.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymAlternatief.Migrations;

internal enum MigrationCommand
{
    Status,
    Apply,
}

internal static class MigrationCli
{
    internal const int SuccessExitCode = 0;
    internal const int OperationFailureExitCode = 1;
    internal const int CommandErrorExitCode = 2;
    internal const int ConfigurationErrorExitCode = 3;

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter? output = null,
        TextWriter? error = null,
        Func<string?>? connectionStringProvider = null,
        Func<MigrationCommand, string, TextWriter, CancellationToken, Task>?
            execute = null,
        CancellationToken cancellationToken = default)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        if (!TryParse(arguments, out var command))
        {
            await error.WriteLineAsync("Specify one command: status or apply.");
            return CommandErrorExitCode;
        }

        connectionStringProvider ??= () => Environment.GetEnvironmentVariable(
            "ConnectionStrings__gymalternatief");
        var connectionString = connectionStringProvider();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await error.WriteLineAsync(
                "Database configuration is missing. Set ConnectionStrings__gymalternatief.");
            return ConfigurationErrorExitCode;
        }

        try
        {
            await (execute ?? ExecuteAsync)(
                command,
                connectionString,
                output,
                cancellationToken);
            return SuccessExitCode;
        }
        catch (Exception)
        {
            await error.WriteLineAsync(
                "The migration command failed. Check database availability and configuration.");
            return OperationFailureExitCode;
        }
    }

    internal static bool TryParse(
        IReadOnlyList<string> arguments,
        out MigrationCommand command)
    {
        command = default;
        if (arguments.Count != 1)
        {
            return false;
        }

        return Enum.TryParse(arguments[0], ignoreCase: true, out command)
            && Enum.IsDefined(command);
    }

    private static async Task ExecuteAsync(
        MigrationCommand command,
        string connectionString,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new AppDbContext(options);

        if (command == MigrationCommand.Status)
        {
            var applied = (await context.Database.GetAppliedMigrationsAsync(
                cancellationToken)).ToArray();
            var pending = (await context.Database.GetPendingMigrationsAsync(
                cancellationToken)).ToArray();

            await WriteMigrationsAsync(output, "Applied", applied);
            await WriteMigrationsAsync(output, "Pending", pending);
            return;
        }

        var migrations = (await context.Database.GetPendingMigrationsAsync(
            cancellationToken)).ToArray();
        if (migrations.Length == 0)
        {
            await output.WriteLineAsync("No pending migrations.");
            return;
        }

        await context.Database.MigrateAsync(cancellationToken);
        await WriteMigrationsAsync(output, "Applied", migrations);
    }

    private static async Task WriteMigrationsAsync(
        TextWriter output,
        string state,
        string[] migrations)
    {
        await output.WriteLineAsync($"{state} migrations: {migrations.Length}");
        foreach (var migration in migrations)
        {
            await output.WriteLineAsync($"  {state.ToLowerInvariant()}: {migration}");
        }
    }
}
