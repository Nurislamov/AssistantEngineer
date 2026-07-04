using AssistantEngineer.Api.Services.DatabaseMigrations;
using AssistantEngineer.Infrastructure.DatabaseMigrations;
using AssistantEngineer.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Persistence;

public sealed class PostgresMigrationRunnerTests
{
    [Fact]
    public void MigrationCommandRecognizesOnlyExplicitArgument()
    {
        Assert.True(PostgresMigrationCommand.IsMigrationCommand(["--migrate-database"]));
        Assert.True(PostgresMigrationCommand.IsMigrationCommand(["--environment", "Production", "--migrate-database"]));
        Assert.False(PostgresMigrationCommand.IsMigrationCommand([]));
        Assert.False(PostgresMigrationCommand.IsMigrationCommand(["migrate-database"]));
    }

    [Fact]
    public async Task RunnerReturnsSuccessWhenNoPendingMigrations()
    {
        var database = new FakeMigrationDatabase(
            appliedMigrations: ["20260630195828_AddTelegramBroadcasts"],
            pendingMigrations: []);
        var reporter = new CapturingReporter();
        var runner = new PostgresMigrationRunner(database, reporter);

        var exitCode = await runner.RunAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal(0, database.MigrateCallCount);
        Assert.Contains(reporter.InfoMessages, message => message == "No pending migrations for AppDbContext.");
        Assert.Contains(reporter.InfoMessages, message => message == "Latest applied migration: 20260630195828_AddTelegramBroadcasts.");
        Assert.Empty(reporter.ErrorMessages);
    }

    [Fact]
    public async Task RunnerAppliesPendingMigrationsOnceAndReportsFinalStatus()
    {
        var database = new FakeMigrationDatabase(
            appliedMigrations: ["20260629193430_AddTelegramOperatorInbox"],
            pendingMigrations: ["20260630195828_AddTelegramBroadcasts"],
            appliedMigrationsAfterMigrate: ["20260629193430_AddTelegramOperatorInbox", "20260630195828_AddTelegramBroadcasts"]);
        var reporter = new CapturingReporter();
        var runner = new PostgresMigrationRunner(database, reporter);

        var exitCode = await runner.RunAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal(1, database.MigrateCallCount);
        Assert.Contains(reporter.InfoMessages, message => message == "- 20260630195828_AddTelegramBroadcasts");
        Assert.Contains(reporter.InfoMessages, message => message == "Pending migrations applied successfully.");
        Assert.Contains(reporter.InfoMessages, message => message == "Latest applied migration: 20260630195828_AddTelegramBroadcasts.");
        Assert.Empty(reporter.ErrorMessages);
    }

    [Fact]
    public async Task RunnerReturnsFailureAndRedactsPasswordLikeValues()
    {
        var database = new FakeMigrationDatabase(
            appliedMigrations: [],
            pendingMigrations: ["20260630195828_AddTelegramBroadcasts"],
            migrateException: new InvalidOperationException(
                "Connection failed;Host=db;Password=secret-value;Username=app;Pwd=short-secret"));
        var reporter = new CapturingReporter();
        var runner = new PostgresMigrationRunner(database, reporter);

        var exitCode = await runner.RunAsync();

        Assert.Equal(1, exitCode);
        var error = Assert.Single(reporter.ErrorMessages);
        Assert.Contains("Password=[REDACTED]", error, StringComparison.Ordinal);
        Assert.Contains("Pwd=[REDACTED]", error, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-value", error, StringComparison.Ordinal);
        Assert.DoesNotContain("short-secret", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AppDbContextMigrationRegistrationRequiresDefaultConnectionString()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAppDbContextPersistence(configuration, "Production"));

        Assert.Equal("Connection string 'DefaultConnection' is not configured.", exception.Message);
    }

    [Fact]
    public void MigrationOnlyCommandDoesNotRegisterNormalRuntimeOrHostedServices()
    {
        var commandPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "DatabaseMigrations",
            "PostgresMigrationCommand.cs");
        var hostPath = Path.Combine(TestPaths.ApiProjectPath, "AssistantEngineerApiHost.cs");
        var commandSource = File.ReadAllText(commandPath);
        var hostSource = File.ReadAllText(hostPath);

        Assert.Contains("PostgresMigrationCommand.IsMigrationCommand(args)", hostSource, StringComparison.Ordinal);
        Assert.Contains("return await PostgresMigrationCommand.RunAsync(args);", hostSource, StringComparison.Ordinal);
        Assert.True(
            hostSource.IndexOf("PostgresMigrationCommand.IsMigrationCommand(args)", StringComparison.Ordinal) <
            hostSource.IndexOf("WebApplication.CreateBuilder(args)", StringComparison.Ordinal));
        Assert.DoesNotContain("AddAssistantEngineerModules", commandSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHostedService", commandSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".Run()", commandSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionMigrationScriptUsesExplicitApiContainerCommandOnly()
    {
        var scriptPath = Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "deployment",
            "apply-production-migrations.sh");
        var source = File.ReadAllText(scriptPath);

        Assert.Contains("set -euo pipefail", source, StringComparison.Ordinal);
        Assert.Contains(
            "docker compose run --rm assistantengineer-api dotnet AssistantEngineer.Api.dll --migrate-database",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain("docker compose up", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionStrings__DefaultConnection", source, StringComparison.Ordinal);
    }

    private sealed class FakeMigrationDatabase : IPostgresMigrationDatabase
    {
        private readonly IReadOnlyList<string> _appliedMigrations;
        private readonly IReadOnlyList<string> _pendingMigrations;
        private readonly IReadOnlyList<string>? _appliedMigrationsAfterMigrate;
        private readonly Exception? _migrateException;

        public FakeMigrationDatabase(
            IReadOnlyList<string> appliedMigrations,
            IReadOnlyList<string> pendingMigrations,
            IReadOnlyList<string>? appliedMigrationsAfterMigrate = null,
            Exception? migrateException = null)
        {
            _appliedMigrations = appliedMigrations;
            _pendingMigrations = pendingMigrations;
            _appliedMigrationsAfterMigrate = appliedMigrationsAfterMigrate;
            _migrateException = migrateException;
        }

        public int MigrateCallCount { get; private set; }

        public Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(MigrateCallCount > 0 && _appliedMigrationsAfterMigrate is not null
                ? _appliedMigrationsAfterMigrate
                : _appliedMigrations);

        public Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_pendingMigrations);

        public Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            MigrateCallCount++;
            if (_migrateException is not null)
            {
                throw _migrateException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingReporter : IPostgresMigrationReporter
    {
        public List<string> InfoMessages { get; } = [];
        public List<string> ErrorMessages { get; } = [];

        public void Info(string message) => InfoMessages.Add(message);

        public void Error(string message) => ErrorMessages.Add(message);
    }
}
