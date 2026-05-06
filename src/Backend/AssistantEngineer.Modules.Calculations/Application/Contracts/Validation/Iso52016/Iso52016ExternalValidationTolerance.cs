namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

public sealed record Iso52016ExternalValidationTolerance(
    double AbsoluteTolerance,
    double RelativeTolerancePercent);
