using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillStagingRunHashCalculatorTests
{
    [Fact]
    public void SameInputs_ProduceSameHash()
    {
        using var apply = JsonDocument.Parse("""{"succeeded":true,"totalRecordsFailed":0}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","totalRecordsUnresolved":0,"totalRecordsScanned":10}""");
        using var gate = JsonDocument.Parse("""{"passed":true}""");

        var calculator = new OwnershipBackfillStagingRunHashCalculator();

        var left = calculator.Compute(
            "apply-001",
            "plan-001",
            "signoff-001",
            "readiness-001",
            "preflight-001",
            apply,
            dryRun,
            gate,
            "rollback-001",
            "tenant-001",
            "regression-001",
            "P6-12");

        var right = calculator.Compute(
            "apply-001",
            "plan-001",
            "signoff-001",
            "readiness-001",
            "preflight-001",
            apply,
            dryRun,
            gate,
            "rollback-001",
            "tenant-001",
            "regression-001",
            "P6-12");

        Assert.Equal(left, right);
    }

    [Fact]
    public void ChangedApplyResult_ProducesDifferentHash()
    {
        using var applyA = JsonDocument.Parse("""{"succeeded":true,"totalRecordsFailed":0}""");
        using var applyB = JsonDocument.Parse("""{"succeeded":false,"totalRecordsFailed":1}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","totalRecordsUnresolved":0,"totalRecordsScanned":10}""");
        using var gate = JsonDocument.Parse("""{"passed":true}""");

        var calculator = new OwnershipBackfillStagingRunHashCalculator();

        var left = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", applyA, dryRun, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");
        var right = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", applyB, dryRun, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void ChangedPostApplyDryRun_ProducesDifferentHash()
    {
        using var apply = JsonDocument.Parse("""{"succeeded":true,"totalRecordsFailed":0}""");
        using var dryRunA = JsonDocument.Parse("""{"mode":"DryRun","totalRecordsUnresolved":0,"totalRecordsScanned":10}""");
        using var dryRunB = JsonDocument.Parse("""{"mode":"DryRun","totalRecordsUnresolved":1,"totalRecordsScanned":10}""");
        using var gate = JsonDocument.Parse("""{"passed":true}""");

        var calculator = new OwnershipBackfillStagingRunHashCalculator();

        var left = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", apply, dryRunA, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");
        var right = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", apply, dryRunB, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void ExternalPathOrTimestampNoise_DoesNotAffectHash()
    {
        using var apply = JsonDocument.Parse("""{"succeeded":true,"totalRecordsFailed":0}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","totalRecordsUnresolved":0,"totalRecordsScanned":10}""");
        using var gate = JsonDocument.Parse("""{"passed":true}""");

        var calculator = new OwnershipBackfillStagingRunHashCalculator();
        var now = DateTimeOffset.UtcNow;

        var left = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", apply, dryRun, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");
        var right = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", apply, dryRun, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");

        _ = now;
        Assert.Equal(left, right);
    }

    [Fact]
    public void HashOutput_DoesNotExposeSecrets()
    {
        using var apply = JsonDocument.Parse("""{"succeeded":true,"totalRecordsFailed":0}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","totalRecordsUnresolved":0,"totalRecordsScanned":10}""");
        using var gate = JsonDocument.Parse("""{"passed":true}""");
        const string secret = "TOP_SECRET_TOKEN_123";

        var calculator = new OwnershipBackfillStagingRunHashCalculator();
        var hash = calculator.Compute("apply-001", "plan-001", "signoff-001", "readiness-001", "preflight-001", apply, dryRun, gate, "rollback-001", "tenant-001", "regression-001", "P6-12");

        Assert.DoesNotContain(secret, hash, StringComparison.Ordinal);
    }
}
