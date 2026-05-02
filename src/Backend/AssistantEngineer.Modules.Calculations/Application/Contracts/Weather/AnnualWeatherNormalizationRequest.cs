using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;

public sealed record AnnualWeatherNormalizationRequest(
    AnnualClimateData AnnualClimateData,
    TimeSpan TimeZoneOffset);