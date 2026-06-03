namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;

public sealed class EngineeringArtifactStorageOptions
{
    public const string SectionName = "EngineeringArtifacts";

    public string Provider { get; set; } = EngineeringArtifactStorageProviders.InMemory;
    public string? RootPath { get; set; } = "artifacts/engineering";
    public long MaxArtifactBytes { get; set; } = 10 * 1024 * 1024;
    public bool EnableSha256Verification { get; set; } = true;
}

public static class EngineeringArtifactStorageProviders
{
    public const string InMemory = "InMemory";
    public const string FileSystem = "FileSystem";
}
