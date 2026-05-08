using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationZoneIntegrationInput(
    string CalculationId,
    BuildingThermalTopology Topology,
    IReadOnlyList<NaturalVentilationOpeningGeometry> Openings,
    IReadOnlyList<NaturalVentilationOpeningControlRule> ControlRules,
    IReadOnlyList<NaturalVentilationHourlyZoneEnvironment> HourlyEnvironments,
    NaturalVentilationFlowConfiguration FlowConfiguration,
    double? DefaultAirDensityKgPerCubicMeter,
    double? DefaultAirSpecificHeatJPerKgKelvin,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
