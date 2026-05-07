using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundBoundaryIso52016BoundaryProfileMappingResult(
    IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> SurfaceBoundaryConditions,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
