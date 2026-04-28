using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016GroundBoundaryTemperatureRequest(
    AnnualWeatherSolarProfile WeatherSolarProfile,
    Iso52016GroundBoundaryTemperatureOptions Options);