namespace AssistantEngineer.Tests.Architecture;

public sealed class EngineeringWorkflowContractsExtractionGuardTests
{
    [Fact]
    public void ApiContractsFolderNoLongerHostsEngineeringWorkflowDtos()
    {
        var apiContractsPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Contracts",
            "EngineeringWorkflow");

        var contractFiles = Directory.Exists(apiContractsPath)
            ? Directory.GetFiles(apiContractsPath, "*.cs", SearchOption.TopDirectoryOnly)
            : [];

        Assert.Empty(contractFiles);
    }

    [Fact]
    public void EngineeringWorkflowModuleHostsExtractedWorkflowContracts()
    {
        var moduleContractsPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application",
            "Contracts",
            "EngineeringWorkflow");

        Assert.True(Directory.Exists(moduleContractsPath), $"Missing module contracts path: {moduleContractsPath}");

        var fileNames = Directory.GetFiles(moduleContractsPath, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "EngineeringCalculationJobDtos.cs",
                "EngineeringCalculationScenarioDtos.cs",
                "EngineeringWorkflowDtos.cs"
            ],
            fileNames);
    }

    [Fact]
    public void ApiWorkflowFolderNoLongerHostsMovedBuilders()
    {
        var apiWorkflowPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Workflow");

        var movedFileNames = new[]
        {
            "EngineeringWorkflowCatalog.cs",
            "EngineeringWorkflowInputSnapshot.cs",
            "EngineeringWorkflowInputSnapshotBuilder.cs",
            "EngineeringWorkflowTracePreviewService.cs",
            "EngineeringWorkflowReportPreviewService.cs",
            "IEngineeringWorkflowInputSnapshotBuilder.cs",
            "IEngineeringWorkflowTracePreviewService.cs",
            "IEngineeringWorkflowReportPreviewService.cs"
        };

        foreach (var movedFileName in movedFileNames)
        {
            var movedPath = Path.Combine(apiWorkflowPath, movedFileName);
            Assert.False(File.Exists(movedPath), $"Moved workflow builder should not remain in API: {movedPath}");
        }
    }
}
