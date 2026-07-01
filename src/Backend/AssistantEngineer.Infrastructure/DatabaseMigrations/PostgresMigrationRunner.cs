namespace AssistantEngineer.Infrastructure.DatabaseMigrations;

public sealed class PostgresMigrationRunner
{
    private readonly IPostgresMigrationDatabase _database;
    private readonly IPostgresMigrationReporter _reporter;

    public PostgresMigrationRunner(
        IPostgresMigrationDatabase database,
        IPostgresMigrationReporter reporter)
    {
        _database = database;
        _reporter = reporter;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _reporter.Info("AssistantEngineer PostgreSQL migration runner started.");

            var pendingMigrations = await _database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Count == 0)
            {
                _reporter.Info("No pending migrations for AppDbContext.");
                await ReportFinalStatusAsync(cancellationToken);
                return 0;
            }

            _reporter.Info("Pending migrations:");
            foreach (var migration in pendingMigrations)
            {
                _reporter.Info($"- {migration}");
            }

            await _database.MigrateAsync(cancellationToken);

            _reporter.Info("Pending migrations applied successfully.");
            await ReportFinalStatusAsync(cancellationToken);
            return 0;
        }
        catch (Exception exception)
        {
            _reporter.Error($"PostgreSQL migration failed: {Sanitize(exception.Message)}");
            return 1;
        }
    }

    public static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "An error occurred.";
        }

        var sanitized = RedactConnectionStringSecret(value, "Password");
        sanitized = RedactConnectionStringSecret(sanitized, "Pwd");
        return sanitized.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    private async Task ReportFinalStatusAsync(CancellationToken cancellationToken)
    {
        var appliedMigrations = await _database.GetAppliedMigrationsAsync(cancellationToken);
        if (appliedMigrations.Count == 0)
        {
            _reporter.Info("Applied migrations: none.");
            return;
        }

        _reporter.Info($"Applied migrations: {appliedMigrations.Count}.");
        _reporter.Info($"Latest applied migration: {appliedMigrations[^1]}.");
    }

    private static string RedactConnectionStringSecret(string value, string key)
    {
        var marker = key + "=";
        var start = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        while (start >= 0)
        {
            var secretStart = start + marker.Length;
            var secretEnd = value.IndexOf(';', secretStart);
            if (secretEnd < 0)
            {
                secretEnd = value.Length;
            }

            value = string.Concat(value.AsSpan(0, secretStart), "[REDACTED]", value.AsSpan(secretEnd));
            start = value.IndexOf(marker, secretStart + "[REDACTED]".Length, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }
}
