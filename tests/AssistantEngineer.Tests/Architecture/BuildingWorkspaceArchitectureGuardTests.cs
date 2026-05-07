namespace AssistantEngineer.Tests.Architecture;

public class BuildingWorkspaceArchitectureGuardTests
{
    [Fact]
    public void BuildingWorkspace_RemainsWithinPhaseOneDecompositionBoundaries()
    {
        var workspacePath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Frontend",
            "src",
            "widgets",
            "building-workspace",
            "ui",
            "BuildingWorkspace.tsx");

        Assert.True(File.Exists(workspacePath), $"BuildingWorkspace.tsx was not found: {workspacePath}");

        var lines = File.ReadAllLines(workspacePath).Length;
        Assert.True(
            lines <= 700,
            $"BuildingWorkspace.tsx grew beyond guard threshold (actual: {lines}, allowed: 700).");

        var content = File.ReadAllText(workspacePath);
        Assert.Contains("useBuildingWorkspaceData", content, StringComparison.Ordinal);
        Assert.Contains("BuildingWorkspaceTabs", content, StringComparison.Ordinal);
        Assert.Contains("CalculationsPanel", content, StringComparison.Ordinal);
        Assert.Contains("ReportsPanel", content, StringComparison.Ordinal);
        Assert.DoesNotContain("redux", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("zustand", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mobx", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildingWorkspace_DecompositionFilesExist()
    {
        var requiredFiles = new[]
        {
            "src/Frontend/src/widgets/building-workspace/model/useBuildingWorkspaceData.ts",
            "src/Frontend/src/widgets/building-workspace/model/useBuildingCalculationExecution.ts",
            "src/Frontend/src/widgets/building-workspace/model/useBuildingReports.ts",
            "src/Frontend/src/widgets/building-workspace/model/useFloorsRoomsMutations.ts",
            "src/Frontend/src/widgets/building-workspace/model/useEnvelopeMutations.ts",
            "src/Frontend/src/widgets/building-workspace/model/useVentilationMutations.ts",
            "src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspaceTabs.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/CalculationsPanel.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/EnvelopePanel.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/FloorsRoomsPanel.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/WallEditor.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/WindowEditor.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/VentilationPanel.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/ReportsPanel.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/RoomSelect.tsx",
            "src/Frontend/src/widgets/building-workspace/ui/JsonBlock.tsx"
        };

        foreach (var relativePath in requiredFiles)
        {
            var fullPath = Path.Combine(relativePath.Split('/').Prepend(TestPaths.RepoRoot).ToArray());
            Assert.True(File.Exists(fullPath), $"Expected decomposition file to exist: {relativePath}");
        }
    }
}
