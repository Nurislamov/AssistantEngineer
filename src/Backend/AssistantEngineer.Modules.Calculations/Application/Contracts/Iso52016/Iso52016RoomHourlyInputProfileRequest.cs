namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomHourlyInputProfileRequest(
    string RoomCode,
    Iso52016WeatherSolarContext WeatherSolarContext,
    Iso52016RoomSolarGainProfile SolarGainProfile,
    Iso52016RoomInternalGainProfile InternalGainProfile,
    double TransmissionHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double HeatingSetpointC,
    double CoolingSetpointC);