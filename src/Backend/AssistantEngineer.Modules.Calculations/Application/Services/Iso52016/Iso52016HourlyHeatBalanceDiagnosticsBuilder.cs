namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal static class Iso52016HourlyHeatBalanceDiagnosticsBuilder
{
    public static string BuildSolarWindowContext(
        int hourOfYear,
        string surfaceCode,
        int windowId) =>
        $"ISO 52016 hour {hourOfYear} {surfaceCode} window {windowId}";
}
