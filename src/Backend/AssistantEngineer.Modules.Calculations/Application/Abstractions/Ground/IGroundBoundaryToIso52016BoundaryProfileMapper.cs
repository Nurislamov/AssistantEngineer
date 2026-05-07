using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundBoundaryToIso52016BoundaryProfileMapper
{
    GroundBoundaryIso52016BoundaryProfileMappingResult Map(
        GroundBoundaryTemperatureLookup lookup);
}
