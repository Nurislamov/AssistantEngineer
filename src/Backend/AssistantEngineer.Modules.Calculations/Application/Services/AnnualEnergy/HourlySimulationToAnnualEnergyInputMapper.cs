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
        string? diagnosticsContext = null,
        bool infiltrationSplitAvailable = false,
        bool ventilationSubcomponentSplitAvailable = false)
    {
        ArgumentNullException.ThrowIfNull(hourlyRecords);

        var records = hourlyRecords
            .OrderBy(record => record.HourIndex)
            .ToArray();

        var diagnostics = BuildComponentDiagnostics(
            records,
            diagnosticsContext,
            infiltrationSplitAvailable,
            ventilationSubcomponentSplitAvailable);
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
        string? diagnosticsContext,
        bool infiltrationSplitAvailable,
        bool ventilationSubcomponentSplitAvailable)
    {
        if (records.Count == 0)
            return [];

        var hasLoad = records.Any(record =>
            record.HeatingLoadW > 0 ||
            record.CoolingLoadW > 0);

        if (!hasLoad)
            return [];

        var diagnostics = new List<CalculationDiagnostic>();

        AddMissingComponentDiagnostic(
            diagnostics,
            records,
            componentName: "transmission",
            fieldName: nameof(AnnualEnergyBalanceHourInput.TransmissionW),
            valueSelector: record => record.TransmissionW,
            diagnosticsContext);

        var hasVentilationSubcomponentSplit = HasVentilationSubcomponentSplit(records);
        var ventilationSplitAvailable =
            ventilationSubcomponentSplitAvailable ||
            hasVentilationSubcomponentSplit;

        if (!ventilationSplitAvailable)
        {
            AddMissingComponentDiagnostic(
                diagnostics,
                records,
                componentName: "ventilation",
                fieldName: nameof(AnnualEnergyBalanceHourInput.VentilationW),
                valueSelector: record => record.VentilationW,
                diagnosticsContext);
        }

        AddVentilationSubcomponentDiagnostics(
            diagnostics,
            records,
            ventilationSplitAvailable,
            diagnosticsContext);

        if (!infiltrationSplitAvailable)
        {
            AddMissingComponentDiagnostic(
                diagnostics,
                records,
                componentName: "infiltration",
                fieldName: nameof(AnnualEnergyBalanceHourInput.InfiltrationW),
                valueSelector: record => record.InfiltrationW,
                diagnosticsContext);
        }

        AddMissingComponentDiagnostic(
            diagnostics,
            records,
            componentName: "ground",
            fieldName: nameof(AnnualEnergyBalanceHourInput.GroundW),
            valueSelector: record => record.GroundW,
            diagnosticsContext);

        AddSignedComponentBalanceDiagnostics(
            diagnostics,
            records,
            diagnosticsContext,
            infiltrationSplitAvailable);

        return diagnostics;
    }

    private static void AddVentilationSubcomponentDiagnostics(
        ICollection<CalculationDiagnostic> diagnostics,
        IReadOnlyList<AnnualEnergyBalanceHourInput> records,
        bool ventilationSplitAvailable,
        string? diagnosticsContext)
    {
        var hasAggregateVentilation = records.Any(record =>
            Math.Abs(record.VentilationW) > 0.000001 ||
            Math.Abs(record.VentilationBalanceW) > 0.000001);

        if (ventilationSplitAvailable)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "AnnualEnergy.VentilationSubcomponentBreakdownAvailable",
                "Hourly simulation provides mechanical and natural ventilation subcomponent fields. VentilationW remains the aggregate mechanical plus natural ventilation value.",
                diagnosticsContext));
            return;
        }

        if (hasAggregateVentilation)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AnnualEnergy.VentilationSubcomponentBreakdownPartial",
                "Hourly simulation provides aggregate ventilation but does not expose mechanical/natural split.",
                diagnosticsContext));
        }
    }

    private static bool HasVentilationSubcomponentSplit(
        IReadOnlyList<AnnualEnergyBalanceHourInput> records) =>
        records.Any(record =>
            Math.Abs(record.MechanicalVentilationW) > 0.000001 ||
            Math.Abs(record.NaturalVentilationW) > 0.000001 ||
            Math.Abs(record.MechanicalVentilationBalanceW) > 0.000001 ||
            Math.Abs(record.NaturalVentilationBalanceW) > 0.000001);

    private static void AddMissingComponentDiagnostic(
        ICollection<CalculationDiagnostic> diagnostics,
        IReadOnlyList<AnnualEnergyBalanceHourInput> records,
        string componentName,
        string fieldName,
        Func<AnnualEnergyBalanceHourInput, double> valueSelector,
        string? diagnosticsContext)
    {
        if (records.Any(record => Math.Abs(valueSelector(record)) > 0.000001))
            return;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "AnnualEnergy.HourlyComponentBreakdownPartial",
            $"Hourly simulation did not provide component {componentName} ({fieldName}); annual component breakdown excludes it.",
            diagnosticsContext));
    }

    private static void AddSignedComponentBalanceDiagnostics(
        ICollection<CalculationDiagnostic> diagnostics,
        IReadOnlyList<AnnualEnergyBalanceHourInput> records,
        string? diagnosticsContext,
        bool infiltrationSplitAvailable)
    {
        var availableSignedComponents = new List<string>();

        if (records.Any(record => Math.Abs(record.TransmissionBalanceW) > 0.000001))
            availableSignedComponents.Add(nameof(AnnualEnergyBalanceHourInput.TransmissionBalanceW));

        if (records.Any(record => Math.Abs(record.VentilationBalanceW) > 0.000001))
            availableSignedComponents.Add(nameof(AnnualEnergyBalanceHourInput.VentilationBalanceW));

        if (records.Any(record => Math.Abs(record.MechanicalVentilationBalanceW) > 0.000001))
            availableSignedComponents.Add(nameof(AnnualEnergyBalanceHourInput.MechanicalVentilationBalanceW));

        if (records.Any(record => Math.Abs(record.NaturalVentilationBalanceW) > 0.000001))
            availableSignedComponents.Add(nameof(AnnualEnergyBalanceHourInput.NaturalVentilationBalanceW));

        if (records.Any(record => Math.Abs(record.InfiltrationBalanceW) > 0.000001))
            availableSignedComponents.Add(nameof(AnnualEnergyBalanceHourInput.InfiltrationBalanceW));

        if (records.Any(record => Math.Abs(record.GroundBalanceW) > 0.000001))
            availableSignedComponents.Add(nameof(AnnualEnergyBalanceHourInput.GroundBalanceW));

        if (availableSignedComponents.Count > 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "AnnualEnergy.SignedComponentBalanceAvailable",
                "Hourly simulation provides signed component balance fields. Positive values mean heat gain to the room/building; negative values mean heat loss from the room/building. Available fields: " +
                string.Join(", ", availableSignedComponents) +
                ".",
                diagnosticsContext));
        }

        var hasInfiltrationMagnitude = records.Any(record =>
            Math.Abs(record.InfiltrationW) > 0.000001);

        var hasInfiltrationSignedBalance = records.Any(record =>
            Math.Abs(record.InfiltrationBalanceW) > 0.000001);

        if (!infiltrationSplitAvailable &&
            !hasInfiltrationMagnitude &&
            !hasInfiltrationSignedBalance)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable",
                "Hourly simulation does not expose infiltration as a separate signed component. If infiltration is modelled by the current hourly path, it remains included in the combined ventilation contribution.",
                diagnosticsContext));
        }
    }
}

public sealed record HourlySimulationAnnualEnergyInputMappingResult(
    AnnualEnergyBalanceInput Input,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    bool IsTrueHourly8760,
    int HourlyRecordCount);
