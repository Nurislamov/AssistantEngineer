namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public interface IOwnershipBackfillApplyPlanWriter
{
    Task WriteAsync(
        OwnershipBackfillPlanResult result,
        string outputDirectory,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default);
}
