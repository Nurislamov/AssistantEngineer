namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyHandoffResult(
    string CalculationMethodLabel,
    string SourceModule,
    BuildingUsefulEnergyToSystemEnergyHandoff BuildingHandoff,
    DomesticHotWaterUsefulEnergyToSystemEnergyHandoff? DomesticHotWaterHandoff,
    SystemEnergyInput SystemEnergyInput,
    SystemEnergyResult? SystemEnergyResult,
    IReadOnlyList<SystemEnergyHandoffDiagnostics> Diagnostics);
