using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6StagingApplyExecutorDesignGovernanceTests
{
    [Fact]
    public void StagingExecutorDesignArtifactsExist()
    {
        Assert.True(File.Exists(DesignDocPath), $"Missing staging executor design doc: {DesignDocPath}");
        Assert.True(File.Exists(DesignJsonPath), $"Missing staging executor design json: {DesignJsonPath}");
        Assert.True(File.Exists(DesignSchemaPath), $"Missing staging executor design schema: {DesignSchemaPath}");
    }

    [Fact]
    public void StagingExecutorDesignJsonHasDisabledFlagsAndStagingEnvironment()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(DesignJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("stagingApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("productionApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());
        Assert.Equal("Staging", root.GetProperty("requiredEnvironment").GetString());
    }

    [Fact]
    public void StagingExecutorComponentsExistAndApplyRemainsDisabled()
    {
        Assert.True(File.Exists(StagingValidatorPath), $"Missing validator: {StagingValidatorPath}");
        Assert.True(File.Exists(DisabledExecutorPath), $"Missing disabled executor: {DisabledExecutorPath}");
        Assert.True(File.Exists(StagingInterfacePath), $"Missing staging executor interface: {StagingInterfacePath}");

        var cli = File.ReadAllText(CliPath);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoEnabledStagingApplyCommandExists()
    {
        var parser = File.ReadAllText(ParserPath);
        Assert.DoesNotContain("string.Equals(command, \"staging-apply\"", parser, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-staging-preflight", parser, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToolSourceContainsNoSaveChangesAndNoDestructiveSql()
    {
        var sourceFiles = Directory.GetFiles(ToolDirectoryPath, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(sourceFiles);

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("SaveChanges(", content, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChangesAsync(", content, StringComparison.Ordinal);
            Assert.DoesNotContain("UPDATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TRUNCATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("INSERT INTO", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InventoryAndGuardrailsContainP6_11References()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P6-11", items);

        using var guardrails = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guards = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-EXECUTOR-DESIGN", guards);
    }

    [Fact]
    public void DocsDoNotClaimStagingExecutionOrProductionEnablement()
    {
        var docs = new[]
        {
            File.ReadAllText(DesignDocPath),
            File.ReadAllText(InventoryMarkdownPath),
            File.ReadAllText(RunbookDocPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("staging apply has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");

    private static string ParserPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCommandLineParser.cs");

    private static string StagingValidatorPath =>
        Path.Combine(ToolDirectoryPath, "Staging", "OwnershipBackfillStagingApplyPreflightValidator.cs");

    private static string DisabledExecutorPath =>
        Path.Combine(ToolDirectoryPath, "Staging", "DisabledStagingOwnershipBackfillApplyExecutor.cs");

    private static string StagingInterfacePath =>
        Path.Combine(ToolDirectoryPath, "Staging", "IStagingOwnershipBackfillApplyExecutor.cs");

    private static string DesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-executor-design.md");

    private static string DesignJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-executor-design.json");

    private static string DesignSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-executor-design.schema.json");

    private static string RunbookDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-runbook.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");
}
