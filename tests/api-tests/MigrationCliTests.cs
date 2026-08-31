using GymAlternatief.Migrations;

namespace GymAlternatief.Api.Tests;

public sealed class MigrationCliTests
{
    [Theory]
    [InlineData("status", 0)]
    [InlineData("STATUS", 0)]
    [InlineData("apply", 1)]
    public void TryParseRecognizesSupportedCommands(string argument, int expected)
    {
        var success = MigrationCli.TryParse([argument], out var command);

        Assert.True(success);
        Assert.Equal((MigrationCommand)expected, command);
    }

    [Theory]
    [InlineData()]
    [InlineData("unknown")]
    [InlineData("status", "apply")]
    public void TryParseRejectsInvalidArguments(params string[] arguments)
    {
        Assert.False(MigrationCli.TryParse(arguments, out _));
    }

    [Fact]
    public async Task InvalidCommandDoesNotReadConfigurationOrEchoArgument()
    {
        const string secretArgument = "Password=do-not-log";
        var configurationRead = false;
        using var error = new StringWriter();

        var exitCode = await MigrationCli.RunAsync(
            [secretArgument],
            error: error,
            connectionStringProvider: () =>
            {
                configurationRead = true;
                return "unused";
            });

        Assert.Equal(MigrationCli.CommandErrorExitCode, exitCode);
        Assert.False(configurationRead);
        Assert.DoesNotContain(secretArgument, error.ToString());
    }

    [Fact]
    public async Task MissingConfigurationReturnsConfigurationExitCode()
    {
        var exitCode = await MigrationCli.RunAsync(
            ["status"],
            connectionStringProvider: () => null);

        Assert.Equal(MigrationCli.ConfigurationErrorExitCode, exitCode);
    }

    [Fact]
    public async Task SupportedCommandReturnsSuccessExitCode()
    {
        MigrationCommand? executedCommand = null;

        var exitCode = await MigrationCli.RunAsync(
            ["apply"],
            connectionStringProvider: () => "configured",
            execute: (command, _, _, _) =>
            {
                executedCommand = command;
                return Task.CompletedTask;
            });

        Assert.Equal(MigrationCli.SuccessExitCode, exitCode);
        Assert.Equal(MigrationCommand.Apply, executedCommand);
    }

    [Fact]
    public async Task OperationFailureReturnsNonZeroWithoutLoggingDetails()
    {
        const string secret = "Password=do-not-log";
        using var error = new StringWriter();

        var exitCode = await MigrationCli.RunAsync(
            ["apply"],
            error: error,
            connectionStringProvider: () => secret,
            execute: (_, _, _, _) => throw new InvalidOperationException(secret));

        Assert.Equal(MigrationCli.OperationFailureExitCode, exitCode);
        Assert.DoesNotContain(secret, error.ToString());
    }
}
