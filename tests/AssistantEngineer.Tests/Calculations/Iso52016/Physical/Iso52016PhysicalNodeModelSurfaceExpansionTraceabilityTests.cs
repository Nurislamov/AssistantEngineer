using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalNodeModelSurfaceExpansionTraceabilityTests
{
    [Fact]
    public void Manifest_DocumentsSurfaceExpansionAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalSurfaceModelExpansionManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-02", root.GetProperty("stageId").GetString());

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

        Assert.Contains("ISO52016-inspired physical surface/construction expansion stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());
    }

    [Fact]
    public void VerificationScript_GuardsSurfaceExpansionFilesAndTests()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-physical-surface-model-stage.ps1");

        Assert.True(File.Exists(scriptPath), $"Verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Iso52016PhysicalSurface.cs", script);
        Assert.Contains("Iso52016PhysicalConstructionLayer.cs", script);
        Assert.Contains("Iso52016PhysicalSurfaceBoundaryType.cs", script);
        Assert.Contains("Iso52016PhysicalSurfaceModelExpansionManifest.json", script);
        Assert.Contains("FullyQualifiedName~Iso52016PhysicalSurface", script);
        Assert.Contains("AE-ISO52016-002", script);
    }

    [Fact]
    public void MatrixAllVerificationScript_ReferencesSurfaceExpansionStage()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(scriptPath), $"All-verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("SkipPhysicalSurfaceModel", script);
        Assert.Contains("verify-iso52016-physical-surface-model-stage.ps1", script);
        Assert.Contains("Iso52016PhysicalSurfaceModelExpansionManifest.json", script);
    }

    [Fact]
    public void StageDocumentation_UsesGuardedValidationAnchorLanguage()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalSurfaceModelExpansion.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("AE-ISO52016-002 Step 02", doc);
        Assert.Contains("surface and construction expansion", doc);
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