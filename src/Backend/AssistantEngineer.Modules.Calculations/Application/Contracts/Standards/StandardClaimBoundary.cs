namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

public sealed record StandardClaimBoundary(
    IReadOnlyList<string> AllowedClaims,
    IReadOnlyList<string> ForbiddenClaims,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> Assumptions);
