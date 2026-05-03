using System.Text.Json;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusSmoke002And003PlaceholderReferenceAlignmentTests
{
    [Theory]
    [InlineData("EP-SMOKE-002", 18.0, 3600.0)]
    [InlineData("EP-SMOKE-003", 28.8, 1200.0)]
    public void PlaceholderReferenceMatchesSmokeFixtureAssistantInputForCatalogScaffold(
        string caseId,
        double expectedCoolingEnergyKWh,
        double expectedPeakCoolingLoadW)
    {
        var repoRoot = FindRepositoryRoot();

        var referencePath = Path.Combine(
            repoRoot,
            "tests",
            "fixtures",
            "validation",
            "energyplus",
            caseId,
            "reference-output.placeholder.json");

        Assert.True(File.Exists(referencePath), $"Reference placeholder was not found: {referencePath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(referencePath));

        var referenceOutputs = document
            .RootElement
            .GetProperty("referenceOutputs");

        Assert.Equal(0.0, referenceOutputs.GetProperty("annualHeatingEnergyKwh").GetDouble());
        Assert.Equal(0.0, referenceOutputs.GetProperty("peakHeatingLoadW").GetDouble());
        Assert.Equal(expectedCoolingEnergyKWh, referenceOutputs.GetProperty("annualCoolingEnergyKwh").GetDouble());
        Assert.Equal(expectedPeakCoolingLoadW, referenceOutputs.GetProperty("peakCoolingLoadW").GetDouble());
        Assert.Equal(1.0, referenceOutputs.GetProperty("directionalResponseIndex").GetDouble());
    }

    [Theory]
    [InlineData("EP-SMOKE-002")]
    [InlineData("EP-SMOKE-003")]
    public void PlaceholderReferenceKeepsNonClaimLanguage(
        string caseId)
    {
        var repoRoot = FindRepositoryRoot();

        var referencePath = Path.Combine(
            repoRoot,
            "tests",
            "fixtures",
            "validation",
            "energyplus",
            caseId,
            "reference-output.placeholder.json");

        using var document = JsonDocument.Parse(
            File.ReadAllText(referencePath));

        var text = document.RootElement.ToString();

        Assert.Contains("does not claim EnergyPlus validation", text);
        Assert.Contains("does not claim exact EnergyPlus numerical parity", text);
        Assert.Contains("does not claim ASHRAE 140 validation coverage", text);
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