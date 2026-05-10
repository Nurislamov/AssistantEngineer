using AssistantEngineer.Api.Services.Calculations.Workflow;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Facades;

namespace AssistantEngineer.Tests.Architecture;

public class EngineeringWorkflowP1InputSnapshotGuardTests
{
    [Fact]
    public void StateBuilderDependsOnInputSnapshotBuilderInsteadOfBuildingFacades()
    {
        var dependencies = typeof(EngineeringWorkflowStateBuilder)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IEngineeringWorkflowInputSnapshotBuilder), dependencies);
        Assert.DoesNotContain(typeof(IBuildingsFacade), dependencies);
        Assert.DoesNotContain(typeof(IEngineeringCoreStatusFacade), dependencies);
    }

    [Fact]
    public void InputSnapshotBuilderOwnsCurrentBuildingFacadeBoundary()
    {
        var dependencies = typeof(EngineeringWorkflowInputSnapshotBuilder)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IBuildingsFacade), dependencies);
        Assert.Contains(typeof(IEngineeringCoreStatusFacade), dependencies);
    }

    [Fact]
    public void StateBuilderDoesNotOwnPerRoomWorkflowInputQueries()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Workflow",
            "EngineeringWorkflowStateBuilder.cs");
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("GetRoomWallsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomWindowsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomVentilationParametersAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomGroundContactAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach (var room in rooms)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void InputSnapshotBuilderIsTemporaryBoundaryForPerRoomQueriesUntilRepositoryBatchingExists()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Workflow",
            "EngineeringWorkflowInputSnapshotBuilder.cs");
        var source = File.ReadAllText(path);

        Assert.Contains("GetRoomWallsAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetRoomWindowsAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetRoomVentilationParametersAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetRoomGroundContactAsync", source, StringComparison.Ordinal);
    }
}