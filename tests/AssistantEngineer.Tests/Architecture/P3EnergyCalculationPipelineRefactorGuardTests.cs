using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P3EnergyCalculationPipelineRefactorGuardTests
{
    [Fact]
    public void EnergyCalculationPipelineService_ShouldRemainFocusedFacade()
    {
        var root = FindRepositoryRoot();
        var file = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "EnergyCalculationPipelineService.cs", SearchOption.AllDirectories)
            .Single();

        var lines = File.ReadAllLines(file);

        Assert.True(
            lines.Length <= 550,
            $"EnergyCalculationPipelineService.cs should remain below 550 lines after P3-14. Actual: {lines.Length}.");
    }

    [Fact]
    public void EnergyCalculationPipelineService_ShouldUseFocusedPartialComponents()
    {
        var root = FindRepositoryRoot();
        var componentFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "EnergyCalculationPipeline*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToArray();

        Assert.Contains("EnergyCalculationPipelineRoomInputBuilder.cs", componentFiles);
        Assert.Contains("EnergyCalculationPipelineAggregationExecutor.cs", componentFiles);
        Assert.Contains("EnergyCalculationPipelinePreferencesLoader.cs", componentFiles);
    }

    [Fact]
    public void EnergyCalculationPipelineService_ShouldNotContainExtractedHelperImplementations()
    {
        var root = FindRepositoryRoot();
        var file = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "EnergyCalculationPipelineService.cs", SearchOption.AllDirectories)
            .Single();

        var text = File.ReadAllText(file);

        Assert.DoesNotContain("private RoomLoadCalculationInput BuildRoom", text, StringComparison.Ordinal);
        Assert.DoesNotContain("private SolarCalculationInput CreateSolar", text, StringComparison.Ordinal);
        Assert.DoesNotContain("private VentilationAndInfiltrationLoadInput CreateVentilation", text, StringComparison.Ordinal);
        Assert.DoesNotContain("private InternalGainCalculationInput CreateInternal", text, StringComparison.Ordinal);
        Assert.DoesNotContain("private FloorEnergyCalculationResultDto AggregateFloor", text, StringComparison.Ordinal);
        Assert.DoesNotContain("private BuildingEnergyCalculationResultDto AggregateBuilding", text, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AssistantEngineer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}