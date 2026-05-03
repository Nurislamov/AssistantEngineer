using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixSolverStageTraceabilityTests
{
    [Fact]
    public void Manifest_ClosesExpectedStageWorkItems()
    {
        var repoRoot = FindRepositoryRoot();

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixSolverStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(manifestPath));

        var root = document.RootElement;

        Assert.Equal(
            "ISO52016-MATRIX-SOLVER",
            root.GetProperty("stageId").GetString());

        Assert.Equal(
            "Closed",
            root.GetProperty("status").GetString());

        var closedWorkItems = root
            .GetProperty("closedWorkItems")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("AE-ISO52016-001", closedWorkItems);
        Assert.Contains("AE-GAINS-001", closedWorkItems);
        Assert.Contains("AE-ZONES-001", closedWorkItems);

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", nonClaims);
        Assert.Contains("No exact EnergyPlus numerical parity claim.", nonClaims);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
    }

    [Fact]
    public void TraceabilityMatrix_MapsEveryClosedWorkItemToImplementationGuards()
    {
        var repoRoot = FindRepositoryRoot();

        var matrixPath = Path.Combine(
            repoRoot,
            "docs",
            "traceability",
            "Iso52016MatrixSolverTraceabilityMatrix.json");

        Assert.True(File.Exists(matrixPath), $"Traceability matrix was not found: {matrixPath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(matrixPath));

        var items = document.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .ToArray();

        Assert.Contains(items, item =>
            item.GetProperty("workItem").GetString() == "AE-ISO52016-001" &&
            item.GetProperty("implementationGuards").EnumerateArray().Any(guard =>
                guard.GetString() == "Iso52016MatrixHourlySolver"));

        Assert.Contains(items, item =>
            item.GetProperty("workItem").GetString() == "AE-GAINS-001" &&
            item.GetProperty("implementationGuards").EnumerateArray().Any(guard =>
                guard.GetString() == "Iso52016InternalGainReferenceDataProvider"));

        Assert.Contains(items, item =>
            item.GetProperty("workItem").GetString() == "AE-ZONES-001" &&
            item.GetProperty("implementationGuards").EnumerateArray().Any(guard =>
                guard.GetString() == "AdjacentUnconditioned"));
    }

    [Fact]
    public void SourceTree_StillContainsMatrixSolverIntegrationAndApiGuards()
    {
        var repoRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repoRoot, "src", "Backend");

        var sourceText = string.Join(
            Environment.NewLine,
            Directory
                .GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains("Iso52016MatrixHourlySolver", sourceText);
        Assert.Contains("Iso52016InternalGainReferenceDataProvider", sourceText);
        Assert.Contains("AdjacentUnconditioned", sourceText);
        Assert.Contains("Iso52016MatrixRoomEnergySimulationService", sourceText);
        Assert.Contains("Iso52016MatrixRoomEnergySimulationResultMapper", sourceText);
        Assert.Contains("Iso52016BuildingEnergySimulationCommand", sourceText);
        Assert.Contains("SimulateIso52016", sourceText);
        Assert.DoesNotContain("Iso52016SimulationEngine", sourceText);
        Assert.DoesNotContain("V2Matrix", sourceText);
        Assert.DoesNotContain("Legacy", sourceText);
        Assert.Contains("Matrix", sourceText);
    }

    [Fact]
    public void Documentation_IncludesApiPathAndNonParityClaim()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixSolverStage.md");

        Assert.True(File.Exists(docPath), $"Documentation was not found: {docPath}");

        var text = File.ReadAllText(docPath);

        Assert.Contains("POST /api/v1/buildings/{buildingId}/energy-analysis/iso52016/simulate", text);
        Assert.Contains("AE-ISO52016-001", text);
        Assert.Contains("AE-GAINS-001", text);
        Assert.Contains("AE-ZONES-001", text);
        Assert.Contains("does not claim exact numerical parity", text);
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