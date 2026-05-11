using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016WeatherSolarContextBuilder
{
    Result<Iso52016WeatherSolarContext> Build(
        Iso52016WeatherSolarContextRequest request);
}