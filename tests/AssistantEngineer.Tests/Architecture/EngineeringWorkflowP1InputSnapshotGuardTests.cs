using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
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
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application",
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
    public void InputSnapshotBuilderUsesBulkWorkflowInputQueryWithoutPerRoomNPlusOneCalls()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application",
            "Workflow",
            "EngineeringWorkflowInputSnapshotBuilder.cs");
        var source = File.ReadAllText(path);

        Assert.Contains("GetEngineeringWorkflowBulkInputAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomWallsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomWindowsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomVentilationParametersAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRoomGroundContactAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach (var room", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RoomRepositoryContractAndEfImplementationExposeBulkWorkflowInputReader()
    {
        var contractMethod = typeof(IRoomRepository).GetMethod(
            "ListWithEngineeringInputsByBuildingIdAsync",
            new[] { typeof(int), typeof(CancellationToken) });
        Assert.NotNull(contractMethod);

        var repositoryPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Infrastructure",
            "Persistence",
            "Repositories",
            "RoomRepository.cs");

        var source = File.ReadAllText(repositoryPath);
        Assert.Contains("ListWithEngineeringInputsByBuildingIdAsync", source, StringComparison.Ordinal);
        Assert.Contains(".Include(room => room.Windows)", source, StringComparison.Ordinal);
        Assert.Contains(".Include(room => room.Walls)", source, StringComparison.Ordinal);
        Assert.Contains(".Include(room => room.VentilationParameters)", source, StringComparison.Ordinal);
    }
}
