namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalTopologyArchitectureTests
{
    [Fact]
    public void TopologyServices_DoNotReferenceInfrastructureOrExternalRuntimeDependencies()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Topology");

        Assert.NotEmpty(serviceFiles);

        var forbiddenFragments = new[]
        {
            "AssistantEngineer.Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "ClosedXML",
            "using EnergyPlus",
            "using pyBuildingEnergy"
        };

        foreach (var filePath in serviceFiles)
        {
            var content = File.ReadAllText(filePath);
            foreach (var forbiddenFragment in forbiddenFragments)
            {
                Assert.DoesNotContain(forbiddenFragment, content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TopologyContracts_StayInApplicationContractsOrAbstractions()
    {
        var contractFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Topology",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Abstractions/Topology");

        Assert.NotEmpty(contractFiles);

        foreach (var filePath in contractFiles)
        {
            var isUnderContracts = filePath.Contains(
                $"{Path.DirectorySeparatorChar}Application{Path.DirectorySeparatorChar}Contracts{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);
            var isUnderAbstractions = filePath.Contains(
                $"{Path.DirectorySeparatorChar}Application{Path.DirectorySeparatorChar}Abstractions{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);

            Assert.True(isUnderContracts || isUnderAbstractions);
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
