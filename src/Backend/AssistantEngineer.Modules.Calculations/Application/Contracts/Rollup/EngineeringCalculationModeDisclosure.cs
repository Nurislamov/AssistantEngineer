namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationModeDisclosure(
    string Summary,
    string DefaultOrOptInStatus,
    IReadOnlyList<string> ClaimBoundary,
    IReadOnlyList<string> ForbiddenClaims);
