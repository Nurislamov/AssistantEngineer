using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IIso52016GroundBoundaryTemperatureProvider
{
    Result<Iso52016GroundBoundaryTemperatureProfile> BuildProfile(
        Iso52016GroundBoundaryTemperatureRequest request);
}