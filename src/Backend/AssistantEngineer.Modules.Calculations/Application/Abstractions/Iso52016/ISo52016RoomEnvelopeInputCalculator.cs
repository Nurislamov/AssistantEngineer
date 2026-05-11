using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016RoomEnvelopeInputCalculator
{
    Result<Iso52016RoomEnvelopeInput> Calculate(
        Room room,
        Iso52016RoomSimulationDefaults defaults);
}