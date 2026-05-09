using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyUsefulLoadInput(
    string LoadId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    SystemEnergyEndUse EndUse,
    IReadOnlyList<double> HourlyUsefulEnergyKWh8760,
    IReadOnlyList<double>? MonthlyUsefulEnergyKWh,
    double? AnnualUsefulEnergyKWh,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<double>? HourlySystemLoadKWh8760 = null,
    double TimeStepHours = 1.0,
    SystemEnergyLossOwnershipPolicy LossOwnershipPolicy = SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
    IReadOnlyList<string>? Assumptions = null);
