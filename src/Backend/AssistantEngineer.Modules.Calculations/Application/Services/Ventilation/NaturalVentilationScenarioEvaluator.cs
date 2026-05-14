using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class NaturalVentilationScenarioEvaluator
{
    public static NaturalVentilationAirflowResolution Evaluate(
        VentilationAndInfiltrationLoadInput input,
        List<CalculationDiagnostic> diagnostics)
    {
        if (input.NaturalVentilationEnhancedResult is not null)
        {
            var enhancedAirflowM3PerHour = Math.Max(0.0, input.NaturalVentilationEnhancedResult.AirflowM3PerHour);
            diagnostics.Add(VentilationDiagnosticsBuilder.NaturalVentilationEnhancedResultUsed(
                input.NaturalVentilationEnhancedResult.SelectedBranch,
                input.DiagnosticsContext));

            return new NaturalVentilationAirflowResolution(
                VentilationInputNormalizer.CreateAirflowResult(input.VolumeM3, enhancedAirflowM3PerHour),
                input.NaturalVentilationEnhancedResult);
        }

        var airflowM3PerHour = 0.0;
        var hasNaturalInput = false;

        AddM3PerHour(
            input.NaturalVentilationAirflowM3PerHour,
            "Ventilation.NaturalVentilationAirflowM3PerHour",
            ref airflowM3PerHour,
            ref hasNaturalInput,
            diagnostics,
            input.DiagnosticsContext);

        if (!hasNaturalInput)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.NoNaturalVentilationAirflow(input.DiagnosticsContext));
        }

        return new NaturalVentilationAirflowResolution(
            VentilationInputNormalizer.CreateAirflowResult(input.VolumeM3, airflowM3PerHour),
            null);
    }

    public static IReadOnlyList<NaturalVentilationOpeningResult> Evaluate(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationCalculationInput hourlyInput,
        NaturalVentilationHourlyZoneEnvironment environment,
        double density,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        INaturalVentilationAirflowCalculator airflowCalculator)
    {
        if (input.FlowConfiguration is NaturalVentilationFlowConfiguration.ScheduledAirflow or NaturalVentilationFlowConfiguration.CustomAirflow)
        {
            return BuildPrescribedOpeningResults(
                input,
                hourlyInput,
                environment,
                density,
                diagnostics);
        }

        var airflowResult = airflowCalculator.Calculate(hourlyInput);
        foreach (var diagnostic in airflowResult.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        return airflowResult.Openings;
    }

    private static void AddM3PerHour(
        double? airflowM3PerHour,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<CalculationDiagnostic> diagnostics,
        string? context)
    {
        if (!airflowM3PerHour.HasValue)
            return;

        if (airflowM3PerHour.Value < 0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                code,
                "Airflow cannot be negative.",
                context));

            return;
        }

        totalAirflowM3PerHour += airflowM3PerHour.Value;
        hasInput = true;
    }

    private static IReadOnlyList<NaturalVentilationOpeningResult> BuildPrescribedOpeningResults(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationCalculationInput hourlyInput,
        NaturalVentilationHourlyZoneEnvironment environment,
        double density,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var prescribedAirflow = Math.Max(0.0, environment.PrescribedAirflowCubicMetersPerSecond ?? 0.0);
        if (prescribedAirflow <= 0.0)
        {
            diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-ZERO",
                $"Flow configuration '{input.FlowConfiguration}' used prescribed airflow mode with zero airflow at hour {environment.HourIndex}."));
        }

        var openings = hourlyInput.Openings
            .Where(opening => opening.IsOperable != false)
            .ToArray();

        if (openings.Length == 0 || prescribedAirflow <= 0.0)
        {
            if (openings.Length == 0)
            {
                diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                    "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-NO-OPENINGS",
                    $"Flow configuration '{input.FlowConfiguration}' has no operable openings at hour {environment.HourIndex}; airflow is set to zero."));
            }

            return hourlyInput.Openings.Select(opening => new NaturalVentilationOpeningResult(
                OpeningId: opening.OpeningId,
                RoomId: opening.RoomId,
                ZoneId: opening.ZoneId,
                SurfaceId: opening.SurfaceId,
                EffectiveOpeningAreaSquareMeters: Math.Max(0.0, opening.OpeningAreaSquareMeters * (opening.OpeningFraction ?? 0.0)),
                DischargeCoefficient: opening.DischargeCoefficient ?? 0.0,
                WindPressureDifferencePa: null,
                StackPressureDifferencePa: null,
                CombinedPressureDifferencePa: null,
                AirflowCubicMetersPerSecond: 0.0,
                AirflowCubicMetersPerHour: 0.0,
                AirflowKilogramsPerSecond: 0.0,
                Diagnostics:
                [
                    NaturalVentilationZoneDiagnosticsBuilder.Info(
                        "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-OPENING-CLOSED",
                        $"Opening '{opening.OpeningId}' has zero airflow in prescribed airflow mode.")
                ])).ToArray();
        }

        var totalWeight = openings.Sum(opening => Math.Max(0.0, opening.OpeningFraction ?? 0.0));
        if (totalWeight <= 0.0)
        {
            diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-NO-OPEN-FRACTION",
                $"Flow configuration '{input.FlowConfiguration}' has no open fractions at hour {environment.HourIndex}; airflow is set to zero."));
            return BuildPrescribedOpeningResults(
                input,
                hourlyInput,
                environment with { PrescribedAirflowCubicMetersPerSecond = 0.0 },
                density,
                diagnostics);
        }

        diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
            "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-USED",
            $"Flow configuration '{input.FlowConfiguration}' used prescribed airflow mode at hour {environment.HourIndex}."));

        return hourlyInput.Openings.Select(opening =>
        {
            var weight = Math.Max(0.0, opening.OpeningFraction ?? 0.0);
            var openingAirflowM3PerS = prescribedAirflow * (weight / totalWeight);
            var openingAirflowM3PerH = openingAirflowM3PerS * 3600.0;
            var openingAirflowKgPerS = openingAirflowM3PerS * density;
            return new NaturalVentilationOpeningResult(
                OpeningId: opening.OpeningId,
                RoomId: opening.RoomId,
                ZoneId: opening.ZoneId,
                SurfaceId: opening.SurfaceId,
                EffectiveOpeningAreaSquareMeters: Math.Max(0.0, opening.OpeningAreaSquareMeters * (opening.OpeningFraction ?? 0.0)),
                DischargeCoefficient: opening.DischargeCoefficient ?? 0.0,
                WindPressureDifferencePa: null,
                StackPressureDifferencePa: null,
                CombinedPressureDifferencePa: null,
                AirflowCubicMetersPerSecond: openingAirflowM3PerS,
                AirflowCubicMetersPerHour: openingAirflowM3PerH,
                AirflowKilogramsPerSecond: openingAirflowKgPerS,
                Diagnostics:
                [
                    NaturalVentilationZoneDiagnosticsBuilder.Info(
                        "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-OPENING-DISTRIBUTED",
                        $"Opening '{opening.OpeningId}' received a deterministic share of prescribed airflow.")
                ]);
        }).ToArray();
    }
}
