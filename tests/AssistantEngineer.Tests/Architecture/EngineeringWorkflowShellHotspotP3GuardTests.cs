namespace AssistantEngineer.Tests.Architecture;

public sealed class EngineeringWorkflowShellHotspotP3GuardTests
{
    [Fact]
    public void EngineeringWorkflowShell_RemainsFacadeSizedAfterP3Phase10()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow",
            "ui",
            "EngineeringWorkflowShell.tsx");

        Assert.True(File.Exists(path), $"Workflow shell file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 300,
            $"EngineeringWorkflowShell must remain a focused UI facade after P3-10. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 300.");
    }

    [Fact]
    public void EngineeringWorkflowShell_UsesExtractedHookAndStepComponent()
    {
        var workflowPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow");

        var shellPath = Path.Combine(workflowPath, "ui", "EngineeringWorkflowShell.tsx");
        var shellSource = File.ReadAllText(shellPath);

        Assert.True(File.Exists(Path.Combine(workflowPath, "model", "useEngineeringWorkflowShell.ts")));
        Assert.True(File.Exists(Path.Combine(workflowPath, "ui", "EngineeringWorkflowStepContent.tsx")));

        Assert.Contains("useEngineeringWorkflowShell", shellSource, StringComparison.Ordinal);
        Assert.Contains("EngineeringWorkflowStepContent", shellSource, StringComparison.Ordinal);
    }
}
