namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public enum ThermalBoundaryKind
{
    Outdoor = 1,
    Ground = 2,
    AdjacentConditionedZone = 3,
    AdjacentUnconditionedZone = 4,
    Adiabatic = 5,
    InternalPartition = 6,
    Other = 7
}
