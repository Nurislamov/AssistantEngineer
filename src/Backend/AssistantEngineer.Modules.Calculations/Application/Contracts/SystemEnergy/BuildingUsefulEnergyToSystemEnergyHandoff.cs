namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record BuildingUsefulEnergyToSystemEnergyHandoff(
    int BuildingId,
    string CalculationMethodLabel,
    string SourceModule,
    IReadOnlyList<SystemEnergyHandoffEntry> Entries,
    IReadOnlyList<SystemEnergyHandoffDiagnostics> Diagnostics);
