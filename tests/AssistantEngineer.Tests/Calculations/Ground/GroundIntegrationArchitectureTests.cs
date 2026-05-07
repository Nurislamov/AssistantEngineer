namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundIntegrationArchitectureTests
{
    [Fact]
    public void GroundIntegrationServices_DoNotReferenceInfrastructureOrForbiddenFrameworks()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ground");

        Assert.NotEmpty(serviceFiles);

        var filteredFiles = serviceFiles
            .Where(path =>
                path.EndsWith("GroundBoundaryTopologyMapper.cs", StringComparison.Ordinal) ||
                path.EndsWith("BuildingGroundBoundaryCalculator.cs", StringComparison.Ordinal) ||
                path.EndsWith("GroundBoundaryTemperatureLookupBuilder.cs", StringComparison.Ordinal) ||
                path.EndsWith("ThermalZoneBoundaryGroundTemperatureAdapter.cs", StringComparison.Ordinal) ||
                path.EndsWith("GroundBoundaryToIso52016BoundaryProfileMapper.cs", StringComparison.Ordinal))
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
