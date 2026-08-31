namespace GymAlternatief.Migrations;

internal static class MigrationProgram
{
    public static Task<int> Main(string[] arguments)
    {
        return MigrationCli.RunAsync(arguments);
    }
}
