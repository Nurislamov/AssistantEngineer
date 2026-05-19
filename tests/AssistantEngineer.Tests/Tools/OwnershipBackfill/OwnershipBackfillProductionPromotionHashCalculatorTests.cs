using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Production;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillProductionPromotionHashCalculatorTests
{
    [Fact]
    public void SameArtifacts_ProduceSameHash()
    {
        using var staging = JsonDocument.Parse("""{"accepted":true,"stagingRunHash":"s1"}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","runId":"r1"}""");
        using var gate = JsonDocument.Parse("""{"passed":true,"runId":"g1"}""");
        using var plan = JsonDocument.Parse("""{"planHash":"p1","summaryDraft":{"mode":"PlanOnly"}}""");
        using var signoff = JsonDocument.Parse("""{"planHash":"p1","signoffId":"so1"}""");
        using var readiness = JsonDocument.Parse("""{"passed":true,"applyInputHash":"a1"}""");
        using var previous = JsonDocument.Parse("""[{"recordType":"Project","recordId":"1"}]""");

        var calculator = new OwnershipBackfillProductionPromotionHashCalculator();

        var left = calculator.Compute(staging, dryRun, gate, plan, signoff, readiness, previous, "CHG-1", "P6-13");
        var right = calculator.Compute(staging, dryRun, gate, plan, signoff, readiness, previous, "CHG-1", "P6-13");

        Assert.Equal(left, right);
    }

    [Fact]
    public void ChangedProductionPlan_ChangesHash()
    {
        using var staging = JsonDocument.Parse("""{"accepted":true,"stagingRunHash":"s1"}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","runId":"r1"}""");
        using var gate = JsonDocument.Parse("""{"passed":true,"runId":"g1"}""");
        using var planA = JsonDocument.Parse("""{"planHash":"p1","summaryDraft":{"mode":"PlanOnly"}}""");
        using var planB = JsonDocument.Parse("""{"planHash":"p2","summaryDraft":{"mode":"PlanOnly"}}""");
        using var signoff = JsonDocument.Parse("""{"planHash":"p1","signoffId":"so1"}""");
        using var readiness = JsonDocument.Parse("""{"passed":true,"applyInputHash":"a1"}""");
        using var previous = JsonDocument.Parse("""[{"recordType":"Project","recordId":"1"}]""");

        var calculator = new OwnershipBackfillProductionPromotionHashCalculator();

        var left = calculator.Compute(staging, dryRun, gate, planA, signoff, readiness, previous, "CHG-1", "P6-13");
        var right = calculator.Compute(staging, dryRun, gate, planB, signoff, readiness, previous, "CHG-1", "P6-13");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void ChangedStagingAcceptance_ChangesHash()
    {
        using var stagingA = JsonDocument.Parse("""{"accepted":true,"stagingRunHash":"s1"}""");
        using var stagingB = JsonDocument.Parse("""{"accepted":true,"stagingRunHash":"s2"}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","runId":"r1"}""");
        using var gate = JsonDocument.Parse("""{"passed":true,"runId":"g1"}""");
        using var plan = JsonDocument.Parse("""{"planHash":"p1","summaryDraft":{"mode":"PlanOnly"}}""");
        using var signoff = JsonDocument.Parse("""{"planHash":"p1","signoffId":"so1"}""");
        using var readiness = JsonDocument.Parse("""{"passed":true,"applyInputHash":"a1"}""");
        using var previous = JsonDocument.Parse("""[{"recordType":"Project","recordId":"1"}]""");

        var calculator = new OwnershipBackfillProductionPromotionHashCalculator();

        var left = calculator.Compute(stagingA, dryRun, gate, plan, signoff, readiness, previous, "CHG-1", "P6-13");
        var right = calculator.Compute(stagingB, dryRun, gate, plan, signoff, readiness, previous, "CHG-1", "P6-13");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void ChangedChangeRequestId_ChangesHash()
    {
        using var staging = JsonDocument.Parse("""{"accepted":true,"stagingRunHash":"s1"}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","runId":"r1"}""");
        using var gate = JsonDocument.Parse("""{"passed":true,"runId":"g1"}""");
        using var plan = JsonDocument.Parse("""{"planHash":"p1","summaryDraft":{"mode":"PlanOnly"}}""");
        using var signoff = JsonDocument.Parse("""{"planHash":"p1","signoffId":"so1"}""");
        using var readiness = JsonDocument.Parse("""{"passed":true,"applyInputHash":"a1"}""");
        using var previous = JsonDocument.Parse("""[{"recordType":"Project","recordId":"1"}]""");

        var calculator = new OwnershipBackfillProductionPromotionHashCalculator();

        var left = calculator.Compute(staging, dryRun, gate, plan, signoff, readiness, previous, "CHG-1", "P6-13");
        var right = calculator.Compute(staging, dryRun, gate, plan, signoff, readiness, previous, "CHG-2", "P6-13");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Hash_DoesNotExposeSecrets()
    {
        using var staging = JsonDocument.Parse("""{"accepted":true,"stagingRunHash":"s1"}""");
        using var dryRun = JsonDocument.Parse("""{"mode":"DryRun","runId":"r1"}""");
        using var gate = JsonDocument.Parse("""{"passed":true,"runId":"g1"}""");
        using var plan = JsonDocument.Parse("""{"planHash":"p1","summaryDraft":{"mode":"PlanOnly"}}""");
        using var signoff = JsonDocument.Parse("""{"planHash":"p1","signoffId":"so1"}""");
        using var readiness = JsonDocument.Parse("""{"passed":true,"applyInputHash":"a1"}""");
        using var previous = JsonDocument.Parse("""[{"recordType":"Project","recordId":"1"}]""");

        var calculator = new OwnershipBackfillProductionPromotionHashCalculator();
        var hash = calculator.Compute(staging, dryRun, gate, plan, signoff, readiness, previous, "CHG-1", "P6-13");

        Assert.DoesNotContain("password", hash, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", hash, StringComparison.OrdinalIgnoreCase);
    }
}
