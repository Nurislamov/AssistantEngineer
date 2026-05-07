using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundTemperatureProfileProvider
{
    GroundTemperatureProfileResult BuildProfile(GroundClimateInput climate);
}
