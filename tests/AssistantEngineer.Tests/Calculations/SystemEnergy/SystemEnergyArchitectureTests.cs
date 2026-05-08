namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyArchitectureTests
{
    [Fact]
    public void SystemEnergyFoundationServices_DoNotReferenceInfrastructureOrForbiddenFrameworks()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SystemEnergy");

        Assert.NotEmpty(serviceFiles);

        var filteredFiles = serviceFiles
            .Where(path =>
                path.EndsWith("SystemEnergyUsefulLoadValidator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyModuleChainInputValidator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyModuleCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyModuleChainCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyGenerationHandoffBuilder.cs", StringComparison.Ordinal) ||
                path.EndsWith("DomesticHotWaterSystemEnergyHandoffAdapter.cs", StringComparison.Ordinal))
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
