namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationJobServiceHotspotP3GuardTests
{
    [Fact]
    public void EngineeringCalculationJobService_RemainsFacadeSizedAfterP3Phase2()
    {
        var path = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "EngineeringCalculationJobService.cs");

        Assert.True(File.Exists(path), $"Job service file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 500,
            $"EngineeringCalculationJobService must remain a focused orchestration facade after P3-08. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 500.");
    }

    [Fact]
    public void JobServiceUsesExtractedLifecycleComponents()
    {
        var servicePath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "EngineeringCalculationJobService.cs");
        var jobsPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Jobs");

        Assert.True(File.Exists(Path.Combine(jobsPath, "EngineeringCalculationJobStatusTransitionPolicy.cs")));
        Assert.True(File.Exists(Path.Combine(jobsPath, "EngineeringCalculationJobEventRecorder.cs")));
        Assert.True(File.Exists(Path.Combine(jobsPath, "EngineeringCalculationJobExecutionOrchestrator.cs")));

        var source = File.ReadAllText(servicePath);
        Assert.Contains("EngineeringCalculationJobStatusTransitionPolicy", source, StringComparison.Ordinal);
        Assert.Contains("EngineeringCalculationJobEventRecorder", source, StringComparison.Ordinal);
        Assert.Contains("EngineeringCalculationJobExecutionOrchestrator", source, StringComparison.Ordinal);
    }
}
