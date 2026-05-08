namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyPrimaryEnergyArchitectureTests
{
    [Fact]
    public void SystemEnergyPrimaryEnergyServices_DoNotReferenceInfrastructureOrForbiddenFrameworks()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SystemEnergy");

        Assert.NotEmpty(serviceFiles);

        var filteredFiles = serviceFiles
            .Where(path =>
                path.EndsWith("SystemEnergyFactorSetValidator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyDefaultFactorSetProvider.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyEmissionCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyPrimaryEnergyCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("SystemEnergyCalculationSummaryBuilder.cs", StringComparison.Ordinal))
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
