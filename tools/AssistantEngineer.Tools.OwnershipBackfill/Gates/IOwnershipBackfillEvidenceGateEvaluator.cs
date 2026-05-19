namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public interface IOwnershipBackfillEvidenceGateEvaluator
{
    OwnershipBackfillGateResult Evaluate(
        OwnershipBackfillEvidenceBundle evidence,
        OwnershipBackfillGateOptions options);
}
