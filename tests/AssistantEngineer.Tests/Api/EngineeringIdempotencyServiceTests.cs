using AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api;

public class EngineeringIdempotencyServiceTests
{
    [Fact]
    public async Task EvaluateAndRecordSuccessReplaySameScopeAndPayload()
    {
        var service = CreateService();
        var request = new { scenarioId = "scenario-1", projectId = 1, mode = "ExecuteAvailableModules" };
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(request);

        var first = await service.EvaluateAsync("key-1", "scope:run:1", hash, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, first.Kind);

        await service.RecordSuccessAsync("key-1", "scope:run:1", hash, "{\"scenarioId\":\"scenario-1\"}", "scenario-1", CancellationToken.None);

        var second = await service.EvaluateAsync("key-1", "scope:run:1", hash, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Replay, second.Kind);
        Assert.Equal("scenario-1", second.ReplayPayload?.ResponseReferenceId);
    }

    [Fact]
    public async Task EvaluateReturnsConflictForSameScopeAndDifferentPayloadFingerprint()
    {
        var service = CreateService();
        var hashA = EngineeringIdempotencyRequestFingerprint.Compute(new { scenarioId = "a", projectId = 1 });
        var hashB = EngineeringIdempotencyRequestFingerprint.Compute(new { scenarioId = "b", projectId = 1 });

        var first = await service.EvaluateAsync("key-2", "scope:run:1", hashA, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, first.Kind);

        var second = await service.EvaluateAsync("key-2", "scope:run:1", hashB, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Conflict, second.Kind);
        Assert.Equal("ENGINEERING_IDEMPOTENCY_CONFLICT", second.ConflictCode);
    }

    [Fact]
    public async Task EvaluateUsesScopeBoundaryForSameKey()
    {
        var service = CreateService();
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { scenarioId = "same", projectId = 1 });

        var firstScope = await service.EvaluateAsync("key-scope", "scope:run:1", hash, CancellationToken.None);
        var secondScope = await service.EvaluateAsync("key-scope", "scope:jobs:1", hash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, firstScope.Kind);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, secondScope.Kind);
    }

    [Fact]
    public async Task MaxEntriesEvictsOldestEntry()
    {
        var service = CreateService(new EngineeringIdempotencyOptions
        {
            Enabled = true,
            TtlMinutes = 1440,
            MaxEntries = 1,
            MaxCachedResponseBytes = 1024
        });

        var hashA = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "a" });
        var hashB = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "b" });

        Assert.Equal(
            EngineeringIdempotencyEvaluationKind.Proceed,
            (await service.EvaluateAsync("key-a", "scope:1", hashA, CancellationToken.None)).Kind);
        await service.RecordSuccessAsync("key-a", "scope:1", hashA, "{\"id\":\"a\"}", "a", CancellationToken.None);

        Assert.Equal(
            EngineeringIdempotencyEvaluationKind.Proceed,
            (await service.EvaluateAsync("key-b", "scope:2", hashB, CancellationToken.None)).Kind);
        await service.RecordSuccessAsync("key-b", "scope:2", hashB, "{\"id\":\"b\"}", "b", CancellationToken.None);

        var replayA = await service.EvaluateAsync("key-a", "scope:1", hashA, CancellationToken.None);
        Assert.Equal(EngineeringIdempotencyEvaluationKind.Proceed, replayA.Kind);
    }

    [Fact]
    public async Task EvaluateBypassesWhenKeyMissing()
    {
        var service = CreateService();
        var hash = EngineeringIdempotencyRequestFingerprint.Compute(new { id = "no-key" });

        var result = await service.EvaluateAsync(null, "scope:run:1", hash, CancellationToken.None);

        Assert.Equal(EngineeringIdempotencyEvaluationKind.Bypass, result.Kind);
    }

    private static InMemoryEngineeringIdempotencyService CreateService(EngineeringIdempotencyOptions? options = null)
    {
        return new InMemoryEngineeringIdempotencyService(
            Options.Create(options ?? new EngineeringIdempotencyOptions
            {
                Enabled = true,
                TtlMinutes = 60,
                MaxEntries = 100,
                MaxCachedResponseBytes = 8192
            }),
            NullLogger<InMemoryEngineeringIdempotencyService>.Instance);
    }
}
