using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyAuxiliaryLoadInput(
    string AuxiliaryId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    SystemEnergyEndUse EndUse,
    SystemEnergyCarrier Carrier,
    IReadOnlyList<double> HourlyAuxiliaryEnergyKWh8760,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
