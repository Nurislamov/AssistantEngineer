namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterSystemLossArchitectureTests
{
    [Fact]
    public void DomesticHotWaterSystemLossServices_DoNotReferenceInfrastructureOrForbiddenFrameworks()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/DomesticHotWater");

        Assert.NotEmpty(serviceFiles);

        var filteredFiles = serviceFiles
            .Where(path =>
                path.EndsWith("DomesticHotWaterSystemLossInputValidator.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterStorageLossCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterDistributionLossCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterCirculationLossCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterLossCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterSystemLoadCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterEn15316HandoffBuilder.cs", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(filteredFiles);

        var forbiddenFragments = new[]
        {
            "AssistantEngineer.Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "ClosedXML",
            "using EnergyPlus"
        };

        foreach (var filePath in filteredFiles)
        {
            var content = File.ReadAllText(filePath);
            foreach (var forbiddenFragment in forbiddenFragments)
            {
                Assert.DoesNotContain(forbiddenFragment, content, StringComparison.Ordinal);
            }
        }
    }

    private static IReadOnlyList<string> EnumerateFiles(params string[] relativeDirectories) =>
        relativeDirectories
            .Select(relative => Path.Combine(
                TestPaths.RepoRoot,
                relative.Replace('/', Path.DirectorySeparatorChar)))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            .ToArray();
}
