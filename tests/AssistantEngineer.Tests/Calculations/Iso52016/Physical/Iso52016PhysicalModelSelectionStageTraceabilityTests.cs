using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalModelSelectionStageTraceabilityTests
{
    [Fact]
    public void Manifest_DocumentsPhysicalModelSelectionStageAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalModelSelectionStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-10", root.GetProperty("stageId").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired physical model selection adapter stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Reduced Matrix remains the default application path.", claimBoundary);
        Assert.Contains("Physical node model requires explicit strategy selection.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not StandardReference numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
    }

    [Fact]
    public void VerificationRegistry_GuardsSelectionAdapterFilesAndTests()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-10",
            "requiredSourceFiles",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Abstractions/Iso52016/Physical/IIso52016PhysicalModelSelectionService.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-10",
            "requiredTestFiles",
            "tests/AssistantEngineer.Tests/Calculations/Iso52016/Physical/Iso52016PhysicalModelSelectionServiceTests.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-10",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalModelSelectionStageManifest.json");
    }

    [Fact]
    public void VerificationRegistry_ReferencesSelectionAdapterStageWrapper()
    {
        RegistryContainsAlias(
            "AE-ISO52016-002-STEP-10",
            "scripts/iso52016/verify-iso52016-physical-model-selection-stage.ps1");
    }

    [Fact]
    public void StageDocument_StatesNoExternalParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalModelSelectionStage.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Reduced Matrix remains the default", doc);
        Assert.Contains("physical node model requires explicit strategy selection", doc);
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
