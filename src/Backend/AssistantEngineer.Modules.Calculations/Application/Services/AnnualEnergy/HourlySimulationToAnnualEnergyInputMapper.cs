using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

public sealed class HourlySimulationToAnnualEnergyInputMapper
{
    public const string TrueHourlySimulationSource = "TrueHourlySimulation";

    public HourlySimulationAnnualEnergyInputMappingResult Map(
        int buildingId,
        string? buildingName,
        double buildingAreaM2,
        int year,
        IReadOnlyList<AnnualEnergyBalanceHourInput> hourlyRecords,
        string? diagnosticsContext = null)
    {
        ArgumentNullException.ThrowIfNull(hourlyRecords);

        var records = hourlyRecords
            .OrderBy(record => record.HourIndex)
            .ToArray();
        var diagnostics = BuildComponentDiagnostics(records, diagnosticsContext);
        var isTrueHourly8760 = records.Length == 8760;

        return new HourlySimulationAnnualEnergyInputMappingResult(
            new AnnualEnergyBalanceInput(
                BuildingId: buildingId,
                BuildingName: buildingName,
                BuildingAreaM2: buildingAreaM2,
                Year: year,
                Hours: records,
                UsesSyntheticWeather: false,
                WeatherSource: TrueHourlySimulationSource,
                DiagnosticsContext: diagnosticsContext,
                EnergyDataSource: TrueHourlySimulationSource,
                IsTrueHourly8760: isTrueHourly8760,
                ActualMethod: "EnergyCalculationParityAnnualAggregationAdapter"),
            diagnostics,
            isTrueHourly8760,
            records.Length);
    }

    private static IReadOnlyList<CalculationDiagnostic> BuildComponentDiagnostics(
        IReadOnlyList<AnnualEnergyBalanceHourInput> records,
        string? diagnosticsContext)
    {
        if (records.Count == 0)
            return [];

        var hasLoad = records.Any(record => record.HeatingLoadW > 0 || record.CoolingLoadW > 0);
        if (!hasLoad)
            return [];

        var diagnostics = new List<CalculationDiagnostic>();
        AddMissingComponentDiagnostic(
            diagnostics,
            records,
            "transmission",
            "TransmissionW",
            record => record.TransmissionW,
            diagnosticsContext);
        AddMissingComponentDiagnostic(
            diagnostics,
            records,
            "ventilation",
            "VentilationW",
            record => record.VentilationW,
            diagnosticsContext);
        AddMissingComponentDiagnostic(
            diagnostics,
            records,
            "infiltration",
            "InfiltrationW",
            record => record.InfiltrationW,
            diagnosticsContext);
        AddMissingComponentDiagnostic(
            diagnostics,
            records,
            "ground",
            "GroundW",
            record => record.GroundW,
            diagnosticsContext);

        return diagnostics;
    }

    private static void AddMissingComponentDiagnostic(
        ICollection<CalculationDiagnostic> diagnostics,
        IReadOnlyList<AnnualEnergyBalanceHourInput> records,
        string componentName,
        string fieldName,
        Func<AnnualEnergyBalanceHourInput, double> valueSelector,
        string? diagnosticsContext)
    {
        if (records.Any(record => valueSelector(record) != 0))
            return;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "AnnualEnergy.HourlyComponentBreakdownPartial",
            $"Hourly simulation did not provide component {componentName} ({fieldName}); annual component breakdown excludes it.",
            diagnosticsContext));
    }
}

public sealed record HourlySimulationAnnualEnergyInputMappingResult(
    AnnualEnergyBalanceInput Input,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    bool IsTrueHourly8760,
    int HourlyRecordCount);
