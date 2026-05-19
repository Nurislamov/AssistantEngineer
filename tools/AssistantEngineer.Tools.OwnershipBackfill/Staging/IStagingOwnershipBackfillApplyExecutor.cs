namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public interface IStagingOwnershipBackfillApplyExecutor
{
    Task<OwnershipBackfillStagingApplyExecutionDisabledResult> ExecuteAsync(
        OwnershipBackfillStagingApplyPreflightOptions options,
        CancellationToken cancellationToken = default);
}
