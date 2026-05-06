using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalRoomModelDiagnosticsStageTraceabilityTests
{
    [Fact]
    public void Manifest_DocumentsDiagnosticsStageAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalRoomModelDiagnosticsStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-06", root.GetProperty("stageId").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired physical room model diagnostics stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
    }

    [Fact]
    public void VerificationRegistry_GuardsDiagnosticsFilesAndTests()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-06",
            "requiredSourceFiles",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Physical/Iso52016PhysicalRoomModelDiagnosticsBuilder.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-06",
            "requiredTestFiles",
            "tests/AssistantEngineer.Tests/Calculations/Iso52016/Physical/Iso52016PhysicalRoomModelDiagnosticsBuilderTests.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-06",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalRoomModelDiagnosticsStageManifest.json");
    }

    [Fact]
    public void VerificationRegistry_ReferencesDiagnosticsStageWrapper()
    {
        RegistryContainsAlias(
            "AE-ISO52016-002-STEP-06",
            "scripts/iso52016/verify-iso52016-physical-room-model-diagnostics-stage.ps1");
    }

    [Fact]
    public void StageDocument_StatesNoExternalParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalRoomModelDiagnosticsStage.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("internal engineering anchors only", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy numerical equivalence", doc);
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
