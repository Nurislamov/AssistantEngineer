namespace AssistantEngineer.Tools.OwnershipBackfill.Signoff;

public sealed class OwnershipBackfillPlanSignoffValidationResult
{
    public bool Passed { get; init; }
    public int ExitCode { get; init; }
    public required IReadOnlyList<OwnershipBackfillPlanSignoffFinding> Findings { get; init; }
    public OwnershipBackfillPlanSignoffArtifact? Artifact { get; init; }
}
