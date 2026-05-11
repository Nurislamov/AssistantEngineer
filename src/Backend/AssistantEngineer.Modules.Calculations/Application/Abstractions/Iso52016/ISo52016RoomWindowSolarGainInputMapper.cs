using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016RoomWindowSolarGainInputMapper
{
    Result<IReadOnlyList<Iso52016WindowSolarGainInput>> Map(
        Room room,
        Iso52016RoomSimulationDefaults defaults);
}