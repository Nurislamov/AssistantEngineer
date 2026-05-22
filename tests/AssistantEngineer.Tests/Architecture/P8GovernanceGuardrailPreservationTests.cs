using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8GovernanceGuardrailPreservationTests
{
    [Fact]
    public void CriticalGuardrailsRemainActiveAndReferenced()
    {
        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));

        var entries = guardrails.RootElement.GetProperty("guardrails").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);

        var byId = entries.ToDictionary(
            item => item.GetProperty("guardrailId").GetString() ?? string.Empty,
            item => item,
            StringComparer.Ordinal);

        var requiredGuardrails = new[]
        {
            "SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-DESIGN-DISABLED",
            "SEC-GUARD-FALSE-CLAIMS",
            "SEC-GUARD-SECURITY-GOVERNANCE-RELEASE-BOUNDARY",
            "SEC-GUARD-ROUTE-INVENTORY-CLAIMS-CONSISTENCY",
            "SEC-GUARD-MODULE-BOUNDARY-MATRIX",
            "SEC-GUARD-WORKFLOW-CONTROLLER-SHELL-CHARACTERIZATION",
            "SEC-GUARD-PROTECTED-ENDPOINT-AUTHORIZATION-GATE-CHARACTERIZATION",
            "SEC-GUARD-OWNERSHIP-BACKFILL-CLI-UX",
            "SEC-GUARD-OWNERSHIPBACKFILL-CLI-PARSER-SIMPLIFICATION",
            "SEC-GUARD-GOVERNANCE-TEST-BRITTLENESS-REDUCTION",
            "SEC-GUARD-P8-ENGINEERING-DOMAIN-CLOSURE"
        };

        foreach (var guardrailId in requiredGuardrails)
        {
            Assert.True(byId.TryGetValue(guardrailId, out var entry), $"Missing critical guardrail: {guardrailId}");
            Assert.Equal("Active", entry.GetProperty("status").GetString());
        }
    }

    [Fact]
    public void P8BrittlenessReductionReportConfirmsGuardrailsNotWeakened()
    {
        using var report = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "governance-test-brittleness-reduction.json"));

        Assert.False(report.RootElement.GetProperty("guardrailsWeakened").GetBoolean());
    }
}
