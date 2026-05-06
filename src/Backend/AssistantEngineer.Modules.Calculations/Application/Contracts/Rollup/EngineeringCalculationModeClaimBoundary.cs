namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationModeClaimBoundary(
    IReadOnlyList<string> RequiredClaims,
    IReadOnlyList<string> ForbiddenClaims);
