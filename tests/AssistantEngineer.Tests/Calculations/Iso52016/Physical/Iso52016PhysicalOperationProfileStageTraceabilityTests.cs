using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalOperationProfileStageTraceabilityTests
{
    [Fact]
    public void Manifest_DocumentsOperationProfileStageAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016PhysicalOperationProfileStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-04", root.GetProperty("stageId").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();

        Assert.Contains("ISO52016-inspired physical operation profile stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not StandardReference numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
    }

    [Fact]
    public void VerificationRegistry_GuardsOperationProfileFilesAndTests()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-04",
            "requiredSourceFiles",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Physical/Iso52016PhysicalHourlyOperationCondition.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-04",
            "requiredSourceFiles",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Matrix/Iso52016MatrixHourlyBoundaryConductanceOverride.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-04",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalOperationProfileStageManifest.json");
        RegistryContainsTestFilter("AE-ISO52016-002-STEP-04", "FullyQualifiedName~Iso52016PhysicalOperationProfile");
    }

    [Fact]
    public void VerificationRegistry_ReferencesOperationProfileStageWrapper()
    {
        RegistryContainsAlias(
            "AE-ISO52016-002-STEP-04",
            "scripts/iso52016/verify-iso52016-physical-operation-profile-stage.ps1");
    }

    [Fact]
    public void StageDocument_StatesNoExternalExactMatchClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "calculations", "Iso52016PhysicalOperationProfileStage.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("internal engineering anchors only", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not StandardReference numerical equivalence", doc);
        Assert.Contains("not EnergyPlus numerical equivalence", doc);
        Assert.Contains("not ASHRAE Standard 140 benchmark-grade claim", doc);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(directory.FullName, "src", "Backend", "AssistantEngineer.Modules.Calculations");
            var tests = Path.Combine(directory.FullName, "tests", "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate AssistantEngineer repository root from test base directory.");
    }
}
