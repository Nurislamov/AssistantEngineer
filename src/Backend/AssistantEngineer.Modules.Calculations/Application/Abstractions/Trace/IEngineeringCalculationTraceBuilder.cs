using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Models.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;

public interface IEngineeringCalculationTraceBuilder
{
    EngineeringCalculationTrace BuildRoomHeatingTrace(RoomHeatingLoadTraceInput input);
}
