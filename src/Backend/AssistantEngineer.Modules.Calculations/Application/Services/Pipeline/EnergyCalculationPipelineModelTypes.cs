using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed record PipelineClimateContext(
    AnnualClimateData? AnnualClimateData,
    bool IsCompleteAnnualClimateData);

internal sealed record RoomGroundContext(
    double? HeatingGroundTemperatureC,
    double? CoolingGroundTemperatureC,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> Assumptions)
{
    public static RoomGroundContext Empty { get; } = new(null, null, [], []);
}

internal sealed record RoomSolarContext(
    IReadOnlyDictionary<int, double> IrradianceByWindowId,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> Assumptions)
{
    public static RoomSolarContext Empty { get; } = new(
        new Dictionary<int, double>(),
        [],
        []);
}

internal sealed record AnnualEnergyAdapterInput(
    AnnualEnergyBalanceInput Input,
    string Source,
    bool IsTrueHourly8760,
    int HourlyRecordCount,
    IReadOnlyList<CalculationDiagnostic> Diagnostics);

internal sealed record EffectiveVentilationAssumption(
    double EffectiveAirChangesPerHour,
    double EffectiveMechanicalAirflowM3PerHour,
    double EffectiveInfiltrationAirChangesPerHour,
    double EffectiveInfiltrationAirflowM3PerHour,
    string Source);
