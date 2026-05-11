using AssistantEngineer.Api.Services.Calculations.Idempotency;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api;

public class EngineeringIdempotencyDurableServiceTests
{
    [Fact]
    public async Task DurableStore_FirstReservationSucceeds_SecondPendingConflicts()
    {
        await using var harness = await DurableHarness.CreateAsync();
        var service = harness.Service;
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "pending" });

        var first = await service.EvaluateAsync("key-pending", "scope:run:1", hash, CancellationToken.None);
        var second = await service.EvaluateAsync("key-pending", "scope:run:1", hash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, first.Kind);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Conflict, second.Kind);
        Assert.Equal("ENGINEERING_IDEMPOTENCY_IN_PROGRESS", second.ConflictCode);
    }

    [Fact]
    public async Task DurableStore_SameScopeKeyDifferentFingerprintConflicts()
    {
        await using var harness = await DurableHarness.CreateAsync();
        var service = harness.Service;

        var firstHash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "a" });
        var secondHash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "b" });

        var first = await service.EvaluateAsync("key-conflict", "scope:run:1", firstHash, CancellationToken.None);
        var second = await service.EvaluateAsync("key-conflict", "scope:run:1", secondHash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, first.Kind);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Conflict, second.Kind);
        Assert.Equal("ENGINEERING_IDEMPOTENCY_CONFLICT", second.ConflictCode);
    }

    [Fact]
    public async Task DurableStore_RecordSuccessProducesReplay()
    {
        await using var harness = await DurableHarness.CreateAsync();
        var service = harness.Service;
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "replay" });

        var first = await service.EvaluateAsync("key-replay", "scope:run:1", hash, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, first.Kind);

        await service.RecordSuccessAsync(
            "key-replay",
            "scope:run:1",
            hash,
            "{\"scenarioId\":\"scenario-replay\"}",
            "scenario-replay",
            CancellationToken.None);

        var replay = await service.EvaluateAsync("key-replay", "scope:run:1", hash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Replay, replay.Kind);
        Assert.Equal("scenario-replay", replay.ReplayPayload?.ResponseReferenceId);
    }

    [Fact]
    public async Task DurableStore_SameKeyDifferentScopeAllowed()
    {
        await using var harness = await DurableHarness.CreateAsync();
        var service = harness.Service;
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "scope" });

        var firstScope = await service.EvaluateAsync("key-scope", "scope:run:1", hash, CancellationToken.None);
        var secondScope = await service.EvaluateAsync("key-scope", "scope:jobs:1", hash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, firstScope.Kind);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, secondScope.Kind);
    }

    [Fact]
    public async Task DurableStore_ConcurrentSameKeyAllowsSingleProceed()
    {
        await using var harness = await DurableHarness.CreateAsync();
        var service = harness.Service;
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "race" });

        var firstTask = service.EvaluateAsync("key-race", "scope:run:1", hash, CancellationToken.None);
        var secondTask = service.EvaluateAsync("key-race", "scope:run:1", hash, CancellationToken.None);

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, results.Count(item => item.Kind == EngineeringIdempotencyEvaluationKind.Proceed));
        Assert.Equal(1, results.Count(item => item.Kind == EngineeringIdempotencyEvaluationKind.Conflict));
    }

    [Fact]
    public async Task DurableStore_ExpiredReservationAllowsProceedAgain()
    {
        await using var harness = await DurableHarness.CreateAsync();
        var service = harness.Service;
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "expired" });

        var first = await service.EvaluateAsync("key-expired", "scope:run:1", hash, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, first.Kind);

        await using (var scope = harness.ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>();
            await dbContext.IdempotencyRecords
                .Where(item => item.Scope == "scope:run:1" && item.IdempotencyKey == "key-expired")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.ExpiresAtUtc, DateTimeOffset.UtcNow.AddSeconds(-5)));
        }

        var second = await service.EvaluateAsync("key-expired", "scope:run:1", hash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, second.Kind);
    }

    [Fact]
    public async Task DurableStore_ReplaySurvivesServiceRestartWithSameDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-idempotency-{Guid.NewGuid():N}.db");
        try
        {
            await using (var first = await DurableHarness.CreateAsync(dbPath: dbPath))
            {
                var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "restart" });
                var proceed = await first.Service.EvaluateAsync("key-restart", "scope:run:1", hash, CancellationToken.None);
                Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, proceed.Kind);

                await first.Service.RecordSuccessAsync(
                    "key-restart",
                    "scope:run:1",
                    hash,
                    "{\"scenarioId\":\"scenario-restart\"}",
                    "scenario-restart",
                    CancellationToken.None);
            }

            await using var second = await DurableHarness.CreateAsync(dbPath: dbPath);
            var replayHash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "restart" });
            var replay = await second.Service.EvaluateAsync("key-restart", "scope:run:1", replayHash, CancellationToken.None);

            Assert.Equal(EngineeringIdempotencyEvaluationKind.Replay, replay.Kind);
            Assert.Equal("scenario-restart", replay.ReplayPayload?.ResponseReferenceId);
        }
        finally
        {
            TryDeleteSqliteFiles(dbPath);
        }
    }

    private static void TryDeleteSqliteFiles(string path)
    {
        TryDeleteFile(path);
        TryDeleteFile(path + "-shm");
        TryDeleteFile(path + "-wal");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // SQLite cleanup is best-effort in tests.
        }
    }

    private sealed class DurableHarness : IAsyncDisposable
    {
        private DurableHarness(ServiceProvider serviceProvider, string? dbPath)
        {
            ServiceProvider = serviceProvider;
            DbPath = dbPath;
            Service = ActivatorUtilities.CreateInstance<EfEngineeringIdempotencyService>(serviceProvider);
        }

        public ServiceProvider ServiceProvider { get; }

        public EfEngineeringIdempotencyService Service { get; }

        public string? DbPath { get; }

        public static async Task<DurableHarness> CreateAsync(
            EngineeringIdempotencyOptions? options = null,
            string? dbPath = null)
        {
            var effectivePath = dbPath ?? Path.Combine(Path.GetTempPath(), $"assistant-engineer-idempotency-{Guid.NewGuid():N}.db");
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IOptions<EngineeringIdempotencyOptions>>(Options.Create(options ?? new EngineeringIdempotencyOptions
            {
                Enabled = true,
                TtlMinutes = 60,
                MaxEntries = 1000,
                MaxCachedResponseBytes = 262144
            }));

            services.AddDbContext<EngineeringWorkflowPersistenceDbContext>(builder =>
                builder.UseSqlite($"Data Source={effectivePath};Cache=Shared;Mode=ReadWriteCreate"));

            var serviceProvider = services.BuildServiceProvider();

            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            return new DurableHarness(serviceProvider, effectivePath);
        }

        public async ValueTask DisposeAsync()
        {
            await ServiceProvider.DisposeAsync();
            if (!string.IsNullOrWhiteSpace(DbPath))
            {
                TryDeleteSqliteFiles(DbPath);
            }
        }
    }
}
