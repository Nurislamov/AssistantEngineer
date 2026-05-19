namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public interface IOwnershipBackfillApplyExecutor
{
    Task<OwnershipBackfillApplyExecutionResult> ExecuteAsync(
        OwnershipBackfillApplyExecutionRequest request,
        CancellationToken cancellationToken = default);
}

