using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalBoundaryClassificationResult(
    IReadOnlyList<ThermalZoneDefinition> Zones,
    IReadOnlyList<NormalizedThermalBoundary> Boundaries,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics)
{
    public bool IsValid =>
        Diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error);
}
