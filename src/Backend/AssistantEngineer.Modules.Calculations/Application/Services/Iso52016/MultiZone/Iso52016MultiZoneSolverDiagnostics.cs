using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneSolverDiagnostics
{
    internal static void AddWarningOnce(
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ISet<string> warningKeys,
        string key,
        string code,
        string message)
    {
        if (!warningKeys.Add(key))
            return;

        diagnostics.Add(CreateWarning(code, message));
    }

    internal static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Error,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneHourlySolver",
            Source: "Iso52016MultiZoneHourlySolver",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.HeatTransfer);

    internal static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Warning,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneHourlySolver",
            Source: "Iso52016MultiZoneHourlySolver",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.HeatTransfer);

    internal static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Info,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneHourlySolver",
            Source: "Iso52016MultiZoneHourlySolver",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.HeatTransfer);
}
