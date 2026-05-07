using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record BuildingGroundBoundaryCalculationInput(
    BuildingThermalTopology Topology,
    IReadOnlyDictionary<string, GroundSurfaceMetadata> GroundSurfaceMetadataBySurfaceId,
    StandardCalculationDisclosure? DisclosureOverride);
