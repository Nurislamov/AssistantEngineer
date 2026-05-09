using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundHeatTransferResult(
    IReadOnlyList<double> GroundTemperatureProfileCelsius,
    double EquivalentGroundHeatTransferCoefficientWPerKelvin,
    IReadOnlyList<double> HeatFlowProfileWatts,
    double AnnualHeatLossKiloWattHours,
    double AnnualHeatGainKiloWattHours,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
