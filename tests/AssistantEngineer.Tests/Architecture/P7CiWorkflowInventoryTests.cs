using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7CiWorkflowInventoryTests
{
    [Fact]
    public void WorkflowInventoryRemainsSafeForApplyDisabledBoundary()
    {
        var workflowDirectory = Path.Combine(TestPaths.RepoRoot, ".github", "workflows");
        var workflowFiles = Directory.Exists(workflowDirectory)
            ? Directory.GetFiles(workflowDirectory, "*.yml", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(workflowDirectory, "*.yaml", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        if (workflowFiles.Length == 0)
        {
            using var audit = GovernanceJsonTestHelper.Parse(
                GovernancePathHelper.SecurityDocPath("ci-github-checks-visibility-audit.json"));
            Assert.False(audit.RootElement.GetProperty("ciWorkflowPresent").GetBoolean());

            var limitations = GovernanceJsonTestHelper.StringSet(audit.RootElement.GetProperty("remainingLimitations"));
            Assert.Contains(limitations, item => item.Contains("status visibility", StringComparison.OrdinalIgnoreCase));
            return;
        }

        using var auditWhenPresent = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("ci-github-checks-visibility-audit.json"));
        Assert.True(auditWhenPresent.RootElement.GetProperty("ciWorkflowPresent").GetBoolean());

        var allContent = string.Join(
            Environment.NewLine,
            workflowFiles.Select(path => File.ReadAllText(path)));
        var lower = allContent.ToLowerInvariant();

        Assert.DoesNotContain("--enable-apply", lower, StringComparison.Ordinal);
        Assert.DoesNotContain("ownershipbackfill", lower, StringComparison.Ordinal);
        Assert.DoesNotContain("connection-string", lower, StringComparison.Ordinal);
        Assert.DoesNotContain("password=", lower, StringComparison.Ordinal);

        Assert.Contains("pull_request", lower, StringComparison.Ordinal);
        Assert.Contains("push", lower, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch", lower, StringComparison.Ordinal);

        var hasBuildOrTestStep = lower.Contains("dotnet build", StringComparison.Ordinal) ||
                                 lower.Contains("dotnet test", StringComparison.Ordinal);
        var hasReleaseReadyGate = lower.Contains("assert-engineering-core-v1-release-ready.ps1", StringComparison.Ordinal);

        if (!hasBuildOrTestStep)
        {
            var auditDoc = File.ReadAllText(GovernancePathHelper.SecurityDocPath("ci-github-checks-visibility-audit.md"));
            Assert.Contains("direct step or script-contained gate", auditDoc, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(hasReleaseReadyGate, "At least one workflow must expose release-ready gate visibility.");
    }
}
