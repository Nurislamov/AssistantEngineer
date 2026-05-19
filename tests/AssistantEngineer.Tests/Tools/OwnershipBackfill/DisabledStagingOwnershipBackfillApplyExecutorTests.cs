using AssistantEngineer.Tools.OwnershipBackfill.Staging;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class DisabledStagingOwnershipBackfillApplyExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_AlwaysReturnsExecutedFalse()
    {
        var executor = new DisabledStagingOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(new OwnershipBackfillStagingApplyPreflightOptions
        {
            EnvironmentName = "Staging",
            ApplyInputHash = "hash",
            ReadinessResultPath = "r.json",
            PlanPath = "p.json",
            SignoffPath = "s.json",
            BackupReference = "backup",
            RollbackReadinessReference = "rollback",
            OperatorId = "operator",
            SchemaVersion = "v1",
            EnableStagingApply = true,
            ConfirmNoProductionConnection = true
        });

        Assert.False(result.Executed);
        Assert.Contains("disabled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.NonClaims);
    }

    [Fact]
    public async Task ExecuteAsync_InProductionStillNotExecuted()
    {
        var executor = new DisabledStagingOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(new OwnershipBackfillStagingApplyPreflightOptions
        {
            EnvironmentName = "Production",
            ApplyInputHash = "hash",
            ReadinessResultPath = "r.json",
            PlanPath = "p.json",
            SignoffPath = "s.json",
            BackupReference = "backup",
            RollbackReadinessReference = "rollback",
            OperatorId = "operator",
            SchemaVersion = "v1",
            EnableStagingApply = true,
            ConfirmNoProductionConnection = true
        });

        Assert.False(result.Executed);
        Assert.Contains("No ownership backfill execution claim.", result.NonClaims, StringComparer.Ordinal);
    }
}
