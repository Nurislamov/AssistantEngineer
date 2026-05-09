using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneCalculationResult(
    string BuildingId,
    IReadOnlyList<ThermalZoneNode> Zones,
    IReadOnlyList<ThermalZoneBoundaryLink> BoundaryLinks,
    IReadOnlyList<InterZoneConductanceLink> InterZoneConductanceLinks,
    IReadOnlyList<InterZoneAirflowLink> InterZoneAirflowLinks,
    IReadOnlyList<MultiZoneHourlyResult> HourlyResults,
    MultiZoneAnnualSummary AnnualSummary,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<MultiZoneMonthlySummary>? MonthlySummaries = null)
{
    public bool IsValid =>
        Diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error);

    public IReadOnlyList<MultiZoneMonthlySummary> MonthlySummariesOrEmpty =>
        MonthlySummaries ?? [];
}
