namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomHourlyInputRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double OutdoorTemperatureC,
    double GroundBoundaryTemperatureC,
    double HeatingSetpointC,
    double CoolingSetpointC,
    double TransmissionHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double TotalHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double SolarGainsW,
    double InternalGainsW,
    double TotalGainsW);