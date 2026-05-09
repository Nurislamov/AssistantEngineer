namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

public enum ExternalComparisonStatus
{
    Planned,
    FixtureDefined,
    ExternalOutputImported,
    Compared,
    PassedTolerance,
    FailedTolerance,
    NotAValidationClaim
}
