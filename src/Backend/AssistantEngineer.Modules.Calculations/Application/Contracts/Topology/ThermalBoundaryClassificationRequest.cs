namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalBoundaryClassificationRequest(
    IReadOnlyList<ThermalZoneDefinition> Zones,
    bool AllowUnknownExposure = false,
    bool TreatSameUseAdjacentAsAdiabatic = true,
    double SameUseAdjacentConductanceFactor = 0.0,
    bool RequireSolarMetadataForTransparentExteriorBoundaries = false);
