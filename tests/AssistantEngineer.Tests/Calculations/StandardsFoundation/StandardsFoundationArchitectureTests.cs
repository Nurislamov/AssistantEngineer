namespace AssistantEngineer.Tests.Calculations.StandardsFoundation;

public sealed class StandardsFoundationArchitectureTests
{
    [Fact]
    public void FoundationContracts_DoNotReferenceInfrastructure()
    {
        var contractFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Standards",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Topology");

        Assert.NotEmpty(contractFiles);

        foreach (var filePath in contractFiles)
        {
            var content = File.ReadAllText(filePath);
            Assert.DoesNotContain("AssistantEngineer.Infrastructure", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FoundationServices_DoNotReferenceFrameworkOrExternalSimulationTooling()
    {
        var serviceFiles = EnumerateFiles(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Standards",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Common/Profiles");

        Assert.NotEmpty(serviceFiles);

        var standardsServiceFiles = serviceFiles
            .Where(path =>
                path.EndsWith("AnnualProfileShapeValidator.cs", StringComparison.Ordinal) ||
                path.Contains($"{Path.DirectorySeparatorChar}Services{Path.DirectorySeparatorChar}Standards{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(standardsServiceFiles);

        var forbiddenFragments = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "ClosedXML",
            "using EnergyPlus",
            "using pyBuildingEnergy",
            "AssistantEngineer.Infrastructure"
        };

        foreach (var filePath in standardsServiceFiles)
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
