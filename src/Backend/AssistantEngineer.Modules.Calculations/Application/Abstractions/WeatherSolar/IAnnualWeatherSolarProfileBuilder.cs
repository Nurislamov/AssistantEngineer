using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.WeatherSolar;

public interface IAnnualWeatherSolarProfileBuilder
{
    Result<AnnualWeatherSolarProfile> Build(
        AnnualWeatherSolarProfileRequest request);
}