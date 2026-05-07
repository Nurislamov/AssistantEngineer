using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationCalculationResult(
    string CalculationId,
    NaturalVentilationFlowConfiguration FlowConfiguration,
    double TotalAirflowCubicMetersPerSecond,
    double TotalAirflowCubicMetersPerHour,
    double TotalAirflowKilogramsPerSecond,
    IReadOnlyList<NaturalVentilationOpeningResult> Openings,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
