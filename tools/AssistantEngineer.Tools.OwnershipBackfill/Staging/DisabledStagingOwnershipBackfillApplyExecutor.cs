using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public sealed class DisabledStagingOwnershipBackfillApplyExecutor : IStagingOwnershipBackfillApplyExecutor
{
    public const string DisabledMessage = "Staging apply executor is designed but disabled in P6-11. No ownership metadata was written.";

    public Task<OwnershipBackfillStagingApplyExecutionDisabledResult> ExecuteAsync(
        OwnershipBackfillStagingApplyPreflightOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new OwnershipBackfillStagingApplyExecutionDisabledResult
        {
            Executed = false,
            Message = DisabledMessage,
            NonClaims =
            [
                .. OwnershipBackfillConstants.NonClaims,
                "No staging apply execution claim.",
                "No production apply enabled claim."
            ]
        });
    }
}
