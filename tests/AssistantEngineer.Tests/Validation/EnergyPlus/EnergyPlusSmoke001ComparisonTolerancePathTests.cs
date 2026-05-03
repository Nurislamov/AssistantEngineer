using System.Text.Json;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusSmoke001ComparisonTolerancePathTests
{
    [Fact]
    public void EpSmoke001ComparisonMetricsContainAssistantAndReferencePaths()
    {
        var repoRoot = FindRepositoryRoot();

        var tolerancePath = Path.Combine(
            repoRoot,
            "tests",
            "fixtures",
            "validation",
            "energyplus",
            "EP-SMOKE-001",
            "comparison-tolerances.json");

        Assert.True(File.Exists(tolerancePath), $"Tolerance file was not found: {tolerancePath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(tolerancePath));

        foreach (var metric in document.RootElement.GetProperty("metrics").EnumerateArray())
        {
            Assert.True(metric.TryGetProperty("metricId", out _));
            Assert.True(metric.TryGetProperty("assistantEngineerPath", out var assistantPath));
            Assert.True(metric.TryGetProperty("referencePath", out var referencePath));

            Assert.False(string.IsNullOrWhiteSpace(assistantPath.GetString()));
            Assert.False(string.IsNullOrWhiteSpace(referencePath.GetString()));
        }
    }

    [Fact]
    public void EpSmoke001AssistantInputContainsCoolingFallbackValuesForComparison()
    {
        var repoRoot = FindRepositoryRoot();

        var inputPath = Path.Combine(
            repoRoot,
            "tests",
            "fixtures",
            "validation",
            "energyplus",
            "EP-SMOKE-001",
            "assistantengineer-input.json");

        Assert.True(File.Exists(inputPath), $"Assistant input file was not found: {inputPath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(inputPath));

        var formula = document.RootElement.GetProperty("calculationFormula");

        Assert.Equal(
            0.0,
            formula.GetProperty("expectedCoolingEnergyKwh").GetDouble());

        Assert.Equal(
            0.0,
            formula.GetProperty("expectedPeakCoolingLoadW").GetDouble());
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