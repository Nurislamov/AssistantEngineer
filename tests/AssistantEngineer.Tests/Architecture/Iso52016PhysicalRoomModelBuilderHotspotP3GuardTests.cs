namespace AssistantEngineer.Tests.Architecture;

public sealed class Iso52016PhysicalRoomModelBuilderHotspotP3GuardTests
{
    [Fact]
    public void Iso52016PhysicalRoomModelBuilder_RemainsFacadeSizedAfterP3Phase11()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Physical",
            "Iso52016PhysicalRoomModelBuilder.cs");

        Assert.True(File.Exists(path), $"Physical room model builder file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 120,
            $"Iso52016PhysicalRoomModelBuilder must remain a focused orchestration facade after P3-11. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 120.");
    }

    [Fact]
    public void Iso52016PhysicalRoomModelBuilder_UsesExtractedFocusedComponents()
    {
        var root = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Physical");

        var builderPath = Path.Combine(root, "Iso52016PhysicalRoomModelBuilder.cs");
        var builderSource = File.ReadAllText(builderPath);

        Assert.True(File.Exists(Path.Combine(root, "Iso52016PhysicalRoomModelValidation.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016PhysicalRoomModelMapping.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016PhysicalThreeNodeRequestBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016PhysicalSurfaceExpandedRequestBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016PhysicalRoomModelRequestFactory.cs")));

        Assert.Contains("Iso52016PhysicalRoomModelValidation.Validate", builderSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016PhysicalThreeNodeRequestBuilder.Build", builderSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016PhysicalSurfaceExpandedRequestBuilder.Build", builderSource, StringComparison.Ordinal);
    }
}
