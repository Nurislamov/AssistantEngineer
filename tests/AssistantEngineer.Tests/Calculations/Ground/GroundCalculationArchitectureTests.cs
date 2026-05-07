namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundCalculationArchitectureTests
{
    [Fact]
    public void GroundApplicationServices_DoNotReferenceInfrastructureOrForbiddenFrameworks()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ground");

        Assert.NotEmpty(serviceFiles);

        var filteredFiles = serviceFiles
            .Where(path =>
                path.EndsWith("GroundGeometryNormalizer.cs", StringComparison.Ordinal) ||
                path.EndsWith("GroundBoundaryInputValidator.cs", StringComparison.Ordinal) ||
                path.EndsWith("GroundTemperatureProfileProvider.cs", StringComparison.Ordinal) ||
                path.EndsWith("GroundBoundaryCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("GroundCalculationDiagnosticsFactory.cs", StringComparison.Ordinal))
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
