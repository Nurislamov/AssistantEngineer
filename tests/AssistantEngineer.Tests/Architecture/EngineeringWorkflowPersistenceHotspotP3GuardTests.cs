namespace AssistantEngineer.Tests.Architecture;

public class EngineeringWorkflowPersistenceHotspotP3GuardTests
{
    [Fact]
    public void EngineeringWorkflowPersistenceService_RemainsFacadeSizedAfterP3Phase1()
    {
        var path = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Persistence",
            "EngineeringWorkflowPersistenceService.cs");

        Assert.True(File.Exists(path), $"Persistence service file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 500,
            $"EngineeringWorkflowPersistenceService must remain a focused orchestration facade after P3-07. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 500.");
    }

    [Fact]
    public void PersistencePayloadAndArtifactHelpersExistAndAreUsedByFacade()
    {
        var persistenceDirectory = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Persistence");

        var payloadHelperPath = Path.Combine(persistenceDirectory, "EngineeringWorkflowPersistencePayloadService.cs");
        var artifactHelperPath = Path.Combine(persistenceDirectory, "EngineeringWorkflowArtifactPersistenceService.cs");
        var facadePath = Path.Combine(persistenceDirectory, "EngineeringWorkflowPersistenceService.cs");

        Assert.True(File.Exists(payloadHelperPath), $"Missing payload helper: {payloadHelperPath}");
        Assert.True(File.Exists(artifactHelperPath), $"Missing artifact helper: {artifactHelperPath}");

        var facade = File.ReadAllText(facadePath);
        Assert.Contains("EngineeringWorkflowPersistencePayloadService", facade, StringComparison.Ordinal);
        Assert.Contains("EngineeringWorkflowArtifactPersistenceService", facade, StringComparison.Ordinal);
    }
}
