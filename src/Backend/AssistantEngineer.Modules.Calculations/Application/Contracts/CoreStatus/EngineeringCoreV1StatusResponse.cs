namespace AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;

public sealed record EngineeringCoreV1StatusResponse(
    string CoreName,
    string Version,
    string Status,
    bool FormulaGatesClosed,
    bool Weather8760GatesClosed,
    bool AnnualHourly8760GateClosed,
    bool SuccessfulResultsMustNotContainErrorDiagnostics,
    IReadOnlyList<EngineeringCoreV1GateStatus> FormulaGates,
    IReadOnlyList<string> ExplicitNonClaims,
    IReadOnlyList<string> OutOfScopeV1,
    IReadOnlyList<string> PlannedValidation,
    IReadOnlyList<string> RequiredAnnual8760Flags,
    IReadOnlyList<string> DocumentationFiles);

public sealed record EngineeringCoreV1GateStatus(
    string CalculationId,
    string Name,
    string Status,
    string Priority,
    string Scope,
    string Limitation);