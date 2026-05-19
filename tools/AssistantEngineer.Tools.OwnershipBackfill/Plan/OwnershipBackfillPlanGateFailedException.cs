namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillPlanGateFailedException(string message) : InvalidOperationException(message)
{
}
