using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalZoneBoundaryCalculationInput(
    BuildingThermalTopology Topology,
    IReadOnlyDictionary<string, double>? ZoneAirTemperaturesCelsius,
    IReadOnlyDictionary<string, double>? AdjacentUnconditionedTemperaturesCelsius,
    double? OutdoorTemperatureCelsius,
    double? GroundTemperatureCelsius,
    StandardCalculationDisclosure? DisclosureOverride);
