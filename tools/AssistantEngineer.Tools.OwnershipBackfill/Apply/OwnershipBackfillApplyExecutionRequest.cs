using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyExecutionRequest
{
    public required OwnershipBackfillApplyOptions Options { get; init; }
    public required OwnershipBackfillPlanResult Plan { get; init; }
    public required OwnershipBackfillPlanSignoffArtifact Signoff { get; init; }
    public bool TestOnlyExecution { get; init; }
    public required string ExecutionProvider { get; init; }
    public required IOwnershipBackfillTestRecordStore TestRecordStore { get; init; }
}

