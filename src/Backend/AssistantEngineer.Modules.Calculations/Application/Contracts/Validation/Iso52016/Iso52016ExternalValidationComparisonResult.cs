namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

public sealed record Iso52016ExternalValidationComparisonResult(
    string FixtureId,
    Iso52016ExternalValidationStatus Status,
    IReadOnlyList<Iso52016ExternalValidationDelta> Deltas,
    IReadOnlyList<string> FailedMetrics)
{
    public bool IsSuccess => Status == Iso52016ExternalValidationStatus.Passed;
}
