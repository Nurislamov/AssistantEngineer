using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationWeatherSolarScenarioStep : IEngineeringCalculationWeatherSolarScenarioStep
{
    public ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        var weatherStatus = request.State.WeatherSolarSettings.WeatherSourceStatus ?? "Unavailable";
        if (weatherStatus.Equals("Unavailable", StringComparison.OrdinalIgnoreCase) ||
            weatherStatus.Equals("n/a", StringComparison.OrdinalIgnoreCase))
        {
            return ScenarioModuleExecution.Skip(
                "Weather and solar readiness data is unavailable.",
                "Provide weather/solar readiness input to execute dependent modules.");
        }

        return ScenarioModuleExecution.Execute(
        [
            new EngineeringCalculationModuleValueDto("weather_status", "Weather source status", weatherStatus),
            new EngineeringCalculationModuleValueDto("timezone_summary", "Location/timezone summary", request.State.WeatherSolarSettings.LocationTimezoneSummary),
            new EngineeringCalculationModuleValueDto("solar_readiness", "Solar chain readiness", request.State.WeatherSolarSettings.SolarChainReadinessSummary)
        ], "WorkflowState.WeatherSolarSettings");
    }
}