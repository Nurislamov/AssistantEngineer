using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016V2ReducedRoomModelBuilder
{
    Result<Iso52016V2HourlySolverRequest> Build(
        Iso52016V2ReducedRoomModelRequest request);
}