namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationArchitectureTests
{
    [Fact]
    public void NaturalVentilationFoundationServices_DoNotReferenceInfrastructureOrForbiddenFrameworks()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation");

        Assert.NotEmpty(serviceFiles);

        var filteredFiles = serviceFiles
            .Where(path =>
                path.EndsWith("NaturalVentilationDiagnosticsFactory.cs", StringComparison.Ordinal) ||
                path.EndsWith("NaturalVentilationOpeningGeometryNormalizer.cs", StringComparison.Ordinal) ||
                path.EndsWith("NaturalVentilationInputValidator.cs", StringComparison.Ordinal) ||
                path.EndsWith("NaturalVentilationPressureCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("NaturalVentilationAirflowCalculator.cs", StringComparison.Ordinal))
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
