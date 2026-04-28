using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Weather;

public interface IAnnualWeatherDataNormalizer
{
    Result<AnnualWeatherDataSet> Normalize(
        AnnualWeatherNormalizationRequest request);
}