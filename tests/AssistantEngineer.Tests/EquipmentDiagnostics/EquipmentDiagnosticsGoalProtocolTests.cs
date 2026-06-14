namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsGoalProtocolTests
{
    private static readonly string[] RequiredDocuments =
    [
        "goal-protocol.md",
        "goal-run-template.md",
        "phase-spec-template.md",
        "final-audit-template.md",
        "goal-run-report-validator.md",
        "goal-run-report.schema.json"
    ];

    [Fact]
    public void GoalProtocolDocumentsExist()
    {
        foreach (var document in RequiredDocuments)
        {
            Assert.True(File.Exists(DocumentPath(document)), document);
        }
    }

    [Fact]
    public void GoalProtocolContainsRequiredStagesAndSafetyStatements()
    {
        var content = ReadDocument("goal-protocol.md");
        AssertContainsAll(content,
            "Intake",
            "Constraints",
            "Brownfield Recon",
            "Roadmap",
            "Adaptive Phases",
            "Preflight",
            "Phase Verification Evidence",
            "Recovery",
            "Final Audit",
            "Generated Artifacts Policy",
            "no runtime AI agent",
            "no RAG/vector search",
            "no Telegram command execution",
            "no production/public release claim",
            "Generated artifacts are not committed",
            "scripts/engineering-core/verify-engineering-core-v1.ps1");
    }

    [Fact]
    public void TemplatesContainRequiredFields()
    {
        AssertContainsAll(ReadDocument("goal-run-template.md"),
            "Goal id",
            "Title",
            "Source branch",
            "Target branch",
            "Scope",
            "Out of scope",
            "Constraints",
            "Preflight commands",
            "Phase List",
            "Verification Matrix",
            "Warnings",
            "Blockers",
            "Final Audit Result",
            "Generated Artifacts",
            "Handoff Notes");

        AssertContainsAll(ReadDocument("phase-spec-template.md"),
            "Deliverables",
            "Acceptance Criteria",
            "Mandatory Commands",
            "Evidence Required",
            "Forbidden Changes",
            "Completion Marker");

        AssertContainsAll(ReadDocument("final-audit-template.md"),
            "Roadmap Coverage",
            "Phase Completion",
            "Mandatory Commands",
            "Changed Files Review",
            "Forbidden Files",
            "Forbidden Claims",
            "Secrets Scan",
            "Generated Artifacts",
            "Merge Readiness");
    }

    [Fact]
    public void GoalProtocolDocumentsContainNoForbiddenClaims()
    {
        var forbiddenClaims = new[]
        {
            "production ready",
            "public release ready",
            "fully autonomous engineer",
            "autonomous production execution",
            "AI diagnosis",
            "RAG diagnosis",
            "vector search diagnosis",
            "full vendor manual coverage",
            string.Concat("full ", "parity"),
            "ManualVerified promotion"
        };

        foreach (var document in RequiredDocuments)
        {
            var content = ReadDocument(document);
            foreach (var claim in forbiddenClaims)
            {
                Assert.DoesNotContain(claim, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void GoalProtocolIsReferencedByOperationsEquipmentDiagnosticsAndInventory()
    {
        Assert.Contains("engineering-workflow/goal-protocol.md", ReadRepoFile("docs", "operations", "README.md"), StringComparison.Ordinal);
        Assert.Contains("engineering-workflow/goal-protocol.md", ReadRepoFile("docs", "equipment-diagnostics", "README.md"), StringComparison.Ordinal);
        Assert.Contains("docs/engineering-workflow/goal-protocol.md", ReadRepoFile("docs", "architecture", "scripts-tools-inventory.json"), StringComparison.Ordinal);
        Assert.Contains("documentationReferences", ReadRepoFile("docs", "architecture", "scripts-tools-inventory.schema.json"), StringComparison.Ordinal);
        Assert.Contains("goal-run-report-validator.md", ReadRepoFile("docs", "architecture", "scripts-tools-inventory.json"), StringComparison.Ordinal);
    }

    private static void AssertContainsAll(string content, params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReadDocument(string name) => File.ReadAllText(DocumentPath(name));

    private static string DocumentPath(string name) =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-workflow", name);

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine([TestPaths.RepoRoot, .. segments]));
}
