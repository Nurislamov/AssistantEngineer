using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalModelSelectionApplicationGuardTests
{
    [Fact]
    public void SelectionContracts_KeepReducedMatrixAsDefaultAndPhysicalAsExplicitOptIn()
    {
        var repoRoot = FindRepositoryRoot();

        var strategyPath = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts",
            "Iso52016",
            "Physical",
            "Iso52016PhysicalModelSelectionStrategy.cs");

        var requestPath = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts",
            "Iso52016",
            "Physical",
            "Iso52016PhysicalModelSelectionRequest.cs");

        Assert.True(File.Exists(strategyPath), $"Strategy contract was not found: {strategyPath}");
        Assert.True(File.Exists(requestPath), $"Selection request contract was not found: {requestPath}");

        var strategy = File.ReadAllText(strategyPath);
        var request = File.ReadAllText(requestPath);

        Assert.Contains("ReducedMatrix", strategy);
        Assert.Contains("PhysicalNodeModel", strategy);
        Assert.Contains("Iso52016PhysicalModelSelectionStrategy.ReducedMatrix", request);
        Assert.True(
            request.Contains("Iso52016PhysicalRoomModelRequest", StringComparison.OrdinalIgnoreCase) ||
            request.Contains("Physical", StringComparison.OrdinalIgnoreCase),
            "Selection contract must keep an explicit physical-model opt-in marker without relying on one brittle property name.");
    }

    [Fact]
    public void SelectionService_UsesReducedBuilderAndPhysicalBuilderWithoutCreatingNewSolver()
    {
        var repoRoot = FindRepositoryRoot();

        var servicePath = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Physical",
            "Iso52016PhysicalModelSelectionService.cs");

        Assert.True(File.Exists(servicePath), $"Selection service was not found: {servicePath}");

        var service = File.ReadAllText(servicePath);

        Assert.Contains("ISo52016MatrixReducedRoomModelBuilder", service);
        Assert.Contains("ISo52016PhysicalRoomModelBuilder", service);
        Assert.Contains("ISo52016MatrixHourlySolver", service);
        Assert.Contains("ReducedMatrix", service);
        Assert.Contains("PhysicalNodeModel", service);
        Assert.DoesNotContain("new Iso52016MatrixHourlySolver(", service, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplicationGuardManifest_DocumentsClaimBoundaryAndDependencies()
    {
        var repoRoot = FindRepositoryRoot();

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalModelSelectionApplicationGuardManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-11", root.GetProperty("stageId").GetString());

        var dependsOn = root
            .GetProperty("dependsOn")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("AE-ISO52016-002-STEP-10", dependsOn);

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired physical model selection application guard.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not StandardReference numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
    }

    [Fact]
    public void VerificationRegistryExposesApplicationGuardStage()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-11",
            "requiredTestFiles",
            "tests/AssistantEngineer.Tests/Calculations/Iso52016/Physical/Iso52016PhysicalModelSelectionApplicationGuardTests.cs");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-11",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalModelSelectionApplicationGuardManifest.json");
        RegistryContainsAlias(
            "AE-ISO52016-002-STEP-11",
            "scripts/iso52016/verify-iso52016-physical-model-selection-application-guard.ps1");
    }

    [Fact]
    public void StageDocument_StatesNoExternalExactMatchClaims()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalModelSelectionApplicationGuard.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("reduced Matrix path remains the default", doc);
        Assert.Contains("physical node model path is explicit opt-in", doc);
        Assert.Contains("validation/internal engineering anchors only", doc);
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
