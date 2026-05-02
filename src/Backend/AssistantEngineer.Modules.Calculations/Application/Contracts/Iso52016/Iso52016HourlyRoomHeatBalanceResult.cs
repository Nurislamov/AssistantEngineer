namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyRoomHeatBalanceResult(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double OutdoorTemperatureC,
    double IndoorTemperatureBeforeHvacC,
    double IndoorTemperatureAfterHvacC,
    double HeatingSetpointC,
    double CoolingSetpointC,
    double SolarGainsW,
    double InternalGainsW,
    double TotalGainsW,
    double TransmissionHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double TotalHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double HeatingLoadW,
    double CoolingLoadW,
    double HeatingEnergyKWh,
    double CoolingEnergyKWh);