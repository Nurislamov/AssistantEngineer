namespace AssistantEngineer.Tools.OwnershipBackfill.Signoff;

public interface IOwnershipBackfillPlanSignoffWriter
{
    Task WriteAsync(
        OwnershipBackfillPlanSignoffArtifact artifact,
        string outputDirectory,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default);
}
