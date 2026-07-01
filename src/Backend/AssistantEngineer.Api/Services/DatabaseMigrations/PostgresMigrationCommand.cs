using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Infrastructure.DatabaseMigrations;
using Microsoft.Extensions.Hosting;

namespace AssistantEngineer.Api.Services.DatabaseMigrations;

internal static class PostgresMigrationCommand
{
    public const string Argument = "--migrate-database";

    public static bool IsMigrationCommand(IReadOnlyList<string> args) =>
        args.Any(argument => string.Equals(argument, Argument, StringComparison.Ordinal));

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter? output = null,
        TextWriter? error = null,
        CancellationToken cancellationToken = default)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        try
        {
            var builder = Host.CreateApplicationBuilder(RemoveMigrationArgument(args));
            builder.Configuration.AddApiConfiguration();
            builder.Logging.ClearProviders();
            builder.Services.AddPostgresMigrationRunner(
                builder.Configuration,
                builder.Environment.EnvironmentName,
                output,
                error);

            using var host = builder.Build();
            using var scope = host.Services.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<PostgresMigrationRunner>();
            return await runner.RunAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            error.WriteLine($"PostgreSQL migration command failed: {PostgresMigrationRunner.Sanitize(exception.Message)}");
            return 1;
        }
    }

    private static string[] RemoveMigrationArgument(IEnumerable<string> args) =>
        args
            .Where(argument => !string.Equals(argument, Argument, StringComparison.Ordinal))
            .ToArray();
}
