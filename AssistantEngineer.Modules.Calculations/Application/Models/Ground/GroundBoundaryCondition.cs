namespace AssistantEngineer.Modules.Calculations.Application.Models.Ground;

public sealed class GroundBoundaryCondition
{
    public double HeatTransferCoefficientWPerK { get; init; }
    public double GroundTemperatureWeight { get; init; }
    public double OutdoorTemperatureWeight { get; init; }
    public double IndoorTemperatureWeight { get; init; }
}