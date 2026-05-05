using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Result of the application-facing model selection adapter.
/// The selected strategy, generated Matrix request and Matrix solver output are kept together for diagnostics and auditability.
/// </summary>
public sealed record Iso52016PhysicalModelSelectionResult(
    string ZoneCode,
    Iso52016PhysicalModelSelectionStrategy Strategy,
    Iso52016MatrixHourlySolverRequest MatrixSolverRequest,
    Iso52016MatrixHourlySolverProfile MatrixSolverProfile);