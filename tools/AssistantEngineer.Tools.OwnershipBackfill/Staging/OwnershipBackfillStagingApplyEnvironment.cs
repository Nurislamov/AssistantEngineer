namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public sealed class OwnershipBackfillStagingApplyEnvironment
{
    public string? EnvironmentName { get; init; }
    public string? DatabaseProvider { get; init; }
    public bool IsStaging { get; init; }
    public bool IsProduction { get; init; }
    public string? EnvironmentMarker { get; init; }
}
