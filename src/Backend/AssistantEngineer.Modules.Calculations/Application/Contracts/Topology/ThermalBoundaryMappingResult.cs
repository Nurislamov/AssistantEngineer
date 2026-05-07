using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalBoundaryMappingResult<TBoundary>(
    TBoundary? Value,
    bool IsSupported,
    bool IsLossy,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics)
    where TBoundary : struct, Enum;
