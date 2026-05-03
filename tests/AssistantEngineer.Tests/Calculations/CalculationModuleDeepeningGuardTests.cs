using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Calculations;

public class CalculationModuleDeepeningGuardTests
{
    [Fact]
    public void DeepeningDocumentsScriptsAndGeneratedInventoryExist()
    {
        var requiredFiles = new[]
        {
            PlanPath,
            BoundaryPolicyPath,
            InventoryScriptPath,
            VerifyScriptPath,
            InventoryJsonPath,
            InventoryMarkdownPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(requiredFile), $"Required calculation module deepening artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void InventoryDeclaresDeepeningBaselineAndCountsCalculationAssets()
    {
        using var document = ReadJson(InventoryJsonPath);
        var root = document.RootElement;

        Assert.Equal("Calculation Module Deepening Inventory", root.GetProperty("inventoryName").GetString());
        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("DeepeningBaseline", root.GetProperty("status").GetString());
        Assert.Equal("2026-01-01 00:00:00 UTC", root.GetProperty("generatedAtUtc").GetString());

        var totals = root.GetProperty("totals");

        Assert.True(totals.GetProperty("serviceFiles").GetInt32() >= 10);
        Assert.True(totals.GetProperty("calculationTests").GetInt32() >= 5);
        Assert.True(totals.GetProperty("keyEngines").GetInt32() >= 10);
        Assert.Equal(0, totals.GetProperty("missingKeyEngines").GetInt32());
    }

    [Fact]
    public void InventoryTracksKeyCalculationEngines()
    {
        using var document = ReadJson(InventoryJsonPath);
        var keyEngines = document.RootElement.GetProperty("keyEngines");

        var requiredEngines = new[]
        {
            "RoomLoadCalculationEngine",
            "LoadAggregationEngine",
            "AnnualEnergyBalanceEngine",
            "HourlySimulationToAnnualEnergyInputMapper",
            "SystemEnergyEngine",
            "EquipmentSizingEngine",
            "TransmissionHeatTransferEngine",
            "VentilationAndInfiltrationLoadEngine",
            "InternalGainEngine",
            "WindowSolarGainEngine",
            "AnnualWeatherSolarProfileBuilder",
            "EnergyCalculationPipelineService"
        };

        foreach (var requiredEngine in requiredEngines)
        {
            var engine = keyEngines.GetProperty(requiredEngine);
            Assert.True(engine.GetProperty("exists").GetBoolean(), $"Expected key calculation engine to exist: {requiredEngine}");
            Assert.False(string.IsNullOrWhiteSpace(engine.GetProperty("path").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(engine.GetProperty("layer").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(engine.GetProperty("purpose").GetString()));
        }
    }

    [Fact]
    public void DeepeningPlanDocumentsAxesInvariantsAndNonClaims()
    {
        var content = File.ReadAllText(PlanPath);

        var requiredPhrases = new[]
        {
            "Calculation Module Deepening Plan",
            "Input normalization and units policy",
            "Scenario fixtures",
            "Diagnostics consistency",
            "Balance invariants",
            "Method strategy isolation",
            "room load components sum to total load",
            "monthly fallback is never presented as true hourly 8760 simulation",
            "does not claim exact EnergyPlus numerical parity",
            "does not claim ASHRAE 140 validation coverage",
            "does not claim full ISO 52016"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BoundaryPolicyProtectsCalculationsModuleFromInfrastructureAndSilentFallbacks()
    {
        var content = File.ReadAllText(BoundaryPolicyPath);

        var requiredPhrases = new[]
        {
            "Calculation Module Boundary Policy",
            "EF Core persistence details",
            "ASP.NET Core controllers",
            "Infrastructure report generators",
            "ClosedXML",
            "Successful calculation results must not carry error diagnostics",
            "No fallback may be silent",
            "Reporting module may consume calculation results",
            "Calculations module must not know about Excel rendering"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InventoryMarkdownDocumentsKeyEnginesDeepeningAxesAndNonClaims()
    {
        var content = File.ReadAllText(InventoryMarkdownPath);

        var requiredPhrases = new[]
        {
            "Calculation Module Deepening Inventory",
            "DeepeningBaseline",
            "RoomLoadCalculationEngine",
            "LoadAggregationEngine",
            "AnnualEnergyBalanceEngine",
            "SystemEnergyEngine",
            "EnergyCalculationPipelineService",
            "Deepening axes",
            "Required non-claims",
            "does not claim exact EnergyPlus numerical parity"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void VerifyScriptRunsGeneratorAndGuardTests()
    {
        var content = File.ReadAllText(VerifyScriptPath);

        Assert.Contains("generate-calculation-module-inventory.ps1", content, StringComparison.Ordinal);
        Assert.Contains("CalculationModuleDeepeningGuardTests", content, StringComparison.Ordinal);
        Assert.Contains("dotnet test", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string PlanPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "CalculationModuleDeepeningPlan.md");

    private static string BoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "CalculationModuleBoundaryPolicy.md");

    private static string InventoryScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-calculation-module-inventory.ps1");

    private static string VerifyScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-calculation-module-deepening.ps1");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "calculations", "CalculationModuleInventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "calculations", "CalculationModuleInventory.md");
}
