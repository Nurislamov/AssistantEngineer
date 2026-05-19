namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public interface IOwnershipBackfillApplyPlanGenerator
{
    Task<OwnershipBackfillPlanResult> GenerateAsync(
        OwnershipBackfillPlanOptions options,
        CancellationToken cancellationToken = default);
}
