namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record DomesticHotWaterUsefulEnergyToSystemEnergyHandoff(
    string CalculationId,
    string CalculationMethodLabel,
    string SourceModule,
    IReadOnlyList<SystemEnergyHandoffEntry> Entries,
    IReadOnlyList<SystemEnergyHandoffDiagnostics> Diagnostics);
