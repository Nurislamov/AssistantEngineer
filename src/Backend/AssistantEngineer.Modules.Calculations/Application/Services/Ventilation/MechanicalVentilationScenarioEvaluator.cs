using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class MechanicalVentilationScenarioEvaluator
{
    public static VentilationAirflowResult Evaluate(
        VentilationAndInfiltrationLoadInput input,
        List<CalculationDiagnostic> diagnostics)
    {
        var airflowM3PerHour = 0.0;
        var hasMechanicalInput = false;

        AddM3PerHour(
            input.MechanicalAirflowM3PerHour,
            "Ventilation.MechanicalAirflowM3PerHour",
            ref airflowM3PerHour,
            ref hasMechanicalInput,
            diagnostics,
            input.DiagnosticsContext);

        AddLitersPerSecond(
            input.AirflowLitersPerSecond,
            "Ventilation.AirflowLitersPerSecond",
            ref airflowM3PerHour,
            ref hasMechanicalInput,
            diagnostics,
            input.DiagnosticsContext);

        if (input.AirflowPerPersonLps.HasValue)
        {
            var lps = AirflowNormalizer.AirflowPerPersonToLitersPerSecond(
                input.AirflowPerPersonLps.Value,
                input.OccupancyPeople);

            AddConvertedLitersPerSecond(
                lps,
                "Ventilation.AirflowPerPerson",
                ref airflowM3PerHour,
                ref hasMechanicalInput,
                diagnostics,
                input.DiagnosticsContext);
        }

        if (input.AirflowPerAreaLpsM2.HasValue)
        {
            var lps = AirflowNormalizer.AirflowPerAreaToLitersPerSecond(
                input.AirflowPerAreaLpsM2.Value,
                input.AreaM2);

            AddConvertedLitersPerSecond(
                lps,
                "Ventilation.AirflowPerArea",
                ref airflowM3PerHour,
                ref hasMechanicalInput,
                diagnostics,
                input.DiagnosticsContext);
        }

        if (input.AirChangesPerHour.HasValue)
        {
            var m3PerHour = AirflowNormalizer.AirChangesPerHourToM3PerHour(
                input.VolumeM3,
                input.AirChangesPerHour.Value);

            AddConvertedM3PerHour(
                m3PerHour,
                "Ventilation.AirChangesPerHour",
                ref airflowM3PerHour,
                ref hasMechanicalInput,
                diagnostics,
                input.DiagnosticsContext);
        }

        if (!hasMechanicalInput)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.NoMechanicalAirflow(input.DiagnosticsContext));
        }

        airflowM3PerHour *= input.ScheduleFactor;

        return VentilationInputNormalizer.CreateAirflowResult(
            input.VolumeM3,
            airflowM3PerHour);
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

    private static void AddLitersPerSecond(
        double? airflowLitersPerSecond,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<CalculationDiagnostic> diagnostics,
        string? context)
    {
        if (!airflowLitersPerSecond.HasValue)
            return;

        var m3PerHour = AirflowNormalizer.LitersPerSecondToM3PerHour(
            airflowLitersPerSecond.Value);

        AddConvertedM3PerHour(
            m3PerHour,
            code,
            ref totalAirflowM3PerHour,
            ref hasInput,
            diagnostics,
            context);
    }

    private static void AddConvertedLitersPerSecond(
        Result<double> airflowLitersPerSecond,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<CalculationDiagnostic> diagnostics,
        string? context)
    {
        if (airflowLitersPerSecond.IsFailure)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                code,
                airflowLitersPerSecond.Error,
                context));

            return;
        }

        AddLitersPerSecond(
            airflowLitersPerSecond.Value,
            code,
            ref totalAirflowM3PerHour,
            ref hasInput,
            diagnostics,
            context);
    }

    private static void AddConvertedM3PerHour(
        Result<double> airflowM3PerHour,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<CalculationDiagnostic> diagnostics,
        string? context)
    {
        if (airflowM3PerHour.IsFailure)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                code,
                airflowM3PerHour.Error,
                context));

            return;
        }

        totalAirflowM3PerHour += airflowM3PerHour.Value;
        hasInput = true;
    }
}
