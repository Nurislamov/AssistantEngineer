using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1CiWorkflowTests
{
    [Fact]
    public void EngineeringCoreV1WorkflowExists()
    {
        Assert.True(
            File.Exists(WorkflowPath),
            $"Engineering Core V1 workflow must exist: {WorkflowPath}");
    }

    [Fact]
    public void EngineeringCoreV1CiDocumentExists()
    {
        Assert.True(
            File.Exists(CiDocumentPath),
            $"Engineering Core V1 CI document must exist: {CiDocumentPath}");
    }

    [Fact]
    public void WorkflowRunsOnPullRequestPushAndManualDispatch()
    {
        var content = ReadWorkflow();

        Assert.Contains("pull_request:", content, StringComparison.Ordinal);
        Assert.Contains("push:", content, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowTargetsEngineeringCoreBranches()
    {
        var content = ReadWorkflow();

        var requiredBranches = new[]
        {
            "main",
            "master",
            "develop",
            "Energy_Calculation_Parity"
        };

        foreach (var branch in requiredBranches)
        {
            Assert.Contains(
                branch,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WorkflowSetsUpDotnetNodeAndFrontendDependencies()
    {
        var content = ReadWorkflow();

        Assert.Contains("actions/setup-dotnet@v4", content, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.x", content, StringComparison.Ordinal);

        Assert.Contains("actions/setup-node@v4", content, StringComparison.Ordinal);
        Assert.Contains("node-version: 22", content, StringComparison.Ordinal);

        Assert.Contains("npm ci --prefix .\\src\\Frontend", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowRunsEngineeringCoreVerificationScript()
    {
        var content = ReadWorkflow();

        Assert.Contains(
            ".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Verify Engineering Core V1",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowRestoresBackendSolution()
    {
        var content = ReadWorkflow();

        Assert.Contains(
            "dotnet restore .\\AssistantEngineer.sln",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CiDocumentExplainsWhatWorkflowVerifies()
    {
        var content = ReadCiDocument();

        var requiredPhrases = new[]
        {
            "formula audit matrix",
            "Engineering Core V1 status endpoint/facade",
            "report disclosures",
            "frontend visibility guards",
            "EPW/PVGIS 8760 weather gates",
            "annual true hourly 8760 gate",
            "EnergyPlus/ASHRAE 140 validation harness scaffold"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CiDocumentKeepsNonClaimsVisible()
    {
        var content = ReadCiDocument();

        var requiredNonClaims = new[]
        {
            "exact EnergyPlus numerical parity",
            "exact pyBuildingEnergy numerical parity",
            "ASHRAE 140 validation coverage",
            "full ISO 52016 node/matrix solver parity"
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(
                requiredNonClaim,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CiDocumentReferencesLocalVerificationCommand()
    {
        var content = ReadCiDocument();

        Assert.Contains(
            ".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "-Fast",
            content,
            StringComparison.Ordinal);
    }

    private static string WorkflowPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            ".github",
            "workflows",
            "engineering-core-v1.yml");

    private static string CiDocumentPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "ci",
            "EngineeringCoreV1CI.md");

    private static string ReadWorkflow() =>
        File.ReadAllText(WorkflowPath);

    private static string ReadCiDocument() =>
        File.ReadAllText(CiDocumentPath);
}
