using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class InfiltrationScenarioEvaluator
{
    public static VentilationAirflowResult Evaluate(
        VentilationAndInfiltrationLoadInput input,
        List<CalculationDiagnostic> diagnostics)
    {
        var airflowM3PerHour = 0.0;
        var hasInfiltrationInput = false;

        AddM3PerHour(
            input.InfiltrationAirflowM3PerHour,
            "Ventilation.InfiltrationAirflowM3PerHour",
            ref airflowM3PerHour,
            ref hasInfiltrationInput,
            diagnostics,
            input.DiagnosticsContext);

        if (input.InfiltrationAirChangesPerHour.HasValue)
        {
            var m3PerHour = AirflowNormalizer.AirChangesPerHourToM3PerHour(
                input.VolumeM3,
                input.InfiltrationAirChangesPerHour.Value);

            AddConvertedM3PerHour(
                m3PerHour,
                "Ventilation.InfiltrationAirChangesPerHour",
                ref airflowM3PerHour,
                ref hasInfiltrationInput,
                diagnostics,
                input.DiagnosticsContext);
        }

        if (!hasInfiltrationInput)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.NoInfiltrationAirflow(input.DiagnosticsContext));
        }

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
