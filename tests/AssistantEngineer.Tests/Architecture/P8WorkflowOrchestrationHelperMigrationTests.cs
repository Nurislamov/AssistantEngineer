using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8WorkflowOrchestrationHelperMigrationTests
{
    [Fact]
    public void MigrationArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            MigrationMarkdownPath,
            MigrationJsonPath,
            MigrationSchemaPath);
    }

    [Fact]
    public void MigrationJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(MigrationJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("workflowBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());
        Assert.False(root.GetProperty("dtoShapesChanged").GetBoolean());
        Assert.False(root.GetProperty("authorizationSemanticsChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());
    }

    [Fact]
    public void MigrationJsonListsExpectedHelpers()
    {
        using var document = GovernanceJsonTestHelper.Parse(MigrationJsonPath);
        var helpers = document.RootElement.GetProperty("helpersMigrated")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Workflow/EngineeringWorkflowDiagnosticsService.cs", helpers);
        Assert.Contains("src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Workflow/EngineeringWorkflowStateBuilder.cs", helpers);
        Assert.Contains("src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Workflow/EngineeringWorkflowSubmissionService.cs", helpers);
    }

    [Fact]
    public void MigrationJsonContainsNonClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(MigrationJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, value => value.Contains("No workflow behavior change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No public API route change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DTO shape change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No authorization behavior change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No calculation physics change claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void P8_01AllowlistRemovesMigratedEntriesAndKeepsDeferredStagesExplicit()
    {
        using var allowlist = GovernanceJsonTestHelper.Parse(AllowlistPath);
        var entries = allowlist.RootElement.EnumerateArray().ToArray();

        var paths = entries
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var removed = new[]
        {
            "src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/EngineeringWorkflowDiagnosticsService.cs",
            "src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/EngineeringWorkflowStateBuilder.cs",
            "src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/EngineeringWorkflowSubmissionService.cs",
            "src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/IEngineeringWorkflowDiagnosticsService.cs",
            "src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/IEngineeringWorkflowStateBuilder.cs",
            "src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/IEngineeringWorkflowSubmissionService.cs"
        };

        foreach (var removedPath in removed)
            Assert.DoesNotContain(removedPath, paths);

        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("reason").GetString()));
            Assert.Equal("P8-03F", entry.GetProperty("proposedStage").GetString());
        }
    }

    [Fact]
    public void AuditMarksWorkflowControllerFindingAsInProgressForP8_03EOrP8_03F()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F08", StringComparison.Ordinal));

        Assert.Contains(
            finding.GetProperty("resolutionStatus").GetString(),
            new[] { "InProgress", "PartiallyAddressed", "Characterized" });
        Assert.Contains(finding.GetProperty("resolutionStage").GetString(), new[] { "P8-03E", "P8-03F" });
    }

    private static string MigrationMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-orchestration-helper-migration.md");

    private static string MigrationJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-orchestration-helper-migration.json");

    private static string MigrationSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-orchestration-helper-migration.schema.json");

    private static string AllowlistPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "architecture", "engineeringworkflow-boundary-allowlist.json");
}
