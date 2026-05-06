using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalNodeModelStageTraceabilityTests
{
    [Fact]
    public void Manifest_DocumentsPhysicalNodeModelStageAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalNodeModelStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-01", root.GetProperty("stageId").GetString());

        var closedWorkItems = root
            .GetProperty("closedWorkItems")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("AE-ISO52016-002", closedWorkItems);

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired physical node model builder stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());
    }

    [Fact]
    public void VerificationRegistry_GuardsRequiredFilesAndRunsPhysicalNodeModelTests()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-01",
            "requiredSourceFiles",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Physical/Iso52016PhysicalRoomModelBuilder.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-01",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalNodeModelStageManifest.json");
        RegistryContainsTestFilter("AE-ISO52016-002-STEP-01", "FullyQualifiedName~Iso52016PhysicalRoomModelBuilder");
    }

    [Fact]
    public void VerificationRegistry_ReferencesPhysicalNodeModelStageWrapper()
    {
        RegistryContainsAlias(
            "AE-ISO52016-002-STEP-01",
            "scripts/iso52016/verify-iso52016-physical-node-model-stage.ps1");
    }

    [Fact]
    public void StageDocumentation_UsesGuardedValidationAnchorLanguage()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalNodeModelStage.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("AE-ISO52016-002", doc);
        Assert.Contains("ISO52016-inspired physical node model", doc);
        Assert.Contains("validation/internal engineering anchors only", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy numerical equivalence", doc);
        Assert.Contains("not EnergyPlus numerical equivalence", doc);
        Assert.Contains("not ASHRAE Standard 140 benchmark-grade claim", doc);

        Assert.DoesNotContain("complete numerical equivalence achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnergyPlus numerical equivalence achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pyBuildingEnergy numerical equivalence achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ASHRAE Standard 140 benchmark-grade claim passed", doc, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}
