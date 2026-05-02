using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record VentilationAndInfiltrationLoadResult(
    int RoomId,
    double IndoorTemperatureC,
    double OutdoorTemperatureC,
    double DeltaTC,
    double AirDensityKgPerM3,
    double AirSpecificHeatJPerKgK,
    MechanicalVentilationLoadResult MechanicalVentilation,
    InfiltrationLoadResult Infiltration,
    NaturalVentilationLoadResult NaturalVentilation,
    double TotalHeatingLoadW,
    double TotalCoolingLoadW,
    double SignedHeatFlowW,
    IReadOnlyList<CalculationDiagnostic> Diagnostics)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}