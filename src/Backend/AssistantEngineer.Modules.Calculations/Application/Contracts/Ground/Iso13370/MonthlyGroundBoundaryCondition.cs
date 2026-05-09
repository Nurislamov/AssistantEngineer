namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record MonthlyGroundBoundaryCondition(
    int Month,
    double OutdoorTemperatureC,
    double VirtualGroundTemperatureC,
    double EquivalentGroundHeatTransferCoefficientWPerK);

