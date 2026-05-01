using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

public sealed record TransmissionElementResult(
    int ElementId,
    TransmissionElementType ElementType,
    int RoomId,
    TransmissionBoundaryType BoundaryType,
    double AreaM2,
    double UValueWPerM2K,
    double DeltaTC,
    double HeatFlowW,
    bool IsIncludedInLoad,
    IReadOnlyList<CalculationDiagnostic> Diagnostics);