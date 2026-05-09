namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public enum BoundaryExposureKind
{
    ExteriorAir = 1,
    Ground = 2,
    AdjacentConditionedZone = 3,
    AdjacentUnconditionedZone = 4,
    SameUseAdjacentZone = 5,
    Adiabatic = 6,
    InternalMass = 7,
    Unknown = 8
}
