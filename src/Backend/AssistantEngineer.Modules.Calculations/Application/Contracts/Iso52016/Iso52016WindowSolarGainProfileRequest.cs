using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016WindowSolarGainProfileRequest(
    Iso52016WeatherSolarContext WeatherSolarContext,
    CardinalDirection Orientation,
    double WindowAreaM2,
    double SolarHeatGainCoefficient,
    double FrameFraction = 0.0,
    double ShadingFactor = 1.0);