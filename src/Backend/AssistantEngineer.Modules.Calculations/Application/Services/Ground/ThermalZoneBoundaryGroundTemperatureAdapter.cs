using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class ThermalZoneBoundaryGroundTemperatureAdapter : IThermalZoneGroundBoundaryInputAdapter
{
    private readonly IGroundBoundaryTemperatureLookupBuilder _lookupBuilder;

    public ThermalZoneBoundaryGroundTemperatureAdapter(
        IGroundBoundaryTemperatureLookupBuilder lookupBuilder)
    {
        _lookupBuilder = lookupBuilder ?? throw new ArgumentNullException(nameof(lookupBuilder));
    }

    public ThermalZoneGroundBoundaryInputAdapterResult BuildGroundTemperatureInputs(
        BuildingGroundBoundaryCalculationResult groundResult)
    {
        ArgumentNullException.ThrowIfNull(groundResult);

        var lookup = _lookupBuilder.Build(groundResult);
        var representativeBySurfaceId = new Dictionary<string, double>(
            lookup.RepresentativeGroundTemperatureBySurfaceId,
            StringComparer.Ordinal);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(lookup.Diagnostics);

        if (representativeBySurfaceId.Count == 0)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-BUILDING-REPRESENTATIVE-TEMPERATURE-MISSING",
                "Building representative ground temperature is missing because no representative surface temperatures were available."));
            return new ThermalZoneGroundBoundaryInputAdapterResult(
                RepresentativeBuildingGroundTemperatureCelsius: null,
                RepresentativeGroundTemperatureBySurfaceId: representativeBySurfaceId,
                Diagnostics: diagnostics);
        }

        var representativeWithWeights = representativeBySurfaceId
            .Select(pair => new
            {
                pair.Key,
                Temperature = pair.Value,
                HasWeight = groundResult.SurfaceHeatTransferCoefficientsWPerKelvin.TryGetValue(pair.Key, out var h) &&
                            double.IsFinite(h) &&
                            h > 0.0,
                Weight = groundResult.SurfaceHeatTransferCoefficientsWPerKelvin.TryGetValue(pair.Key, out var h2) ? h2 : 0.0
            })
            .ToArray();

        var allHaveWeights = representativeWithWeights.All(item => item.HasWeight);
        if (allHaveWeights)
        {
            var totalWeight = representativeWithWeights.Sum(item => item.Weight);
            if (totalWeight > 0.0 && double.IsFinite(totalWeight))
            {
                var weightedTemperature = representativeWithWeights.Sum(item => item.Temperature * item.Weight) / totalWeight;
                diagnostics.Add(CreateInfo(
                    "AE-GROUND-BUILDING-REPRESENTATIVE-TEMPERATURE-WEIGHTED",
                    "Building representative ground temperature was calculated as an H-weighted average of surface representative temperatures."));
                return new ThermalZoneGroundBoundaryInputAdapterResult(
                    RepresentativeBuildingGroundTemperatureCelsius: weightedTemperature,
                    RepresentativeGroundTemperatureBySurfaceId: representativeBySurfaceId,
                    Diagnostics: diagnostics);
            }
        }

        var simpleAverage = representativeBySurfaceId.Values.Average();
        diagnostics.Add(CreateInfo(
            "AE-GROUND-BUILDING-REPRESENTATIVE-TEMPERATURE-SIMPLE-AVERAGE",
            "Building representative ground temperature was calculated as a simple average because complete positive H weights were unavailable."));

        return new ThermalZoneGroundBoundaryInputAdapterResult(
            RepresentativeBuildingGroundTemperatureCelsius: simpleAverage,
            RepresentativeGroundTemperatureBySurfaceId: representativeBySurfaceId,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.Aggregation,
            "ThermalZoneBoundaryGroundTemperatureAdapter");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.Aggregation,
            "ThermalZoneBoundaryGroundTemperatureAdapter");
}
