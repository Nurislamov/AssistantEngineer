using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class VentilationDiagnosticsBuilder
{
    public static CalculationDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            context);

    public static CalculationDiagnostic AirConstantsUsed(
        double airDensity,
        double airSpecificHeat,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Info,
            "Ventilation.AirConstantsUsed",
            $"Air constants used: density {airDensity} kg/m3, specific heat {airSpecificHeat} J/(kg K).",
            context);

    public static CalculationDiagnostic NoMechanicalAirflow(string? context) =>
        new(
            CalculationDiagnosticSeverity.Warning,
            "Ventilation.NoMechanicalAirflow",
            "No mechanical ventilation airflow was supplied; mechanical ventilation load is zero.",
            context);

    public static CalculationDiagnostic NoInfiltrationAirflow(string? context) =>
        new(
            CalculationDiagnosticSeverity.Warning,
            "Ventilation.NoInfiltrationAirflow",
            "No infiltration airflow was supplied; no infiltration load is assumed.",
            context);

    public static CalculationDiagnostic NoNaturalVentilationAirflow(string? context) =>
        new(
            CalculationDiagnosticSeverity.Info,
            "Ventilation.NoNaturalVentilationAirflow",
            "No natural ventilation airflow was supplied; natural ventilation load is zero.",
            context);

    public static CalculationDiagnostic NaturalVentilationEnhancedResultUsed(
        string selectedBranch,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Info,
            "Ventilation.NaturalVentilationEnhancedResultUsed",
            $"Enhanced EN16798-style standard-based natural ventilation result used with branch '{selectedBranch}'.",
            context);
}
