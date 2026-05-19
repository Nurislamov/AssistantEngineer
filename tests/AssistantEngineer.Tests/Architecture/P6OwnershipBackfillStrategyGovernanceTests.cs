using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillStrategyGovernanceTests
{
    [Fact]
    public void OwnershipBackfillStrategyArtifactsExist()
    {
        Assert.True(File.Exists(StrategyMarkdownPath), $"Missing ownership backfill strategy document: {StrategyMarkdownPath}");
        Assert.True(File.Exists(StrategyJsonPath), $"Missing ownership backfill strategy JSON: {StrategyJsonPath}");
        Assert.True(File.Exists(StrategySchemaPath), $"Missing ownership backfill strategy schema: {StrategySchemaPath}");
        Assert.True(File.Exists(EvidenceModelPath), $"Missing ownership backfill evidence model: {EvidenceModelPath}");
        Assert.True(File.Exists(EvidenceSchemaPath), $"Missing ownership backfill evidence schema: {EvidenceSchemaPath}");
    }

    [Fact]
    public void StrategyDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(StrategyMarkdownPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Backfill principles",
            "## Source-of-truth hierarchy",
            "## Dry-run metrics",
            "## Dry-run output model",
            "## Batch plan",
            "## Safety checks",
            "## Rollback notes",
            "## Governance gates",
            "## Future steps"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EvidenceModelDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(EvidenceModelPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Evidence artifact types",
            "## Dry-run summary schema",
            "## Apply summary schema",
            "## Unresolved records schema",
            "## Previous-values snapshot schema",
            "## Storage/retention guidance",
            "## Redaction policy",
            "## Review checklist",
            "## Future automation"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RequiredNonClaimsArePresent()
    {
        var strategy = File.ReadAllText(StrategyMarkdownPath);
        var evidence = File.ReadAllText(EvidenceModelPath);

        var requiredStrategyNonClaims = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No database row-level security claim",
            "No global EF query filter claim",
            "No ownership backfill execution claim",
            "No external identity provider integration claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredStrategyNonClaims)
        {
            Assert.Contains(phrase, strategy, StringComparison.OrdinalIgnoreCase);
        }

        var requiredEvidenceNonClaims = new[]
        {
            "No ownership backfill execution claim",
            "No full multi-tenant isolation claim yet",
            "No database row-level security claim",
            "No global EF query filter claim",
            "No production security certification claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredEvidenceNonClaims)
        {
            Assert.Contains(phrase, evidence, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void StrategyJsonParsesAndIsStrategyOnlyWithoutExecution()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(StrategyJsonPath));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("P6-00", root.GetProperty("stage").GetString());
        Assert.Equal("StrategyOnly", root.GetProperty("mode").GetString());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());

        Assert.True(root.GetProperty("sourceOfTruthHierarchy").GetArrayLength() > 0);
        Assert.True(root.GetProperty("dryRunMetrics").GetArrayLength() > 0);
        Assert.True(root.GetProperty("safetyChecks").GetArrayLength() > 0);
        Assert.True(root.GetProperty("governanceGates").GetArrayLength() > 0);
    }

    [Fact]
    public void EvidenceSchemaContainsRequiredDefinitions()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EvidenceSchemaPath));
        var root = document.RootElement;
        JsonElement definitions;

        if (root.TryGetProperty("definitions", out var explicitDefinitions))
        {
            definitions = explicitDefinitions;
        }
        else if (root.TryGetProperty("$defs", out var defs))
        {
            definitions = defs;
        }
        else
        {
            definitions = root.GetProperty("properties")
                .GetProperty("definitions")
                .GetProperty("properties");
        }

        Assert.True(definitions.TryGetProperty("dryRunSummary", out _));
        Assert.True(definitions.TryGetProperty("applySummary", out _));
        Assert.True(definitions.TryGetProperty("unresolvedRecord", out _));
        Assert.True(definitions.TryGetProperty("previousValueSnapshot", out _));
    }

    [Fact]
    public void ProductionInventoryContainsP6_00RoadmapItem()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-00", roadmapItems);
    }

    [Fact]
    public void SecurityGuardrailsReferenceP6BackfillStrategyGuard()
    {
        var markdown = File.ReadAllText(SecurityGuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-STRATEGY-EVIDENCE", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(SecurityGuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-STRATEGY-EVIDENCE", guardIds);
    }

    [Fact]
    public void FutureBackfillArtifactsAreIgnoredAndStrategyDocsAreNotIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);

        Assert.Contains("artifacts/ownership-backfill/", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-dry-run-summary", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-apply-summary", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-unresolved-records", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-previous-values", gitignore, StringComparison.Ordinal);

        Assert.DoesNotContain("docs/security/ownership-backfill-strategy.json", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docs/security/ownership-backfill-evidence.schema.json", gitignore, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocsDoNotClaimBackfillExecutedOrFullIsolationOrGlobalFiltersOrDbRls()
    {
        var docs = new[]
        {
            File.ReadAllText(StrategyMarkdownPath),
            File.ReadAllText(EvidenceModelPath),
            File.ReadAllText(ProductionInventoryMarkdownPath),
            File.ReadAllText(TenantMatrixMarkdownPath),
            File.ReadAllText(AuthorizationRolloutMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been completed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill is fully completed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string StrategyMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.md");

    private static string StrategyJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.json");

    private static string StrategySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.schema.json");

    private static string EvidenceModelPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-evidence-model.md");

    private static string EvidenceSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-evidence.schema.json");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string TenantMatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string AuthorizationRolloutMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string SecurityGuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string SecurityGuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
